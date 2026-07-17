using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class ApplicationStateControllerTests
{
    [TestMethod]
    public void StartsInactive()
    {
        var controller = new ApplicationStateController();

        Assert.AreEqual(ApplicationRunState.Inactive, controller.Current.Kind);
        Assert.IsFalse(controller.Current.HasActiveOverlay);
    }

    [TestMethod]
    public void ManualAndAutomaticStatesRequireTargets()
    {
        var controller = new ApplicationStateController();

        Assert.ThrowsException<ArgumentException>(
            () => controller.SetManualActive(nint.Zero));
        Assert.ThrowsException<ArgumentException>(
            () => controller.SetAutomaticActive(nint.Zero));
    }

    [TestMethod]
    public void RaisesOneEventForARealTransition()
    {
        var controller = new ApplicationStateController();
        var events = new List<ApplicationStateChangedEventArgs>();
        controller.Changed += (_, eventArgs) => events.Add(eventArgs);

        controller.SetManualActive((nint)42);
        controller.SetManualActive((nint)42);
        controller.SetInactive();

        Assert.AreEqual(2, events.Count);
        Assert.AreEqual(ApplicationRunState.Inactive, events[0].Previous.Kind);
        Assert.AreEqual(ApplicationRunState.ManualActive, events[0].Current.Kind);
        Assert.AreEqual((nint)42, events[0].Current.TargetWindow);
        Assert.AreEqual(ApplicationRunState.Inactive, events[1].Current.Kind);
    }

    [TestMethod]
    public void EmergencyStateNormalizesEmptyMessage()
    {
        var controller = new ApplicationStateController();

        controller.SetEmergency("  ");

        Assert.AreEqual(ApplicationRunState.Emergency, controller.Current.Kind);
        Assert.AreEqual("All overlays stopped.", controller.Current.Message);
        Assert.IsFalse(controller.Current.HasActiveOverlay);
    }
}
