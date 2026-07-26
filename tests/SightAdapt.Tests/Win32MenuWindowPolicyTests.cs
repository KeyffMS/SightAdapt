using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class Win32MenuWindowPolicyTests
{
    [TestMethod]
    public void NativePopupMenuClassIsExact()
    {
        Assert.IsTrue(
            Win32MenuWindowPolicy
                .IsPopupMenuClass("#32768"));
        Assert.IsFalse(
            Win32MenuWindowPolicy
                .IsPopupMenuClass("#32767"));
        Assert.IsFalse(
            Win32MenuWindowPolicy
                .IsPopupMenuClass(null));
    }

    [TestMethod]
    public void SameProcessOrGuiThreadAssociatesMenu()
    {
        Assert.IsTrue(
            Win32MenuWindowPolicy
                .IsAssociatedWithTarget(
                    10,
                    20,
                    11,
                    20));
        Assert.IsTrue(
            Win32MenuWindowPolicy
                .IsAssociatedWithTarget(
                    10,
                    20,
                    10,
                    21));
        Assert.IsFalse(
            Win32MenuWindowPolicy
                .IsAssociatedWithTarget(
                    10,
                    20,
                    11,
                    21));
    }

    [TestMethod]
    public void MissingAssociationIdentifiersAreRejected()
    {
        Assert.IsFalse(
            Win32MenuWindowPolicy
                .IsAssociatedWithTarget(
                    0,
                    20,
                    10,
                    20));
        Assert.IsFalse(
            Win32MenuWindowPolicy
                .IsAssociatedWithTarget(
                    10,
                    20,
                    0,
                    20));
    }

    [TestMethod]
    public void WindowSetComparisonIgnoresEnumerationOrder()
    {
        Assert.IsTrue(
            Win32MenuWindowTracker.HaveSameWindowSet(
                [(nint)10, (nint)20],
                [(nint)20, (nint)10]));
        Assert.IsFalse(
            Win32MenuWindowTracker.HaveSameWindowSet(
                [(nint)10],
                [(nint)10, (nint)20]));
    }

    [TestMethod]
    public void CandidatePolicyAcceptsOnlyVisibleAssociatedNativeMenu()
    {
        var candidate = new MenuWindowCandidate(
            (nint)20,
            Exists: true,
            Visible: true,
            Minimized: false,
            WindowClass: Win32MenuWindowPolicy.PopupMenuClassName,
            ThreadId: 11,
            ProcessId: 20,
            Bounds: new Rect
            {
                Left = 1,
                Top = 2,
                Right = 101,
                Bottom = 202,
            });

        Assert.IsTrue(
            Win32MenuWindowPolicy.IsCandidate(
                (nint)10,
                targetThreadId: 10,
                targetProcessId: 20,
                candidate));
        Assert.IsFalse(
            Win32MenuWindowPolicy.IsCandidate(
                (nint)20,
                targetThreadId: 10,
                targetProcessId: 20,
                candidate));
        Assert.IsFalse(
            Win32MenuWindowPolicy.IsCandidate(
                (nint)10,
                targetThreadId: 30,
                targetProcessId: 40,
                candidate));
    }

    [TestMethod]
    public void SnapshotPublisherSuppressesEquivalentSetsAndCanReset()
    {
        var publisher = new MenuWindowSnapshotPublisher();

        Assert.IsTrue(publisher.TryUpdate(
            [(nint)10, (nint)20],
            out var first));
        CollectionAssert.AreEquivalent(
            new[] { (nint)10, (nint)20 },
            first);
        Assert.IsFalse(publisher.TryUpdate(
            [(nint)20, (nint)10],
            out _));

        publisher.Reset();

        Assert.IsTrue(publisher.TryUpdate(
            [(nint)20, (nint)10],
            out _));
    }

}
