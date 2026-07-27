using System.Runtime.InteropServices;

namespace SightAdapt;

internal sealed class Win32MenuWindowsChangedEventArgs(
    IReadOnlyList<nint> windows) : EventArgs
{
    public IReadOnlyList<nint> Windows { get; } =
        windows?.ToArray() ??
        throw new ArgumentNullException(nameof(windows));
}

internal interface IWin32MenuWindowTracker : IDisposable
{
    event EventHandler<Win32MenuWindowsChangedEventArgs>? Changed;

    void Start(nint targetWindow);

    void Stop();

    void Refresh();
}

internal readonly record struct MenuWindowCandidate(
    nint Window,
    bool Exists,
    bool Visible,
    bool Minimized,
    string WindowClass,
    uint ThreadId,
    uint ProcessId,
    Rect Bounds);

internal static class Win32MenuWindowPolicy
{
    public const string PopupMenuClassName = "#32768";

    public static bool IsPopupMenuClass(
        string? windowClass)
    {
        return string.Equals(
            windowClass,
            PopupMenuClassName,
            StringComparison.Ordinal);
    }

    public static bool IsAssociatedWithTarget(
        uint targetThreadId,
        uint targetProcessId,
        uint candidateThreadId,
        uint candidateProcessId)
    {
        if (targetThreadId == 0 ||
            targetProcessId == 0 ||
            candidateThreadId == 0 ||
            candidateProcessId == 0)
        {
            return false;
        }

        return candidateProcessId == targetProcessId ||
               candidateThreadId == targetThreadId;
    }

    public static bool IsCandidate(
        nint targetWindow,
        uint targetThreadId,
        uint targetProcessId,
        MenuWindowCandidate candidate)
    {
        return candidate.Window != nint.Zero &&
            candidate.Window != targetWindow &&
            candidate.Exists &&
            candidate.Visible &&
            !candidate.Minimized &&
            IsPopupMenuClass(candidate.WindowClass) &&
            IsAssociatedWithTarget(
                targetThreadId,
                targetProcessId,
                candidate.ThreadId,
                candidate.ProcessId) &&
            candidate.Bounds.Width > 0 &&
            candidate.Bounds.Height > 0;
    }
}

internal interface IMenuRefreshSignalSource : IDisposable
{
    event EventHandler? RefreshRequested;

    void Start();

    void Stop();
}

internal sealed class WinEventMenuRefreshSignalSource :
    IMenuRefreshSignalSource
{
    private readonly INativeMenuEventApi _nativeApi;
    private readonly MenuEventMessageWindow _messageWindow;
    private readonly WinEventCallback _callback;
    private nint _hook;
    private bool _disposed;

    public WinEventMenuRefreshSignalSource()
        : this(NativeMenuEventApi.Default)
    {
    }

    internal WinEventMenuRefreshSignalSource(
        INativeMenuEventApi nativeApi)
    {
        _nativeApi = nativeApi ??
            throw new ArgumentNullException(nameof(nativeApi));
        _messageWindow = new MenuEventMessageWindow(
            nativeApi,
            RaiseRefreshRequested);
        _callback = WinEventCallback;
    }

    public event EventHandler? RefreshRequested;

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_hook != nint.Zero)
        {
            return;
        }

        _hook = _nativeApi.InstallHook(_callback);
        if (_hook == nint.Zero)
        {
            var errorCode = Marshal.GetLastWin32Error();
            Diagnostics.Report(
                nameof(WinEventMenuRefreshSignalSource),
                "Install WinEvent menu hook",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.Recovered,
                NativeCall.FormatFailure(
                    "Install WinEvent menu hook",
                    errorCode),
                nativeErrorCode: errorCode);
        }
    }

    public void Stop()
    {
        if (_disposed || _hook == nint.Zero)
        {
            return;
        }

        NativeCall.BestEffort(
            _nativeApi.RemoveHook(_hook),
            "Remove WinEvent menu hook");
        _hook = nint.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _messageWindow.Dispose();
        _disposed = true;
    }

    private void WinEventCallback(
        nint hook,
        uint eventType,
        nint window,
        int objectId,
        int childId,
        uint eventThread,
        uint eventTime)
    {
        _ = hook;
        _ = eventType;
        _ = window;
        _ = objectId;
        _ = childId;
        _ = eventThread;
        _ = eventTime;

        if (!_disposed)
        {
            _messageWindow.RequestRefresh();
        }
    }

    private void RaiseRefreshRequested()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    private sealed class MenuEventMessageWindow :
        NativeWindow,
        IDisposable
    {
        private const int RefreshMessage =
            NativeConstants.WmApp + 0x47;

        private readonly INativeMenuEventApi _nativeApi;
        private readonly Action _refresh;
        private int _refreshPending;
        private bool _disposed;

        public MenuEventMessageWindow(
            INativeMenuEventApi nativeApi,
            Action refresh)
        {
            _nativeApi = nativeApi ??
                throw new ArgumentNullException(nameof(nativeApi));
            _refresh = refresh ??
                throw new ArgumentNullException(nameof(refresh));

            CreateHandle(new CreateParams
            {
                Caption = "SightAdapt Native Menu Event Window",
                Parent = NativeConstants.HwndMessage,
            });
        }

        public void RequestRefresh()
        {
            if (_disposed ||
                Interlocked.Exchange(
                    ref _refreshPending,
                    1) != 0)
            {
                return;
            }

            if (_nativeApi.PostMessage(
                    Handle,
                    RefreshMessage))
            {
                return;
            }

            var errorCode = Marshal.GetLastWin32Error();
            Interlocked.Exchange(
                ref _refreshPending,
                0);
            Diagnostics.Report(
                nameof(MenuEventMessageWindow),
                "Post native menu refresh message",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.Recovered,
                NativeCall.FormatFailure(
                    "Post native menu refresh message",
                    errorCode),
                nativeErrorCode: errorCode);
        }

        protected override void WndProc(
            ref Message message)
        {
            if (message.Msg == RefreshMessage)
            {
                Interlocked.Exchange(
                    ref _refreshPending,
                    0);
                _refresh();
                return;
            }

            base.WndProc(ref message);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            DestroyHandle();
            _disposed = true;
        }
    }
}

internal interface IMenuWindowEnumerator
{
    bool TryEnumerate(
        nint targetWindow,
        uint targetThreadId,
        uint targetProcessId,
        out nint[] windows);
}

internal sealed class NativeMenuWindowEnumerator :
    IMenuWindowEnumerator
{
    private readonly INativeMenuEventApi _menuApi;
    private readonly INativeWindowApi _windowApi;

    public NativeMenuWindowEnumerator()
        : this(
            NativeMenuEventApi.Default,
            NativeWindowApi.Default)
    {
    }

    internal NativeMenuWindowEnumerator(
        INativeMenuEventApi menuApi,
        INativeWindowApi windowApi)
    {
        _menuApi = menuApi ??
            throw new ArgumentNullException(nameof(menuApi));
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
    }

    public bool TryEnumerate(
        nint targetWindow,
        uint targetThreadId,
        uint targetProcessId,
        out nint[] windows)
    {
        var candidates = new List<nint>();
        var succeeded = _menuApi.EnumerateWindows(
            (window, _) =>
            {
                var candidate = ReadCandidate(window);
                if (Win32MenuWindowPolicy.IsCandidate(
                        targetWindow,
                        targetThreadId,
                        targetProcessId,
                        candidate))
                {
                    candidates.Add(window);
                }

                return true;
            });

        if (!NativeCall.TryTransient(
                succeeded,
                "Enumerate native popup menus"))
        {
            windows = [];
            return false;
        }

        windows = candidates
            .Distinct()
            .ToArray();
        return true;
    }

    private MenuWindowCandidate ReadCandidate(
        nint window)
    {
        var threadId =
            _windowApi.GetWindowThreadProcessId(
                window,
                out var processId);
        _windowApi.TryGetVisibleWindowBounds(
            window,
            out var bounds);

        return new MenuWindowCandidate(
            window,
            _windowApi.IsWindow(window),
            _windowApi.IsWindowVisible(window),
            _windowApi.IsMinimized(window),
            _windowApi.GetWindowClass(window),
            threadId,
            processId,
            bounds);
    }
}

internal sealed class MenuWindowSnapshotPublisher
{
    private nint[] _lastPublished = [];

    public bool TryUpdate(
        IReadOnlyCollection<nint> windows,
        out nint[] snapshot)
    {
        ArgumentNullException.ThrowIfNull(windows);

        snapshot = windows
            .Where(window => window != nint.Zero)
            .Distinct()
            .ToArray();
        if (HaveSameWindowSet(
                _lastPublished,
                snapshot))
        {
            return false;
        }

        _lastPublished = snapshot;
        return true;
    }

    public void Reset()
    {
        _lastPublished = [];
    }

    public static bool HaveSameWindowSet(
        IReadOnlyCollection<nint> first,
        IReadOnlyCollection<nint> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return first.Count == second.Count &&
               first.ToHashSet().SetEquals(second);
    }
}

internal sealed class Win32MenuWindowTracker :
    IWin32MenuWindowTracker
{
    internal static int DefaultIntervalMilliseconds =>
        RuntimeTimingPolicy.Default.MenuPollMilliseconds;

    private readonly System.Windows.Forms.Timer _timer;
    private readonly IMenuRefreshSignalSource _signalSource;
    private readonly IMenuWindowEnumerator _enumerator;
    private readonly INativeWindowApi _windowApi;
    private readonly MenuWindowSnapshotPublisher _publisher = new();
    private nint _targetWindow;
    private bool _disposed;

    public Win32MenuWindowTracker(
        int? intervalMilliseconds = null)
        : this(
            new WinEventMenuRefreshSignalSource(),
            new NativeMenuWindowEnumerator(),
            NativeWindowApi.Default,
            intervalMilliseconds ??
                RuntimeTimingPolicy.Default.MenuPollMilliseconds)
    {
    }

    internal Win32MenuWindowTracker(
        IMenuRefreshSignalSource signalSource,
        IMenuWindowEnumerator enumerator,
        INativeWindowApi windowApi,
        int intervalMilliseconds)
    {
        if (intervalMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(intervalMilliseconds));
        }

        _signalSource = signalSource ??
            throw new ArgumentNullException(nameof(signalSource));
        _enumerator = enumerator ??
            throw new ArgumentNullException(nameof(enumerator));
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
        _timer = new System.Windows.Forms.Timer
        {
            Interval = intervalMilliseconds,
        };
        _timer.Tick += TimerTick;
        _signalSource.RefreshRequested += SignalSourceRefreshRequested;
    }

    public event EventHandler<
        Win32MenuWindowsChangedEventArgs>? Changed;

    public void Start(nint targetWindow)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException(
                "A target window is required.",
                nameof(targetWindow));
        }

        _targetWindow = targetWindow;
        _publisher.Reset();
        _signalSource.Start();
        _timer.Start();
        Refresh();
    }

    public void Stop()
    {
        if (_disposed)
        {
            return;
        }

        _timer.Stop();
        _signalSource.Stop();
        _targetWindow = nint.Zero;
        _publisher.Reset();
    }

    public void Refresh()
    {
        if (_disposed || _targetWindow == nint.Zero)
        {
            return;
        }

        if (!TryGetAssociation(
                _targetWindow,
                out var targetThreadId,
                out var targetProcessId) ||
            !IsTargetSessionForeground(
                _targetWindow,
                targetThreadId,
                targetProcessId))
        {
            Publish([]);
            return;
        }

        if (_enumerator.TryEnumerate(
                _targetWindow,
                targetThreadId,
                targetProcessId,
                out var windows))
        {
            Publish(windows);
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _timer.Tick -= TimerTick;
        _timer.Dispose();
        _signalSource.RefreshRequested -=
            SignalSourceRefreshRequested;
        _signalSource.Dispose();
        _disposed = true;
    }

    internal static bool HaveSameWindowSet(
        IReadOnlyCollection<nint> first,
        IReadOnlyCollection<nint> second)
    {
        return MenuWindowSnapshotPublisher.HaveSameWindowSet(
            first,
            second);
    }

    private void TimerTick(
        object? sender,
        EventArgs eventArgs)
    {
        Refresh();
    }

    private void SignalSourceRefreshRequested(
        object? sender,
        EventArgs eventArgs)
    {
        Refresh();
    }

    private bool IsTargetSessionForeground(
        nint targetWindow,
        uint targetThreadId,
        uint targetProcessId)
    {
        var foreground = _windowApi.GetRootWindow(
            _windowApi.GetForegroundWindow());
        if (foreground == targetWindow)
        {
            return true;
        }

        return foreground != nint.Zero &&
            Win32MenuWindowPolicy.IsPopupMenuClass(
                _windowApi.GetWindowClass(foreground)) &&
            TryGetAssociation(
                foreground,
                out var foregroundThreadId,
                out var foregroundProcessId) &&
            Win32MenuWindowPolicy.IsAssociatedWithTarget(
                targetThreadId,
                targetProcessId,
                foregroundThreadId,
                foregroundProcessId);
    }

    private bool TryGetAssociation(
        nint window,
        out uint threadId,
        out uint processId)
    {
        threadId =
            _windowApi.GetWindowThreadProcessId(
                window,
                out processId);
        return threadId != 0 && processId != 0;
    }

    private void Publish(
        IReadOnlyCollection<nint> windows)
    {
        if (!_publisher.TryUpdate(
                windows,
                out var snapshot))
        {
            return;
        }

        Changed?.Invoke(
            this,
            new Win32MenuWindowsChangedEventArgs(
                snapshot));
    }
}
