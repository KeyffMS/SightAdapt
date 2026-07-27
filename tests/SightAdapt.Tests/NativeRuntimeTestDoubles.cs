namespace SightAdapt.Tests;

internal sealed class FakeNativeWindowApi : INativeWindowApi
{
    private readonly Dictionary<nint, WindowState> _windows = [];
    private nint _nextHandle = (nint)10000;

    public nint ForegroundWindow { get; set; }

    public bool PositionSucceeds { get; set; } = true;

    public bool LayeredOpacitySucceeds { get; set; } = true;

    public bool RegisterHotKeySucceeds { get; set; } = true;

    public bool UnregisterHotKeySucceeds { get; set; } = true;

    public List<PositionCall> PositionCalls { get; } = [];

    public List<(nint Window, int Command)> ShowCalls { get; } = [];

    public List<nint> InvalidatedWindows { get; } = [];

    public void SetWindow(
        nint window,
        bool exists = true,
        bool visible = true,
        bool minimized = false,
        string windowClass = "ApplicationWindow",
        uint threadId = 10,
        uint processId = 20,
        nint? rootWindow = null,
        Rect? visibleBounds = null,
        Rect? clientBounds = null)
    {
        _windows[window] = new WindowState(
            exists,
            visible,
            minimized,
            windowClass,
            threadId,
            processId,
            rootWindow ?? window,
            visibleBounds ?? CreateRect(0, 0, 100, 100),
            clientBounds ?? CreateRect(5, 5, 95, 95));
    }

    public nint GetForegroundWindow() => ForegroundWindow;

    public nint GetRootWindow(nint window) =>
        Get(window).RootWindow;

    public bool IsWindow(nint window) => Get(window).Exists;

    public bool IsWindowVisible(nint window) => Get(window).Visible;

    public bool IsMinimized(nint window) => Get(window).Minimized;

    public uint GetWindowThreadProcessId(
        nint window,
        out uint processId)
    {
        var state = Get(window);
        processId = state.ProcessId;
        return state.ThreadId;
    }

    public string GetWindowTitle(nint window) => $"Window {window}";

    public string GetWindowClass(nint window) => Get(window).WindowClass;

    public bool TryGetVisibleWindowBounds(
        nint window,
        out Rect rect)
    {
        var state = Get(window);
        rect = state.VisibleBounds;
        return state.Exists;
    }

    public bool TryGetClientBounds(
        nint window,
        out Rect rect)
    {
        var state = Get(window);
        rect = state.ClientBounds;
        return state.Exists;
    }

    public nint CreateWindow(
        int extendedStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        nint parent)
    {
        var handle = _nextHandle++;
        SetWindow(handle);
        return handle;
    }

    public bool DestroyWindow(nint window)
    {
        if (_windows.TryGetValue(window, out var state))
        {
            _windows[window] = state with { Exists = false };
        }

        return true;
    }

    public bool SetWindowPosition(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags)
    {
        PositionCalls.Add(new PositionCall(
            window,
            insertAfter,
            x,
            y,
            width,
            height,
            flags));
        return PositionSucceeds;
    }

    public bool SetLayeredOpacity(nint window, byte alpha) =>
        LayeredOpacitySucceeds;

    public bool ShowWindow(nint window, int command)
    {
        ShowCalls.Add((window, command));
        return true;
    }

    public bool Invalidate(nint window, bool erase)
    {
        InvalidatedWindows.Add(window);
        return true;
    }

    public bool RegisterHotKey(
        nint window,
        int id,
        uint modifiers,
        uint key) => RegisterHotKeySucceeds;

    public bool UnregisterHotKey(nint window, int id) =>
        UnregisterHotKeySucceeds;

    private WindowState Get(nint window)
    {
        return _windows.TryGetValue(window, out var state)
            ? state
            : new WindowState(
                Exists: false,
                Visible: false,
                Minimized: false,
                WindowClass: string.Empty,
                ThreadId: 0,
                ProcessId: 0,
                RootWindow: nint.Zero,
                VisibleBounds: default,
                ClientBounds: default);
    }

    private static Rect CreateRect(
        int left,
        int top,
        int right,
        int bottom)
    {
        return new Rect
        {
            Left = left,
            Top = top,
            Right = right,
            Bottom = bottom,
        };
    }

    internal sealed record WindowState(
        bool Exists,
        bool Visible,
        bool Minimized,
        string WindowClass,
        uint ThreadId,
        uint ProcessId,
        nint RootWindow,
        Rect VisibleBounds,
        Rect ClientBounds);

    internal sealed record PositionCall(
        nint Window,
        nint InsertAfter,
        int X,
        int Y,
        int Width,
        int Height,
        uint Flags);
}

internal sealed class FakeNativeMagnificationApi : INativeMagnificationApi
{
    public bool SourceSucceeds { get; set; } = true;

    public List<(nint Window, Rect Source)> SourceCalls { get; } = [];

    public bool Initialize() => true;

    public bool Uninitialize() => true;

    public bool SetWindowSource(nint window, Rect source)
    {
        SourceCalls.Add((window, source));
        return SourceSucceeds;
    }

    public bool SetWindowTransform(
        nint window,
        ref MagTransform transform) => true;

    public bool SetColorEffect(
        nint window,
        ref MagColorEffect effect) => true;

    public bool SetWindowFilterList(
        nint window,
        uint filterMode,
        int count,
        nint[] windows) => true;
}

internal sealed class FakeOverlayGeometryResolver : IOverlayGeometryResolver
{
    public bool Succeeds { get; set; } = true;

    public OverlayGeometry Geometry { get; set; } = new(
        new Rect
        {
            Left = 10,
            Top = 20,
            Right = 110,
            Bottom = 220,
        },
        new Rect
        {
            Left = 30,
            Top = 40,
            Right = 130,
            Bottom = 240,
        });

    public bool TryResolve(
        nint targetWindow,
        OverlayScope scope,
        out OverlayGeometry geometry)
    {
        geometry = Geometry;
        return Succeeds;
    }
}

internal sealed class RecordingDiagnosticSink : IDiagnosticSink
{
    public List<DiagnosticEvent> Events { get; } = [];

    public void Write(DiagnosticEvent diagnosticEvent)
    {
        Events.Add(diagnosticEvent);
    }
}

internal sealed class FakeMenuWindowTracker : IWin32MenuWindowTracker
{
    public event EventHandler<Win32MenuWindowsChangedEventArgs>? Changed;

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int DisposeCount { get; private set; }

    public nint TargetWindow { get; private set; }

    public void Start(nint targetWindow)
    {
        StartCount++;
        TargetWindow = targetWindow;
    }

    public void Stop()
    {
        StopCount++;
        TargetWindow = nint.Zero;
    }

    public void Refresh()
    {
    }

    public void Raise(params nint[] windows)
    {
        Changed?.Invoke(
            this,
            new Win32MenuWindowsChangedEventArgs(windows));
    }

    public void Dispose()
    {
        DisposeCount++;
    }
}

internal sealed class FakeOverlayWindowFactory : IOverlayWindowFactory
{
    private nint _nextHandle = (nint)5000;

    public List<FakeOverlayWindow> Created { get; } = [];

    public bool FailNextMenuFilter { get; set; }

    public IOverlayWindow Create(
        nint targetWindow,
        ResolvedVisualEffect effect,
        OverlayScope scope,
        MagnifierOverlayTargetKind targetKind)
    {
        var failFilter =
            targetKind == MagnifierOverlayTargetKind.TransientPopup &&
            FailNextMenuFilter;
        if (targetKind == MagnifierOverlayTargetKind.TransientPopup)
        {
            FailNextMenuFilter = false;
        }

        var window = new FakeOverlayWindow(
            _nextHandle++,
            targetWindow,
            effect,
            scope,
            targetKind)
        {
            ThrowOnSetExcludedWindows = failFilter,
        };
        Created.Add(window);
        return window;
    }
}

internal sealed class FakeOverlayWindow : IOverlayWindow
{
    private bool _disposed;

    public FakeOverlayWindow(
        nint handle,
        nint targetHandle,
        ResolvedVisualEffect effect,
        OverlayScope scope,
        MagnifierOverlayTargetKind targetKind)
    {
        Handle = handle;
        TargetHandle = targetHandle;
        Effect = effect;
        Scope = scope;
        TargetKind = targetKind;
    }

    public event EventHandler? Closed;

    public nint Handle { get; }

    public nint TargetHandle { get; private set; }

    public bool IsDisposed => _disposed;

    public ResolvedVisualEffect Effect { get; private set; }

    public OverlayScope Scope { get; private set; }

    public MagnifierOverlayTargetKind TargetKind { get; }

    public IOverlayWindow? Owner { get; private set; }

    public int ShowCount { get; private set; }

    public int CloseCount { get; private set; }

    public int DisposeCount { get; private set; }

    public int RetargetCount { get; private set; }

    public bool ThrowOnSetExcludedWindows { get; set; }

    public IReadOnlyList<nint> ExcludedWindows { get; private set; } = [];

    public void SetOwner(IOverlayWindow? owner)
    {
        Owner = owner;
    }

    public void Show()
    {
        ShowCount++;
    }

    public void Close()
    {
        CloseCount++;
    }

    public void Retarget(
        nint targetWindow,
        ResolvedVisualEffect effect,
        OverlayScope scope)
    {
        TargetHandle = targetWindow;
        Effect = effect;
        Scope = scope;
        RetargetCount++;
    }

    public void SetExcludedWindows(IEnumerable<nint> windows)
    {
        if (ThrowOnSetExcludedWindows)
        {
            throw new InvalidOperationException("filter failure");
        }

        ExcludedWindows = windows.ToArray();
    }

    public void RaiseClosed()
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        DisposeCount++;
        _disposed = true;
    }
}

internal sealed class FakeMenuRefreshSignalSource : IMenuRefreshSignalSource
{
    public event EventHandler? RefreshRequested;

    public int StartCount { get; private set; }

    public int StopCount { get; private set; }

    public int DisposeCount { get; private set; }

    public void Start()
    {
        StartCount++;
    }

    public void Stop()
    {
        StopCount++;
    }

    public void Raise()
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        DisposeCount++;
    }
}

internal sealed class FakeMenuWindowEnumerator : IMenuWindowEnumerator
{
    public int CallCount { get; private set; }

    public bool Succeeds { get; set; } = true;

    public nint[] Windows { get; set; } = [];

    public bool TryEnumerate(
        nint targetWindow,
        uint targetThreadId,
        uint targetProcessId,
        out nint[] windows)
    {
        CallCount++;
        windows = Windows.ToArray();
        return Succeeds;
    }
}
