namespace SightAdapt.Demo;

internal sealed class ApplicationIdentityCache
{
    internal const int DefaultCapacity = 64;

    private readonly int _capacity;
    private readonly Dictionary<uint, CacheEntry> _entries = [];
    private readonly object _sync = new();
    private long _accessSequence;

    public ApplicationIdentityCache(int capacity = DefaultCapacity)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        _capacity = capacity;
    }

    public int Count
    {
        get
        {
            lock (_sync)
            {
                return _entries.Count;
            }
        }
    }

    public bool TryGet(
        uint processId,
        out ApplicationIdentity identity)
    {
        if (processId == 0)
        {
            identity = null!;
            return false;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(processId, out var entry))
            {
                identity = null!;
                return false;
            }

            identity = entry.Identity;
            _entries[processId] = entry with
            {
                AccessSequence = NextAccessSequence(),
            };
            return true;
        }
    }

    public void Set(
        uint processId,
        ApplicationIdentity identity)
    {
        if (processId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }

        ArgumentNullException.ThrowIfNull(identity);

        lock (_sync)
        {
            if (!_entries.ContainsKey(processId) &&
                _entries.Count >= _capacity)
            {
                RemoveLeastRecentlyUsed();
            }

            _entries[processId] = new CacheEntry(
                identity,
                NextAccessSequence());
        }
    }

    public void Remove(uint processId)
    {
        lock (_sync)
        {
            _entries.Remove(processId);
        }
    }

    private long NextAccessSequence()
    {
        return ++_accessSequence;
    }

    private void RemoveLeastRecentlyUsed()
    {
        var leastRecentlyUsed = _entries
            .MinBy(pair => pair.Value.AccessSequence);
        _entries.Remove(leastRecentlyUsed.Key);
    }

    private sealed record CacheEntry(
        ApplicationIdentity Identity,
        long AccessSequence);
}
