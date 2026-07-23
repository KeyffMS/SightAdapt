namespace SightAdapt;

internal readonly record struct ProcessIdentityKey(
    uint ProcessId,
    ulong CreationTime)
{
    public bool IsValid =>
        ProcessId != 0 && CreationTime != 0;
}

internal sealed class ApplicationIdentityCache
{
    internal const int DefaultCapacity = 64;

    private readonly int _capacity;
    private readonly Dictionary<ProcessIdentityKey, CacheEntry> _entries = [];
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
        ProcessIdentityKey key,
        out ApplicationIdentity identity)
    {
        if (!key.IsValid)
        {
            identity = null!;
            return false;
        }

        lock (_sync)
        {
            if (!_entries.TryGetValue(key, out var entry))
            {
                identity = null!;
                return false;
            }

            identity = entry.Identity;
            _entries[key] = entry with
            {
                AccessSequence = NextAccessSequence(),
            };
            return true;
        }
    }

    public void Set(
        ProcessIdentityKey key,
        ApplicationIdentity identity)
    {
        if (!key.IsValid)
        {
            throw new ArgumentOutOfRangeException(nameof(key));
        }

        ArgumentNullException.ThrowIfNull(identity);

        lock (_sync)
        {
            RemoveOtherLifetimes(key);

            if (!_entries.ContainsKey(key) &&
                _entries.Count >= _capacity)
            {
                RemoveLeastRecentlyUsed();
            }

            _entries[key] = new CacheEntry(
                identity,
                NextAccessSequence());
        }
    }

    public void Remove(ProcessIdentityKey key)
    {
        lock (_sync)
        {
            _entries.Remove(key);
        }
    }

    private long NextAccessSequence()
    {
        return ++_accessSequence;
    }

    private void RemoveOtherLifetimes(
        ProcessIdentityKey key)
    {
        var staleKeys = _entries.Keys
            .Where(candidate =>
                candidate.ProcessId == key.ProcessId &&
                candidate != key)
            .ToArray();

        foreach (var staleKey in staleKeys)
        {
            _entries.Remove(staleKey);
        }
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