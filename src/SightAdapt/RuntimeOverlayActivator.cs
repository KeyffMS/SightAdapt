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
    private readonly IRuntimeEnvironment _environment;

    public RuntimeOverlayActivator(
        ApplicationStateController stateController,
        IRuntimeOverlay overlay,
        IRuntimeEnvironment environment)
    {
        _stateController = stateController ??
            throw new ArgumentNullException(nameof(stateController));
        _overlay = overlay ??
            throw new ArgumentNullException(nameof(overlay));
        _environment = environment ??
            throw new ArgumentNullException(nameof(environment));
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
        {
            _overlay.Disable();
            var message =
                RuntimeMessages.OverlayCreationFailed(exception);
            _stateController.SetFault(
                message,
                activationMode == RuntimeActivationMode.Automatic
                    ? targetWindow
                    : nint.Zero);
            _environment.ShowNotification(message);
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
}
