namespace BuduPing.Core;

/// <summary>Bounded burst queue. Messages are never evicted by ambient taps.</summary>
public sealed class PingBacklog
{
    private readonly LinkedList<PingEvent> _items = new();
    public const int Capacity = 64;
    public int Count => _items.Count;

    public bool TryEnqueue(PingEvent ping, out PingEvent? replaced)
    {
        replaced = null;
        if (_items.Any(p => p.Id == ping.Id)) return true;
        bool ambient = string.IsNullOrWhiteSpace(ping.Text);
        var ambientNodes = new List<LinkedListNode<PingEvent>>();
        for (var n = _items.First; n != null; n = n.Next)
            if (string.IsNullOrWhiteSpace(n.Value.Text)) ambientNodes.Add(n);
        // Keep only two waiting ambient reactions; explicit messages use the full queue.
        if ((ambient && ambientNodes.Count >= 2) || _items.Count >= Capacity)
        {
            if (ambientNodes.Count == 0) return false;
            var victim = ambientNodes[0];
            replaced = victim.Value;
            _items.Remove(victim);
        }
        _items.AddLast(ping);
        return true;
    }

    public PingEvent? Take()
    {
        if (_items.First == null) return null;
        var ping = _items.First.Value;
        _items.RemoveFirst();
        return ping;
    }

    public void Clear() => _items.Clear();
}

public static class PingPolicy
{
    public static bool CanCombine(PingEvent current, PingEvent next, double secondsSinceTap)
        => string.Equals(current.Type, next.Type, StringComparison.OrdinalIgnoreCase)
           && string.IsNullOrWhiteSpace(current.Text) && string.IsNullOrWhiteSpace(next.Text)
           && current.IsLocal == next.IsLocal
           && string.Equals(current.Character, next.Character, StringComparison.Ordinal)
           && !current.HasFixedVariant && !next.HasFixedVariant && secondsSinceTap >= 0 && secondsSinceTap <= 2.0;

    // Old messages are kept for a day; ambient taps expire after ten minutes.
    public static bool IsExpired(PingEvent ping, long nowMs)
        => ping.Ts > 0 && nowMs - ping.Ts >
           (string.IsNullOrWhiteSpace(ping.Text) ? 600_000L : 86_400_000L);
}
