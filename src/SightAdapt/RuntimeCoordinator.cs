namespace SightAdapt;

internal sealed record ApplicationAssignmentToggleNotification(
    string DisplayName,
    bool WasCreated,
    bool IsEnabled);

internal sealed class RuntimeCoordinator
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly ApplicationStateController _stateController;
    private readonly IRuntimeTargetResolver _targetResolver;
    private readonly IRuntimeFeedback _feedback;
    private readonly RuntimeOverlayActivator _overlayActivator;
    private readonly AutomaticActivationService _automaticActivation;
    private readonly Func<IReadOnlySightAdaptSettings> _readSettings;
    private bool _committingSettings;

    internal RuntimeCoordinator(
        SettingsCoordinator settingsCoordinator,
        ApplicationStateController stateController,
        IRuntimeOverlay overlay,
        IRuntimeTargetResolver targetResolver,
        IRuntimeFeedback feedback,
        Func<IReadOnlySightAdaptSettings>? readSettings = null)
    {
        _settingsCoordinator = settingsCoordinator ??
            throw new ArgumentNullException(nameof(settingsCoordinator));
        _stateController = stateController ??
            throw new ArgumentNullException(nameof(stateController));
        _targetResolver = targetResolver ??
            throw new ArgumentNullException(nameof(targetResolver));
        _feedback = feedback ??
            throw new ArgumentNullException(nameof(feedback));
        _readSettings = readSettings ??
            (() => _settingsCoordinator.Current);
        _overlayActivator = new RuntimeOverlayActivator(
            stateController,
            overlay,
            feedback);
        _automaticActivation = new AutomaticActivationService(
            stateController,
            _overlayActivator,
            targetResolver);
    }

    public void ToggleForActiveWindow()
    {
        var targetWindow =
            _targetResolver.ResolveTargetWindow();
        if (targetWindow == nint.Zero)
        {
            _feedback.ShowNotification(
                RuntimeMessages.NoSupportedWindow);
            return;
        }

        var settings = ReadSettings();
        if (_overlayActivator.Overlay.IsActive &&
            _overlayActivator.Overlay.TargetWindow == targetWindow)
        {
            _overlayActivator.Disable();

            if (settings.AutomaticMode &&
                _automaticActivation.IsConfigured(
                    settings,
                    targetWindow))
            {
                _stateController.SuppressAutomaticFor(
                    targetWindow);
            }

            return;
        }

        var identity =
            _targetResolver.ResolveIdentity(targetWindow);
        var assignment = identity is null
            ? null
            : ProfileResolver.FindAssignment(
                settings,
                identity);

        _stateController.ClearAutomaticSuppression();
        _overlayActivator.Activate(
            settings,
            targetWindow,
            RuntimeActivationMode.Manual,
            assignment);
    }

    public void ToggleActiveApplicationAssignment()
    {
        var targetWindow =
            _targetResolver.ResolveTargetWindow();
        var identity = targetWindow == nint.Zero
            ? null
            : _targetResolver.ResolveIdentity(targetWindow);
        if (identity is null)
        {
            _feedback.ShowNotification(
                RuntimeMessages.IdentityUnavailable);
            return;
        }

        var commit = CommitSettings(settings =>
        {
            var result =
                ApplicationAssignmentService.Toggle(
                    settings,
                    identity);

            if (result.IsEnabled)
            {
                AutomaticModeManagementService.Enable(settings);
            }

            return new ApplicationAssignmentToggleNotification(
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
        var settings = ReadSettings();
        if (result.IsEnabled)
        {
            _automaticActivation.Resume(settings);
        }
        else
        {
            _automaticActivation.HandleSettingsChanged(settings);
        }

        _feedback.ShowNotification(
            RuntimeMessages.AssignmentToggled(result));
    }

    public void SetAutomaticMode(bool enabled)
    {
        var commit = CommitSettings(settings =>
            AutomaticModeManagementService.Set(
                settings,
                enabled));

        var settings = ReadSettings();
        if (!commit.Succeeded)
        {
            _feedback.SynchronizeAutomaticMode(
                settings.AutomaticMode);
            ShowCommitError(commit.ErrorMessage);
            return;
        }

        if (enabled)
        {
            _automaticActivation.Resume(settings);
        }
        else
        {
            _automaticActivation.HandleSettingsChanged(settings);
        }
    }

    public void HandleForegroundWindowChanged(
        nint candidate)
    {
        _stateController.ObserveForeground(candidate);

        if (_stateController.Current.Kind ==
                ApplicationRunState.ManualActive &&
            _stateController.Current.TargetWindow != candidate)
        {
            _overlayActivator.Disable();
        }

        _automaticActivation.Evaluate(
            ReadSettings(),
            candidate);
    }

    public void HandleSettingsChanged()
    {
        HandleSettingsChanged(ReadSettings());
    }

    internal void HandleSettingsChanged(
        IReadOnlySightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (_committingSettings)
        {
            return;
        }

        _automaticActivation.HandleSettingsChanged(settings);
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
        _overlayActivator.EmergencyDisable();

        var commit = CommitSettings(settings =>
            AutomaticModeManagementService.Disable(settings));

        if (commit.Succeeded)
        {
            _feedback.ShowNotification(
                "All overlays were disabled. Automatic mode is off.");
            return;
        }

        var settings = ReadSettings();
        _feedback.SynchronizeAutomaticMode(
            settings.AutomaticMode);
        _feedback.ShowNotification(
            "All overlays were disabled for this session, but " +
            (commit.ErrorMessage ??
             "automatic mode could not be saved."));
    }

    public void DisableForExit()
    {
        _overlayActivator.DisableForExit();
    }

    private IReadOnlySightAdaptSettings ReadSettings()
    {
        return _readSettings();
    }

    private SettingsCommitResult<T> CommitSettings<T>(
        Func<SightAdaptSettings, T> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        _committingSettings = true;
        try
        {
            return _settingsCoordinator.Commit(mutation);
        }
        finally
        {
            _committingSettings = false;
        }
    }

    private void ShowCommitError(string? message)
    {
        _feedback.ShowNotification(
            string.IsNullOrWhiteSpace(message)
                ? RuntimeMessages.SettingsChangeFailed
                : message);
    }
}
