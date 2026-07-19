using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class TrayPresenter : IDisposable
{
    private readonly TrayIconSet _icons;
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly ToolStripMenuItem _automaticModeItem;
    private bool _synchronizingAutomaticMode;
    private bool _disposed;

    public TrayPresenter(
        bool automaticMode,
        Action toggleForActiveWindow,
        Action toggleActiveApplicationProfile,
        Action<bool> automaticModeChanged,
        Action showConfiguration,
        Action emergencyDisable,
        Action exit)
    {
        ArgumentNullException.ThrowIfNull(toggleForActiveWindow);
        ArgumentNullException.ThrowIfNull(toggleActiveApplicationProfile);
        ArgumentNullException.ThrowIfNull(automaticModeChanged);
        ArgumentNullException.ThrowIfNull(showConfiguration);
        ArgumentNullException.ThrowIfNull(emergencyDisable);
        ArgumentNullException.ThrowIfNull(exit);

        _icons = new TrayIconSet();

        _statusItem = new ToolStripMenuItem("Overlay disabled")
        {
            Enabled = false,
        };
        AppTheme.StyleMenuItem(
            _statusItem,
            AppTheme.TextSecondary,
            FontStyle.Bold,
            "status");

        _toggleItem = new ToolStripMenuItem(
            "Toggle visual correction for active window");
        _toggleItem.Click += (_, _) => toggleForActiveWindow();
        AppTheme.StyleMenuItem(_toggleItem);

        var toggleProfileItem = new ToolStripMenuItem(
            "Toggle automatic profile for current application");
        toggleProfileItem.Click += (_, _) => toggleActiveApplicationProfile();
        AppTheme.StyleMenuItem(toggleProfileItem);

        _automaticModeItem = new ToolStripMenuItem("Automatic mode")
        {
            CheckOnClick = true,
            Checked = automaticMode,
        };
        _automaticModeItem.CheckedChanged += (_, _) =>
        {
            if (!_synchronizingAutomaticMode)
            {
                automaticModeChanged(_automaticModeItem.Checked);
            }
        };
        AppTheme.StyleMenuItem(_automaticModeItem);

        var configureItem = new ToolStripMenuItem(
            "Configure applications and colors...");
        configureItem.Click += (_, _) => showConfiguration();
        AppTheme.StyleMenuItem(
            configureItem,
            AppTheme.AccentHover,
            FontStyle.Bold);

        var disableItem = new ToolStripMenuItem(
            "Emergency disable all overlays");
        disableItem.Click += (_, _) => emergencyDisable();
        AppTheme.StyleMenuItem(
            disableItem,
            AppTheme.Danger,
            FontStyle.Bold,
            "danger");

        var exitItem = new ToolStripMenuItem("Exit SightAdapt");
        exitItem.Click += (_, _) => exit();
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
            Icon = _icons.Inactive,
            Text = $"{ProductInfo.DisplayName} · Inactive",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => toggleForActiveWindow();
    }

    public void SetAutomaticMode(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _synchronizingAutomaticMode = true;
        try
        {
            _automaticModeItem.Checked = enabled;
        }
        finally
        {
            _synchronizingAutomaticMode = false;
        }
    }

    public void ApplyState(
        ApplicationState state,
        bool automaticMode,
        string? windowTitle,
        string profileName)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(state);

        switch (state.Kind)
        {
            case ApplicationRunState.ManualActive:
                ApplyActiveState("Manual", windowTitle, profileName);
                break;
            case ApplicationRunState.AutomaticActive:
                ApplyActiveState("Automatic", windowTitle, profileName);
                break;
            case ApplicationRunState.Fault:
                _statusItem.Text = string.IsNullOrWhiteSpace(state.Message)
                    ? "Visual correction failed"
                    : Truncate(state.Message, 60);
                _toggleItem.Text = "Toggle visual correction for active window";
                SetIcon(TrayIconState.Emergency, "Attention required");
                break;
            case ApplicationRunState.Emergency:
                _statusItem.Text = automaticMode
                    ? "All overlays stopped · Automatic mode still configured"
                    : "All overlays stopped · Automatic mode off";
                _toggleItem.Text = "Resume visual correction for active window";
                SetIcon(TrayIconState.Emergency, "All overlays stopped");
                break;
            default:
                _statusItem.Text = automaticMode
                    ? "Overlay disabled · Automatic mode active"
                    : "Overlay disabled · Inactive";
                _toggleItem.Text =
                    "Toggle visual correction for active window";
                SetIcon(TrayIconState.Inactive, "Inactive");
                break;
        }
    }

    public Icon GetIcon(ApplicationRunState state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return state switch
        {
            ApplicationRunState.ManualActive or
            ApplicationRunState.AutomaticActive => _icons.Active,
            ApplicationRunState.Fault or
            ApplicationRunState.Emergency => _icons.Emergency,
            _ => _icons.Inactive,
        };
    }

    public void ShowStartup(
        string? localToggleShortcut,
        string? profileToggleShortcut)
    {
        var localToggleText = localToggleShortcut ?? "tray menu";
        var profileToggleText = profileToggleShortcut ?? "tray menu";

        _notifyIcon.BalloonTipTitle =
            $"{ProductInfo.DisplayName} is running";
        _notifyIcon.BalloonTipText =
            $"Local toggle: {localToggleText}. " +
            $"Saved profile toggle: {profileToggleText}. " +
            "Emergency disable is available from the tray menu.";
        _notifyIcon.ShowBalloonTip(5000);
    }

    public void ShowNotification(string message)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _notifyIcon.BalloonTipTitle = ProductInfo.DisplayName;
        _notifyIcon.BalloonTipText = Truncate(message, 240);
        _notifyIcon.ShowBalloonTip(4000);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
        _icons.Dispose();
        _disposed = true;
    }

    private void ApplyActiveState(
        string modeText,
        string? windowTitle,
        string profileName)
    {
        _statusItem.Text = string.IsNullOrWhiteSpace(windowTitle)
            ? $"{modeText} · {profileName} · Active"
            : $"{modeText}: {Truncate(windowTitle, 28)} · {profileName}";
        _toggleItem.Text = "Disable visual correction";
        SetIcon(TrayIconState.Active, "Active");
    }

    private void SetIcon(TrayIconState state, string stateText)
    {
        _notifyIcon.Icon = _icons.Get(state);
        _notifyIcon.Text =
            $"{ProductInfo.DisplayName} · {stateText}";
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..(maximumLength - 1)] + "…";
    }
}
