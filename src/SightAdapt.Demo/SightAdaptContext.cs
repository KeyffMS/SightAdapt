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
    private readonly System.Windows.Forms.Timer _emergencyIconTimer;
    private readonly SettingsStore _settingsStore;
    private readonly SightAdaptSettings _settings;
    private readonly TrayIconSet _trayIcons;

    private MagnifierOverlay? _overlay;
    private ConfigurationForm? _configurationForm;
    private OverlayActivationMode _overlayMode;
    private nint _lastExternalWindow;
    private nint _automaticSuppressedWindow;
    private bool _updatingAutomaticMode;
    private bool _disposed;

    public SightAdaptContext()
    {
        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        _trayIcons = new TrayIconSet();

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

        var addApplicationItem = new ToolStripMenuItem("Add current application");
        addApplicationItem.Click += (_, _) => AddActiveApplication();
        AppTheme.StyleMenuItem(addApplicationItem);

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
            addApplicationItem,
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

        _emergencyIconTimer = new System.Windows.Forms.Timer
        {
            Interval = 5000,
        };
        _emergencyIconTimer.Tick += EmergencyIconTimerTick;

        _hotkeys = new HotkeyWindow(HandleHotkey);

        _foregroundTracker = new System.Windows.Forms.Timer
        {
            Interval = 250,
            Enabled = true,
        };
        _foregroundTracker.Tick += (_, _) => TrackForegroundWindow();

        var toggleText = _hotkeys.ToggleShortcut ?? "tray menu";
        var addText = _hotkeys.AddApplicationShortcut ?? "tray menu";
        var emergencyText = _hotkeys.EmergencyShortcut ?? "tray menu";

        _notifyIcon.BalloonTipTitle = $"{ProductInfo.DisplayName} is running";
        _notifyIcon.BalloonTipText =
            $"Toggle: {toggleText}. Add application: {addText}. Emergency disable: {emergencyText}.";
        _notifyIcon.ShowBalloonTip(5000);

        var loadWarning = _settingsStore.LastLoadWarning;
        if (!string.IsNullOrWhiteSpace(loadWarning))
        {
            ShowEmergencyState(loadWarning);
        }
    }

    protected override void ExitThreadCore()
    {
        _configurationForm?.Close();
        DisableOverlay();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            DisableOverlay();
            _configurationForm?.Dispose();
            _foregroundTracker.Dispose();
            _emergencyIconTimer.Stop();
            _emergencyIconTimer.Dispose();
            _hotkeys.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _trayIcons.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void HandleHotkey(int id)
    {
        if (id == HotkeyWindow.ToggleId)
        {
            ToggleForActiveWindow();
        }
        else if (id == HotkeyWindow.AddApplicationId)
        {
            AddActiveApplication();
        }
        else if (id == HotkeyWindow.EmergencyId)
        {
            EmergencyDisable();
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

        if (_overlay is not null &&
            !_overlay.IsDisposed &&
            _overlay.TargetHandle == target)
        {
            DisableOverlay();

            if (_settings.AutomaticMode && IsConfiguredApplication(target))
            {
                _automaticSuppressedWindow = target;
            }

            return;
        }

        _automaticSuppressedWindow = nint.Zero;
        ActivateOverlay(target, OverlayActivationMode.Manual);
    }

    private void AddActiveApplication()
    {
        var target = ResolveTargetWindow();
        if (target == nint.Zero ||
            !ApplicationDiscovery.TryGetIdentity(target, out var identity))
        {
            ShowNotification(
                "The active application's executable path could not be read. Use the configuration panel to select its .exe file.");
            return;
        }

        var profile = _settings.Applications.FirstOrDefault(
            candidate => string.Equals(
                candidate.ExecutablePath,
                identity.ExecutablePath,
                StringComparison.OrdinalIgnoreCase));

        var added = profile is null;
        if (profile is null)
        {
            profile = new ApplicationProfile();
            _settings.Applications.Add(profile);
        }

        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
        profile.Enabled = true;
        profile.Effect = "invert";

        _settings.AutomaticMode = true;
        _automaticSuppressedWindow = nint.Zero;
        HandleSettingsChanged();

        ShowNotification(
            added
                ? $"Added to automatic inversion: {identity.DisplayName} ({identity.ExecutableName})."
                : $"{identity.DisplayName} is already configured and was enabled.");
    }

    private void ActivateOverlay(nint target, OverlayActivationMode mode)
    {
        if (_overlay is not null &&
            !_overlay.IsDisposed &&
            _overlay.TargetHandle == target)
        {
            _overlayMode = mode;
            UpdateOverlayStatus(target, mode);
            return;
        }

        DisableOverlay();

        try
        {
            var overlay = new MagnifierOverlay(target);
            overlay.FormClosed += OverlayClosed;
            overlay.Show();
            _overlay = overlay;
            _overlayMode = mode;
            UpdateOverlayStatus(target, mode);
        }
        catch (Exception exception)
        {
            DisableOverlay();
            ShowEmergencyState($"Could not create the overlay: {exception.Message}");
        }
    }

    private void DisableOverlay()
    {
        if (_overlay is not null)
        {
            _overlay.FormClosed -= OverlayClosed;
            _overlay.Close();
            _overlay.Dispose();
            _overlay = null;
        }

        _overlayMode = OverlayActivationMode.None;
        _statusItem.Text = "Overlay disabled · Inactive";
        _toggleItem.Text = "Toggle inversion for active window";
        UpdateTrayIconForCurrentState();
    }

    private void OverlayClosed(object? sender, FormClosedEventArgs eventArgs)
    {
        if (ReferenceEquals(sender, _overlay))
        {
            _overlay?.Dispose();
            _overlay = null;
            _overlayMode = OverlayActivationMode.None;
            _statusItem.Text = "Overlay disabled · Inactive";
            _toggleItem.Text = "Toggle inversion for active window";
            UpdateTrayIconForCurrentState();
        }
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

        if (_overlayMode == OverlayActivationMode.Manual &&
            (_overlay is null ||
             _overlay.IsDisposed ||
             _overlay.TargetHandle != candidate))
        {
            DisableOverlay();
        }

        EvaluateAutomaticForWindow(candidate);
    }

    private void EvaluateAutomaticForWindow(nint target)
    {
        if (!_settings.AutomaticMode ||
            _overlayMode == OverlayActivationMode.Manual ||
            !IsSupportedTarget(target))
        {
            return;
        }

        if (target == _automaticSuppressedWindow)
        {
            if (_overlayMode == OverlayActivationMode.Automatic)
            {
                DisableOverlay();
            }

            return;
        }

        if (!ApplicationDiscovery.TryGetIdentity(target, out var identity))
        {
            if (_overlayMode == OverlayActivationMode.Automatic)
            {
                DisableOverlay();
            }

            return;
        }

        var shouldInvert = _settings.Applications.Any(
            profile => profile.Enabled &&
                string.Equals(profile.Effect, "invert", StringComparison.OrdinalIgnoreCase) &&
                profile.Matches(identity));

        if (shouldInvert)
        {
            ActivateOverlay(target, OverlayActivationMode.Automatic);
        }
        else if (_overlayMode == OverlayActivationMode.Automatic)
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
            _settings.Applications.Any(
                profile => profile.Enabled &&
                    string.Equals(profile.Effect, "invert", StringComparison.OrdinalIgnoreCase) &&
                    profile.Matches(identity));
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
            ShowEmergencyState($"Settings could not be saved: {exception.Message}");
        }

        _configurationForm?.RefreshProfiles();

        if (!_settings.AutomaticMode)
        {
            _automaticSuppressedWindow = nint.Zero;

            if (_overlayMode == OverlayActivationMode.Automatic)
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
        DisableOverlay();
        _statusItem.Text = "All overlays stopped · Automatic mode off";
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
            Icon = _overlay is not null ? _trayIcons.Active : _trayIcons.Inactive,
        };

        form.FormClosed += (_, _) => _configurationForm = null;
        _configurationForm = form;
        form.Show();
    }

    private void UpdateOverlayStatus(nint target, OverlayActivationMode mode)
    {
        _emergencyIconTimer.Stop();

        var title = NativeMethods.GetWindowTitle(target);
        var modeText = mode == OverlayActivationMode.Automatic
            ? "Automatic"
            : "Manual";

        _statusItem.Text = string.IsNullOrWhiteSpace(title)
            ? $"{modeText} inversion enabled · Active"
            : $"{modeText}: {Truncate(title, 34)} · Active";
        _toggleItem.Text = "Disable inversion";
        SetTrayIcon(TrayIconState.Active);
    }

    private void ShowEmergencyState(string message)
    {
        _emergencyIconTimer.Stop();
        SetTrayIcon(TrayIconState.Emergency);
        _emergencyIconTimer.Start();
        ShowNotification(message);
    }

    private void EmergencyIconTimerTick(object? sender, EventArgs eventArgs)
    {
        _emergencyIconTimer.Stop();
        UpdateTrayIconForCurrentState();
    }

    private void UpdateTrayIconForCurrentState()
    {
        if (_emergencyIconTimer.Enabled)
        {
            return;
        }

        var hasActiveOverlay = _overlay is not null && !_overlay.IsDisposed;
        SetTrayIcon(hasActiveOverlay ? TrayIconState.Active : TrayIconState.Inactive);
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

        if (state == TrayIconState.Inactive && _overlay is null)
        {
            _statusItem.Text = "Overlay disabled · Inactive";
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

    private enum OverlayActivationMode
    {
        None,
        Manual,
        Automatic,
    }
}
