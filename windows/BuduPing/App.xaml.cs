using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Hardcodet.Wpf.TaskbarNotification;
using BuduPing.Core;
using BuduPing.Services;
using BuduPing.Ui;

namespace BuduPing;

/// <summary>
/// Điểm vào app: nạp config → tray icon → overlay → Firebase SSE → presence heartbeat.
/// App sống trong tray; thoát hẳn chỉ qua menu tray (OnExplicitShutdown).
/// </summary>
public partial class App : Application
{
    public AppConfig Config { get; private set; } = new();

    private TaskbarIcon? _tray;
    private MenuItem? _soundMenuItem;
    private OverlayWindow? _overlay;
    private SettingsWindow? _settings;
    private FirebaseSync? _sync;
    private StickerService? _stickers;
    private SoundService? _sounds;
    private CancellationTokenSource? _presenceCts;
    private ReplyHotkey? _hotkey;
    private MenuItem? _discreetMenu;
    private bool _replying;
    private DateTime _lastReply = DateTime.MinValue;
    public bool IsExiting => _exiting;
    public bool HotkeyAvailable => _hotkey?.Registered == true;
    public bool CanReply => _overlay?.LastPing != null;
    public int PendingCount => _overlay?.PendingCount ?? 0;
    private readonly Random _rng = new();
    private bool _exiting;
    private Mutex? _instance;
    private EventWaitHandle? _activate;
    private RegisteredWaitHandle? _activateWait;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Length == 2 && e.Args[0] == "--memory-review") { StartMemoryReview(e.Args[1]); return; }
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        if (e.Args.Length == 2 && e.Args[0] == "--ui-review")
        {
            RenderUiReview(e.Args[1]);
            return;
        }

        // A second manual launch activates the existing process instead of consuming more RAM.
        string instanceName = "Local\\BuduPing-" + System.Security.Principal.WindowsIdentity.GetCurrent().User!.Value;
        _instance = new Mutex(true, instanceName, out bool firstInstance);
        if (!firstInstance)
        {
            try { using var signal = EventWaitHandle.OpenExisting(instanceName + "-activate"); signal.Set(); } catch { }
            Shutdown(); return;
        }
        _activate = new EventWaitHandle(false, EventResetMode.AutoReset, instanceName + "-activate");
        _activateWait = ThreadPool.RegisterWaitForSingleObject(_activate,
            (_, _) => Dispatcher.InvokeAsync(OpenSettings), null, Timeout.Infinite, false);

        Config = AppConfigStore.Load();
        AppConfigStore.ApplyAutostart(Config);

        string stickersRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers");
        _stickers = new StickerService(stickersRoot);
        _stickers.Preload(); // chỉ nạp bytes GIF nhúng; decode lười khi hiện (nhẹ RAM)

        _sounds = new SoundService();

        _overlay = new OverlayWindow(_stickers, this);


        SetupTray();
        StartSync(Config.PairCode);
        StartPresence();

        _hotkey = new ReplyHotkey();
        _hotkey.Pressed += () => _ = ReplyToLastAsync();
        MemoryTrim.Record("startup", _stickers.DecodedBytes);
        if (!Config.HasSeenSetup) { OpenSettings(); Config.HasSeenSetup = true; AppConfigStore.Save(Config); }
        else if (!e.Args.Contains("--autostart")) new StartupNotice(Config).Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _activateWait?.Unregister(null); _activate?.Dispose(); _instance?.Dispose();
        base.OnExit(e);
    }

    // Offline screenshot mode for CI. Never loads user config, writes presence, or starts Firebase.
    private void RenderUiReview(string directory)
    {
        Directory.CreateDirectory(directory);
        Config = new AppConfig { PairCode = PairCode.Generate(), HasSeenSetup = true };
        _stickers = new StickerService(Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers"));
        _stickers.Preload();
        _hotkey = new ReplyHotkey();
        _settings = new SettingsWindow(this);
        _settings.RefreshUi(); _settings.Show();
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromMilliseconds(800) };
        timer.Tick += async (_, _) =>
        {
            timer.Stop();
            try
            {
                SaveWindow("settings-top.png");
                if (_settings.FindName("SettingsScroll") is ScrollViewer scroll) scroll.ScrollToEnd();
                _settings.UpdateLayout();
                SaveWindow("settings-options.png");
                _settings.Hide();
                if (_stickers.HasForCharacter("sad", "dudu") || !_stickers.HasForCharacter("love", "dudu") ||
                    !_stickers.HasForCharacter("sad", "bubu"))
                    throw new InvalidOperationException("Native character catalog mapping is incorrect.");
                _overlay = new OverlayWindow(_stickers, this);
                _overlay.ShowPing(new PingEvent { Id = "review-message", Type = "hug", From = "Người thương", Character = "dudu",
                    Text = "Một cái ôm gửi đến bạn. Hôm nay mình vẫn ở đây nhé!", IsLocal = true });
                await Task.Delay(950);
                SaveVisual((FrameworkElement)_overlay.Content, "overlay-message.png");
                _overlay.ResetPair();
                _overlay.ShowPing(new PingEvent { Id = "review-combo-0", Type = "love", IsLocal = true });
                for (int i = 1; i < 13; i++)
                {
                    await Task.Delay(120);
                    _overlay.ShowPing(new PingEvent { Id = "review-combo-" + i, Type = "love", IsLocal = true });
                }
                await Task.Delay(950);
                SaveVisual((FrameworkElement)_overlay.Content, "overlay-combo.png");
                if (_overlay.ActiveComboCount != 13 || _overlay.ActiveParticleCount > PresentationPolicy.MaxParticles)
                    throw new InvalidOperationException("Combo count or particle bound failed.");
                _overlay.ResetPair();
                Config.OverlayScale = .6;
                _overlay.ShowPing(new PingEvent { Type = "love", Variant = "love-03", IsLocal = true });
                await Task.Delay(750);
                SaveVisual((FrameworkElement)_overlay.Content, "overlay-small.png");
                _overlay.ResetPair();
                Config.OverlayScale = 1;
                _overlay.ShowPing(new PingEvent { Type = "hello", IsLocal = true });
                await Task.Delay(850);
                SaveVisual((FrameworkElement)_overlay.Content, "overlay-hello.png");
                Config.DiscreetMode = true; Config.MyCharacter = "bubu";
                _overlay.ResetPair();
                SendTestPing(); await Task.Delay(200);
                AssertDiscreet();
                ShowReplyConfirmation(_stickers.PickVariant("hug", "bubu"));
                await Task.Delay(200); AssertDiscreet();
                SaveVisual((FrameworkElement)_overlay.Content, "overlay-discreet.png", peek: true);
                // Burst regression: counters remain exact while work stays bounded.
                Config.DiscreetMode = false; Config.OverlayScale = 1;
                _overlay.ResetPair();
                for (int i = 0; i < 200; i++) _overlay.ShowPing(new PingEvent
                    { Type = "love", Variant = "love-03", Character = "dudu", VariantLocked = false, IsLocal = true });
                await Task.Delay(750);
                if (_overlay.ActiveComboCount != 200 || _overlay.ActiveParticleCount > PresentationPolicy.MaxParticles)
                    throw new InvalidOperationException("200-tap spam did not remain bounded.");
                _overlay.ResetPair();
                var notice = new StartupNotice(Config, autoDismiss: false); notice.Show(); notice.UpdateLayout();
                SaveVisual((FrameworkElement)notice.Content, "startup-notice.png"); notice.Close();
                File.WriteAllText(Path.Combine(directory, "behavior-checks.json"),
                    "{\"combo13\":true,\"spam200\":true,\"discreetTest\":true,\"discreetReply\":true}");
                void AssertDiscreet()
                {
                    if (!Config.DiscreetMode || !_overlay.IsPeeking ||
                        ((FrameworkElement)_overlay.FindName("Bubble")).Visibility != Visibility.Collapsed)
                        throw new InvalidOperationException("Test/reply bypassed discreet mode.");
                }
            }
            catch (Exception error)
            {
                File.WriteAllText(Path.Combine(directory, "review-error.txt"), error.ToString());
                Environment.ExitCode = 1;
            }
            finally { _overlay?.ResetPair(); _exiting = true; _hotkey.Dispose(); Shutdown(Environment.ExitCode); }
        };
        timer.Start();
        void SaveWindow(string name)
        {
            _settings.UpdateLayout();
            SaveVisual((FrameworkElement)_settings.Content, name);
        }
        void SaveVisual(FrameworkElement visual, string name, bool peek = false)
        {
            visual.UpdateLayout();
            if (_overlay != null && ReferenceEquals(visual, _overlay.Content))
            {
                var scale = (System.Windows.Media.ScaleTransform)_overlay.FindName("ContentScale");
                var slide = (System.Windows.Media.TranslateTransform)_overlay.FindName("ContentSlide");
                var panel = (FrameworkElement)_overlay.FindName("ContentPanel");
                File.WriteAllText(Path.Combine(directory, name + ".json"), System.Text.Json.JsonSerializer.Serialize(new
                {
                    width = visual.ActualWidth, height = visual.ActualHeight, scaleX = scale.ScaleX, scaleY = scale.ScaleY,
                    slideY = slide.Y, panelHeight = panel.ActualHeight, panelWidth = panel.ActualWidth,
                    dpi = System.Windows.Media.VisualTreeHelper.GetDpi(visual).PixelsPerDip,
                    burstX = _overlay.LastBurstCenter.X, burstY = _overlay.LastBurstCenter.Y,
                    artworkDip = 244 * PresentationPolicy.ArtworkScale * Config.OverlayScale,
                    sizeSetting = Config.OverlayScale, particles = _overlay.ActiveParticleCount, combo = _overlay.ActiveComboCount,
                }));
                if (Math.Abs(scale.ScaleX - (peek ? .42 : 1)) > .01 || Math.Abs(scale.ScaleY - (peek ? .42 : 1)) > .01 || Math.Abs(slide.Y) > .1)
                    throw new InvalidOperationException("The native entrance did not settle at full size and position.");
                if (!peek && _overlay.LastBurstCenter.Y < visual.ActualHeight / 2)
                    throw new InvalidOperationException("Particles were placed outside the settled sticker area.");
            }
            var bitmap = new System.Windows.Media.Imaging.RenderTargetBitmap(
                (int)Math.Ceiling(visual.ActualWidth), (int)Math.Ceiling(visual.ActualHeight),
                96, 96, System.Windows.Media.PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            using var output = File.Create(Path.Combine(directory, name)); encoder.Save(output);
        }
    }

    // ================= Tray =================

    private void SetupTray()
    {
        _tray = new TaskbarIcon();
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var s = asm.GetManifestResourceStream("BuduPing.Assets.tray.ico");
            if (s != null) _tray.Icon = new Icon(s); // giữ stream sống cùng app
        }
        catch { /* không có icon vẫn chạy */ }

        UpdateTrayTooltip();
        _tray.TrayMouseDoubleClick += (_, _) => OpenSettings();

        var menu = new ContextMenu();
        var open = new MenuItem { Header = "⚙️ Mở cài đặt" };
        open.Click += (_, _) => OpenSettings();
        _soundMenuItem = new MenuItem
        {
            Header = "🔊 Tiếng hiệu ứng",
            IsCheckable = true,
            IsChecked = Config.SoundOn
        };
        _soundMenuItem.Click += (_, _) => SetSound(_soundMenuItem.IsChecked);
        var quit = new MenuItem { Header = "❌ Thoát BuduPing" };
        quit.Click += (_, _) => QuitApp();

        menu.Items.Add(open);
        menu.Items.Add(_soundMenuItem);
        _discreetMenu = new MenuItem { Header = "Chế độ kín đáo", IsCheckable = true, IsChecked = Config.DiscreetMode };
        _discreetMenu.Click += (_, _) => SetDiscreet(_discreetMenu.IsChecked);
        var reply = new MenuItem { Header = "Gửi ôm đáp lại (Ctrl+Alt+Space)" };
        reply.Click += (_, _) => _ = ReplyToLastAsync();
        var pending = new MenuItem { Header = "Mở xem các lời nhắn đang chờ" };
        pending.Click += (_, _) => OpenPending();
        menu.Items.Add(_discreetMenu); menu.Items.Add(reply); menu.Items.Add(pending);
        menu.Items.Add(new Separator());
        menu.Items.Add(quit);
        _tray.ContextMenu = menu;
    }

    private void UpdateTrayTooltip()
    {
        if (_tray != null)
            _tray.ToolTipText = $"BuduPing 💗\nMã cặp: {PairCode.ToDisplay(Config.PairCode)}";
    }

    public void OpenSettings()
    {
        _settings ??= new SettingsWindow(this);
        _settings.RefreshUi();
        _settings.Show();
        _settings.Activate();
    }

    // ================= Firebase sync + presence =================

    private void StartSync(string pairCode)
    {
        _sync?.Dispose();
        var sync = new FirebaseSync(PairCode.ToPairId(pairCode));
        _sync = sync;
        sync.PingReceived += ping => Dispatcher.InvokeAsync(() =>
        {
            if (ReferenceEquals(_sync, sync)) { _overlay?.ShowPing(ping); _settings?.RefreshDeliveryState(); }
        });
        sync.Start();
        _ = _sync.PutPresenceAsync(); // báo online ngay
    }

    private void OnPingReceived(PingEvent ping)
    {
        // FirebaseSync chạy trên background thread → marshal về UI thread
        Dispatcher.InvokeAsync(() => _overlay?.ShowPing(ping));
    }

    private void StartPresence()
    {
        _presenceCts = new CancellationTokenSource();
        _ = PresenceLoopAsync(_presenceCts.Token);
    }

    private async Task PresenceLoopAsync(CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(20));
        try
        {
            do { if (_sync != null) await _sync.PutPresenceAsync(ct); }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) { }
    }

    public void CompletePing(PingEvent ping, string status)
    {
        _sync?.Complete(ping, status);
        _settings?.RefreshDeliveryState();
    }
    public void DeferPing(PingEvent ping) { if (!ping.IsLocal) _sync?.Defer(ping.Id); }
    public void RefreshDeferred() { if (_sync != null) _ = _sync.RefreshDeferredAsync(); }

    public void SetDiscreet(bool on)
    {
        Config.DiscreetMode = on; AppConfigStore.Save(Config);
        if (on) _sounds?.Stop();
        if (_discreetMenu != null) _discreetMenu.IsChecked = on;
        _overlay?.ApplyPreferences();
        if (!on) _overlay?.ShowPending();
        _settings?.RefreshUi();
    }

    public void OpenPending()
    {
        Config.HideMessageText = false;
        SetDiscreet(false);
    }

    public void ApplyPreferences()
    {
        AppConfigStore.Save(Config);
        _overlay?.ApplyPreferences();
    }

    public async Task ReplyToLastAsync()
    {
        var ping = _overlay?.LastPing;
        if (_replying || ping == null || _sync == null || (DateTime.UtcNow - _lastReply).TotalSeconds < 1) return;
        _replying = true;
        string variant = _stickers?.PickVariant("hug", Config.MyCharacter) ?? "";
        bool sent = await _sync.ReplyAsync(ping, variant);
        _replying = false;
        if (sent)
        {
            _lastReply = DateTime.UtcNow;
            ShowReplyConfirmation(variant);
        }
        else _tray?.ShowBalloonTip("BuduPing", "Chưa gửi được cái ôm. Thử lại khi có mạng nhé.", BalloonIcon.Warning);
    }

    internal void ShowReplyConfirmation(string variant)
        => _overlay?.ShowPing(new PingEvent { Type = "hug", Variant = variant, Character = Config.MyCharacter,
            Text = "Đã gửi một cái ôm đáp lại.", IsLocal = true });

    public void RegeneratePairCode()
    {
        if (_sync != null) _ = _sync.DeletePresenceAsync();
        _overlay?.ResetPair();
        Config.PairCode = PairCode.Generate();
        AppConfigStore.Save(Config);
        StartSync(Config.PairCode); // mã mới → inbox mới, mã cũ mất hiệu lực
        UpdateTrayTooltip();
        _settings?.RefreshUi();
    }

    // ================= Cài đặt: tiếng + autostart =================

    public void SetSound(bool on)
    {
        Config.SoundOn = on;
        if (!on) _sounds?.Stop();
        AppConfigStore.Save(Config);
        if (_soundMenuItem != null) _soundMenuItem.IsChecked = on;
    }

    public void SetAutostart(bool on)
    {
        Config.Autostart = on;
        AppConfigStore.Save(Config);
        AppConfigStore.ApplyAutostart(Config);
    }

    // ================= Âm pop =================

    private void LoadPopSound() => _sounds = new SoundService();

    public void PlayPop(int comboCount = 1, string character = "", string type = "")
    {
        if (Config.SoundOn && !Config.DiscreetMode) _sounds?.Play(comboCount, character, type);
    }


    // ================= Test sticker nội bộ =================

    public void SendTestPing()
    {
        string[] types = _stickers?.KnownTypes.Where(type => _stickers.HasForCharacter(type, Config.MyCharacter)).ToArray()
            ?? Array.Empty<string>();
        if (types.Length == 0) return;
        var ping = new PingEvent
        {
            IsLocal = true,
            Type = types[_rng.Next(types.Length)],
            Text = "Sticker test nè 💌",
            From = "Tester",
        };
        var variant = _stickers?.PickVariant(ping.Type, Config.MyCharacter) ?? "";
        if (string.IsNullOrEmpty(variant) && !string.IsNullOrEmpty(Config.MyCharacter)) return;
        Dispatcher.InvokeAsync(() => _overlay?.ShowPing(ping with { Variant = variant, Character = Config.MyCharacter }));
    }

    // ================= Thoát hẳn =================

    private async void QuitApp()
    {
        if (_exiting) return;
        _exiting = true;
        try
        {
            _presenceCts?.Cancel();
            _hotkey?.Dispose();
            _sync?.Stop();
            if (_sync != null) await _sync.DeletePresenceAsync(); // chấm xám trên điện thoại
        }
        catch { }
        finally
        {
            _sync?.Dispose();
            _sounds?.Dispose();
            _tray?.Dispose();
            Shutdown();
        }
    }
}
