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
    private SoundPlayer? _popPlayer;
    private System.Threading.Timer? _presenceTimer;
    private readonly Random _rng = new();
    private bool _exiting;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        Config = AppConfigStore.Load();
        AppConfigStore.ApplyAutostart(Config);

        string stickersRoot = Path.Combine(AppContext.BaseDirectory, "Assets", "Stickers");
        _stickers = new StickerService(stickersRoot);
        _stickers.Preload(); // decode sẵn toàn bộ GIF → hiện là hiện ngay

        LoadPopSound();

        _overlay = new OverlayWindow(_stickers, this);
        _settings = new SettingsWindow(this);

        SetupTray();
        StartSync(Config.PairCode);
        StartPresence();
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
        if (_settings == null) return;
        _settings.RefreshUi();
        _settings.Show();
        _settings.Activate();
    }

    // ================= Firebase sync + presence =================

    private void StartSync(string pairCode)
    {
        _sync?.Dispose();
        _sync = new FirebaseSync(PairCode.ToPairId(pairCode));
        _sync.PingReceived += OnPingReceived;
        _sync.Start();
        _ = _sync.PutPresenceAsync(); // báo online ngay
    }

    private void OnPingReceived(PingEvent ping)
    {
        // FirebaseSync chạy trên background thread → marshal về UI thread
        Dispatcher.InvokeAsync(() => _overlay?.ShowPing(ping));
    }

    private void StartPresence()
    {
        // Heartbeat mỗi 20s → điện thoại hiện chấm xanh khi desktop còn sống (kể cả chạy tray)
        _presenceTimer = new System.Threading.Timer(
            _ => { try { _sync?.PutPresenceAsync().GetAwaiter().GetResult(); } catch { } },
            null, TimeSpan.Zero, TimeSpan.FromSeconds(20));
    }

    public void DeleteFromInbox(string eventId) => _sync?.DeleteFromInbox(eventId);

    public void RegeneratePairCode()
    {
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

    private void LoadPopSound()
    {
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            var s = asm.GetManifestResourceStream("BuduPing.Assets.Sounds.pop.wav");
            if (s != null)
            {
                _popPlayer = new SoundPlayer(s);
                _popPlayer.Load(); // decode sẵn, phát là kêu ngay
            }
        }
        catch { }
    }

    public void PlayPop()
    {
        if (!Config.SoundOn || _popPlayer == null) return;
        try { _popPlayer.Play(); } catch { }
    }

    // ================= Test sticker nội bộ =================

    public void SendTestPing()
    {
        string[] types = _stickers?.KnownTypes.Count > 0
            ? _stickers.KnownTypes.ToArray()
            : new[] { "love", "miss", "hug", "angry", "cheer", "funny" };
        var ping = new PingEvent
        {
            Type = types[_rng.Next(types.Length)],
            Text = "Sticker test nè 💌",
            From = "Tester",
        };
        Dispatcher.InvokeAsync(() => _overlay?.ShowPing(ping));
    }

    // ================= Thoát hẳn =================

    private async void QuitApp()
    {
        if (_exiting) return;
        _exiting = true;
        try
        {
            _presenceTimer?.Dispose();
            _sync?.Stop();
            if (_sync != null) await _sync.DeletePresenceAsync(); // chấm xám trên điện thoại
        }
        catch { }
        finally
        {
            _sync?.Dispose();
            _tray?.Dispose();
            Shutdown();
        }
    }
}
