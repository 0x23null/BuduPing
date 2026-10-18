using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
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
    private const int MaxParticles = 24;   // pool hạt cố định
    private const double ComboWindowSec = 2.0;  // tính từ lần chạm GẦN NHẤT (v0.1.0 tính từ lúc bắt đầu hiện → spam là kẹt hàng đợi)
    private const double InterruptAfterSec = 1.0; // khác cảm xúc mà cái đang hiện đã chiếu quá 1s → nhường chỗ ngay
    private const double ShowSeconds = 2.8;     // v0.1.0 để 5s → spam sticker phải chờ nhau rất lâu

    private readonly StickerService _stickers;
    private readonly App _app;
    private readonly Queue<PingEvent> _queue = new();
    private readonly DispatcherTimer _hideTimer;
    private readonly Random _rng = new();

    private bool _isShowing;
    private bool _isHiding;
    private string _activeType = "";
    private int _activeCount;
    private DateTime _windowStartUtc;
    private DateTime _lastTapUtc;
    private bool _lv3Done;

    // Pool hạt: animate 1 vòng CompositionTarget.Rendering, hết hạt thì unsubscribe → idle 0% CPU
    private readonly List<Particle> _particles = new(MaxParticles);
    private DateTime _lastTick;

    private sealed class Particle
    {
        public FrameworkElement El = null!;
        public TranslateTransform Move = new();
        public ScaleTransform Scale = new();
        public double Vx, Vy, Gravity;
        public double Age, Life = 1, FadeIn = 0.12;
        public double SwayAmp, SwayFreq, Phase;
        public double StartScale = 1, EndScale = 1;
    }

    public OverlayWindow(StickerService stickers, App app)
    {
        _stickers = stickers;
        _app = app;
        InitializeComponent();

        _hideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(ShowSeconds) };
        _hideTimer.Tick += (_, _) => HideCurrent();

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
        var wa = SystemParameters.WorkArea; // trừ taskbar
        Left = wa.Right - Width - 16;
        Top = wa.Bottom - Height - 16;
    }

    // ================= Luồng event: queue + combo =================

    /// <summary>Nhận event từ Firebase (đã marshal về UI thread). Gọi DeleteFromInbox ngay khi nhận.</summary>
    public void ShowPing(PingEvent ping)
    {
        _app.DeleteFromInbox(ping.Id); // đã nhận → xoá khỏi inbox, reconnect không hiện lại

        var now = DateTime.UtcNow;
        bool sameType = string.Equals(ping.Type, _activeType, StringComparison.OrdinalIgnoreCase);
        if (_isShowing && !_isHiding && sameType &&
            (now - _lastTapUtc).TotalSeconds <= ComboWindowSec)
        {
            // Combo: cùng type, chạm liên tiếp → gộp + nới giờ ẩn, không xếp hàng
            _activeCount++;
            _lastTapUtc = now;
            ApplyComboVisual();
            RestartHideTimer();
            return;
        }

        if (_isShowing && !_isHiding && !sameType &&
            (now - _windowStartUtc).TotalSeconds >= InterruptAfterSec)
        {
            // Khác cảm xúc, cái đang hiện đã chiếu >1s → nhường chỗ ngay, bỏ hàng đợi cũ
            _queue.Clear();
            Display(ping);
            return;
        }

        if (_isShowing)
        {
            if (_queue.Count >= MaxQueue) _queue.Dequeue(); // đầy → bỏ cái cũ nhất
            _queue.Enqueue(ping);
            return;
        }

        Display(ping);
    }

    private void Display(PingEvent ping)
    {
        _isShowing = true;
        _isHiding = false;
        _activeType = ping.Type;
        _activeCount = 1;
        _lv3Done = false;
        _windowStartUtc = DateTime.UtcNow;
        _lastTapUtc = _windowStartUtc;

        var bmp = _stickers.GetRandom(ping.Type);
        if (bmp != null) ImageBehavior.SetAnimatedSource(StickerImage, bmp);

        string header = $"💌 {StickerService.LabelFor(ping.Type)}";
        if (!string.IsNullOrWhiteSpace(ping.From)) header += $" · từ {ping.From.Trim()}";
        BubbleHeader.Text = header;
        string text = (ping.Text ?? "").Trim();
        if (text.Length > 80) text = text[..80];
        if (text.Length == 0)
        {
            Bubble.Visibility = Visibility.Collapsed;
            BubbleTail.Visibility = Visibility.Collapsed;
        }
        else
        {
            BubbleText.Text = text;
            Bubble.Visibility = Visibility.Visible;
            BubbleTail.Visibility = Visibility.Visible;
        }

        ComboBadge.Visibility = Visibility.Collapsed;
        StickerScale.ScaleX = StickerScale.ScaleY = 1;
        StickerShake.X = StickerShake.Y = 0;

        Visibility = Visibility.Visible;
        PlaceBottomRight();
        PlayEntrance();

        BurstFor(ping.Type, StickerCenter(), density: 1);
        _app.PlayPop();
        RestartHideTimer();
    }

    private void ApplyComboVisual()
    {
        var center = StickerCenter();
        if (_activeCount >= 5 && !_lv3Done)
        {
            // Lv3: BÙNG NỔ CỤC BỘ — sticker lớn + mini sticker chạy nối đuôi ở mép dưới + mưa hạt quanh sticker
            _lv3Done = true;
            ComboBadgeText.Text = "x" + _activeCount;
            ComboBadge.Visibility = Visibility.Visible;
            PopSticker(1.45);
            SpawnMiniParade();
            BurstFor(_activeType, center, density: 3);
            _app.PlayPop(2);
        }
        else if (_activeCount >= 3)
        {
            // Lv2: sticker to ~1.3x + badge xN + hạt dày hơn
            ComboBadgeText.Text = "x" + _activeCount;
            ComboBadge.Visibility = Visibility.Visible;
            PopSticker(1.3);
            BurstFor(_activeType, center, density: 2);
            _app.PlayPop(1);
        }
        else
        {
            PopSticker(1 + 0.06 * _activeCount);
            _app.PlayPop(1);
        }
    }

    private void RestartHideTimer()
    {
        _hideTimer.Stop();
        _hideTimer.Start(); // tự ẩn sau 5s kể từ lần chạm cuối
    }

    private void HideCurrent()
    {
        if (!_isShowing || _isHiding) return;
        _isHiding = true;
        _hideTimer.Stop();

        var sb = new Storyboard();
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
        Storyboard.SetTarget(fade, ContentPanel);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));
        var slide = new DoubleAnimation(30, TimeSpan.FromMilliseconds(180))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(slide, ContentSlide);
        Storyboard.SetTargetProperty(slide, new PropertyPath(TranslateTransform.YProperty));
        sb.Children.Add(fade);
        sb.Children.Add(slide);
        sb.Completed += (_, _) =>
        {
            Visibility = Visibility.Collapsed;
            _isShowing = false;
            _isHiding = false;
            ClearParticles();
            // Nhả frame GIF đã decode + trim working set → idle RAM thấp
            ImageBehavior.SetAnimatedSource(StickerImage, null);
            _stickers.ReleaseDecoded();
            MemoryTrim.Request();
            if (_queue.Count > 0) Display(_queue.Dequeue());
        };
        sb.Begin();
    }

    // ================= Animation vào/ra =================

    private void PlayEntrance()
    {
        // Spring scale 0.6 → 1.08 → 1.0 trong ~260ms + fade + trượt lên
        ContentScale.ScaleX = ContentScale.ScaleY = 0.6;
        ContentSlide.Y = 24;
        ContentPanel.Opacity = 0;

        var sb = new Storyboard();
        var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
        AddScaleKeyframes(sb, ContentScale, ScaleTransform.ScaleXProperty, ease);
        AddScaleKeyframes(sb, ContentScale, ScaleTransform.ScaleYProperty, ease);

        var fade = new DoubleAnimation(1, TimeSpan.FromMilliseconds(200));
        Storyboard.SetTarget(fade, ContentPanel);
        Storyboard.SetTargetProperty(fade, new PropertyPath(OpacityProperty));
        var rise = new DoubleAnimation(0, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease };
        Storyboard.SetTarget(rise, ContentSlide);
        Storyboard.SetTargetProperty(rise, new PropertyPath(TranslateTransform.YProperty));
        sb.Children.Add(fade);
        sb.Children.Add(rise);
        sb.Begin();
    }

    private static void AddScaleKeyframes(Storyboard sb, ScaleTransform target,
        DependencyProperty prop, IEasingFunction ease)
    {
        var anim = new DoubleAnimationUsingKeyFrames();
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.08,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(150)), ease));
        anim.KeyFrames.Add(new EasingDoubleKeyFrame(1.0,
            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(260)), ease));
        Storyboard.SetTarget(anim, target);
        Storyboard.SetTargetProperty(anim, new PropertyPath(prop));
        sb.Children.Add(anim);
    }

    private void PopSticker(double toScale)
    {
        var sb = new Storyboard();
        var ease = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 1.6 };
        foreach (var prop in new[] { ScaleTransform.ScaleXProperty, ScaleTransform.ScaleYProperty })
        {
            var anim = new DoubleAnimation(toScale, TimeSpan.FromMilliseconds(200)) { EasingFunction = ease };
            Storyboard.SetTarget(anim, StickerScale);
            Storyboard.SetTargetProperty(anim, new PropertyPath(prop));
            sb.Children.Add(anim);
        }
        sb.Begin();
    }

    private void ShakeSticker()
    {
        var anim = new DoubleAnimationUsingKeyFrames();
        double[] xs = { 0, -9, 8, -6, 5, 0 };
        for (int i = 0; i < xs.Length; i++)
            anim.KeyFrames.Add(new LinearDoubleKeyFrame(xs[i],
                KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60 * (i + 1)))));
        Storyboard.SetTarget(anim, StickerShake);
        Storyboard.SetTargetProperty(anim, new PropertyPath(TranslateTransform.XProperty));
        var sb = new Storyboard();
        sb.Children.Add(anim);
        sb.Begin();
    }

    private Point StickerCenter()
    {
        try
        {
            var p = StickerImage.TranslatePoint(
                new Point(StickerImage.ActualWidth / 2, StickerImage.ActualHeight * 0.4), Root);
            if (!double.IsNaN(p.X) && !double.IsInfinity(p.X)) return p;
        }
        catch { }
        return new Point(Width / 2, Height - 140);
    }

    // ================= Hạt cục bộ (pool 24) =================

    private Particle AddParticle(FrameworkElement el, double x, double y, bool behind = false)
    {
        if (_particles.Count >= MaxParticles)
        {
            // pool đầy → tái chế hạt cũ nhất
            var old = _particles[0];
            ParticleCanvas.Children.Remove(old.El);
            _particles.RemoveAt(0);
        }
        var p = new Particle { El = el };
        var tg = new TransformGroup();
        tg.Children.Add(p.Move);
        tg.Children.Add(p.Scale);
        el.RenderTransform = tg;
        el.RenderTransformOrigin = new Point(0.5, 0.5);
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
                ParticleCanvas.Children.Remove(p.El);
                _particles.RemoveAt(i);
                continue;
            }
            p.Vy += p.Gravity * dt;
            double px = p.Vx * p.Age + Math.Sin(p.Age * p.SwayFreq + p.Phase) * p.SwayAmp;
            double py = p.Vy * p.Age + 0.5 * p.Gravity * p.Age * p.Age;
            p.Move.X = px;
            p.Move.Y = py;
            double t = p.Age / p.Life;
            p.El.Opacity = t < p.FadeIn ? t / p.FadeIn : 1 - (t - p.FadeIn) / (1 - p.FadeIn);
            double s = p.StartScale + (p.EndScale - p.StartScale) * t;
            p.Scale.ScaleX = p.Scale.ScaleY = s;
        }

        if (_particles.Count == 0)
            CompositionTarget.Rendering -= OnTick; // hết hạt → dừng tick, idle 0% CPU
    }

    private static TextBlock Glyph(string s, double size, double opacity = 1)
        => new() { Text = s, FontSize = size, Opacity = opacity, IsHitTestVisible = false };

    private void BurstFor(string type, Point c, int density)
    {
        var kind = StickerService.ParticleKindFor(type);
        switch (kind)
        {
            case ParticleKind.HeartRise:
            {
                string[] hearts = { "💗", "💖", "💕", "❤️" };
                int n = 6 * density;
                for (int i = 0; i < n; i++)
                {
                    var p = AddParticle(Glyph(hearts[_rng.Next(hearts.Length)], 18 + _rng.NextDouble() * 10),
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
                    var p = AddParticle(Glyph(hearts[_rng.Next(hearts.Length)], 14 + _rng.NextDouble() * 8),
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
                    var q = AddParticle(Glyph("🧡", 16), c.X - 50 + _rng.NextDouble() * 100, c.Y);
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
        for (int i = 0; i < 3; i++)
        {
            var bmp = _stickers.GetRandom(_activeType);
            var img = new Image { Width = 68, Height = 68, IsHitTestVisible = false };
            RenderOptions.SetBitmapScalingMode(img, BitmapScalingMode.HighQuality);
            if (bmp != null) ImageBehavior.SetAnimatedSource(img, bmp);
            var p = AddParticle(img, -70 - i * 85, Height - 100);
            p.Vx = 150; p.Vy = 0;
            p.Life = 2.8; p.FadeIn = 0.08;
            p.SwayAmp = 6; p.SwayFreq = 6; p.Phase = i * 2; // tưng tưng khi chạy
        }
    }
}
