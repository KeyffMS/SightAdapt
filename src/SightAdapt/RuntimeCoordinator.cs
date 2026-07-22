namespace SightAdapt;

internal sealed record ApplicationProfileToggleNotification(
    string DisplayName,
    bool WasCreated,
    bool IsEnabled);

internal enum RuntimeActivationMode
{
    Manual,
    Automatic,
}

internal interface IRuntimeOverlay
{
    bool IsActive { get; }

    nint TargetWindow { get; }

    void Activate(
        nint targetWindow,
        VisualProfile visualProfile,
        OverlayScope overlayScope);

    void Disable();
}

internal sealed class RuntimeCoordinator
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly ApplicationStateController _stateController;
    private readonly IRuntimeOverlay _overlay;
    private readonly Func<nint> _resolveTargetWindow;
    private readonly Func<nint, bool> _isSupportedTarget;
    private readonly Func<nint, ApplicationIdentity?> _resolveIdentity;
    private readonly Action<string> _showNotification;
    private readonly Action<bool> _synchronizeAutomaticMode;

    public RuntimeCoordinator(
        SettingsCoordinator settingsCoordinator,
        ApplicationStateController stateController,
        IRuntimeOverlay overlay,
        Func<nint> resolveTargetWindow,
        Func<nint, bool> isSupportedTarget,
        Func<nint, ApplicationIdentity?> resolveIdentity,
        Action<string> showNotification,
        Action<bool> synchronizeAutomaticMode)
    {
        _settingsCoordinator = settingsCoordinator ??
            throw new ArgumentNullException(nameof(settingsCoordinator));
        _stateController = stateController ??
            throw new ArgumentNullException(nameof(stateController));
        _overlay = overlay ??
            throw new ArgumentNullException(nameof(overlay));
        _resolveTargetWindow = resolveTargetWindow ??
            throw new ArgumentNullException(nameof(resolveTargetWindow));
        _isSupportedTarget = isSupportedTarget ??
            throw new ArgumentNullException(nameof(isSupportedTarget));
        _resolveIdentity = resolveIdentity ??
            throw new ArgumentNullException(nameof(resolveIdentity));
        _showNotification = showNotification ??
            throw new ArgumentNullException(nameof(showNotification));
        _synchronizeAutomaticMode = synchronizeAutomaticMode ??
            throw new ArgumentNullException(nameof(synchronizeAutomaticMode));
    }

    private SightAdaptSettings Settings =>
        _settingsCoordinator.Current;

    public void ToggleForActiveWindow()
    {
        var target = _resolveTargetWindow();
        if (target == nint.Zero)
        {
            _showNotification(
                "No supported application window is currently available.");
            return;
        }

        if (_overlay.IsActive &&
            _overlay.TargetWindow == target)
        {
            DisableOverlay();

            if (Settings.AutomaticMode &&
                IsConfiguredApplication(target))
            {
                _stateController.SuppressAutomaticFor(target);
            }

            return;
        }

        var identity = _resolveIdentity(target);
        var assignment = identity is null
            ? null
            : ProfileResolver.FindAssignment(
                Settings,
                identity);

        _stateController.ClearAutomaticSuppression();
        ActivateOverlay(
            target,
            RuntimeActivationMode.Manual,
            assignment);
    }

    public void ToggleActiveApplicationProfile()
    {
        var target = _resolveTargetWindow();
        var identity = target == nint.Zero
            ? null
            : _resolveIdentity(target);
        if (identity is null)
        {
            _showNotification(
                "The active application's executable path could not be read. " +
                "Use the configuration panel to select its .exe file.");
            return;
        }

        var commit = _settingsCoordinator.Commit(settings =>
        {
            var result =
                ApplicationProfileManagementService.Toggle(
                    settings,
                    identity);

            if (result.IsEnabled)
            {
                AutomaticModeManagementService.Enable(settings);
            }

            return new ApplicationProfileToggleNotification(
                identity.DisplayName,
                result.WasCreated,
                result.IsEnabled);
        });

        if (!commit.Succeeded || commit.Value is null)
        {
            ShowCommitError(commit.ErrorMessage);
            return;
        }

        var result = commit.Value;
        if (result.IsEnabled)
        {
            ResumeAutomaticOperation();
        }

        _showNotification(result.IsEnabled
            ? result.WasCreated
                ? $"Soft invert profile added and enabled: " +
                  $"{result.DisplayName}."
                : $"Automatic profile enabled: {result.DisplayName}."
            : $"Automatic profile disabled: {result.DisplayName}.");
    }

    public void SetAutomaticMode(bool enabled)
    {
        var commit = _settingsCoordinator.Commit(settings =>
            AutomaticModeManagementService.Set(settings, enabled));

        if (!commit.Succeeded)
        {
            _synchronizeAutomaticMode(Settings.AutomaticMode);
            ShowCommitError(commit.ErrorMessage);
            return;
        }

        if (enabled)
        {
            ResumeAutomaticOperation();
        }
    }

    public void HandleForegroundWindowChanged(nint candidate)
    {
        _stateController.ObserveForeground(candidate);

        if (_stateController.Current.Kind ==
                ApplicationRunState.ManualActive &&
            _stateController.Current.TargetWindow != candidate)
        {
            DisableOverlay();
        }

        EvaluateAutomaticForWindow(candidate);
    }

    public void HandleSettingsChanged()
    {
        if (_stateController.Current.Kind ==
            ApplicationRunState.ManualActive)
        {
            RefreshManualOverlayFromSettings();
            return;
        }

        if (!Settings.AutomaticMode)
        {
            _stateController.ClearAutomaticSuppression();

            if (_stateController.Current.Kind ==
                ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
            }

            return;
        }

        if (!_stateController.AllowsAutomaticActivation)
        {
            return;
        }

        var target = _resolveTargetWindow();
        if (target != nint.Zero)
        {
            EvaluateAutomaticForWindow(target);
        }
    }

    public void HandleOverlayClosed()
    {
        if (_stateController.Current.HasActiveOverlay)
        {
            _stateController.SetInactive();
        }
    }

    public void EmergencyDisable()
    {
        _overlay.Disable();
        _stateController.SetEmergency(
            "All overlays were disabled.");

        var commit = _settingsCoordinator.Commit(settings =>
            AutomaticModeManagementService.Disable(settings));

        if (commit.Succeeded)
        {
            _showNotification(
                "All overlays were disabled. Automatic mode is off.");
        }
        else
        {
            _synchronizeAutomaticMode(Settings.AutomaticMode);
            _showNotification(
                "All overlays were disabled for this session, but " +
                (commit.ErrorMessage ??
                 "automatic mode could not be saved."));
        }
    }

    public void DisableForExit()
    {
        _overlay.Disable();
        _stateController.SetInactive();
    }

    private void ResumeAutomaticOperation()
    {
        if (_stateController.Current.Kind is
            ApplicationRunState.Emergency or
            ApplicationRunState.Fault)
        {
            _stateController.SetInactive();
        }

        _stateController.ClearAutomaticSuppression();

        var target = _resolveTargetWindow();
        if (target != nint.Zero)
        {
            EvaluateAutomaticForWindow(target);
        }
    }

    private void ActivateOverlay(
        nint target,
        RuntimeActivationMode activationMode,
        ApplicationProfile? assignment = null)
    {
        try
        {
            var visualProfile =
                ProfileResolver.ResolveVisualProfile(
                    Settings,
                    assignment);
            _overlay.Activate(
                target,
                visualProfile,
                assignment?.OverlayScope ??
                    OverlayScopePolicy.Default);

            switch (activationMode)
            {
                case RuntimeActivationMode.Manual:
                    _stateController.SetManualActive(
                        target,
                        visualProfile.Id);
                    break;
                case RuntimeActivationMode.Automatic:
                    _stateController.SetAutomaticActive(
                        target,
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
                $"Could not create the overlay: {exception.Message}";
            _stateController.SetFault(
                message,
                activationMode == RuntimeActivationMode.Automatic
                    ? target
                    : nint.Zero);
            _showNotification(message);
        }
    }

    private void DisableOverlay()
    {
        _overlay.Disable();
        _stateController.SetInactive();
    }

    private void EvaluateAutomaticForWindow(nint target)
    {
        var currentState = _stateController.Current.Kind;
        if (!Settings.AutomaticMode ||
            !_stateController.AllowsAutomaticActivation ||
            currentState == ApplicationRunState.ManualActive ||
            !_isSupportedTarget(target))
        {
            return;
        }

        if (_stateController.IsAutomaticSuppressedFor(target))
        {
            if (currentState ==
                ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
                _stateController.SuppressAutomaticFor(target);
            }

            return;
        }

        var identity = _resolveIdentity(target);
        if (identity is null)
        {
            if (currentState ==
                ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
            }

            return;
        }

        var assignment =
            ProfileResolver.FindEnabledAssignment(
                Settings,
                identity);

        if (assignment is not null)
        {
            ActivateOverlay(
                target,
                RuntimeActivationMode.Automatic,
                assignment);
        }
        else if (currentState ==
                 ApplicationRunState.AutomaticActive)
        {
            DisableOverlay();
        }
    }

    private bool IsConfiguredApplication(nint target)
    {
        var identity = _resolveIdentity(target);
        return identity is not null &&
            ProfileResolver.FindEnabledAssignment(
                Settings,
                identity) is not null;
    }

    private void RefreshManualOverlayFromSettings()
    {
        var target = _stateController.Current.TargetWindow;
        if (target == nint.Zero ||
            !_isSupportedTarget(target))
        {
            DisableOverlay();
            return;
        }

        var identity = _resolveIdentity(target);
        var assignment = identity is null
            ? null
            : ProfileResolver.FindAssignment(
                Settings,
                identity);

        ActivateOverlay(
            target,
            RuntimeActivationMode.Manual,
            assignment);
    }

    private void ShowCommitError(string? message)
    {
        _showNotification(
            string.IsNullOrWhiteSpace(message)
                ? "Settings could not be changed."
                : message);
    }
}