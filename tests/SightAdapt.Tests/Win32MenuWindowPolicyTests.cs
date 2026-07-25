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
}
