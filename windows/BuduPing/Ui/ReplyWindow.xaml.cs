using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BuduPing.Core;
using BuduPing.Services;

namespace BuduPing.Ui;

/// <summary>
/// Compact picker opened by Ctrl+Alt+Space or the tray: one click sends a mood (plus optional text).
/// Enter sends the focused tile, so hotkey → Enter still sends a hug. Closes on Esc or focus loss.
/// </summary>
public partial class ReplyWindow : Window
{
    private readonly App _app;
    private readonly StickerService _stickers;
    private bool _sending;
    internal string DefaultType { get; private set; } = "hug";

    public ReplyWindow(App app, StickerService stickers)
    {
        _app = app; _stickers = stickers;
        InitializeComponent();
        Button? first = null;
        foreach (var mood in Emotions.All)
        {
            var tile = new Button
            {
                Style = (Style)FindResource("Tile"), Tag = mood.Type,
                IsEnabled = _stickers.HasForCharacter(mood.Type, _app.Config.MyCharacter),
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock { Text = mood.Emoji, FontSize = 22, TextAlignment = TextAlignment.Center },
                        new TextBlock { Text = StickerService.LabelFor(mood.Type), FontSize = 11, TextAlignment = TextAlignment.Center,
                            Margin = new Thickness(0, 2, 0, 0) },
                    },
                },
            };
            AutomationProperties.SetName(tile, "Gửi " + StickerService.LabelFor(mood.Type));
            tile.Click += async (_, _) => await SendAsync(mood.Type);
            tile.GotKeyboardFocus += (_, _) => { DefaultType = mood.Type; UpdateHint(); };
            TileGrid.Children.Add(tile);
            if (mood.Type == "hug" && tile.IsEnabled) first = tile;
        }
        first ??= TileGrid.Children.OfType<Button>().FirstOrDefault(b => b.IsEnabled);
        if (first != null) DefaultType = (string)first.Tag;
        UpdateHint();
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape) { e.Handled = true; Close(); }
            else if (e.Key == Key.Enter && ReferenceEquals(Keyboard.FocusedElement, MessageBox)) { e.Handled = true; _ = SendAsync(DefaultType); }
        };
        Deactivated += (_, _) => { if (!_sending) Close(); };
        Loaded += (_, _) =>
        {
            DisplayPlacement.Place(this, _app.Config.MonitorDevice, _app.Config.Corner, 1);
            Activate();
            (first as IInputElement)?.Focus();
        };
    }

    private void MessageBox_TextChanged(object sender, TextChangedEventArgs e)
        => Placeholder.Visibility = MessageBox.Text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

    private void UpdateHint()
        => Hint.Text = _sending ? "Đang gửi…" : $"Bấm một cảm xúc để gửi · Enter gửi {StickerService.LabelFor(DefaultType)}";

    private async Task SendAsync(string type)
    {
        if (_sending || !Emotions.IsKnown(type)) return;
        _sending = true; UpdateHint();
        foreach (var child in TileGrid.Children.OfType<Button>()) child.IsHitTestVisible = false;
        bool sent = await _app.SendReplyAsync(type, MessageBox.Text);
        _sending = false;
        if (sent) { Close(); return; }
        foreach (var child in TileGrid.Children.OfType<Button>()) child.IsHitTestVisible = true;
        Hint.Text = "Chưa gửi được. Thử lại khi có mạng nhé.";
        Hint.Foreground = new SolidColorBrush(Color.FromRgb(169, 63, 101));
    }
}
