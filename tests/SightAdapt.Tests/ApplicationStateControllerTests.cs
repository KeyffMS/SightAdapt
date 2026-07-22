using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ApplicationStateControllerTests
{
    [TestMethod]
    public void StartsInactive()
    {
        var controller =
            new ApplicationStateController();

        Assert.AreEqual(
            ApplicationRunState.Inactive,
            controller.Current.Kind);
        Assert.IsFalse(
            controller.Current.HasActiveOverlay);
        Assert.IsTrue(
            controller.AllowsAutomaticActivation);
    }

    [TestMethod]
    public void ActiveStatesRequireTargetAndProfile()
    {
        var controller =
            new ApplicationStateController();

        Assert.ThrowsException<ArgumentException>(
            () => controller.SetManualActive(
                nint.Zero,
                "profile"));
        Assert.ThrowsException<ArgumentException>(
            () => controller.SetManualActive(
                (nint)42,
                " "));
        Assert.ThrowsException<ArgumentException>(
            () => controller.SetAutomaticActive(
                nint.Zero,
                "profile"));
    }

    [TestMethod]
    public void ProfileChangeOnSameTargetIsARealTransition()
    {
        var controller =
            new ApplicationStateController();
        var events =
            new List<ApplicationStateChangedEventArgs>();
        controller.Changed +=
            (_, eventArgs) =>
                events.Add(eventArgs);

        controller.SetManualActive(
            (nint)42,
            "first");
        controller.SetManualActive(
            (nint)42,
            "second");

        Assert.AreEqual(2, events.Count);
        Assert.AreEqual(
            "first",
            events[0].Current.VisualProfileId);
        Assert.AreEqual(
            "second",
            events[1].Current.VisualProfileId);
    }

    [TestMethod]
    public void IdenticalStateDoesNotRaiseDuplicateEvent()
    {
        var controller =
            new ApplicationStateController();
        var eventCount = 0;
        controller.Changed +=
            (_, _) => eventCount++;

        controller.SetAutomaticActive(
            (nint)42,
            "profile");
        controller.SetAutomaticActive(
            (nint)42,
            "profile");

        Assert.AreEqual(1, eventCount);
    }

    [TestMethod]
    public void FaultAndEmergencyBlockAutomaticActivation()
    {
        var controller =
            new ApplicationStateController();

        controller.SetFault("Renderer failed");
        Assert.IsFalse(
            controller.AllowsAutomaticActivation);
        Assert.ThrowsException<InvalidOperationException>(
            () => controller.SetAutomaticActive(
                (nint)42,
                "profile"));

        controller.SetEmergency("Stopped");
        Assert.IsFalse(
            controller.AllowsAutomaticActivation);
        Assert.ThrowsException<InvalidOperationException>(
            () => controller.SetAutomaticActive(
                (nint)42,
                "profile"));
    }

    [TestMethod]
    public void ManualActivationIsExplicitEmergencyOverride()
    {
        var controller =
            new ApplicationStateController();
        controller.SetEmergency("Stopped");

        controller.SetManualActive(
            (nint)42,
            "profile");

        Assert.AreEqual(
            ApplicationRunState.ManualActive,
            controller.Current.Kind);
        Assert.AreEqual(
            (nint)42,
            controller.Current.TargetWindow);
    }

    [TestMethod]
    public void SuppressionClearsAfterForegroundChanges()
    {
        var controller =
            new ApplicationStateController();
        controller.SuppressAutomaticFor(
            (nint)42);

        controller.ObserveForeground(
            (nint)42);
        Assert.IsTrue(
            controller.IsAutomaticSuppressedFor(
                (nint)42));

        controller.ObserveForeground(
            (nint)84);
        Assert.IsFalse(
            controller.IsAutomaticSuppressedFor(
                (nint)42));
    }

    [TestMethod]
    public void EmptyMessagesAreNormalizedByStateKind()
    {
        var controller =
            new ApplicationStateController();

        controller.SetFault(" ");
        Assert.AreEqual(
            "The visual correction could not be applied.",
            controller.Current.Message);

        controller.SetEmergency(" ");
        Assert.AreEqual(
            "All overlays stopped.",
            controller.Current.Message);
    }
}
