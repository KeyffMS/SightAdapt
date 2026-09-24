using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
[DoNotParallelize]
public sealed class RuntimeOverlayActivatorTests
{
    private static readonly nint Target = (nint)0x1234;

    [TestMethod]
    public void ExpectedNativeFailureIsDiagnosedAndTransitionsToFault()
    {
        var state = new ApplicationStateController();
        var overlay = new ThrowingOverlay
        {
            ActivateException = new Win32Exception(
                5,
                "native failure"),
        };
        var feedback = new RecordingFeedback();
        var sink = new RecordingSink();
        using var scope = Diagnostics.UseSink(sink);
        var activator = new RuntimeOverlayActivator(
            state,
            overlay,
            feedback);

        activator.Activate(
            new SightAdaptSettings(),
            Target,
            RuntimeActivationMode.Automatic,
            assignment: null);

        Assert.AreEqual(
            ApplicationRunState.Fault,
            state.Current.Kind);
        Assert.IsTrue(
            state.IsAutomaticSuppressedFor(Target));
        Assert.AreEqual(1, feedback.Notifications.Count);
        Assert.AreEqual(1, overlay.DisableCount);
        var failure = sink.Events.Single(
            item => item.Operation == "Activate overlay");
        Assert.AreEqual(
            DiagnosticFailurePolicy.Recovered,
            failure.FailurePolicy);
        Assert.IsInstanceOfType<Win32Exception>(
            failure.Exception);
        StringAssert.Contains(
            failure.Message,
            "Automatic");
        StringAssert.Contains(
            failure.Message,
            "0x1234");
    }

    [TestMethod]
    public void ContractFailureIsDiagnosedCleanedAndRethrown()
    {
        var state = new ApplicationStateController();
        var expected =
            new InvalidOperationException(
                "contract failure");
        var overlay = new ThrowingOverlay
        {
            ActivateException = expected,
        };
        var feedback = new RecordingFeedback();
        var sink = new RecordingSink();
        using var scope = Diagnostics.UseSink(sink);
        var activator = new RuntimeOverlayActivator(
            state,
            overlay,
            feedback);

        var thrown =
            Assert.ThrowsException<InvalidOperationException>(
                () => activator.Activate(
                    new SightAdaptSettings(),
                    Target,
                    RuntimeActivationMode.Manual,
                    assignment: null));

        Assert.AreSame(expected, thrown);
        Assert.AreEqual(1, overlay.DisableCount);
        Assert.AreEqual(
            ApplicationRunState.Inactive,
            state.Current.Kind);
        Assert.AreEqual(0, feedback.Notifications.Count);
        var failure = sink.Events.Single(
            item => item.Operation == "Activate overlay");
        Assert.AreEqual(
            DiagnosticFailurePolicy.Critical,
            failure.FailurePolicy);
        Assert.AreSame(expected, failure.Exception);
    }

    [TestMethod]
    public void CleanupFailureDoesNotReplaceOperationalFailure()
    {
        var state = new ApplicationStateController();
        var primary = new Win32Exception(
            87,
            "primary failure");
        var cleanup =
            new InvalidOperationException(
                "cleanup failure");
        var overlay = new ThrowingOverlay
        {
            ActivateException = primary,
            DisableException = cleanup,
        };
        var feedback = new RecordingFeedback();
        var sink = new RecordingSink();
        using var scope = Diagnostics.UseSink(sink);
        var activator = new RuntimeOverlayActivator(
            state,
            overlay,
            feedback);

        activator.Activate(
            new SightAdaptSettings(),
            Target,
            RuntimeActivationMode.Automatic,
            assignment: null);

        Assert.AreEqual(
            ApplicationRunState.Fault,
            state.Current.Kind);
        StringAssert.Contains(
            state.Current.Message ?? string.Empty,
            "primary failure");
        Assert.AreEqual(1, feedback.Notifications.Count);
        Assert.AreEqual(2, sink.Events.Count);
        Assert.AreSame(
            primary,
            sink.Events.Single(
                item => item.Operation ==
                    "Activate overlay").Exception);
        Assert.AreSame(
            cleanup,
            sink.Events.Single(
                item => item.Operation ==
                    "Clean up failed activation").Exception);
    }

    [TestMethod]
    public void SuccessfulActivationSetsRequestedRuntimeState()
    {
        var state = new ApplicationStateController();
        var overlay = new ThrowingOverlay();
        var feedback = new RecordingFeedback();
        var sink = new RecordingSink();
        using var scope = Diagnostics.UseSink(sink);
        var activator = new RuntimeOverlayActivator(
            state,
            overlay,
            feedback);

        activator.Activate(
            new SightAdaptSettings(),
            Target,
            RuntimeActivationMode.Manual,
            assignment: null);

        Assert.AreEqual(
            ApplicationRunState.ManualActive,
            state.Current.Kind);
        Assert.AreEqual(Target, state.Current.TargetWindow);
        Assert.AreEqual(1, overlay.ActivationCount);
        Assert.AreEqual(0, overlay.DisableCount);
        Assert.AreEqual(0, feedback.Notifications.Count);
        Assert.AreEqual(0, sink.Events.Count);
    }

    private sealed class ThrowingOverlay :
        IRuntimeOverlay
    {
        public Exception? ActivateException { get; init; }

        public Exception? DisableException { get; init; }

        public bool IsActive { get; private set; }

        public nint TargetWindow { get; private set; }

        public int ActivationCount { get; private set; }

        public int DisableCount { get; private set; }

        public void Activate(
            OverlayActivationRequest request)
        {
            ArgumentNullException.ThrowIfNull(request);
            ActivationCount++;
            if (ActivateException is not null)
            {
                throw ActivateException;
            }

            IsActive = true;
            TargetWindow = request.TargetWindow;
        }

        public void Disable()
        {
            DisableCount++;
            if (DisableException is not null)
            {
                throw DisableException;
            }

            IsActive = false;
            TargetWindow = nint.Zero;
        }
    }

    private sealed class RecordingFeedback :
        IRuntimeFeedback
    {
        public List<string> Notifications { get; } = [];

        public List<bool> AutomaticModes { get; } = [];

        public void ShowNotification(string message)
        {
            Notifications.Add(message);
        }

        public void SynchronizeAutomaticMode(bool enabled)
        {
            AutomaticModes.Add(enabled);
        }
    }

    private sealed class RecordingSink :
        IDiagnosticSink
    {
        public List<DiagnosticEvent> Events { get; } = [];

        public void Write(
            DiagnosticEvent diagnosticEvent)
        {
            Events.Add(diagnosticEvent);
        }
    }
}
