using BuduPing.Core;

internal static class Program
{
    private static int _checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
        _checks++;
    }
    private static PingEvent Ping(string id, string text = "") => new() { Id = id, Type = "hug", Text = text };
    public static void Main()
    {
        var queue = new PingBacklog();
        Check(queue.TryEnqueue(Ping("a"), out _), "First ambient tap accepted");
        queue.TryEnqueue(Ping("message", "Do not lose me"), out _);
        queue.TryEnqueue(Ping("b"), out _);
        queue.TryEnqueue(Ping("c"), out var replaced);
        Check(replaced?.Id == "a", "Old ambient tap is the only evicted item");
        Check(queue.Take()?.Id == "message", "Text remains ahead of later ambient taps");
        Check(queue.Take()?.Id == "b" && queue.Take()?.Id == "c", "Order remains stable");
        Check(queue.Take() == null, "Empty queue returns null");
        for (int i = 0; i < PingBacklog.Capacity; i++) Check(queue.TryEnqueue(Ping($"m{i}", "message"), out _), "All messages fit");
        Check(!queue.TryEnqueue(Ping("overflow", "keep on server"), out _), "Full message queue defers without eviction");
        Check(!queue.TryEnqueue(Ping("tap"), out _), "Ambient tap cannot evict text when full");
        Check(queue.Count == PingBacklog.Capacity, "Queue is bounded");
        queue.TryEnqueue(Ping("m0", "message"), out _);
        Check(queue.Count == PingBacklog.Capacity, "Duplicate keys are idempotent");
        for (int i = 0; i < PingBacklog.Capacity; i++) Check(queue.Take()?.Id == $"m{i}", "Every accepted message survives in order");
        Check(PingPolicy.CanCombine(Ping("a"), Ping("b"), .1), "Ambient taps combine");
        Check(!PingPolicy.CanCombine(Ping("a", "first"), Ping("b"), .1), "Active message never becomes a combo");
        Check(!PingPolicy.CanCombine(Ping("a"), Ping("b", "new text"), .1), "New text never disappears into a combo");
        Check(!PingPolicy.CanCombine(Ping("a"), Ping("b") with { Variant = "hug-02" }, .1), "Explicit variant is respected");
        Check(!PingPolicy.CanCombine(Ping("a"), Ping("b"), 3), "Expired combo window starts a new display");
        var automatic = Ping("auto") with { Variant = "love-03", Character = "bubu", VariantLocked = false };
        Check(PingPolicy.CanCombine(automatic, automatic with { Id = "next" }, .1), "Auto variants combine after character filtering");
        Check(!PingPolicy.CanCombine(automatic, automatic with { Character = "dudu" }, .1), "Different characters never combine");
        Check(!PingPolicy.CanCombine(automatic, automatic with { VariantLocked = true }, .1), "Manual next choice remains separate");
        Check(!PingPolicy.CanCombine(automatic with { VariantLocked = true }, automatic, .1), "Manual current choice remains separate");
        Check(!PingPolicy.CanCombine(automatic, automatic with { IsLocal = true }, .1), "Tests never consume remote combo messages");
        Check(PresentationPolicy.ShouldPeek(automatic with { IsLocal = true }, true, false), "Local test respects discreet mode");
        Check(PresentationPolicy.ShouldPeek(automatic with { Text = "private" }, false, true), "Private text peeks");
        Check(!PresentationPolicy.ShouldPeek(automatic, false, true), "Plain reaction can show with hidden text enabled");
        var seen = new RecentEventIds(3);
        Check(seen.TryRemember("x"), "Fresh event is remembered");
        Check(!seen.TryRemember("x"), "Duplicate event is ignored");
        seen.Forget("x");
        Check(seen.TryRemember("x"), "Deferred event can be retried");
        seen.TryRemember("y"); seen.TryRemember("z");
        Check(!seen.TryRemember("x"), "Evicting a stale generation does not forget a new generation");
        seen.TryRemember("w");
        Check(seen.TryRemember("x"), "Oldest actual generation expires from the bounded window");
        long now = 1_000_000_000;
        Check(PingPolicy.IsExpired(Ping("a") with { Ts = now - 600_001 }, now), "Old ambient tap expires");
        Check(!PingPolicy.IsExpired(Ping("a", "text") with { Ts = now - 600_001 }, now), "Text has a longer TTL");
        Check(PingPolicy.IsExpired(Ping("a", "text") with { Ts = now - 86_400_001 }, now), "Day-old text expires");
        for (int i = 0; i < 200; i++)
        {
            string code = PairCode.Generate();
            Check(PairCode.IsValid(code), "Generated pair codes pass their checksum");
            Check(PairCode.ToPairId(code).Length == 8, "Pair IDs have no separators");
        }
        Console.WriteLine($"PASS: {_checks} queue, delivery-policy and pair-code assertions.");
    }
}
