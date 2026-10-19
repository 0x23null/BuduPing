namespace BuduPing.Core;

/// <summary>Small, bounded feedback. No timers, audio queues or per-tap decoders.</summary>
public static class PresentationPolicy
{
    public const double ArtworkScale = .45;
    public const int MaxParticles = 12;
    public const int AudioGapMs = 110;
    public const int VisualGapMs = 110;
    public const int VariantGapMs = 800;
    public static double NormalizeSize(double scale)
        => !double.IsFinite(scale) ? 1 : scale < .8 ? .6 : scale > 1.15 ? 1.3 : 1;
    public static double ComboScale(int count) => 1 + .025 * Math.Clamp(count - 1, 0, 8);
    public static bool IsAccent(int count) => count is 3 or 5 || count > 5 && count % 5 == 0;
    public static bool ShouldPeek(PingEvent ping, bool discreet, bool hideText)
        => discreet || hideText && !string.IsNullOrWhiteSpace(ping.Text);
}
