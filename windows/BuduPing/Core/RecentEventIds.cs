namespace BuduPing.Core;

/// <summary>A bounded deduplication window. A deferred key can be remembered again safely.</summary>
public sealed class RecentEventIds
{
    private readonly int _capacity;
    private readonly object _gate = new();
    private readonly Dictionary<string, long> _ids = new(StringComparer.Ordinal);
    private readonly Queue<(string Id, long Generation)> _order = new();
    private long _generation;
    public RecentEventIds(int capacity = 2000) => _capacity = Math.Max(1, capacity);
    public bool TryRemember(string id)
    {
        lock (_gate)
        {
            if (_ids.ContainsKey(id)) return false;
            long generation = ++_generation;
            _ids[id] = generation; _order.Enqueue((id, generation));
            while (_order.Count > _capacity)
            {
                var old = _order.Dequeue();
                if (_ids.TryGetValue(old.Id, out long current) && current == old.Generation) _ids.Remove(old.Id);
            }
            return true;
        }
    }
    public void Forget(string id) { lock (_gate) _ids.Remove(id); }
}
