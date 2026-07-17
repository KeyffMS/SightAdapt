using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class SightAdaptContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _automaticModeItem;
    private readonly HotkeyWindow _hotkeys;
    private readonly System.Windows.Forms.Timer _foregroundTracker;
    private readonly System.Windows.Forms.Timer _emergencyStateTimer;
    private readonly SettingsStore _settingsStore;
    private readonly SightAdaptSettings _settings;
    private readonly TrayIconSet _trayIcons;
    private readonly ApplicationStateController _stateController;
    private readonly OverlayController _overlayController;

    private ConfigurationForm? _configurationForm;
    private nint _lastExternalWindow;
    private nint _automaticSuppressedWindow;
    private bool _updatingAutomaticMode;
    private bool _disposed;

    public SightAdaptContext()
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        _trayIcons = new TrayIconSet();
        _stateController = new ApplicationStateController();
        _overlayController = new OverlayController(new VisualTransformCatalog());

        _stateController.Changed += ApplicationStateChanged;
        _overlayController.OverlayClosed += OverlayControllerClosed;

        _statusItem = new ToolStripMenuItem("Overlay disabled")
        {
            Enabled = false,
        };
        AppTheme.StyleMenuItem(
            _statusItem,
            AppTheme.TextSecondary,
            FontStyle.Bold,
            "status");

        _toggleItem = new ToolStripMenuItem("Toggle inversion for active window");
        _toggleItem.Click += (_, _) => ToggleForActiveWindow();
        AppTheme.StyleMenuItem(_toggleItem);

        var toggleProfileItem = new ToolStripMenuItem(
            "Toggle automatic profile for current application");
        toggleProfileItem.Click += (_, _) => ToggleActiveApplicationProfile();
        AppTheme.StyleMenuItem(toggleProfileItem);

        _automaticModeItem = new ToolStripMenuItem("Automatic mode")
        {
            CheckOnClick = true,
            Checked = _settings.AutomaticMode,
        };
        _automaticModeItem.CheckedChanged += AutomaticModeItemCheckedChanged;
        AppTheme.StyleMenuItem(_automaticModeItem);

        var configureItem = new ToolStripMenuItem("Configure applications...");
        configureItem.Click += (_, _) => ShowConfiguration();
        AppTheme.StyleMenuItem(configureItem, AppTheme.AccentHover, FontStyle.Bold);

        var disableItem = new ToolStripMenuItem("Emergency disable all overlays");
        disableItem.Click += (_, _) => EmergencyDisable();
        AppTheme.StyleMenuItem(disableItem, AppTheme.Danger, FontStyle.Bold, "danger");

        var exitItem = new ToolStripMenuItem("Exit SightAdapt");
        exitItem.Click += (_, _) => ExitThread();
        AppTheme.StyleMenuItem(exitItem, AppTheme.TextSecondary);

        var menu = AppTheme.CreateContextMenu();
        menu.Items.AddRange([
            _statusItem,
            new ToolStripSeparator(),
            _toggleItem,
            toggleProfileItem,
            _automaticModeItem,
            configureItem,
            new ToolStripSeparator(),
            disableItem,
            new ToolStripSeparator(),
            exitItem,
        ]);

        _notifyIcon = new NotifyIcon
        {
            Icon = _trayIcons.Inactive,
            Text = $"{ProductInfo.DisplayName} · Inactive",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleForActiveWindow();

        _emergencyStateTimer = new System.Windows.Forms.Timer
        {
            Interval = 5000,
        };
        _emergencyStateTimer.Tick += EmergencyStateTimerTick;

        _hotkeys = new HotkeyWindow(HandleHotkey);

        _foregroundTracker = new System.Windows.Forms.Timer
        {
            Interval = 250,
            Enabled = true,
        };
        _foregroundTracker.Tick += (_, _) => TrackForegroundWindow();

        ApplyApplicationState(_stateController.Current);

        var localToggleText = _hotkeys.LocalToggleShortcut ?? "tray menu";
        var profileToggleText = _hotkeys.ProfileToggleShortcut ?? "tray menu";

        _notifyIcon.BalloonTipTitle = $"{ProductInfo.DisplayName} is running";
        _notifyIcon.BalloonTipText =
            $"Local toggle: {localToggleText}. Saved profile toggle: {profileToggleText}. " +
            "Emergency disable is available from the tray menu.";
        _notifyIcon.ShowBalloonTip(5000);

        if (_settingsStore.SettingsWereMigrated)
        {
            TrySaveMigratedSettings();
        }

        var loadWarning = _settingsStore.LastLoadWarning;
        if (!string.IsNullOrWhiteSpace(loadWarning))
        {
            ShowEmergencyState(loadWarning);
        }
    }

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
            _stateController.Changed -= ApplicationStateChanged;
            _overlayController.OverlayClosed -= OverlayControllerClosed;

            _configurationForm?.Dispose();
            _foregroundTracker.Dispose();
            _emergencyStateTimer.Stop();
            _emergencyStateTimer.Dispose();
            _hotkeys.Dispose();
            _overlayController.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayIcons.Dispose();
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
        var target = ResolveTargetWindow();
        if (target == nint.Zero)
        {
            ShowNotification("No supported application window is currently available.");
            return;
        }

        if (_overlayController.IsActive && _overlayController.TargetWindow == target)
        {
            DisableOverlay();

            if (_settings.AutomaticMode && IsConfiguredApplication(target))
            {
                _automaticSuppressedWindow = target;
            }

            return;
        }

        _automaticSuppressedWindow = nint.Zero;
        ActivateOverlay(target, ApplicationRunState.ManualActive);
    }

    private void ToggleActiveApplicationProfile()
    {
        var target = ResolveTargetWindow();
        if (target == nint.Zero ||
            !ApplicationDiscovery.TryGetIdentity(target, out var identity))
        {
            ShowNotification(
                "The active application's executable path could not be read. " +
                "Use the configuration panel to select its .exe file.");
            return;
        }

        var result = ApplicationProfileToggleService.Toggle(_settings, identity);

        if (result.IsEnabled)
        {
            _settings.AutomaticMode = true;
            _automaticSuppressedWindow = nint.Zero;
        }

        HandleSettingsChanged();

        ShowNotification(result.IsEnabled
            ? result.WasCreated
                ? $"Automatic profile added and enabled: {identity.DisplayName}."
                : $"Automatic profile enabled: {identity.DisplayName}."
            : $"Automatic profile disabled: {identity.DisplayName}.");
    }

    private void ActivateOverlay(
        nint target,
        ApplicationRunState runState,
        ApplicationProfile? assignment = null)
    {
        try
        {
            var visualProfile = ProfileResolver.ResolveVisualProfile(_settings, assignment);
            _overlayController.Activate(target, visualProfile);

            if (runState == ApplicationRunState.AutomaticActive)
            {
                _stateController.SetAutomaticActive(target);
            }
            else
            {
                _stateController.SetManualActive(target);
            }
        }
        catch (Exception exception)
        {
            _overlayController.Disable();
            ShowEmergencyState($"Could not create the overlay: {exception.Message}");
        }
    }

    private void DisableOverlay()
    {
        _overlayController.Disable();
        _stateController.SetInactive();
    }

    private void OverlayControllerClosed(object? sender, EventArgs eventArgs)
    {
        _stateController.SetInactive();
    }

    private void TrackForegroundWindow()
    {
        var candidate = NativeMethods.GetForegroundWindow();
        candidate = NativeMethods.GetAncestor(candidate, NativeMethods.GaRoot);

        if (!IsSupportedTarget(candidate))
        {
            return;
        }

        _lastExternalWindow = candidate;

        if (_automaticSuppressedWindow != nint.Zero &&
            candidate != _automaticSuppressedWindow)
        {
            _automaticSuppressedWindow = nint.Zero;
        }

        if (_stateController.Current.Kind == ApplicationRunState.ManualActive &&
            _stateController.Current.TargetWindow != candidate)
        {
            DisableOverlay();
        }

        EvaluateAutomaticForWindow(candidate);
    }

    private void EvaluateAutomaticForWindow(nint target)
    {
        var currentState = _stateController.Current.Kind;
        if (!_settings.AutomaticMode ||
            currentState is ApplicationRunState.ManualActive or ApplicationRunState.Emergency ||
            !IsSupportedTarget(target))
        {
            return;
        }

        if (target == _automaticSuppressedWindow)
        {
            if (currentState == ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
            }

            return;
        }

        if (!ApplicationDiscovery.TryGetIdentity(target, out var identity))
        {
            if (currentState == ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
            }

            return;
        }

        var assignment = ProfileResolver.FindEnabledAssignment(_settings, identity);
        if (assignment is not null)
        {
            ActivateOverlay(target, ApplicationRunState.AutomaticActive, assignment);
        }
        else if (currentState == ApplicationRunState.AutomaticActive)
        {
            DisableOverlay();
        }
    }

    private nint ResolveTargetWindow()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        foreground = NativeMethods.GetAncestor(foreground, NativeMethods.GaRoot);

        if (IsSupportedTarget(foreground))
        {
            _lastExternalWindow = foreground;
            return foreground;
        }

        return IsSupportedTarget(_lastExternalWindow)
            ? _lastExternalWindow
            : nint.Zero;
    }

    private ApplicationIdentity? GetCurrentApplicationIdentity()
    {
        var target = ResolveTargetWindow();
        return target != nint.Zero &&
            ApplicationDiscovery.TryGetIdentity(target, out var identity)
                ? identity
                : null;
    }

    private bool IsConfiguredApplication(nint target)
    {
        return ApplicationDiscovery.TryGetIdentity(target, out var identity) &&
            ProfileResolver.FindEnabledAssignment(_settings, identity) is not null;
    }

    private static bool IsSupportedTarget(nint window)
    {
        if (window == nint.Zero ||
            !NativeMethods.IsWindow(window) ||
            !NativeMethods.IsWindowVisible(window) ||
            NativeMethods.IsIconic(window))
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return false;
        }

        var windowClass = NativeMethods.GetWindowClass(window);
        return windowClass is not (
            "Shell_TrayWnd" or
            "Shell_SecondaryTrayWnd" or
            "Progman" or
            "WorkerW" or
            "NotifyIconOverflowWindow");
    }

    private void AutomaticModeItemCheckedChanged(object? sender, EventArgs eventArgs)
    {
        if (_updatingAutomaticMode)
        {
            return;
        }

        _settings.AutomaticMode = _automaticModeItem.Checked;
        HandleSettingsChanged();
    }

    private void HandleSettingsChanged()
    {
        _updatingAutomaticMode = true;

        try
        {
            _automaticModeItem.Checked = _settings.AutomaticMode;
        }
        finally
        {
            _updatingAutomaticMode = false;
        }

        try
        {
            _settingsStore.Save(_settings);
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowNotification($"Settings could not be saved: {exception.Message}");
        }

        _configurationForm?.RefreshProfiles();

        if (!_settings.AutomaticMode)
        {
            _automaticSuppressedWindow = nint.Zero;

            if (_stateController.Current.Kind == ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
            }

            return;
        }

        var target = ResolveTargetWindow();
        if (target != nint.Zero)
        {
            EvaluateAutomaticForWindow(target);
        }
    }

    private void EmergencyDisable()
    {
        _settings.AutomaticMode = false;
        _automaticSuppressedWindow = nint.Zero;
        HandleSettingsChanged();
        _overlayController.Disable();
        ShowEmergencyState("All overlays were disabled. Automatic mode is off.");
    }

    private void ShowConfiguration()
    {
        if (_configurationForm is not null && !_configurationForm.IsDisposed)
        {
            _configurationForm.Show();
            _configurationForm.Activate();
            return;
        }

        var form = new ConfigurationForm(
            _settings,
            _settingsStore,
            GetCurrentApplicationIdentity,
            HandleSettingsChanged)
        {
            ShowIcon = true,
            Icon = GetIconForState(_stateController.Current.Kind),
        };

        form.FormClosed += (_, _) => _configurationForm = null;
        _configurationForm = form;
        form.Show();
    }

    private void ApplicationStateChanged(
        object? sender,
        ApplicationStateChangedEventArgs eventArgs)
    {
        ApplyApplicationState(eventArgs.Current);
    }

    private void ApplyApplicationState(ApplicationState state)
    {
        if (state.Kind == ApplicationRunState.Emergency)
        {
            _emergencyStateTimer.Stop();
            _emergencyStateTimer.Start();
        }
        else
        {
            _emergencyStateTimer.Stop();
        }

        switch (state.Kind)
        {
            case ApplicationRunState.ManualActive:
                ApplyActiveState(state.TargetWindow, "Manual");
                break;
            case ApplicationRunState.AutomaticActive:
                ApplyActiveState(state.TargetWindow, "Automatic");
                break;
            case ApplicationRunState.Emergency:
                _statusItem.Text = "All overlays stopped · Automatic mode off";
                _toggleItem.Text = "Toggle inversion for active window";
                SetTrayIcon(TrayIconState.Emergency);
                break;
            default:
                _statusItem.Text = "Overlay disabled · Inactive";
                _toggleItem.Text = "Toggle inversion for active window";
                SetTrayIcon(TrayIconState.Inactive);
                break;
        }

        if (_configurationForm is not null && !_configurationForm.IsDisposed)
        {
            _configurationForm.Icon = GetIconForState(state.Kind);
        }
    }

    private void ApplyActiveState(nint target, string modeText)
    {
        var title = NativeMethods.GetWindowTitle(target);
        _statusItem.Text = string.IsNullOrWhiteSpace(title)
            ? $"{modeText} inversion enabled · Active"
            : $"{modeText}: {Truncate(title, 34)} · Active";
        _toggleItem.Text = "Disable inversion";
        SetTrayIcon(TrayIconState.Active);
    }

    private void ShowEmergencyState(string message)
    {
        _stateController.SetEmergency(message);
        ShowNotification(message);
    }

    private void EmergencyStateTimerTick(object? sender, EventArgs eventArgs)
    {
        _emergencyStateTimer.Stop();

        if (_stateController.Current.Kind == ApplicationRunState.Emergency)
        {
            _stateController.SetInactive();
        }
    }

    private void SetTrayIcon(TrayIconState state)
    {
        _notifyIcon.Icon = _trayIcons.Get(state);
        _notifyIcon.Text = state switch
        {
            TrayIconState.Active => $"{ProductInfo.DisplayName} · Active",
            TrayIconState.Emergency => $"{ProductInfo.DisplayName} · All overlays stopped",
            _ => $"{ProductInfo.DisplayName} · Inactive",
        };
    }

    private Icon GetIconForState(ApplicationRunState state)
    {
        return state switch
        {
            ApplicationRunState.ManualActive or ApplicationRunState.AutomaticActive =>
                _trayIcons.Active,
            ApplicationRunState.Emergency => _trayIcons.Emergency,
            _ => _trayIcons.Inactive,
        };
    }

    private void TrySaveMigratedSettings()
    {
        try
        {
            _settingsStore.Save(_settings);
            ShowNotification("Settings were upgraded to the current profile format.");
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ShowNotification($"Migrated settings could not be saved: {exception.Message}");
        }
    }

    private void ShowNotification(string message)
    {
        _notifyIcon.BalloonTipTitle = ProductInfo.DisplayName;
        _notifyIcon.BalloonTipText = Truncate(message, 240);
        _notifyIcon.ShowBalloonTip(4000);
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..(maximumLength - 1)] + "…";
    }
}
