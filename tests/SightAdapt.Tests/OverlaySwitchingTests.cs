using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class OverlaySwitchingTests
{
    [TestMethod]
    public void ForegroundPollingUsesResponsiveDefault()
    {
        Assert.AreEqual(
            75,
            ForegroundWindowTracker.DefaultIntervalMilliseconds);
        Assert.IsTrue(
            MagnifierOverlay.ForegroundTransitionGraceMilliseconds >
            ForegroundWindowTracker.DefaultIntervalMilliseconds);
    }

    [TestMethod]
    public void ForegroundTrackerPublishesOnlyRealHandleChanges()
    {
        using var tracker = new ForegroundWindowTracker();

        Assert.IsTrue(tracker.ShouldPublish((nint)100));
        Assert.IsFalse(tracker.ShouldPublish((nint)100));
        Assert.IsTrue(tracker.ShouldPublish((nint)200));
        Assert.IsFalse(tracker.ShouldPublish((nint)200));
    }

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

    [TestMethod]
    public void OverlayControllerRetargetsExistingResource()
    {
        var controller = ReadSource("OverlayController.cs");
        var overlay = ReadSource("MagnifierOverlay.cs");

        StringAssert.Contains(controller, "if (IsActive)");
        StringAssert.Contains(controller, "_overlay!.Retarget(");
        Assert.IsFalse(
            controller.Contains(
                "if (IsActive && TargetWindow == targetWindow)",
                StringComparison.Ordinal));
        StringAssert.Contains(overlay, "public void Retarget(");
        StringAssert.Contains(overlay, "IsWithinTransitionGrace()");
        StringAssert.Contains(overlay, "TargetHandle { get; private set; }");
    }

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
    {
        return new ApplicationIdentity(
            displayName,
            executableName,
            $@"C:\Apps\{executableName}");
    }

    private static string ReadSource(string fileName)
    {
        return File.ReadAllText(
            Path.Combine(
                SourceDirectory,
                fileName));
    }

    private static string SourceDirectory =>
        Path.Combine(
            RepositoryRoot,
            "src",
            "SightAdapt");

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(
                        directory.FullName,
                        "src",
                        "SightAdapt")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "The SightAdapt repository root could not be located.");
        }
    }
}
