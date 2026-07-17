using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class SightAdaptContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _toggleItem;
    private readonly HotkeyWindow _hotkeys;
    private readonly System.Windows.Forms.Timer _foregroundTracker;

    private MagnifierOverlay? _overlay;
    private nint _lastExternalWindow;
    private bool _disposed;

    public SightAdaptContext()
    {
        _statusItem = new ToolStripMenuItem("Overlay disabled")
        {
            Enabled = false,
        };

        _toggleItem = new ToolStripMenuItem("Toggle inversion for active window");
        _toggleItem.Click += (_, _) => ToggleForActiveWindow();

        var disableItem = new ToolStripMenuItem("Disable all overlays");
        disableItem.Click += (_, _) => DisableOverlay();

        var exitItem = new ToolStripMenuItem("Exit SightAdapt Demo");
        exitItem.Click += (_, _) => ExitThread();

        var menu = new ContextMenuStrip();
        menu.Items.AddRange([
            _statusItem,
            new ToolStripSeparator(),
            _toggleItem,
            disableItem,
            new ToolStripSeparator(),
            exitItem,
        ]);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "SightAdapt Demo",
            ContextMenuStrip = menu,
            Visible = true,
        };
        _notifyIcon.DoubleClick += (_, _) => ToggleForActiveWindow();

        _hotkeys = new HotkeyWindow(HandleHotkey);

        _foregroundTracker = new System.Windows.Forms.Timer
        {
            Interval = 250,
            Enabled = true,
        };
        _foregroundTracker.Tick += (_, _) => TrackForegroundWindow();

        var toggleText = _hotkeys.ToggleShortcut ?? "tray menu";
        var emergencyText = _hotkeys.EmergencyShortcut ?? "tray menu";

        _notifyIcon.BalloonTipTitle = "SightAdapt Demo is running";
        _notifyIcon.BalloonTipText =
            $"Toggle inversion: {toggleText}. Emergency disable: {emergencyText}.";
        _notifyIcon.ShowBalloonTip(5000);
    }

    protected override void ExitThreadCore()
    {
        DisableOverlay();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            DisableOverlay();
            _foregroundTracker.Dispose();
            _hotkeys.Dispose();
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
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
        else if (id == HotkeyWindow.EmergencyId)
        {
            DisableOverlay();
            ShowNotification("All SightAdapt overlays were disabled.");
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

        if (_overlay is not null && !_overlay.IsDisposed && _overlay.TargetHandle == target)
        {
            DisableOverlay();
            return;
        }

        DisableOverlay();

        try
        {
            var overlay = new MagnifierOverlay(target);
            overlay.FormClosed += OverlayClosed;
            overlay.Show();
            _overlay = overlay;

            var title = NativeMethods.GetWindowTitle(target);
            _statusItem.Text = string.IsNullOrWhiteSpace(title)
                ? "Inversion enabled"
                : $"Inverting: {Truncate(title, 48)}";
            _toggleItem.Text = "Disable inversion";
        }
        catch (Exception exception)
        {
            DisableOverlay();
            ShowNotification($"Could not create the overlay: {exception.Message}");
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

        _statusItem.Text = "Overlay disabled";
        _toggleItem.Text = "Toggle inversion for active window";
    }

    private void OverlayClosed(object? sender, FormClosedEventArgs eventArgs)
    {
        if (ReferenceEquals(sender, _overlay))
        {
            _overlay?.Dispose();
            _overlay = null;
            _statusItem.Text = "Overlay disabled";
            _toggleItem.Text = "Toggle inversion for active window";
        }
    }

    private void TrackForegroundWindow()
    {
        var candidate = NativeMethods.GetForegroundWindow();
        candidate = NativeMethods.GetAncestor(candidate, NativeMethods.GaRoot);

        if (IsSupportedTarget(candidate))
        {
            _lastExternalWindow = candidate;
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

    private void ShowNotification(string message)
    {
        _notifyIcon.BalloonTipTitle = "SightAdapt Demo";
        _notifyIcon.BalloonTipText = message;
        _notifyIcon.ShowBalloonTip(4000);
    }

    private static string Truncate(string value, int maximumLength)
    {
        return value.Length <= maximumLength
            ? value
            : value[..(maximumLength - 1)] + "…";
    }
}
