namespace SightAdapt;

internal sealed class ForegroundWindowChangedEventArgs(nint window) : EventArgs
{
    public nint Window { get; } = window;
}

internal sealed class ForegroundWindowTracker : IDisposable
{
    internal const int DefaultIntervalMilliseconds = 75;

    private readonly System.Windows.Forms.Timer _timer;
    private nint _lastExternalWindow;
    private nint _lastPublishedWindow;
    private bool _disposed;

    public ForegroundWindowTracker(
        int intervalMilliseconds = DefaultIntervalMilliseconds)
    {
        if (intervalMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(intervalMilliseconds));
        }

        _timer = new System.Windows.Forms.Timer
        {
            Interval = intervalMilliseconds,
        };
        _timer.Tick += TimerTick;
    }

    public event EventHandler<ForegroundWindowChangedEventArgs>? Changed;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _timer.Start();
    }

    public nint ResolveTargetWindow()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var foreground = NormalizeTopLevelWindow(
            NativeMethods.GetForegroundWindow());
        if (IsSupportedTarget(foreground))
        {
            _lastExternalWindow = foreground;
            return foreground;
        }

        return IsSupportedTarget(_lastExternalWindow)
            ? _lastExternalWindow
            : nint.Zero;
    }

    public ApplicationIdentity? GetCurrentApplicationIdentity()
    {
        var target = ResolveTargetWindow();
        return target != nint.Zero &&
            ApplicationDiscovery.TryGetIdentity(target, out var identity)
                ? identity
                : null;
    }

    public static bool IsSupportedTarget(nint window)
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

    internal bool ShouldPublish(nint candidate)
    {
        if (candidate == _lastPublishedWindow)
        {
            return false;
        }

        _lastPublishedWindow = candidate;
        return true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Stop();
        _timer.Tick -= TimerTick;
        _timer.Dispose();
        _disposed = true;
    }

    private void TimerTick(object? sender, EventArgs eventArgs)
    {
        var candidate = NormalizeTopLevelWindow(
            NativeMethods.GetForegroundWindow());
        if (!IsSupportedTarget(candidate))
        {
            return;
        }

        _lastExternalWindow = candidate;
        if (!ShouldPublish(candidate))
        {
            return;
        }

        Changed?.Invoke(
            this,
            new ForegroundWindowChangedEventArgs(candidate));
    }

    private static nint NormalizeTopLevelWindow(nint window)
    {
        return NativeMethods.GetAncestor(window, NativeMethods.GaRoot);
    }
}
