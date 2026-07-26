namespace SightAdapt;

internal sealed class ForegroundWindowChangedEventArgs(nint window) : EventArgs
{
    public nint Window { get; } = window;
}

internal sealed class ForegroundWindowTracker : IDisposable
{
    internal static int DefaultIntervalMilliseconds =>
        RuntimeTimingPolicy.Default.ForegroundPollMilliseconds;

    private readonly System.Windows.Forms.Timer _timer;
    private readonly INativeWindowApi _windowApi;
    private nint _lastExternalWindow;
    private nint _lastPublishedWindow;
    private bool _disposed;

    public ForegroundWindowTracker(
        int? intervalMilliseconds = null)
        : this(
            NativeWindowApi.Default,
            intervalMilliseconds ??
                RuntimeTimingPolicy.Default.ForegroundPollMilliseconds)
    {
    }

    internal ForegroundWindowTracker(
        INativeWindowApi windowApi,
        int intervalMilliseconds)
    {
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
        if (intervalMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalMilliseconds));
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
            _windowApi,
            _windowApi.GetForegroundWindow());
        if (IsSupportedTarget(foreground, _windowApi))
        {
            _lastExternalWindow = foreground;
            return foreground;
        }

        return IsSupportedTarget(
                _lastExternalWindow,
                _windowApi)
            ? _lastExternalWindow
            : nint.Zero;
    }

    public ApplicationIdentity? GetCurrentApplicationIdentity()
    {
        var target = ResolveTargetWindow();
        return target != nint.Zero &&
            ApplicationDiscovery.TryGetIdentity(
                target,
                out var identity)
                ? identity
                : null;
    }

    public static bool IsSupportedTarget(nint window)
    {
        return IsSupportedTarget(
            window,
            NativeWindowApi.Default);
    }

    internal static bool IsSupportedTarget(
        nint window,
        INativeWindowApi windowApi)
    {
        ArgumentNullException.ThrowIfNull(windowApi);

        if (window == nint.Zero ||
            !windowApi.IsWindow(window) ||
            !windowApi.IsWindowVisible(window) ||
            windowApi.IsMinimized(window))
        {
            return false;
        }

        windowApi.GetWindowThreadProcessId(
            window,
            out var processId);
        if (processId == (uint)Environment.ProcessId)
        {
            return false;
        }

        var windowClass = windowApi.GetWindowClass(window);
        if (Win32MenuWindowPolicy.IsPopupMenuClass(
                windowClass))
        {
            return false;
        }

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

    private void TimerTick(
        object? sender,
        EventArgs eventArgs)
    {
        var candidate = NormalizeTopLevelWindow(
            _windowApi,
            _windowApi.GetForegroundWindow());
        if (!IsSupportedTarget(candidate, _windowApi))
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

    private static nint NormalizeTopLevelWindow(
        INativeWindowApi windowApi,
        nint window)
    {
        return windowApi.GetRootWindow(window);
    }
}
