namespace SightAdapt;

internal sealed class SightAdaptContext : ApplicationContext
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly ApplicationStateController _stateController;
    private readonly OverlayController _overlayController;
    private readonly ForegroundWindowTracker _foregroundTracker;
    private readonly TrayPresenter _tray;
    private readonly RuntimeCoordinator _runtimeCoordinator;
    private readonly HotkeyWindow _hotkeys;
    private readonly System.Windows.Forms.Timer _faultStateTimer;

    private ConfigurationForm? _configurationForm;
    private bool _disposed;

    public SightAdaptContext()
    {
        _settingsCoordinator = new SettingsCoordinator();
        var initialSettings = _settingsCoordinator.Current;
        _stateController = new ApplicationStateController();
        _overlayController = new OverlayController(
            VisualProfileCatalog.Default);
        _foregroundTracker = new ForegroundWindowTracker();
        _faultStateTimer = new System.Windows.Forms.Timer
        {
            Interval = 5000,
        };

        _tray = new TrayPresenter(
            initialSettings.AutomaticMode,
            ToggleForActiveWindow,
            ToggleActiveApplicationAssignment,
            SetAutomaticMode,
            ShowConfiguration,
            EmergencyDisable,
            ExitThread);

        _runtimeCoordinator = new RuntimeCoordinator(
            _settingsCoordinator,
            _stateController,
            _overlayController,
            _foregroundTracker.ResolveTargetWindow,
            ForegroundWindowTracker.IsSupportedTarget,
            ResolveApplicationIdentity,
            _tray.ShowNotification,
            _tray.SetAutomaticMode);
        _hotkeys = new HotkeyWindow(HandleHotkey);

        _settingsCoordinator.Changed += SettingsChanged;
        _stateController.Changed += ApplicationStateChanged;
        _overlayController.OverlayClosed += OverlayControllerClosed;
        _foregroundTracker.Changed += ForegroundWindowChanged;
        _faultStateTimer.Tick += FaultStateTimerTick;

        ApplyApplicationState(_stateController.Current, initialSettings);
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

    protected override void ExitThreadCore()
    {
        _configurationForm?.Close();
        _runtimeCoordinator.DisableForExit();
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
            ToggleActiveApplicationAssignment();
        }
    }

    private void ToggleForActiveWindow()
    {
        _runtimeCoordinator.ToggleForActiveWindow();
    }

    private void ToggleActiveApplicationAssignment()
    {
        _runtimeCoordinator.ToggleActiveApplicationAssignment();
    }

    private void SetAutomaticMode(bool enabled)
    {
        _runtimeCoordinator.SetAutomaticMode(enabled);
    }

    private void EmergencyDisable()
    {
        _runtimeCoordinator.EmergencyDisable();
    }

    private void OverlayControllerClosed(
        object? sender,
        EventArgs eventArgs)
    {
        _runtimeCoordinator.HandleOverlayClosed();
    }

    private void ForegroundWindowChanged(
        object? sender,
        ForegroundWindowChangedEventArgs eventArgs)
    {
        _runtimeCoordinator.HandleForegroundWindowChanged(
            eventArgs.Window);
    }

    private void SettingsChanged(
        object? sender,
        EventArgs eventArgs)
    {
        var settings = _settingsCoordinator.Current;
        _tray.SetAutomaticMode(settings.AutomaticMode);
        ApplyApplicationState(
            _stateController.Current,
            settings);
        _runtimeCoordinator.HandleSettingsChanged(settings);
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

        ApplyApplicationState(
            eventArgs.Current,
            _settingsCoordinator.Current);
    }

    private void ApplyApplicationState(
        ApplicationState state,
        IReadOnlySightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var title = state.TargetWindow == nint.Zero
            ? null
            : NativeMethods.GetWindowTitle(
                state.TargetWindow);
        var profileName = ProfileResolver.ResolveVisualProfileName(
            settings,
            state.VisualProfileId,
            "Visual correction");

        _tray.ApplyState(
            state,
            settings.AutomaticMode,
            title,
            profileName);

        if (_configurationForm is not null &&
            !_configurationForm.IsDisposed)
        {
            _configurationForm.Icon =
                _tray.GetIcon(state.Kind);
        }
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

    private static ApplicationIdentity? ResolveApplicationIdentity(
        nint window)
    {
        return ApplicationDiscovery.TryGetIdentity(
                window,
                out var identity)
            ? identity
            : null;
    }
}