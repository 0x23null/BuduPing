using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using BuduPing.Core;
using BuduPing.Services;

namespace BuduPing.Ui;

public partial class SettingsWindow : Window
{
    private readonly App _app;
    private bool _suspendEvents = true;
    public SettingsWindow(App app)
    {
        _app = app; InitializeComponent();
        Closing += (_, e) => { if (!_app.IsExiting) { e.Cancel = true; Hide(); } };
    }
    public void RefreshUi()
    {
        _suspendEvents = true;
        CodeText.Text = PairCode.ToDisplay(_app.Config.PairCode);
        SoundCheck.IsChecked = _app.Config.SoundOn;
        AutostartCheck.IsChecked = _app.Config.Autostart;
        DiscreetCheck.IsChecked = _app.Config.DiscreetMode;
        PrivacyCheck.IsChecked = _app.Config.HideMessageText;
        MotionCheck.IsChecked = _app.Config.ReducedMotion;
        var displays = DisplayPlacement.GetDisplays();
        MonitorCombo.ItemsSource = displays;
        MonitorCombo.SelectedItem = displays.FirstOrDefault(x => x.Device == _app.Config.MonitorDevice) ?? displays.FirstOrDefault();
        CornerCombo.SelectedValue = _app.Config.Corner;
        ScaleCombo.SelectedValue = _app.Config.OverlayScale.ToString(CultureInfo.InvariantCulture);
        HotkeyStatus.Text = _app.HotkeyAvailable ? "Ctrl+Alt+Space: gửi một cái ôm đáp lại." :
            "Phím tắt đang được ứng dụng khác sử dụng. Bạn vẫn có thể đáp lại bằng nút dưới đây hoặc menu tray.";
        _suspendEvents = false;
    }
    private void CopyButton_Click(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(PairCode.ToDisplay(_app.Config.PairCode)); } catch { }
    }
    private void RegenButton_Click(object sender, RoutedEventArgs e)
    {
        if (MessageBox.Show("Điện thoại cần nhập lại mã mới. Tiếp tục?", "Đổi mã ghép cặp",
            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes) _app.RegeneratePairCode();
    }
    private void SoundCheck_Changed(object sender, RoutedEventArgs e) { if (!_suspendEvents) _app.SetSound(SoundCheck.IsChecked == true); }
    private void AutostartCheck_Changed(object sender, RoutedEventArgs e) { if (!_suspendEvents) _app.SetAutostart(AutostartCheck.IsChecked == true); }
    private void Preferences_Changed(object sender, RoutedEventArgs e)
    {
        if (_suspendEvents) return;
        _app.Config.HideMessageText = PrivacyCheck.IsChecked == true;
        _app.Config.ReducedMotion = MotionCheck.IsChecked == true;
        _app.SetDiscreet(DiscreetCheck.IsChecked == true);
        _app.ApplyPreferences();
    }
    private void Placement_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (_suspendEvents) return;
        _app.Config.MonitorDevice = (MonitorCombo.SelectedItem as DisplayPlacement.Display)?.Device ?? "";
        _app.Config.Corner = CornerCombo.SelectedValue as string ?? "BottomRight";
        if (double.TryParse(ScaleCombo.SelectedValue as string, CultureInfo.InvariantCulture, out var scale)) _app.Config.OverlayScale = scale;
        _app.ApplyPreferences();
    }
    private async void ReplyButton_Click(object sender, RoutedEventArgs e) => await _app.ReplyToLastAsync();
    private void PendingButton_Click(object sender, RoutedEventArgs e) => _app.OpenPending();
    private void TestButton_Click(object sender, RoutedEventArgs e) => _app.SendTestPing();
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Hide();
}
