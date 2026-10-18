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
