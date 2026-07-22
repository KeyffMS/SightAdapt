Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

function Write-ExistingFile(
    [string]$Path,
    [string]$ExpectedMarker,
    [string]$Content) {
    if (-not (Test-Path $Path)) {
        throw "File '$Path' does not exist."
    }

    $current = Normalize-Newlines (Get-Content -Raw $Path)
    if (-not $current.Contains((Normalize-Newlines $ExpectedMarker))) {
        throw "Expected marker was not found in '$Path'."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

Write-ExistingFile `
    'src/SightAdapt/ApplicationIdentityCache.cs' `
    'private readonly Dictionary<uint, CacheEntry> _entries = [];' @'
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
'@

Replace-Exact 'src/SightAdapt/ApplicationDiscovery.cs' @'
    public static bool TryGetIdentity(
        nint window,
        out ApplicationIdentity identity)
    {
        identity = null!;

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == 0)
        {
            return false;
        }

        if (IdentityCache.TryGet(processId, out identity))
        {
            return true;
        }

        if (!NativeMethods.TryGetProcessPath(
                window,
                out var executablePath))
        {
            IdentityCache.Remove(processId);
            return false;
        }

        try
        {
            identity = FromExecutablePath(executablePath);
            IdentityCache.Set(processId, identity);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException)
        {
            IdentityCache.Remove(processId);
            Debug.WriteLine(
                $"SightAdapt could not resolve application identity: " +
                $"{exception}");
            return false;
        }
    }
'@ @'
    public static bool TryGetIdentity(
        nint window,
        out ApplicationIdentity identity)
    {
        identity = null!;

        if (!NativeMethods.TryGetProcessIdentityKey(
                window,
                out var processKey))
        {
            return false;
        }

        if (IdentityCache.TryGet(processKey, out identity))
        {
            return true;
        }

        if (!NativeMethods.TryGetProcessPath(
                processKey,
                out var executablePath))
        {
            IdentityCache.Remove(processKey);
            return false;
        }

        try
        {
            identity = FromExecutablePath(executablePath);
            IdentityCache.Set(processKey, identity);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException)
        {
            IdentityCache.Remove(processKey);
            Debug.WriteLine(
                $"SightAdapt could not resolve application identity: " +
                $"{exception}");
            return false;
        }
    }
'@

Replace-Exact 'src/SightAdapt/NativeMethods.cs' @'
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        nint process,
        uint flags,
        StringBuilder executablePath,
        ref uint pathLength);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);
'@ @'
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        nint process,
        uint flags,
        StringBuilder executablePath,
        ref uint pathLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(
        nint process,
        out NativeFileTime creationTime,
        out NativeFileTime exitTime,
        out NativeFileTime kernelTime,
        out NativeFileTime userTime);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;

        public readonly ulong ToUInt64()
        {
            return ((ulong)HighDateTime << 32) |
                LowDateTime;
        }
    }
'@

Replace-Exact 'src/SightAdapt/NativeMethods.cs' @'
    public static bool TryGetProcessPath(nint window, out string executablePath)
    {
        executablePath = string.Empty;

        GetWindowThreadProcessId(window, out var processId);
        if (processId == 0)
        {
            return false;
        }

        var process = OpenProcess(ProcessQueryLimitedInformation, false, processId);
        if (process == nint.Zero)
        {
            return false;
        }

        try
        {
            var builder = new StringBuilder(32768);
            var length = (uint)builder.Capacity;

            if (!QueryFullProcessImageName(process, 0, builder, ref length))
            {
                return false;
            }

            executablePath = builder.ToString();
            return !string.IsNullOrWhiteSpace(executablePath);
        }
        finally
        {
            CloseHandle(process);
        }
    }
'@ @'
    public static bool TryGetProcessIdentityKey(
        nint window,
        out ProcessIdentityKey key)
    {
        key = default;
        GetWindowThreadProcessId(window, out var processId);
        if (processId == 0)
        {
            return false;
        }

        var process = OpenProcess(
            ProcessQueryLimitedInformation,
            false,
            processId);
        if (process == nint.Zero)
        {
            return false;
        }

        try
        {
            return TryReadProcessIdentityKey(
                processId,
                process,
                out key);
        }
        finally
        {
            CloseHandle(process);
        }
    }

    public static bool TryGetProcessPath(
        ProcessIdentityKey expectedKey,
        out string executablePath)
    {
        executablePath = string.Empty;
        if (!expectedKey.IsValid)
        {
            return false;
        }

        var process = OpenProcess(
            ProcessQueryLimitedInformation,
            false,
            expectedKey.ProcessId);
        if (process == nint.Zero)
        {
            return false;
        }

        try
        {
            if (!TryReadProcessIdentityKey(
                    expectedKey.ProcessId,
                    process,
                    out var currentKey) ||
                currentKey != expectedKey)
            {
                return false;
            }

            var builder = new StringBuilder(32768);
            var length = (uint)builder.Capacity;
            if (!QueryFullProcessImageName(
                    process,
                    0,
                    builder,
                    ref length))
            {
                return false;
            }

            executablePath = builder.ToString();
            return !string.IsNullOrWhiteSpace(executablePath);
        }
        finally
        {
            CloseHandle(process);
        }
    }

    private static bool TryReadProcessIdentityKey(
        uint processId,
        nint process,
        out ProcessIdentityKey key)
    {
        key = default;
        if (!GetProcessTimes(
                process,
                out var creationTime,
                out _,
                out _,
                out _))
        {
            return false;
        }

        var creationTimeValue = creationTime.ToUInt64();
        if (creationTimeValue == 0)
        {
            return false;
        }

        key = new ProcessIdentityKey(
            processId,
            creationTimeValue);
        return true;
    }
'@

Replace-Exact 'tests/SightAdapt.Tests/OverlaySwitchingTests.cs' @'
    [TestMethod]
    public void IdentityCacheIsBoundedAndRetainsRecentlyUsedEntry()
    {
        var cache = new ApplicationIdentityCache(capacity: 2);
        var reader = CreateIdentity("Reader", "reader.exe");
        var writer = CreateIdentity("Writer", "writer.exe");
        var browser = CreateIdentity("Browser", "browser.exe");

        cache.Set(1, reader);
        cache.Set(2, writer);
        Assert.IsTrue(cache.TryGet(1, out var cachedReader));
        Assert.AreEqual(reader, cachedReader);

        cache.Set(3, browser);

        Assert.AreEqual(2, cache.Count);
        Assert.IsTrue(cache.TryGet(1, out _));
        Assert.IsFalse(cache.TryGet(2, out _));
        Assert.IsTrue(cache.TryGet(3, out _));
    }
'@ @'
    [TestMethod]
    public void IdentityCacheIsBoundedAndRetainsRecentlyUsedEntry()
    {
        var cache = new ApplicationIdentityCache(capacity: 2);
        var reader = CreateIdentity("Reader", "reader.exe");
        var writer = CreateIdentity("Writer", "writer.exe");
        var browser = CreateIdentity("Browser", "browser.exe");
        var readerKey = CreateProcessKey(1, 100);
        var writerKey = CreateProcessKey(2, 200);
        var browserKey = CreateProcessKey(3, 300);

        cache.Set(readerKey, reader);
        cache.Set(writerKey, writer);
        Assert.IsTrue(cache.TryGet(readerKey, out var cachedReader));
        Assert.AreEqual(reader, cachedReader);

        cache.Set(browserKey, browser);

        Assert.AreEqual(2, cache.Count);
        Assert.IsTrue(cache.TryGet(readerKey, out _));
        Assert.IsFalse(cache.TryGet(writerKey, out _));
        Assert.IsTrue(cache.TryGet(browserKey, out _));
    }

    [TestMethod]
    public void IdentityCacheRejectsReusedPidFromDifferentProcessLifetime()
    {
        var cache = new ApplicationIdentityCache(capacity: 2);
        var reader = CreateIdentity("Reader", "reader.exe");
        var browser = CreateIdentity("Browser", "browser.exe");
        var firstLifetime = CreateProcessKey(42, 1000);
        var reusedPid = CreateProcessKey(42, 2000);

        cache.Set(firstLifetime, reader);

        Assert.IsFalse(cache.TryGet(reusedPid, out _));

        cache.Set(reusedPid, browser);

        Assert.AreEqual(1, cache.Count);
        Assert.IsFalse(cache.TryGet(firstLifetime, out _));
        Assert.IsTrue(cache.TryGet(reusedPid, out var cachedBrowser));
        Assert.AreEqual(browser, cachedBrowser);
    }
'@

Replace-Exact 'tests/SightAdapt.Tests/OverlaySwitchingTests.cs' @'
    private static ApplicationIdentity CreateIdentity(
        string displayName,
        string executableName)
'@ @'
    private static ProcessIdentityKey CreateProcessKey(
        uint processId,
        ulong creationTime)
    {
        return new ProcessIdentityKey(
            processId,
            creationTime);
    }

    private static ApplicationIdentity CreateIdentity(
        string displayName,
        string executableName)
'@

Replace-Exact 'docs/ARCHITECTURE.md' `
    '(process path and bounded cache)' `
    '(process lifetime, path, and bounded cache)'
Replace-Exact 'docs/ARCHITECTURE.md' `
    '`ApplicationIdentityCache` is an optimization, not a product source of truth.' `
    '`ApplicationIdentityCache` is an optimization, not a product source of truth. Entries are keyed by both PID and process creation time so a reused PID cannot inherit another process lifetime''s identity.'
