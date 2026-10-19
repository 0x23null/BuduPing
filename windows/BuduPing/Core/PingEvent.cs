namespace BuduPing.Core;

public sealed record PingEvent
{
    // The Firebase push key, never the UUID in the body.
    public string Id { get; init; } = Guid.NewGuid().ToString("N");
    public string Type { get; init; } = "love";
    public string Text { get; init; } = "";
    public string From { get; init; } = "";
    public string Variant { get; init; } = "";
    public string Character { get; init; } = "";
    public bool? VariantLocked { get; init; }
    public bool HasFixedVariant => VariantLocked ?? !string.IsNullOrEmpty(Variant);
    public long Ts { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public bool IsLocal { get; init; }
}
