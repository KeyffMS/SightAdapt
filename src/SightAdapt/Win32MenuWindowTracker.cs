using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SightAdapt;

internal sealed class Win32MenuWindowsChangedEventArgs(
    IReadOnlyList<nint> windows) : EventArgs
{
    public IReadOnlyList<nint> Windows { get; } =
        windows?.ToArray() ??
        throw new ArgumentNullException(
            nameof(windows));
}

internal interface IWin32MenuWindowTracker : IDisposable
{
    event EventHandler<Win32MenuWindowsChangedEventArgs>? Changed;

    void Start(nint targetWindow);

    void Stop();

    void Refresh();
}

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
}

internal sealed class Win32MenuWindowTracker :
    IWin32MenuWindowTracker
{
    internal const int DefaultIntervalMilliseconds = 75;

    private readonly System.Windows.Forms.Timer _timer;
    private readonly MenuEventMessageWindow _messageWindow;
    private readonly NativeMenuMethods.WinEventDelegate
        _winEventCallback;

    private nint _targetWindow;
    private nint _winEventHook;
    private nint[] _lastPublishedWindows = [];
    private bool _disposed;

    public Win32MenuWindowTracker(
        int intervalMilliseconds =
            DefaultIntervalMilliseconds)
    {
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
        _messageWindow =
            new MenuEventMessageWindow(Refresh);
        _winEventCallback = WinEventCallback;
    }

    public event EventHandler<
        Win32MenuWindowsChangedEventArgs>? Changed;

    public void Start(nint targetWindow)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);

        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException(
                "A target window is required.",
                nameof(targetWindow));
        }

        _targetWindow = targetWindow;
        _lastPublishedWindows = [];
        EnsureWinEventHook();
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
        _targetWindow = nint.Zero;
        _lastPublishedWindows = [];
        RemoveWinEventHook();
    }

    public void Refresh()
    {
        if (_disposed ||
            _targetWindow == nint.Zero)
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

        if (!TryEnumerateMenuWindows(
                targetThreadId,
                targetProcessId,
                out var windows))
        {
            return;
        }

        Publish(windows);
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
        _messageWindow.Dispose();
        _disposed = true;
    }

    internal static bool HaveSameWindowSet(
        IReadOnlyCollection<nint> first,
        IReadOnlyCollection<nint> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        return first.Count == second.Count &&
               first.ToHashSet().SetEquals(second);
    }

    private void TimerTick(
        object? sender,
        EventArgs eventArgs)
    {
        Refresh();
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

        if (!_disposed &&
            _targetWindow != nint.Zero)
        {
            _messageWindow.RequestRefresh();
        }
    }

    private void EnsureWinEventHook()
    {
        if (_winEventHook != nint.Zero)
        {
            return;
        }

        _winEventHook =
            NativeMenuMethods.SetWinEventHook(
                NativeMenuMethods.EventSystemMenuStart,
                NativeMenuMethods.EventSystemMenuPopupEnd,
                nint.Zero,
                _winEventCallback,
                0,
                0,
                NativeMenuMethods.WinEventOutOfContext |
                    NativeMenuMethods.WinEventSkipOwnProcess);

        if (_winEventHook == nint.Zero)
        {
            Debug.WriteLine(
                "SightAdapt native menu tracking: " +
                NativeCall.FormatFailure(
                    "Install WinEvent menu hook",
                    Marshal.GetLastWin32Error()));
        }
    }

    private void RemoveWinEventHook()
    {
        if (_winEventHook == nint.Zero)
        {
            return;
        }

        NativeCall.BestEffort(
            NativeMenuMethods.UnhookWinEvent(
                _winEventHook),
            "Remove WinEvent menu hook");
        _winEventHook = nint.Zero;
    }

    private bool TryEnumerateMenuWindows(
        uint targetThreadId,
        uint targetProcessId,
        out nint[] windows)
    {
        var candidates = new List<nint>();
        var succeeded = NativeMenuMethods.EnumWindows(
            (window, _) =>
            {
                if (IsMenuWindowCandidate(
                        window,
                        targetThreadId,
                        targetProcessId))
                {
                    candidates.Add(window);
                }

                return true;
            },
            nint.Zero);

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

    private bool IsMenuWindowCandidate(
        nint window,
        uint targetThreadId,
        uint targetProcessId)
    {
        if (window == nint.Zero ||
            window == _targetWindow ||
            !NativeMethods.IsWindow(window) ||
            !NativeMethods.IsWindowVisible(window) ||
            NativeMethods.IsIconic(window) ||
            !Win32MenuWindowPolicy.IsPopupMenuClass(
                NativeMethods.GetWindowClass(window)) ||
            !TryGetAssociation(
                window,
                out var candidateThreadId,
                out var candidateProcessId) ||
            !Win32MenuWindowPolicy
                .IsAssociatedWithTarget(
                    targetThreadId,
                    targetProcessId,
                    candidateThreadId,
                    candidateProcessId) ||
            !NativeMethods.TryGetVisibleWindowBounds(
                window,
                out var bounds) ||
            bounds.Width <= 0 ||
            bounds.Height <= 0)
        {
            return false;
        }

        return true;
    }

    private static bool IsTargetSessionForeground(
        nint targetWindow,
        uint targetThreadId,
        uint targetProcessId)
    {
        var foreground =
            NativeMethods.GetForegroundWindow();
        foreground = NativeMethods.GetAncestor(
            foreground,
            NativeMethods.GaRoot);

        if (foreground == targetWindow)
        {
            return true;
        }

        return foreground != nint.Zero &&
               Win32MenuWindowPolicy.IsPopupMenuClass(
                   NativeMethods.GetWindowClass(
                       foreground)) &&
               TryGetAssociation(
                   foreground,
                   out var foregroundThreadId,
                   out var foregroundProcessId) &&
               Win32MenuWindowPolicy
                   .IsAssociatedWithTarget(
                       targetThreadId,
                       targetProcessId,
                       foregroundThreadId,
                       foregroundProcessId);
    }

    private static bool TryGetAssociation(
        nint window,
        out uint threadId,
        out uint processId)
    {
        threadId =
            NativeMethods.GetWindowThreadProcessId(
                window,
                out processId);
        return threadId != 0 && processId != 0;
    }

    private void Publish(
        IReadOnlyList<nint> windows)
    {
        var snapshot = windows
            .Where(window => window != nint.Zero)
            .Distinct()
            .ToArray();

        if (HaveSameWindowSet(
                _lastPublishedWindows,
                snapshot))
        {
            return;
        }

        _lastPublishedWindows = snapshot;
        Changed?.Invoke(
            this,
            new Win32MenuWindowsChangedEventArgs(
                snapshot));
    }

    private sealed class MenuEventMessageWindow :
        NativeWindow,
        IDisposable
    {
        private const int RefreshMessage =
            NativeMenuMethods.WmApp + 0x47;

        private readonly Action _refresh;
        private int _refreshPending;
        private bool _disposed;

        public MenuEventMessageWindow(
            Action refresh)
        {
            _refresh = refresh ??
                throw new ArgumentNullException(
                    nameof(refresh));

            CreateHandle(new CreateParams
            {
                Caption =
                    "SightAdapt Native Menu Event Window",
                Parent = NativeMethods.HwndMessage,
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

            if (NativeMenuMethods.PostMessage(
                    Handle,
                    RefreshMessage,
                    nint.Zero,
                    nint.Zero))
            {
                return;
            }

            var errorCode =
                Marshal.GetLastWin32Error();
            Interlocked.Exchange(
                ref _refreshPending,
                0);
            Debug.WriteLine(
                "SightAdapt native menu tracking: " +
                NativeCall.FormatFailure(
                    "Post native menu refresh message",
                    errorCode));
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

    private static class NativeMenuMethods
    {
        public const uint EventSystemMenuStart =
            0x0004;
        public const uint EventSystemMenuPopupEnd =
            0x0007;
        public const uint WinEventOutOfContext =
            0x0000;
        public const uint WinEventSkipOwnProcess =
            0x0002;
        public const int WmApp = 0x8000;

        public delegate bool EnumWindowsDelegate(
            nint window,
            nint parameter);

        public delegate void WinEventDelegate(
            nint hook,
            uint eventType,
            nint window,
            int objectId,
            int childId,
            uint eventThread,
            uint eventTime);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(
            EnumWindowsDelegate callback,
            nint parameter);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        public static extern nint SetWinEventHook(
            uint eventMinimum,
            uint eventMaximum,
            nint module,
            WinEventDelegate callback,
            uint processId,
            uint threadId,
            uint flags);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWinEvent(
            nint hook);

        [DllImport(
            "user32.dll",
            SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool PostMessage(
            nint window,
            int message,
            nint wParam,
            nint lParam);
    }
}
