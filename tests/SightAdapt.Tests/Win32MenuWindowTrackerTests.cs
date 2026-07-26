using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class Win32MenuWindowTrackerTests
{
    [TestMethod]
    public void TrackerCoordinatesSignalAndEnumeratorPorts()
    {
        var signal = new FakeMenuRefreshSignalSource();
        var enumerator = new FakeMenuWindowEnumerator
        {
            Windows = [(nint)101],
        };
        var windows = new FakeNativeWindowApi();
        windows.SetWindow(
            (nint)10,
            threadId: 11,
            processId: 22);
        windows.ForegroundWindow = (nint)10;
        using var tracker = new Win32MenuWindowTracker(
            signal,
            enumerator,
            windows,
            intervalMilliseconds: 1000);
        var publications = new List<IReadOnlyList<nint>>();
        tracker.Changed += (_, eventArgs) =>
            publications.Add(eventArgs.Windows);

        tracker.Start((nint)10);

        Assert.AreEqual(1, signal.StartCount);
        Assert.AreEqual(1, enumerator.CallCount);
        Assert.AreEqual(1, publications.Count);
        CollectionAssert.AreEqual(
            new[] { (nint)101 },
            publications[0].ToArray());

        signal.Raise();
        Assert.AreEqual(2, enumerator.CallCount);
        Assert.AreEqual(
            1,
            publications.Count,
            "Equivalent window sets must not be republished.");

        enumerator.Windows = [(nint)101, (nint)102];
        signal.Raise();
        Assert.AreEqual(2, publications.Count);
    }

    [TestMethod]
    public void LeavingTargetSessionPublishesEmptySnapshot()
    {
        var signal = new FakeMenuRefreshSignalSource();
        var enumerator = new FakeMenuWindowEnumerator
        {
            Windows = [(nint)101],
        };
        var windows = new FakeNativeWindowApi();
        windows.SetWindow(
            (nint)10,
            threadId: 11,
            processId: 22);
        windows.SetWindow(
            (nint)30,
            threadId: 33,
            processId: 44);
        windows.ForegroundWindow = (nint)10;
        using var tracker = new Win32MenuWindowTracker(
            signal,
            enumerator,
            windows,
            intervalMilliseconds: 1000);
        var publications = new List<IReadOnlyList<nint>>();
        tracker.Changed += (_, eventArgs) =>
            publications.Add(eventArgs.Windows);
        tracker.Start((nint)10);

        windows.ForegroundWindow = (nint)30;
        signal.Raise();

        Assert.AreEqual(2, publications.Count);
        Assert.AreEqual(0, publications[1].Count);
    }

    [TestMethod]
    public void DisposeStopsAndDisposesSignalSource()
    {
        var signal = new FakeMenuRefreshSignalSource();
        var windows = new FakeNativeWindowApi();
        windows.SetWindow((nint)10);
        windows.ForegroundWindow = (nint)10;
        var tracker = new Win32MenuWindowTracker(
            signal,
            new FakeMenuWindowEnumerator(),
            windows,
            intervalMilliseconds: 1000);
        tracker.Start((nint)10);

        tracker.Dispose();
        tracker.Dispose();

        Assert.AreEqual(1, signal.DisposeCount);
        Assert.AreEqual(1, signal.StopCount);
    }
}
