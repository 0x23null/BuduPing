namespace BuduPing.Core;

/// <summary>The 16 moods in the same order as the Android grid. Labels live in StickerService.TypeLabels.</summary>
public static class Emotions
{
    public sealed record Entry(string Type, string Emoji);

    public static readonly IReadOnlyList<Entry> All = new[]
    {
        new Entry("hello", "👋"), new Entry("miss", "🥺"), new Entry("love", "❤️"), new Entry("hug", "🤗"),
        new Entry("kiss", "😘"), new Entry("tease", "😤"), new Entry("angry", "😠"), new Entry("sad", "🥲"),
        new Entry("sorry", "🥹"), new Entry("funny", "😂"), new Entry("cheer", "💪"), new Entry("hungry", "🍜"),
        new Entry("goodnight", "🌙"), new Entry("sleepy", "🥱"), new Entry("wow", "😲"), new Entry("shy", "🙈"),
    };

    public static bool IsKnown(string type) => All.Any(e => e.Type == type);
}
