using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class RuntimeTimingPolicyTests
{
    [TestMethod]
    public void DefaultsProvideOneRuntimeTimingSource()
    {
        var timing = RuntimeTimingPolicy.Default;

        Assert.AreEqual(33, timing.OverlayRefreshMilliseconds);
        Assert.AreEqual(75, timing.ForegroundPollMilliseconds);
        Assert.AreEqual(75, timing.MenuPollMilliseconds);
        Assert.AreEqual(125, timing.ForegroundTransitionGraceMilliseconds);
        Assert.AreEqual(5000, timing.FaultRecoveryMilliseconds);
        Assert.IsTrue(
            timing.ForegroundTransitionGraceMilliseconds >
            timing.ForegroundPollMilliseconds);
        Assert.AreEqual(
            timing.ForegroundPollMilliseconds,
            ForegroundWindowTracker.DefaultIntervalMilliseconds);
        Assert.AreEqual(
            timing.MenuPollMilliseconds,
            Win32MenuWindowTracker.DefaultIntervalMilliseconds);
        Assert.AreEqual(
            timing.ForegroundTransitionGraceMilliseconds,
            MagnifierOverlay.ForegroundTransitionGraceMilliseconds);
    }

    [TestMethod]
    public void NonPositiveTimingIsRejected()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new RuntimeTimingPolicy(
                0,
                75,
                75,
                125,
                5000));
    }

    [TestMethod]
    public void TransitionGraceMustExceedForegroundPoll()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new RuntimeTimingPolicy(
                33,
                75,
                75,
                75,
                5000));
    }
}
