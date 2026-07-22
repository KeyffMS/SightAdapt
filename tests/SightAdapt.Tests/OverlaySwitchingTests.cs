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
