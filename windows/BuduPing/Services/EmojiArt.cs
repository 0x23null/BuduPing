using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace BuduPing.Services;

/// <summary>
/// WPF's text stack renders Segoe UI Emoji as BLACK monochrome glyphs (no
/// COLR color-font support) — the owner kept reporting "emoji đen thui".
/// Fix: bundle Twemoji PNGs (CC-BY 4.0, Twitter) and swap every emoji
/// grapheme in the bubble for an inline image, chat-app style. Emoji we do
/// not bundle fall back to plain text.
/// </summary>
public static class EmojiArt
{
    private static readonly Dictionary<string, BitmapImage?> Cache = new();

    private static BitmapImage? Load(string key)
    {
        if (Cache.TryGetValue(key, out var hit)) return hit;
        BitmapImage? img = null;
        var stream = typeof(EmojiArt).Assembly.GetManifestResourceStream($"BuduPing.Assets.Emoji.{key}.png");
        if (stream != null)
        {
            using (stream)
            {
                img = new BitmapImage();
                img.BeginInit();
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.StreamSource = stream;
                img.EndInit();
                img.Freeze();
            }
        }
        Cache[key] = img;
        return img;
    }

    /// <summary>Key = codepoints (hex, FE0F stripped) joined with '-', the Twemoji file naming.</summary>
    private static string KeyFor(string grapheme)
    {
        var parts = new List<string>();
        foreach (var rune in grapheme.EnumerateRunes())
        {
            if (rune.Value == 0xFE0F) continue;
            parts.Add(rune.Value.ToString("x"));
        }
        return string.Join("-", parts);
    }

    private static bool LooksEmoji(string grapheme)
    {
        foreach (var rune in grapheme.EnumerateRunes())
        {
            int v = rune.Value;
            if (v >= 0x1F000 || (v >= 0x2600 && v <= 0x2BFF) || v == 0x2764 || v == 0x2728 || v == 0x2B50) return true;
        }
        return false;
    }

    /// <summary>Replaces a TextBlock's content with text runs + inline emoji images.</summary>
    public static void ApplyTo(TextBlock target, string text, double emojiSize = 19)
    {
        target.Inlines.Clear();
        if (string.IsNullOrEmpty(text)) return;
        var pending = new System.Text.StringBuilder();
        void FlushText()
        {
            if (pending.Length > 0) { target.Inlines.Add(new Run(pending.ToString())); pending.Clear(); }
        }
        var en = StringInfo.GetTextElementEnumerator(text);
        while (en.MoveNext())
        {
            string element = en.GetTextElement();
            var img = LooksEmoji(element) ? Load(KeyFor(element)) : null;
            if (img != null)
            {
                FlushText();
                var image = new Image
                {
                    Source = img,
                    Width = emojiSize,
                    Height = emojiSize,
                    Stretch = Stretch.Uniform,
                    SnapsToDevicePixels = true,
                };
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.HighQuality);
                target.Inlines.Add(new InlineUIContainer(image) { BaselineAlignment = BaselineAlignment.Center });
            }
            else pending.Append(element);
        }
        FlushText();
    }
}
