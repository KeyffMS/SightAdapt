namespace SightAdapt.Demo;

internal sealed record ApplicationProfileToggleNotification(
    string DisplayName,
    bool WasCreated,
    bool IsEnabled);

internal sealed class SightAdaptContext : ApplicationContext
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly ApplicationStateController _stateController;
    private readonly OverlayController _overlayController;
    private readonly ForegroundWindowTracker _foregroundTracker;
    private readonly TrayPresenter _tray;
    private readonly HotkeyWindow _hotkeys;
    private readonly System.Windows.Forms.Timer _faultStateTimer;

    private ConfigurationForm? _configurationForm;
    private bool _disposed;

    public SightAdaptContext()
    {
        _settingsCoordinator = new SettingsCoordinator();
        _stateController = new ApplicationStateController();
        _overlayController = new OverlayController(
            VisualTransformCatalog.Default);
        _foregroundTracker = new ForegroundWindowTracker();
        _faultStateTimer = new System.Windows.Forms.Timer
        {
            Interval = 5000,
        };

        _tray = new TrayPresenter(
            _settingsCoordinator.Current.AutomaticMode,
            ToggleForActiveWindow,
            ToggleActiveApplicationProfile,
            SetAutomaticMode,
            ShowConfiguration,
            EmergencyDisable,
            ExitThread);

        _hotkeys = new HotkeyWindow(HandleHotkey);

        _settingsCoordinator.Changed += SettingsChanged;
        _stateController.Changed += ApplicationStateChanged;
        _overlayController.OverlayClosed += OverlayControllerClosed;
        _foregroundTracker.Changed += ForegroundWindowChanged;
        _faultStateTimer.Tick += FaultStateTimerTick;

        ApplyApplicationState(_stateController.Current);
        _foregroundTracker.Start();
        _tray.ShowStartup(
            _hotkeys.LocalToggleShortcut,
            _hotkeys.ProfileToggleShortcut);

        if (_settingsCoordinator.SettingsWereMigrated)
        {
            TrySaveMigratedSettings();
        }

        if (!string.IsNullOrWhiteSpace(
                _settingsCoordinator.LastLoadWarning))
        {
            _tray.ShowNotification(
                _settingsCoordinator.LastLoadWarning);
        }
    }

    private SightAdaptSettings Settings =>
        _settingsCoordinator.Current;

    protected override void ExitThreadCore()
    {
        _configurationForm?.Close();
        _overlayController.Disable();
        _stateController.SetInactive();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _settingsCoordinator.Changed -= SettingsChanged;
            _stateController.Changed -= ApplicationStateChanged;
            _overlayController.OverlayClosed -= OverlayControllerClosed;
            _foregroundTracker.Changed -= ForegroundWindowChanged;
            _faultStateTimer.Tick -= FaultStateTimerTick;

            _configurationForm?.Dispose();
            _foregroundTracker.Dispose();
            _faultStateTimer.Stop();
            _faultStateTimer.Dispose();
            _hotkeys.Dispose();
            _overlayController.Dispose();
            _tray.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void HandleHotkey(int id)
    {
        if (id == HotkeyWindow.LocalToggleId)
        {
            ToggleForActiveWindow();
        }
        else if (id == HotkeyWindow.ProfileToggleId)
        {
            ToggleActiveApplicationProfile();
        }
    }

    private void ToggleForActiveWindow()
    {
        var target = _foregroundTracker.ResolveTargetWindow();
        if (target == nint.Zero)
        {
            _tray.ShowNotification(
                "No supported application window is currently available.");
            return;
        }

        if (_overlayController.IsActive &&
            _overlayController.TargetWindow == target)
        {
            DisableOverlay();

            if (Settings.AutomaticMode &&
                IsConfiguredApplication(target))
            {
                _stateController.SuppressAutomaticFor(target);
            }

            return;
        }

        ApplicationProfile? assignment = null;
        if (ApplicationDiscovery.TryGetIdentity(
                target,
                out var identity))
        {
            assignment = ProfileResolver.FindAssignment(
                Settings,
                identity);
        }

        _stateController.ClearAutomaticSuppression();
        ActivateOverlay(
            target,
            ApplicationRunState.ManualActive,
            assignment);
    }

    private void ToggleActiveApplicationProfile()
    {
        var target = _foregroundTracker.ResolveTargetWindow();
        if (target == nint.Zero ||
            !ApplicationDiscovery.TryGetIdentity(
                target,
                out var identity))
        {
            _tray.ShowNotification(
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

        _tray.ShowNotification(result.IsEnabled
            ? result.WasCreated
                ? $"Soft invert profile added and enabled: " +
                  $"{result.DisplayName}."
                : $"Automatic profile enabled: {result.DisplayName}."
            : $"Automatic profile disabled: {result.DisplayName}.");
    }

    private void SetAutomaticMode(bool enabled)
    {
        var commit = _settingsCoordinator.Commit(settings =>
            AutomaticModeManagementService.Set(settings, enabled));

        if (!commit.Succeeded)
        {
            _tray.SetAutomaticMode(Settings.AutomaticMode);
            ShowCommitError(commit.ErrorMessage);
            return;
        }

        if (enabled)
        {
            ResumeAutomaticOperation();
        }
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

        var target = _foregroundTracker.ResolveTargetWindow();
        if (target != nint.Zero)
        {
            EvaluateAutomaticForWindow(target);
        }
    }

    private void ActivateOverlay(
        nint target,
        ApplicationRunState runState,
        ApplicationProfile? assignment = null)
    {
        try
        {
            var visualProfile =
                ProfileResolver.ResolveVisualProfile(
                    Settings,
                    assignment);
            _overlayController.Activate(target, visualProfile);

            if (runState == ApplicationRunState.AutomaticActive)
            {
                _stateController.SetAutomaticActive(
                    target,
                    visualProfile.Id);
            }
            else
            {
                _stateController.SetManualActive(
                    target,
                    visualProfile.Id);
            }
        }
        catch (Exception exception)
        {
            _overlayController.Disable();

            var message =
                $"Could not create the overlay: {exception.Message}";
            _stateController.SetFault(
                message,
                runState == ApplicationRunState.AutomaticActive
                    ? target
                    : nint.Zero);
            _tray.ShowNotification(message);
        }
    }

    private void DisableOverlay()
    {
        _overlayController.Disable();
        _stateController.SetInactive();
    }

    private void OverlayControllerClosed(
        object? sender,
        EventArgs eventArgs)
    {
        if (_stateController.Current.HasActiveOverlay)
        {
            _stateController.SetInactive();
        }
    }

    private void ForegroundWindowChanged(
        object? sender,
        ForegroundWindowChangedEventArgs eventArgs)
    {
        var candidate = eventArgs.Window;
        _stateController.ObserveForeground(candidate);

        if (_stateController.Current.Kind ==
                ApplicationRunState.ManualActive &&
            _stateController.Current.TargetWindow != candidate)
        {
            DisableOverlay();
        }

        EvaluateAutomaticForWindow(candidate);
    }

    private void EvaluateAutomaticForWindow(nint target)
    {
        var currentState = _stateController.Current.Kind;
        if (!Settings.AutomaticMode ||
            !_stateController.AllowsAutomaticActivation ||
            currentState == ApplicationRunState.ManualActive ||
            !ForegroundWindowTracker.IsSupportedTarget(target))
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

        if (!ApplicationDiscovery.TryGetIdentity(
                target,
                out var identity))
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
                ApplicationRunState.AutomaticActive,
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
        return ApplicationDiscovery.TryGetIdentity(
                target,
                out var identity) &&
            ProfileResolver.FindEnabledAssignment(
                Settings,
                identity) is not null;
    }

    private void SettingsChanged(
        object? sender,
        EventArgs eventArgs)
    {
        _tray.SetAutomaticMode(Settings.AutomaticMode);
        ApplyApplicationState(_stateController.Current);

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

        var target = _foregroundTracker.ResolveTargetWindow();
        if (target != nint.Zero)
        {
            EvaluateAutomaticForWindow(target);
        }
    }

    private void RefreshManualOverlayFromSettings()
    {
        var target = _stateController.Current.TargetWindow;
        if (target == nint.Zero ||
            !ForegroundWindowTracker.IsSupportedTarget(target))
        {
            DisableOverlay();
            return;
        }

        ApplicationProfile? assignment = null;
        if (ApplicationDiscovery.TryGetIdentity(
                target,
                out var identity))
        {
            assignment = ProfileResolver.FindAssignment(
                Settings,
                identity);
        }

        ActivateOverlay(
            target,
            ApplicationRunState.ManualActive,
            assignment);
    }

    private void EmergencyDisable()
    {
        _overlayController.Disable();
        _stateController.SetEmergency(
            "All overlays were disabled.");
        _faultStateTimer.Stop();

        var commit = _settingsCoordinator.Commit(settings =>
            AutomaticModeManagementService.Disable(settings));

        if (commit.Succeeded)
        {
            _tray.ShowNotification(
                "All overlays were disabled. Automatic mode is off.");
        }
        else
        {
            _tray.SetAutomaticMode(Settings.AutomaticMode);
            _tray.ShowNotification(
                "All overlays were disabled for this session, but " +
                (commit.ErrorMessage ??
                 "automatic mode could not be saved."));
        }
    }

    private void ShowConfiguration()
    {
        if (_configurationForm is not null &&
            !_configurationForm.IsDisposed)
        {
            _configurationForm.Show();
            _configurationForm.Activate();
            return;
        }

        var form = new ConfigurationForm(
            _settingsCoordinator,
            _foregroundTracker.GetCurrentApplicationIdentity)
        {
            ShowIcon = true,
            Icon = _tray.GetIcon(
                _stateController.Current.Kind),
        };

        form.FormClosed += (_, _) =>
            _configurationForm = null;
        _configurationForm = form;
        form.Show();
    }

    private void ApplicationStateChanged(
        object? sender,
        ApplicationStateChangedEventArgs eventArgs)
    {
        if (eventArgs.Current.Kind ==
            ApplicationRunState.Fault)
        {
            _faultStateTimer.Stop();
            _faultStateTimer.Start();
        }
        else
        {
            _faultStateTimer.Stop();
        }

        ApplyApplicationState(eventArgs.Current);
    }

    private void ApplyApplicationState(
        ApplicationState state)
    {
        var title = state.TargetWindow == nint.Zero
            ? null
            : NativeMethods.GetWindowTitle(
                state.TargetWindow);
        var profileName = ResolveProfileName(
            state.VisualProfileId);

        _tray.ApplyState(
            state,
            Settings.AutomaticMode,
            title,
            profileName);

        if (_configurationForm is not null &&
            !_configurationForm.IsDisposed)
        {
            _configurationForm.Icon =
                _tray.GetIcon(state.Kind);
        }
    }

    private string ResolveProfileName(
        string? profileId)
    {
        return Settings.VisualProfiles
            .FirstOrDefault(profile => string.Equals(
                profile.Id,
                profileId,
                StringComparison.OrdinalIgnoreCase))
            ?.Name ?? "Visual correction";
    }

    private void FaultStateTimerTick(
        object? sender,
        EventArgs eventArgs)
    {
        _faultStateTimer.Stop();

        if (_stateController.Current.Kind ==
            ApplicationRunState.Fault)
        {
            _stateController.SetInactive();
        }
    }

    private void TrySaveMigratedSettings()
    {
        var result =
            _settingsCoordinator.PersistCurrent();

        _tray.ShowNotification(result.Succeeded
            ? "Settings were upgraded to the current " +
              "color-profile format."
            : result.ErrorMessage ??
              "Migrated settings could not be saved.");
    }

    private void ShowCommitError(string? message)
    {
        _tray.ShowNotification(
            string.IsNullOrWhiteSpace(message)
                ? "Settings could not be changed."
                : message);
    }
}
