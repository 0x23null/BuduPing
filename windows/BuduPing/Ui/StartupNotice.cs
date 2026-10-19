using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using BuduPing.Core;
using BuduPing.Services;

namespace BuduPing.Ui;

/// <summary>A short process-start acknowledgement; no GIF, sound or persistent timer.</summary>
internal sealed class StartupNotice : Window
{
    public StartupNotice(AppConfig config, bool autoDismiss = true)
    {
        Width = 340; Height = 108;
        WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize;
        AllowsTransparency = true; Background = Brushes.Transparent;
        ShowInTaskbar = false; ShowActivated = false; Topmost = true;
        var text = new StackPanel { Margin = new Thickness(18, 14, 18, 14) };
        text.Children.Add(new TextBlock { Text = "BuduPing đã chạy", FontSize = 16,
            FontWeight = FontWeights.SemiBold, Foreground = new SolidColorBrush(Color.FromRgb(64, 51, 45)) });
        text.Children.Add(new TextBlock { Text = "Nhấn đúp biểu tượng ở khay hệ thống\nđể mở cài đặt.", FontSize = 12,
            Margin = new Thickness(0, 5, 0, 0), Foreground = new SolidColorBrush(Color.FromRgb(116, 96, 87)) });
        Content = new Border { Background = new SolidColorBrush(Color.FromRgb(255, 249, 244)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(232, 217, 210)), BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14), Margin = new Thickness(6), Child = text };
        MouseLeftButtonUp += (_, _) => { (Application.Current as App)?.OpenSettings(); Close(); };
        Loaded += async (_, _) =>
        {
            DisplayPlacement.Place(this, config.MonitorDevice, config.Corner, 1);
            if (!autoDismiss) return;
            if (!config.ReducedMotion) BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(120)));
            await Task.Delay(2400);
            if (!IsVisible) return;
            if (config.ReducedMotion) { Close(); return; }
            var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
            fade.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fade);
        };
    }
}
