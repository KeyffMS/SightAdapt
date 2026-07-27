namespace SightAdapt;

internal sealed class AutomaticActivationService
{
    private readonly ApplicationStateController _stateController;
    private readonly RuntimeOverlayActivator _overlayActivator;
    private readonly IRuntimeEnvironment _environment;

    public AutomaticActivationService(
        ApplicationStateController stateController,
        RuntimeOverlayActivator overlayActivator,
        IRuntimeEnvironment environment)
    {
        _stateController = stateController ??
            throw new ArgumentNullException(nameof(stateController));
        _overlayActivator = overlayActivator ??
            throw new ArgumentNullException(nameof(overlayActivator));
        _environment = environment ??
            throw new ArgumentNullException(nameof(environment));
    }

    public void Evaluate(
        IReadOnlySightAdaptSettings settings,
        nint targetWindow)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var currentState = _stateController.Current.Kind;
        if (!settings.AutomaticMode ||
            !_stateController.AllowsAutomaticActivation ||
            currentState == ApplicationRunState.ManualActive ||
            !_environment.IsSupportedTarget(targetWindow))
        {
            return;
        }

        if (_stateController.IsAutomaticSuppressedFor(targetWindow))
        {
            if (currentState ==
                ApplicationRunState.AutomaticActive)
            {
                _overlayActivator.Disable();
                _stateController.SuppressAutomaticFor(targetWindow);
            }

            return;
        }

        var identity =
            _environment.ResolveIdentity(targetWindow);
        if (identity is null)
        {
            DisableAutomaticOverlayIfActive(currentState);
            return;
        }

        var assignment =
            ProfileResolver.FindEnabledAssignment(
                settings,
                identity);
        if (assignment is not null)
        {
            _overlayActivator.Activate(
                settings,
                targetWindow,
                RuntimeActivationMode.Automatic,
                assignment);
        }
        else
        {
            DisableAutomaticOverlayIfActive(currentState);
        }
    }

    public bool IsConfigured(
        IReadOnlySightAdaptSettings settings,
        nint targetWindow)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var identity =
            _environment.ResolveIdentity(targetWindow);
        return identity is not null &&
            ProfileResolver.FindEnabledAssignment(
                settings,
                identity) is not null;
    }

    public void Resume(
        IReadOnlySightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (_stateController.Current.Kind is
            ApplicationRunState.Emergency or
            ApplicationRunState.Fault)
        {
            _stateController.SetInactive();
        }

        _stateController.ClearAutomaticSuppression();

        var targetWindow =
            _environment.ResolveTargetWindow();
        if (targetWindow != nint.Zero)
        {
            Evaluate(settings, targetWindow);
        }
    }

    public void HandleSettingsChanged(
        IReadOnlySightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (_stateController.Current.Kind ==
            ApplicationRunState.ManualActive)
        {
            RefreshManualOverlay(settings);
            return;
        }

        if (!settings.AutomaticMode)
        {
            _stateController.ClearAutomaticSuppression();
            if (_stateController.Current.Kind ==
                ApplicationRunState.AutomaticActive)
            {
                _overlayActivator.Disable();
            }

            return;
        }

        if (!_stateController.AllowsAutomaticActivation)
        {
            return;
        }

        var targetWindow =
            _environment.ResolveTargetWindow();
        if (targetWindow != nint.Zero)
        {
            Evaluate(settings, targetWindow);
        }
    }

    private void RefreshManualOverlay(
        IReadOnlySightAdaptSettings settings)
    {
        var targetWindow =
            _stateController.Current.TargetWindow;
        if (targetWindow == nint.Zero ||
            !_environment.IsSupportedTarget(targetWindow))
        {
            _overlayActivator.Disable();
            return;
        }

        var identity =
            _environment.ResolveIdentity(targetWindow);
        var assignment = identity is null
            ? null
            : ProfileResolver.FindAssignment(
                settings,
                identity);

        _overlayActivator.Activate(
            settings,
            targetWindow,
            RuntimeActivationMode.Manual,
            assignment);
    }

    private void DisableAutomaticOverlayIfActive(
        ApplicationRunState currentState)
    {
        if (currentState ==
            ApplicationRunState.AutomaticActive)
        {
            _overlayActivator.Disable();
        }
    }
}
