using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;
using WpfAnimatedGif;
using BuduPing.Core;
using BuduPing.Services;

namespace BuduPing.Ui;

/// <summary>
/// Overlay góc phải dưới: click-through, hiện sticker + bong bóng + hạt cục bộ,
/// hàng đợi tối đa 3, combo gộp trong cửa sổ 1.5s (Lv2 ở 3, Lv3 ở 5+).
/// </summary>
public partial class OverlayWindow : Window
{
    #region Win32 click-through
    [DllImport("user32.dll")] private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")] private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TRANSPARENT = 0x20;
    private const int WS_EX_LAYERED = 0x80000;
    private const int WS_EX_TOOLWINDOW = 0x80;
    #endregion

    private const int MaxQueue = 2;
    private const int MaxParticles = PresentationPolicy.MaxParticles;   // pool hạt cố định
    private const double ComboWindowSec = 2.0;  // tính từ lần chạm GẦN NHẤT (v0.1.0 tính từ lúc bắt đầu hiện → spam là kẹt hàng đợi)
    private const double InterruptAfterSec = 1.0; // khác cảm xúc mà cái đang hiện đã chiếu quá 1s → nhường chỗ ngay
    private const double ShowSeconds = 2.8;     // v0.1.0 để 5s → spam sticker phải chờ nhau rất lâu

    private readonly StickerService _stickers;
    private readonly App _app;
    private readonly PingBacklog _queue = new();
    private readonly DispatcherTimer _idleTimer;
    private readonly DispatcherTimer _summaryTimer;
    private readonly Stack<Particle> _recycled = new();
    private readonly Stack<TextBlock> _glyphPool = new();
    private PingEvent? _current;
    private bool _peeking;
    private int _generation;
    private int _expiredCount;
    internal Point LastBurstCenter { get; private set; }
    public PingEvent? LastPing { get; private set; }
    // Edge peek (hello): card chrome is swapped out and restored afterwards.
    private bool _edgePeek;
    private bool _edgeChromeSaved;
    private System.Windows.Media.Brush? _cardBackground;
    private System.Windows.Media.Brush? _cardBorderBrush;
    private Thickness _cardBorderThickness;
    private System.Windows.Media.Effects.Effect? _cardEffect;

    public int PendingCount => _queue.Count;
    private readonly DispatcherTimer _hideTimer;
    private readonly Random _rng = new();

    private bool _isShowing;
    private bool _isHiding;
    private string _activeType = "";
    private int _activeCount;
    private DateTime _windowStartUtc;
    private DateTime _lastTapUtc;
    private bool _lv3Done;
    private long _lastVisualBeat = -PresentationPolicy.VisualGapMs;
    private long _lastVariantBeat = -PresentationPolicy.VariantGapMs;
    private readonly Stopwatch _feedbackClock = Stopwatch.StartNew();
    internal int ActiveComboCount => _activeCount;
    internal int ActiveParticleCount => _particles.Count;
    internal bool IsPeeking => _peeking;

    // Pool hạt: animate 1 vòng CompositionTarget.Rendering, hết hạt thì unsubscribe → idle 0% CPU
    private readonly List<Particle> _particles = new(MaxParticles);
    private DateTime _lastTick;

    private sealed class Particle
    {
        public FrameworkElement El = null!;
        public TranslateTransform Move = new();
        public ScaleTransform Scale = new();
        public double Vx, Vy, Gravity, X, Y;
        public double Age, Life = 1, FadeIn = 0.12;
        public double SwayAmp, SwayFreq, Phase;
        public double StartScale = 1, EndScale = 1, Unit = 1;
    }

    public OverlayWindow(StickerService stickers, App app)
    {
        _stickers = stickers;
        _app = app;
        InitializeComponent();

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(ShowSeconds) };
        _hideTimer.Tick += (_, _) => HideCurrent();

        // First tick comes early (6s): launch-time working-set bulk (runtime +
        // WPF + preloaded GIF bytes) gets handed back quickly instead of
        // lingering ~20s. Post-hide cycles then use the calmer 20s cadence.
        _idleTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
        _idleTimer.Tick += (_, _) =>
        {
            _idleTimer.Stop();
            _idleTimer.Interval = TimeSpan.FromSeconds(20);
            if (_isShowing || MemoryTrim.Disabled) return;
            _stickers.ReleaseDecoded();
            MemoryTrim.TrimIdle(_stickers.DecodedBytes);
        };
        _idleTimer.Start();
        _summaryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _summaryTimer.Tick += (_, _) =>
        {
            _summaryTimer.Stop();
            if (_expiredCount == 0 || _app.Config.DiscreetMode || _isShowing) return;
            int count = _expiredCount; _expiredCount = 0;
            Display(new PingEvent { Type = "love", Text = $"Có {count} lời thương từ lúc bạn vắng mặt.", IsLocal = true });
        };
        Loaded += (_, _) => PlaceBottomRight();
        Microsoft.Win32.SystemEvents.DisplaySettingsChanged += (_, _) =>
            Dispatcher.Invoke(PlaceBottomRight);
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Click-through: chuột bấm xuyên qua overlay + ẩn khỏi Alt+Tab
        var hwnd = new WindowInteropHelper(this).Handle;
        int ex = GetWindowLong(hwnd, GWL_EXSTYLE);
        SetWindowLong(hwnd, GWL_EXSTYLE, ex | WS_EX_TRANSPARENT | WS_EX_LAYERED | WS_EX_TOOLWINDOW);
    }

    private void PlaceBottomRight()
    {
        ContentPanel.VerticalAlignment = _app.Config.Corner.StartsWith("Top", StringComparison.Ordinal)
            ? VerticalAlignment.Top : VerticalAlignment.Bottom;
        // Scale artwork only: small stickers must not make message text unreadable.
        double size = PresentationPolicy.ArtworkScale * PresentationPolicy.NormalizeSize(_app.Config.OverlayScale);
        StickerCard.LayoutTransform = new ScaleTransform(size, size);
        Width = 520;
        Height = 580;
        DisplayPlacement.Place(this, _app.Config.MonitorDevice, _app.Config.Corner, _app.Config.OverlayScale);
    }

    public void RefreshPlacement() => PlaceBottomRight();

    public void ApplyPreferences()
    {
        PlaceBottomRight();
        if (_app.Config.DiscreetMode || _app.Config.HideMessageText)
        {
            Bubble.Visibility = Visibility.Collapsed; BubbleTail.Visibility = Visibility.Collapsed;
        }
        if (_app.Config.DiscreetMode && _isShowing && !_peeking)
        {
            _peeking = true; ClearParticles(); PlayEntrance();
        }
        if (_app.Config.ReducedMotion) ClearParticles();
    }

    public void ResetPair()
    {
        _generation++;
        _queue.Clear(); LastPing = null; _current = null;
        _hideTimer.Stop(); _idleTimer.Stop();
        _isShowing = false; _isHiding = false;
        Visibility = Visibility.Collapsed;
        ClearParticles();
        SetAnimationSource(StickerImage, null);
        _stickers.ReleaseDecoded();
    }

    public void ShowPending()
    {
        if (_app.Config.DiscreetMode || _app.Config.HideMessageText) return;
        if (_peeking) { _isShowing = false; _isHiding = false; _hideTimer.Stop(); }
        if (!_isShowing && _queue.Take() is { } ping) Display(ping);
        else if (!_isShowing) _app.RefreshDeferred();
    }

    private static string EmojiFor(string type) => type switch
    {
        "miss" => "💭", "love" => "🥰", "hug" => "🤗", "kiss" => "😘", "tease" => "😜",
        "angry" => "😠", "sad" => "😢", "sorry" => "🙏", "funny" => "😂", "cheer" => "🎉",
        "hungry" => "😋", "goodnight" => "🌙", _ => "❤",
    };

    public void ShowPing(PingEvent ping)
    {
        if (PingPolicy.IsExpired(ping, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()))
        {
            _app.CompletePing(ping, "expired");
            _expiredCount++; _summaryTimer.Stop(); _summaryTimer.Start();
            return;
        }
        _idleTimer.Stop();
        if (!ping.IsLocal) LastPing = ping;
        if (PresentationPolicy.ShouldPeek(ping, _app.Config.DiscreetMode, _app.Config.HideMessageText))
        {
            // Tests/reply confirmations follow the same privacy rule; never change preferences.
            // Only remote events stay pending for the explicit "open pending" action.
            if (!ping.IsLocal) Enqueue(ping);
            if (!_isShowing || ping.IsLocal) Display(ping, peek: true);
            return;
        }
        var now = DateTime.UtcNow;
        if (_isShowing && !_isHiding && !_peeking && _current != null &&
            PingPolicy.CanCombine(_current, ping, (now - _lastTapUtc).TotalSeconds))
        {
            _activeCount++;
            _lastTapUtc = now;
            ApplyComboVisual();
            _app.CompletePing(ping, "displayed");
            RestartHideTimer();
            return;
        }
        // Never interrupt an explicit message; it needs enough time to be read.
        if (_isShowing && !_isHiding && !_peeking && _queue.Count == 0 &&
            string.IsNullOrWhiteSpace(_current?.Text) &&
            !string.Equals(ping.Type, _activeType, StringComparison.OrdinalIgnoreCase) &&
            (now - _windowStartUtc).TotalSeconds >= InterruptAfterSec)
        {
            Display(ping); return;
        }
        if (_isShowing) { Enqueue(ping); return; }
        Display(ping);
    }

    private void Enqueue(PingEvent ping)
    {
        if (!_queue.TryEnqueue(ping, out var replaced)) { _app.DeferPing(ping); return; }
        if (replaced != null) _app.CompletePing(replaced, "superseded");
    }

    private void Display(PingEvent ping, bool peek = false)
    {
        // Mood switch while the buddy is already on screen: decode the new GIF
        // on a background thread and swap it in with a soft bounce — replaying
        // the full entrance (and blocking the UI thread on a cold decode) made
        // spam-switching feel like a notification re-posting, with a hitch.
        bool switching = _isShowing && !_isHiding && !_peeking && !peek
            && Visibility == Visibility.Visible
            && !string.IsNullOrEmpty(_activeType)
            && !string.Equals(_activeType, ping.Type, StringComparison.OrdinalIgnoreCase);
        _generation++;
        _idleTimer.Stop();
        if (switching)
        {
            int generation = _generation;
            Task.Run(() =>
            {
                var loaded = _stickers.GetRandom(ping.Type, ping.Variant, ping.Character);
                Dispatcher.InvokeAsync(() =>
                {
                    if (generation != _generation) { _app.CompletePing(ping, "superseded"); return; }
                    if (loaded == null) { _app.CompletePing(ping, "unavailable"); return; }
                    ApplyDisplay(ping, peek, loaded, switching: true);
                });
            });
            return;
        }
        var bmp0 = _stickers.GetRandom(ping.Type, ping.Variant, ping.Character);
        if (bmp0 == null)
        {
            _isShowing = false;
            _app.CompletePing(ping, "unavailable");
            return;
        }
        ApplyDisplay(ping, peek, bmp0, switching: false);
    }

    private void ApplyDisplay(PingEvent ping, bool peek, BitmapImage bmp, bool switching)
    {
        var displayWatch = Stopwatch.StartNew();
        _current = ping;
        _peeking = peek;
        _isShowing = true; _isHiding = false;
        _activeType = ping.Type; _activeCount = 1; _lv3Done = false;
        _windowStartUtc = DateTime.UtcNow; _lastTapUtc = _windowStartUtc;
        ClearParticles();
        SetAnimationSource(StickerImage, bmp);
        NormalizeSticker(bmp.PixelWidth, bmp.PixelHeight);
        EmojiArt.ApplyTo(BubbleFrom, string.IsNullOrWhiteSpace(ping.From) ? "Người thương" : ping.From.Trim(), 15);
        UpdateBubbleAvatar(ping.Character);
        BubbleEmotion.Text = StickerService.LabelFor(ping.Type);
        string text = (ping.Text ?? "").Trim();
        bool hiddenText = _app.Config.HideMessageText && text.Length > 0;
        EmojiArt.ApplyTo(BubbleText, hiddenText ? "Bạn có một lời nhắn riêng." : text[..Math.Min(text.Length, 80)]);
        Bubble.Visibility = !peek && text.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
        BubbleTail.Visibility = Bubble.Visibility;
        var glowColor = ping.Type is "miss" or "sad" or "goodnight" or "sorry"
            ? Color.FromArgb(90, 150, 190, 255) : ping.Type is "hug" or "cheer" or "hungry"
            ? Color.FromArgb(90, 255, 185, 115) : Color.FromArgb(90, 255, 175, 205);
        StickerGlow.Fill = new RadialGradientBrush(glowColor, Colors.Transparent);
        StickerGlow.Visibility = Visibility.Collapsed;
        ComboBadge.Visibility = Visibility.Collapsed;
        StickerScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        StickerScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        StickerScale.ScaleX = StickerScale.ScaleY = 1;
        StickerShake.BeginAnimation(TranslateTransform.XProperty, null);
        StickerShake.BeginAnimation(TranslateTransform.YProperty, null);
        StickerTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        StickerShake.X = StickerShake.Y = StickerTilt.Angle = 0;
        Visibility = Visibility.Visible;
        PlaceBottomRight();
        UpdateLayout(); // The first burst needs the arranged sticker position, not its unmeasured origin.
        // Hello (no text, not discreet) = edge peek: the buddy slides out of the
        // right screen rim, body cropped by the rim itself. Text hellos keep the
        // normal corner card so the message stays fully readable.
        bool wantEdge = string.Equals(ping.Type, "hello", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(ping.Text) && !peek && !_app.Config.DiscreetMode;
        if (wantEdge)
        {
            ApplyEdgeChrome();
            DisplayPlacement.PlacePeekRight(this, _app.Config.MonitorDevice);
            PlayEdgeEntrance();
        }
        else if (switching)
        {
            RestoreEdgeChrome();
            PlaySwapBounce();
        }
        else
        {
            RestoreEdgeChrome();
            PlayEntrance();
        }
        if (!peek)
        {
            if (!_app.Config.ReducedMotion)
            {
                int generation = _generation;
                Dispatcher.InvokeAsync(() =>
                {
                    if (generation != _generation || _peeking || _app.Config.ReducedMotion) return;
                    ApplyMoodPose(ping.Type);
                    BurstFor(ping.Type, StickerCenter(), 1);
                }, DispatcherPriority.Loaded);
            }
            _app.PlayPop(1, ping.Character, ping.Type);
            // One callback after composition starts, not when the event merely enters a queue.
            EventHandler? rendered = null;
            rendered = (_, _) =>
            {
                CompositionTarget.Rendering -= rendered;
                if (_current?.Id != ping.Id || _peeking) return;
                _app.CompletePing(ping, hiddenText ? "received" : "displayed");
                MemoryTrim.Record("display", _stickers.DecodedBytes, displayWatch.Elapsed.TotalMilliseconds);
            };
            CompositionTarget.Rendering += rendered;
        }
        RestartHideTimer();
    }

    private void SetAnimationSource(Image image, BitmapImage? source)
    {
        var old = ImageBehavior.GetAnimatedSource(image) as BitmapImage;
        if (ReferenceEquals(old, source)) return;
        ImageBehavior.SetAnimatedSource(image, null);
        if (old != null) _stickers.Unpin(old);
        if (source != null) { _stickers.Pin(source); ImageBehavior.SetAnimatedSource(image, source); }
    }

    private void NormalizeSticker(int width, int height)
    {
        var info = _stickers.LastInfo;
        var b = info?.bounds;
        double x = 0, y = 0, w = width, h = height;
        if (b?.Length == 4 && b[2] > b[0] && b[3] > b[1])
        { x = b[0]; y = b[1]; w = b[2] - b[0]; h = b[3] - b[1]; }
        double scale = 244 / Math.Max(w, h);
        StickerImage.Width = width * scale; StickerImage.Height = height * scale;
        Canvas.SetLeft(StickerImage, (264 - w * scale) / 2 - x * scale);
        Canvas.SetTop(StickerImage, 264 - h * scale - y * scale);
    }

    private void ApplyMoodPose(string type)
    {
        if (_app.Config.ReducedMotion) return;
        if (type is "angry" or "tease") { ShakeSticker(); return; }
        double angle = type switch { "miss" => -9, "goodnight" => 10, "kiss" => -7, _ => 0 };
        if (angle != 0)
            StickerTilt.BeginAnimation(RotateTransform.AngleProperty,
                new DoubleAnimation(angle, TimeSpan.FromMilliseconds(type == "goodnight" ? 800 : 240))
                    { AutoReverse = true, EasingFunction = new SineEase() });
        if (type is "hug" or "cheer" or "funny")
            StickerShake.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(-12, TimeSpan.FromMilliseconds(160))
                    { AutoReverse = true, EasingFunction = new SineEase() });
    }

    private void ApplyComboVisual()
    {
        ComboBadgeText.Text = "x" + _activeCount;
        ComboBadge.Visibility = Visibility.Visible;
        // Audio and motion respond to this received beat. Excess events update the counter,
        // never schedule a backlog of sounds, decodes or animation clocks.
        _app.PlayPop(_activeCount, _current?.Character ?? "", _activeType);
        if (_app.Config.ReducedMotion) return;
        long now = _feedbackClock.ElapsedMilliseconds;
        if (now - _lastVisualBeat < PresentationPolicy.VisualGapMs) return;
        _lastVisualBeat = now;
        bool accent = PresentationPolicy.IsAccent(_activeCount);
        ApplyMoodPose(_activeType);
        if (_activeCount % 4 == 0 && _current != null && !_current.HasFixedVariant &&
            now - _lastVariantBeat >= PresentationPolicy.VariantGapMs)
        {
            _lastVariantBeat = now;
            var variant = _stickers.GetRandom(_activeType, character: _current.Character);
            if (variant != null) { SetAnimationSource(StickerImage, variant); NormalizeSticker(variant.PixelWidth, variant.PixelHeight); }
        }
        // At most 1.20x. A tap gives a brief squash/stretch, not permanent giant growth.
        double target = PresentationPolicy.ComboScale(_activeCount);
        foreach (var prop in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
            StickerScale.BeginAnimation(prop, new DoubleAnimation(target + .055, target, TimeSpan.FromMilliseconds(190))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        if (accent)
        {
            BurstFor(_activeType, StickerCenter(), 1);
            if (_activeCount >= 5 && !_lv3Done) { _lv3Done = true; SpawnMiniParade(); }
        }
        foreach (var prop in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
            BadgeScale.BeginAnimation(prop, new DoubleAnimation(accent ? .8 : .95, 1, TimeSpan.FromMilliseconds(180)));
    }

    private void RestartHideTimer()
    {
        _hideTimer.Stop();
        _hideTimer.Interval = TimeSpan.FromSeconds(_peeking ? 1.8 :
            !string.IsNullOrWhiteSpace(_current?.Text) ? Math.Clamp(2.8 + _current.Text.Length * 0.035, 3.5, 6) : ShowSeconds);
        _hideTimer.Start();
    }

    private void HideCurrent()
    {
        if (!_isShowing || _isHiding) return;
        _isHiding = true;
        _hideTimer.Stop();

        int generation = _generation;
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(_app.Config.ReducedMotion ? 1 : 180));
        var slide = new DoubleAnimation(30, TimeSpan.FromMilliseconds(_app.Config.ReducedMotion ? 1 : 180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        fade.Completed += (_, _) =>
        {
            if (generation != _generation) return;
            Visibility = Visibility.Collapsed;
            RestoreEdgeChrome();
            _isShowing = false;
            _isHiding = false;
            ClearParticles();
            // Nhả frame GIF đã decode + trim working set → idle RAM thấp
            SetAnimationSource(StickerImage, null);
            if (!_app.Config.DiscreetMode && !_app.Config.HideMessageText && _queue.Take() is { } next) Display(next);
            else
            {
                _idleTimer.Start();
                if (!_app.Config.DiscreetMode && !_app.Config.HideMessageText) _app.RefreshDeferred();
            }
        };
        ContentSlide.BeginAnimation(TranslateTransform.YProperty, slide);
        ContentPanel.BeginAnimation(OpacityProperty, fade);
    }

    private void ApplyEdgeChrome()
    {
        if (!_edgeChromeSaved)
        {
            _cardBackground = StickerCard.Background;
            _cardBorderBrush = StickerCard.BorderBrush;
            _cardBorderThickness = StickerCard.BorderThickness;
            _cardEffect = StickerCard.Effect;
            _edgeChromeSaved = true;
        }
        StickerCard.Background = System.Windows.Media.Brushes.Transparent;
        StickerCard.BorderThickness = new Thickness(0);
        StickerCard.Effect = null;
        StickerCard.RenderTransform = new TranslateTransform(30, 0);
        StickerGlow.Visibility = Visibility.Collapsed;
        _edgePeek = true;
    }

    private void RestoreEdgeChrome()
    {
        if (!_edgePeek) return;
        _edgePeek = false;
        ContentSlide.BeginAnimation(TranslateTransform.XProperty, null);
        ContentSlide.X = 0;
        ContentTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        ContentTilt.Angle = 0;
        if (_edgeChromeSaved)
        {
            StickerCard.Background = _cardBackground;
            StickerCard.BorderBrush = _cardBorderBrush;
            StickerCard.BorderThickness = _cardBorderThickness;
            StickerCard.Effect = _cardEffect;
            StickerGlow.Visibility = Visibility.Collapsed;
        }
        StickerCard.RenderTransform = null;
    }

    private void PlayEdgeEntrance()
    {
        ContentScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        ContentScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        ContentSlide.BeginAnimation(TranslateTransform.XProperty, null);
        ContentSlide.BeginAnimation(TranslateTransform.YProperty, null);
        ContentTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        ContentPanel.BeginAnimation(OpacityProperty, null);
        BubbleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        BubbleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        BubbleScale.ScaleX = BubbleScale.ScaleY = 1;
        if (_app.Config.ReducedMotion)
        {
            ContentSlide.X = 0; ContentSlide.Y = 0; ContentTilt.Angle = -2.5;
            ContentScale.ScaleX = ContentScale.ScaleY = 1; ContentPanel.Opacity = 1;
            return;
        }
        ContentSlide.X = 175; ContentSlide.Y = 0;
        ContentScale.ScaleX = ContentScale.ScaleY = 0.92;
        ContentPanel.Opacity = 0;
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        ContentSlide.BeginAnimation(TranslateTransform.XProperty,
            new DoubleAnimation(175, 0, TimeSpan.FromMilliseconds(620)) { EasingFunction = ease });
        ContentTilt.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(-7, -2.5, TimeSpan.FromMilliseconds(620)) { EasingFunction = ease });
        ContentScale.BeginAnimation(ScaleTransform.ScaleXProperty,
            new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(620)) { EasingFunction = ease });
        ContentScale.BeginAnimation(ScaleTransform.ScaleYProperty,
            new DoubleAnimation(0.92, 1, TimeSpan.FromMilliseconds(620)) { EasingFunction = ease });
        ContentPanel.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220)));
    }

    // ================= Animation vào/ra =================

    private void PlayEntrance()
    {
        ContentScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        ContentScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        ContentSlide.BeginAnimation(TranslateTransform.YProperty, null);
        ContentTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        ContentPanel.BeginAnimation(OpacityProperty, null);
        BubbleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
        BubbleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
        BubbleScale.ScaleX = BubbleScale.ScaleY = 1;
        ContentTilt.Angle = 0;
        if (_peeking || _app.Config.ReducedMotion)
        {
            ContentScale.ScaleX = ContentScale.ScaleY = _peeking ? 0.42 : 1;
            ContentSlide.Y = 0; ContentPanel.Opacity = 1;
            return;
        }
        ContentScale.ScaleX = ContentScale.ScaleY = 0.55;
        ContentSlide.Y = 36;
        ContentPanel.Opacity = 0;

        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        StartScaleKeyframes(ContentScale, ScaleTransform.ScaleXProperty, ease);
        StartScaleKeyframes(ContentScale, ScaleTransform.ScaleYProperty, ease);

        var fade = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
        var rise = new DoubleAnimation(0, TimeSpan.FromMilliseconds(680)) { EasingFunction = ease };
        var tilt = new DoubleAnimation(-3, 0, TimeSpan.FromMilliseconds(680)) { EasingFunction = ease };
        ContentPanel.BeginAnimation(OpacityProperty, fade);
        ContentSlide.BeginAnimation(TranslateTransform.YProperty, rise);
        ContentTilt.BeginAnimation(RotateTransform.AngleProperty, tilt);
        if (Bubble.Visibility == Visibility.Visible)
        {
            foreach (var property in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
            {
                var bubble = new DoubleAnimation(.6, 1, TimeSpan.FromMilliseconds(550))
                    { BeginTime = TimeSpan.FromMilliseconds(100), EasingFunction = new BackEase { Amplitude = 1.4, EasingMode = EasingMode.EaseOut } };
                BubbleScale.BeginAnimation(property, bubble);
            }
        }
    }

    /// <summary>Soft in-place reaction when the mood changes mid-display:
    /// a quick dip of the whole panel; the sticker swap lands inside the dip.</summary>
    private void PlaySwapBounce()
    {
        if (_app.Config.ReducedMotion) return;
        var ease = new SineEase { EasingMode = EasingMode.EaseInOut };
        foreach (var property in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
        {
            var anim = new DoubleAnimationUsingKeyFrames();
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(0.86, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(110)), ease));
            anim.KeyFrames.Add(new EasingDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(280)), ease));
            ContentScale.BeginAnimation(property, anim);
        }
    }

    private BitmapSource? _avatarBubu;
    private BitmapSource? _avatarDudu;

    /// <summary>The bubble header shows the sender's own character instead of a
    /// generic heart when we know who sent it: the head of their hello sticker,
    /// cropped big and seated on a bright gradient disc so BOTH faces — white
    /// panda and dark brown bear alike — stay colorful and readable at 26px
    /// on the dark bubble (the full-body frame read as a dark smudge).</summary>
    private void UpdateBubbleAvatar(string character)
    {
        BitmapSource? avatar = character switch
        {
            "bubu" => _avatarBubu ??= LoadAvatar("hello-01", "bubu"),
            "dudu" => _avatarDudu ??= LoadAvatar("hello-03", "dudu"),
            _ => null,
        };
        if (avatar != null)
        {
            BubbleAvatar.Source = avatar;
            BubbleAvatarChip.Visibility = Visibility.Visible;
            BubbleHeartChip.Visibility = Visibility.Collapsed;
        }
        else
        {
            BubbleAvatarChip.Visibility = Visibility.Collapsed;
            BubbleHeartChip.Visibility = Visibility.Visible;
        }
    }

    private BitmapSource? LoadAvatar(string variant, string character)
    {
        var full = _stickers.GetRandom("hello", variant, character);
        if (full == null) return null;
        var b = _stickers.LastInfo?.bounds;
        if (b?.Length != 4 || b[2] <= b[0] || b[3] <= b[1]) return full;
        int x = Math.Max(0, b[0]);
        int y = Math.Max(0, b[1]);
        int w = Math.Min(full.PixelWidth - x, b[2] - b[0]);
        int h = Math.Min(full.PixelHeight - y, (int)((b[3] - b[1]) * 0.68));
        if (w < 8 || h < 8) return full;
        var crop = new CroppedBitmap(full, new Int32Rect(x, y, w, h));
        crop.Freeze();
        return crop;
    }

    private static void StartScaleKeyframes(ScaleTransform target,
        DependencyProperty prop, IEasingFunction ease)
    {
        var anim = new DoubleAnimationUsingKeyFrames();
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.08,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(270)), ease));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(.98, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(460)), ease));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(680)), ease));
        target.BeginAnimation(prop, anim);
    }

    private void PopSticker(double toScale)
    {
        var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 1.6 };
        foreach (var prop in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
        {
            var anim = new DoubleAnimation(toScale, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };
            StickerScale.BeginAnimation(prop, anim);
        }
    }

    private void ShakeSticker()
    {
        var anim = new DoubleAnimationUsingKeyFrames();
        double[] xs = { 0, -9, 8, -6, 5, 0 };
        for (int i = 0; i < xs.Length; i++)
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(xs[i],
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60 * (i + 1)))));
        StickerShake.BeginAnimation(TranslateTransform.XProperty, anim);
    }

    private Point StickerCenter()
    {
        // Layout coordinates stay stable while the entrance scales/slides the content.
        var point = StickerStage.TransformToAncestor(ContentPanel).Transform(new Point(132, 110));
        for (Visual? visual = ContentPanel; visual != null && !ReferenceEquals(visual, Root);
             visual = VisualTreeHelper.GetParent(visual) as Visual)
        {
            var offset = VisualTreeHelper.GetOffset(visual);
            point.Offset(offset.X, offset.Y);
        }
        return point;
    }

    // ================= Hạt cục bộ (pool 24) =================

    private Particle AddParticle(FrameworkElement el, double x, double y, bool behind = false)
    {
        if (_particles.Count >= MaxParticles)
        {
            // pool đầy → tái chế hạt cũ nhất
            var old = _particles[0];
            Recycle(old);
            _particles.RemoveAt(0);
        }
        var p = _recycled.Count > 0 ? _recycled.Pop() : new Particle();
        p.El = el; p.Vx = p.Vy = p.Gravity = p.X = p.Y = p.Age = p.SwayAmp = p.SwayFreq = p.Phase = 0;
        p.Life = p.StartScale = p.EndScale = 1; p.FadeIn = 0.12;
        p.Unit = PresentationPolicy.ArtworkScale * PresentationPolicy.NormalizeSize(_app.Config.OverlayScale);
        var center = StickerCenter();
        x = center.X + (x - center.X) * p.Unit;
        y = center.Y + (y - center.Y) * p.Unit;
        p.Move.X = p.Move.Y = 0; p.Scale.ScaleX = p.Scale.ScaleY = p.Unit;
        var tg = new TransformGroup();
        tg.Children.Add(p.Scale);
        tg.Children.Add(p.Move);
        el.RenderTransform = tg;
        el.RenderTransformOrigin = new Point(0, 0);
        el.Opacity = 0;
        Canvas.SetLeft(el, x);
        Canvas.SetTop(el, y);
        if (behind) ParticleCanvas.Children.Insert(0, el);
        else ParticleCanvas.Children.Add(el);
        _particles.Add(p);

        if (_particles.Count == 1)
        {
            _lastTick = DateTime.UtcNow;
            CompositionTarget.Rendering += OnTick;
        }
        return p;
    }

    private void ClearParticles()
    {
        foreach (var p in _particles) Recycle(p);
        ParticleCanvas.Children.Clear();
        _particles.Clear();
        CompositionTarget.Rendering -= OnTick;
    }

    private void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.UtcNow;
        double dt = Math.Min((now - _lastTick).TotalSeconds, 0.05);
        _lastTick = now;

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            p.Age += dt;
            if (p.Age >= p.Life)
            {
                Recycle(p);
                _particles.RemoveAt(i);
                continue;
            }
            p.Vy += p.Gravity * dt;
            p.X += p.Vx * dt; p.Y += p.Vy * dt;
            double px = p.X + Math.Sin(p.Age * p.SwayFreq + p.Phase) * p.SwayAmp;
            double py = p.Y;
            p.Move.X = px * p.Unit;
            p.Move.Y = py * p.Unit;
            double t = p.Age / p.Life;
            p.El.Opacity = t < p.FadeIn ? t / p.FadeIn : 1 - (t - p.FadeIn) / (1 - p.FadeIn);
            double s = p.StartScale + (p.EndScale - p.StartScale) * t;
            p.Scale.ScaleX = p.Scale.ScaleY = s * p.Unit;
        }

        if (_particles.Count == 0)
            CompositionTarget.Rendering -= OnTick; // hết hạt → dừng tick, idle 0% CPU
    }

    private void Recycle(Particle p)
    {
        ParticleCanvas.Children.Remove(p.El);
        if (p.El is Image image) { SetAnimationSource(image, null); image.Source = null; }
        if (p.El is TextBlock glyph && _glyphPool.Count < MaxParticles) _glyphPool.Push(glyph);
        if (_recycled.Count < MaxParticles) _recycled.Push(p);
    }

    private TextBlock Glyph(string s, double size, double opacity = 1)
    {
        var glyph = _glyphPool.Count > 0 ? _glyphPool.Pop() : new TextBlock { IsHitTestVisible = false };
        glyph.Text = s; glyph.FontSize = size; glyph.Opacity = opacity;
        return glyph;
    }

    private static readonly Geometry HeartGeometry = Geometry.Parse("M 10,18 C -7,6 1,-5 10,3 C 19,-5 27,6 10,18 Z");
    private static FrameworkElement HeartGlyph(double size, string color)
        => new System.Windows.Shapes.Path { Data = HeartGeometry, Stretch = Stretch.Uniform,
            Width = size, Height = size, Fill = (Brush)new BrushConverter().ConvertFromString(color)!, IsHitTestVisible = false };

    private void BurstFor(string type, Point c, int density)
    {
        LastBurstCenter = c;
        var kind = StickerService.ParticleKindFor(type);
        switch (kind)
        {
            case ParticleKind.HeartRise:
            {
                string[] hearts = { "💗", "💖", "💕", "❤️" };
                int n = 6 * density;
                for (int i = 0; i < n; i++)
                {
                    var p = AddParticle(HeartGlyph(18 + _rng.NextDouble() * 10, "#ED719D"),
                        c.X - 60 + _rng.NextDouble() * 120, c.Y - 40 + _rng.NextDouble() * 80);
                    p.Vx = (_rng.NextDouble() - 0.5) * 30;
                    p.Vy = -(50 + _rng.NextDouble() * 45);
                    p.Life = 1.2 + _rng.NextDouble() * 0.6;
                    p.SwayAmp = 12; p.SwayFreq = 3; p.Phase = _rng.NextDouble() * 6.28;
                }
                // glow hồng sau sticker
                var glow = new Ellipse
                {
                    Width = 230, Height = 230, IsHitTestVisible = false,
                    Fill = new RadialGradientBrush(Color.FromArgb(90, 255, 120, 160), Colors.Transparent)
                };
                var g = AddParticle(glow, c.X - 115, c.Y - 115, behind: true);
                g.Life = 1.2; g.StartScale = 0.6; g.EndScale = 1.15; g.FadeIn = 0.2;
                break;
            }
            case ParticleKind.HeartFall:
            {
                string[] hearts = { "🤍", "💗", "💭" };
                for (int i = 0; i < 6 * density; i++)
                {
                    var p = AddParticle(HeartGlyph(14 + _rng.NextDouble() * 8, "#EAA7C1"),
                        c.X - 80 + _rng.NextDouble() * 160, c.Y - 100 - _rng.NextDouble() * 40);
                    p.Vx = (_rng.NextDouble() - 0.5) * 16;
                    p.Vy = 26 + _rng.NextDouble() * 22; // rơi chậm như ngóng
                    p.Life = 1.8 + _rng.NextDouble() * 0.6;
                    p.SwayAmp = 14; p.SwayFreq = 2.2; p.Phase = _rng.NextDouble() * 6.28;
                }
                break;
            }
            case ParticleKind.Ring:
            {
                var ring = new Ellipse
                {
                    Width = 130, Height = 130, IsHitTestVisible = false,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xFF, 0xB3, 0x7E)),
                    StrokeThickness = 7
                };
                var p = AddParticle(ring, c.X - 65, c.Y - 65, behind: true);
                p.Life = 0.9; p.StartScale = 0.35; p.EndScale = 1.5; p.FadeIn = 0.1;
                for (int i = 0; i < 4 * density; i++)
                {
                    var q = AddParticle(HeartGlyph(16, "#EBA361"), c.X - 50 + _rng.NextDouble() * 100, c.Y);
                    q.Vy = -(40 + _rng.NextDouble() * 30); q.Life = 1.2;
                    q.SwayAmp = 10; q.SwayFreq = 3; q.Phase = _rng.NextDouble() * 6.28;
                }
                break;
            }
            case ParticleKind.Puff:
            {
                for (int i = 0; i < 2 + density; i++)
                {
                    var p = AddParticle(Glyph("💢", 24 + i * 4),
                        c.X - 20 + _rng.NextDouble() * 40, c.Y - 120 - i * 12);
                    p.Vy = -18; p.Life = 1.1;
                    p.StartScale = 0.5; p.EndScale = 1.35; p.FadeIn = 0.15;
                }
                if (type.Equals("angry", StringComparison.OrdinalIgnoreCase))
                {
                    // nền đỏ cam rất nhẹ sau sticker
                    var tint = new Ellipse
                    {
                        Width = 210, Height = 210, IsHitTestVisible = false,
                        Fill = new RadialGradientBrush(Color.FromArgb(60, 255, 110, 80), Colors.Transparent)
                    };
                    var t = AddParticle(tint, c.X - 105, c.Y - 105, behind: true);
                    t.Life = 1.0; t.FadeIn = 0.2;
                    ShakeSticker();
                }
                break;
            }
            case ParticleKind.Rain:
            {
                for (int i = 0; i < 10 * density; i++)
                {
                    var p = AddParticle(Glyph("💧", 13 + _rng.NextDouble() * 5),
                        c.X - 90 + _rng.NextDouble() * 180, c.Y - 130 - _rng.NextDouble() * 40, behind: true);
                    p.Vx = -22; p.Vy = 130 + _rng.NextDouble() * 50;
                    p.Life = 1.3 + _rng.NextDouble() * 0.4;
                }
                break;
            }
            case ParticleKind.Bubbles:
            {
                for (int i = 0; i < 7 * density; i++)
                {
                    var p = AddParticle(Glyph("🫧", 15 + _rng.NextDouble() * 10),
                        c.X - 60 + _rng.NextDouble() * 120, c.Y - 20 + _rng.NextDouble() * 60);
                    p.Vy = -(40 + _rng.NextDouble() * 40);
                    p.Life = 1.3 + _rng.NextDouble() * 0.5;
                    p.SwayAmp = 12; p.SwayFreq = 2.6; p.Phase = _rng.NextDouble() * 6.28;
                }
                break;
            }
            case ParticleKind.Star:
            {
                string[] stars = { "✨", "⭐", "💫" };
                for (int i = 0; i < 8 * density; i++)
                {
                    var p = AddParticle(Glyph(stars[_rng.Next(stars.Length)], 16 + _rng.NextDouble() * 10),
                        c.X - 60 + _rng.NextDouble() * 120, c.Y - 30);
                    p.Vx = (_rng.NextDouble() - 0.5) * 120;
                    p.Vy = -(90 + _rng.NextDouble() * 90);
                    p.Gravity = 260; // nảy tưng tưng
                    p.Life = 1.2 + _rng.NextDouble() * 0.4;
                }
                break;
            }
            case ParticleKind.Confetti:
            {
                Color[] colors = { Color.FromRgb(0xFF,0x6B,0x9D), Color.FromRgb(0xFF,0xD1,0x66),
                                   Color.FromRgb(0x6B,0xCB,0x77), Color.FromRgb(0x4D,0x96,0xFF),
                                   Color.FromRgb(0xB9,0x83,0xFF) };
                for (int i = 0; i < 14 * density; i++)
                {
                    var rect = new Rectangle
                    {
                        Width = 6, Height = 10, IsHitTestVisible = false,
                        Fill = new SolidColorBrush(colors[_rng.Next(colors.Length)])
                    };
                    var p = AddParticle(rect, c.X - 40 + _rng.NextDouble() * 80, c.Y - 20);
                    p.Vx = (_rng.NextDouble() - 0.5) * 320;
                    p.Vy = -(120 + _rng.NextDouble() * 160);
                    p.Gravity = 520;
                    p.Life = 1.2 + _rng.NextDouble() * 0.5;
                    p.StartScale = 1; p.EndScale = 0.7;
                }
                break;
            }
            case ParticleKind.Zzz:
            {
                for (int i = 0; i < 3; i++)
                {
                    var p = AddParticle(Glyph("Z", 15 + i * 7),
                        c.X + 60 + i * 8, c.Y - 90 - i * 10);
                    p.Vy = -32 - i * 6; p.Life = 1.5 + i * 0.2;
                    p.StartScale = 0.7; p.EndScale = 1.5; p.FadeIn = 0.2;
                    p.SwayAmp = 8; p.SwayFreq = 2; p.Phase = i;
                }
                break;
            }
            case ParticleKind.Food:
            {
                string[] foods = { "🍜", "🍗", "🍰", "🧋" };
                for (int i = 0; i < 6 * density; i++)
                {
                    var p = AddParticle(Glyph(foods[_rng.Next(foods.Length)], 20 + _rng.NextDouble() * 8),
                        c.X - 60 + _rng.NextDouble() * 120, c.Y - 30);
                    p.Vy = -(55 + _rng.NextDouble() * 35);
                    p.Life = 1.4 + _rng.NextDouble() * 0.5;
                    p.SwayAmp = 14; p.SwayFreq = 2.8; p.Phase = _rng.NextDouble() * 6.28;
                }
                break;
            }
        }
    }

    /// <summary>Lv3: 3 mini sticker chạy nối đuôi ở mép dưới overlay (cục bộ).</summary>
    private void SpawnMiniParade()
    {
        var center = StickerCenter();
        for (int i = 0; i < 2; i++)
        {
            var bmp = ImageBehavior.GetAnimatedSource(StickerImage);
            var img = new Image { Width = 68, Height = 68, IsHitTestVisible = false };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            // Reuse one static frame; three extra GIF decoders would inflate combo peak memory.
            if (bmp != null) img.Source = bmp;
            var p = AddParticle(img, center.X - 100 + i * 140, center.Y + 60);
            p.Vx = (i == 0 ? -1 : 1) * 18; p.Vy = -45;
            p.Life = .9; p.FadeIn = 0.08;
            p.SwayAmp = 6; p.SwayFreq = 6; p.Phase = i * 2; // tưng tưng khi chạy
        }
    }
}
