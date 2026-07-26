using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class OverlayTargetAvailabilityTests
{
    [TestMethod]
    public void ForegroundTargetIsAvailable()
    {
        var api = new FakeNativeWindowApi();
        api.SetWindow((nint)10);
        api.ForegroundWindow = (nint)10;
        var policy = new ForegroundOverlayTargetAvailability(api);

        var result = policy.Evaluate((nint)10);

        Assert.IsTrue(result.Exists);
        Assert.IsTrue(result.IsAvailable);
    }

    [TestMethod]
    public void AssociatedNativeMenuKeepsApplicationAvailable()
    {
        var api = new FakeNativeWindowApi();
        api.SetWindow(
            (nint)10,
            threadId: 100,
            processId: 200);
        api.SetWindow(
            (nint)20,
            windowClass: Win32MenuWindowPolicy.PopupMenuClassName,
            threadId: 101,
            processId: 200);
        api.ForegroundWindow = (nint)20;
        var policy = new ForegroundOverlayTargetAvailability(api);

        var result = policy.Evaluate((nint)10);

        Assert.IsTrue(result.IsAvailable);
    }

    [TestMethod]
    public void UnassociatedPopupDoesNotKeepApplicationAvailable()
    {
        var api = new FakeNativeWindowApi();
        api.SetWindow(
            (nint)10,
            threadId: 100,
            processId: 200);
        api.SetWindow(
            (nint)20,
            windowClass: Win32MenuWindowPolicy.PopupMenuClassName,
            threadId: 300,
            processId: 400);
        api.ForegroundWindow = (nint)20;
        var policy = new ForegroundOverlayTargetAvailability(api);

        Assert.IsFalse(policy.Evaluate((nint)10).IsAvailable);
    }

    [TestMethod]
    public void PopupTargetMustRemainVisibleNativeMenu()
    {
        var api = new FakeNativeWindowApi();
        api.SetWindow(
            (nint)20,
            windowClass: Win32MenuWindowPolicy.PopupMenuClassName);
        var policy = new PopupOverlayTargetAvailability(api);

        Assert.IsTrue(policy.Evaluate((nint)20).IsAvailable);

        api.SetWindow(
            (nint)20,
            visible: false,
            windowClass: Win32MenuWindowPolicy.PopupMenuClassName);
        Assert.IsFalse(policy.Evaluate((nint)20).IsAvailable);

        api.SetWindow(
            (nint)20,
            windowClass: "CustomPopup");
        Assert.IsFalse(policy.Evaluate((nint)20).IsAvailable);
    }
}
