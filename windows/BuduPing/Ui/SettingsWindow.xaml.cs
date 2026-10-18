using System.ComponentModel;
using System.Windows;
using BuduPing.Core;

namespace BuduPing.Ui;

/// <summary>
/// Cửa sổ cài đặt: mã cặp (Copy / Gen mới), công tắc tiếng + autostart, nút Test sticker.
/// Nút X CHỈ ẩn cửa sổ xuống tray — thoát hẳn chỉ qua menu tray.
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly App _app;
    private bool _suspendEvents;

    public SettingsWindow(App app)
    {
        _app = app;
        InitializeComponent();
        Closing += OnClosingHide;
    }

    private void OnClosingHide(object? sender, CancelEventArgs e)
    {
        e.Cancel = true; // X → xuống tray, không thoát app
        Hide();
    }

    /// <summary>Đồng bộ UI với config hiện tại (gọi mỗi lần mở cửa sổ).</summary>
    public void RefreshUi()
    {
        _suspendEvents = true;
        try
        {
            CodeText.Text = PairCode.ToDisplay(_app.Config.PairCode);
            SoundCheck.IsChecked = _app.Config.SoundOn;
            AutostartCheck.IsChecked = _app.Config.Autostart;
        }
        finally { _suspendEvents = false; }
    }

    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Clipboard.SetText(PairCode.ToDisplay(_app.Config.PairCode));
            CopyButton.Content = "✅ Đã copy!";
            var t = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromSeconds(1.5) };
            t.Tick += (_, _) => { CopyButton.Content = "📋 Copy mã"; t.Stop(); };
            t.Start();
        }
        catch { }
    }

    private void RegenButton_Click(object sender, RoutedEventArgs e)
    {
        var r = MessageBox.Show(
            "Gen mã mới sẽ VÔ HIỆU HOÁ mã cũ ngay lập tức.\nĐiện thoại phải nhập lại mã mới để kết nối.",
            "Gen mã cặp mới?", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (r == MessageBoxResult.Yes) _app.RegeneratePairCode();
    }

    private void SoundCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suspendEvents) return;
        _app.SetSound(SoundCheck.IsChecked == true);
    }

    private void AutostartCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_suspendEvents) return;
        _app.SetAutostart(AutostartCheck.IsChecked == true);
    }

    private void TestButton_Click(object sender, RoutedEventArgs e) => _app.SendTestPing();

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
}
