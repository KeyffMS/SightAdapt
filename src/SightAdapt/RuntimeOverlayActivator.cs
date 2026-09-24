using System.ComponentModel;

namespace SightAdapt;

internal enum RuntimeActivationMode
{
    Manual,
    Automatic,
}

internal sealed class RuntimeOverlayActivator
{
    private readonly ApplicationStateController _stateController;
    private readonly IRuntimeOverlay _overlay;
    private readonly IRuntimeFeedback _feedback;

    public RuntimeOverlayActivator(
        ApplicationStateController stateController,
        IRuntimeOverlay overlay,
        IRuntimeFeedback feedback)
    {
        _stateController = stateController ??
            throw new ArgumentNullException(nameof(stateController));
        _overlay = overlay ??
            throw new ArgumentNullException(nameof(overlay));
        _feedback = feedback ??
            throw new ArgumentNullException(nameof(feedback));
    }

    public IRuntimeOverlay Overlay => _overlay;

    public void Activate(
        IReadOnlySightAdaptSettings settings,
        nint targetWindow,
        RuntimeActivationMode activationMode,
        ApplicationAssignment? assignment)
    {
        ArgumentNullException.ThrowIfNull(settings);

        try
        {
            var visualProfile =
                ProfileResolver.ResolveVisualProfile(
                    settings,
                    assignment);
            var menuVisualProfile =
                ProfileResolver.ResolveMenuVisualProfile(
                    settings,
                    assignment);
            var request = new OverlayActivationRequest(
                targetWindow,
                visualProfile,
                menuVisualProfile,
                assignment?.OverlayScope ??
                    OverlayScopePolicy.Default);

            _overlay.Activate(request);

            switch (activationMode)
            {
                case RuntimeActivationMode.Manual:
                    _stateController.SetManualActive(
                        targetWindow,
                        visualProfile.Id);
                    break;
                case RuntimeActivationMode.Automatic:
                    _stateController.SetAutomaticActive(
                        targetWindow,
                        visualProfile.Id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(activationMode));
            }
        }
        catch (Exception exception)
            when (IsExpectedOperationalFailure(exception))
        {
            ReportActivationFailure(
                exception,
                activationMode,
                targetWindow,
                expectedOperationalFailure: true);
            DisableAfterFailure(exception);

            var message =
                RuntimeMessages.OverlayCreationFailed(exception);
            _stateController.SetFault(
                message,
                activationMode == RuntimeActivationMode.Automatic
                    ? targetWindow
                    : nint.Zero);
            _feedback.ShowNotification(message);
        }
        catch (Exception exception)
        {
            ReportActivationFailure(
                exception,
                activationMode,
                targetWindow,
                expectedOperationalFailure: false);
            DisableAfterFailure(exception);
            throw;
        }
    }

    public void Disable()
    {
        _overlay.Disable();
        _stateController.SetInactive();
    }

    public void EmergencyDisable()
    {
        _overlay.Disable();
        _stateController.SetEmergency(
            "All overlays were disabled.");
    }

    public void DisableForExit()
    {
        _overlay.Disable();
        _stateController.SetInactive();
    }

    private static bool IsExpectedOperationalFailure(
        Exception exception)
    {
        return exception is Win32Exception or
            RuntimeOverlayUnavailableException;
    }

    private static void ReportActivationFailure(
        Exception exception,
        RuntimeActivationMode activationMode,
        nint targetWindow,
        bool expectedOperationalFailure)
    {
        Diagnostics.Report(
            nameof(RuntimeOverlayActivator),
            "Activate overlay",
            DiagnosticSeverity.Error,
            expectedOperationalFailure
                ? DiagnosticFailurePolicy.Recovered
                : DiagnosticFailurePolicy.Critical,
            $"Overlay activation failed in {activationMode} mode " +
            $"for target 0x{targetWindow.ToInt64():X}.",
            exception);
    }

    private void DisableAfterFailure(
        Exception primaryException)
    {
        try
        {
            _overlay.Disable();
        }
        catch (Exception cleanupException)
        {
            Diagnostics.Report(
                nameof(RuntimeOverlayActivator),
                "Clean up failed activation",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.BestEffort,
                "Overlay cleanup failed after activation failure; " +
                $"the primary {primaryException.GetType().Name} " +
                "remains authoritative.",
                cleanupException);
        }
    }
}
