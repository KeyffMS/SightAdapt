namespace SightAdapt;

internal readonly record struct ResolvedVisualEffect
{
    public ResolvedVisualEffect(
        string transformId,
        MagColorEffect colorEffect)
    {
        TransformId = !string.IsNullOrWhiteSpace(transformId)
            ? transformId.Trim()
            : throw new ArgumentException(
                "A transform identifier is required.",
                nameof(transformId));
        ColorEffect = colorEffect;
    }

    public string TransformId { get; }

    public MagColorEffect ColorEffect { get; }
}

internal interface IOverlayWindow : IDisposable
{
    event EventHandler? Closed;

    nint Handle { get; }

    nint TargetHandle { get; }

    bool IsDisposed { get; }

    void SetOwner(IOverlayWindow? owner);

    void Show();

    void Close();

    void Retarget(
        nint targetWindow,
        ResolvedVisualEffect effect,
        OverlayScope scope);

    void SetExcludedWindows(IEnumerable<nint> windows);
}

internal interface IOverlayWindowFactory
{
    IOverlayWindow Create(
        nint targetWindow,
        ResolvedVisualEffect effect,
        OverlayScope scope,
        MagnifierOverlayTargetKind targetKind);
}

internal sealed class MagnifierOverlayWindowFactory :
    IOverlayWindowFactory
{
    public IOverlayWindow Create(
        nint targetWindow,
        ResolvedVisualEffect effect,
        OverlayScope scope,
        MagnifierOverlayTargetKind targetKind)
    {
        return new MagnifierOverlayWindow(
            new MagnifierOverlay(
                targetWindow,
                effect.ColorEffect,
                effect.TransformId,
                scope,
                targetKind));
    }
}

internal sealed class MagnifierOverlayWindow : IOverlayWindow
{
    private readonly MagnifierOverlay _overlay;
    private bool _disposed;

    public MagnifierOverlayWindow(
        MagnifierOverlay overlay)
    {
        _overlay = overlay ??
            throw new ArgumentNullException(nameof(overlay));
        _overlay.FormClosed += OverlayFormClosed;
    }

    public event EventHandler? Closed;

    public nint Handle => _overlay.Handle;

    public nint TargetHandle => _overlay.TargetHandle;

    public bool IsDisposed => _overlay.IsDisposed;

    public void SetOwner(IOverlayWindow? owner)
    {
        _overlay.Owner = owner is MagnifierOverlayWindow adapter
            ? adapter._overlay
            : null;
    }

    public void Show()
    {
        _overlay.Show();
    }

    public void Close()
    {
        _overlay.Close();
    }

    public void Retarget(
        nint targetWindow,
        ResolvedVisualEffect effect,
        OverlayScope scope)
    {
        _overlay.Retarget(
            targetWindow,
            effect.ColorEffect,
            effect.TransformId,
            scope);
    }

    public void SetExcludedWindows(
        IEnumerable<nint> windows)
    {
        _overlay.SetExcludedWindows(windows);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _overlay.FormClosed -= OverlayFormClosed;
        _overlay.Dispose();
        _disposed = true;
    }

    private void OverlayFormClosed(
        object? sender,
        FormClosedEventArgs eventArgs)
    {
        Closed?.Invoke(this, EventArgs.Empty);
    }
}

internal sealed class OverlaySession : IDisposable
{
    private readonly IWin32MenuWindowTracker _menuWindowTracker;
    private readonly IOverlayWindowFactory _windowFactory;
    private readonly INativeWindowApi _windowApi;
    private readonly Dictionary<nint, IOverlayWindow>
        _menuWindows = [];
    private readonly Dictionary<IOverlayWindow, nint>
        _menuTargets = new(ReferenceEqualityComparer.Instance);

    private IOverlayWindow? _primaryWindow;
    private ResolvedVisualEffect _menuEffect;
    private bool _synchronizingMenuWindows;
    private bool _disposed;

    public OverlaySession(
        IWin32MenuWindowTracker menuWindowTracker,
        IOverlayWindowFactory windowFactory)
        : this(
            menuWindowTracker,
            windowFactory,
            NativeWindowApi.Default)
    {
    }

    internal OverlaySession(
        IWin32MenuWindowTracker menuWindowTracker,
        IOverlayWindowFactory windowFactory,
        INativeWindowApi windowApi)
    {
        _menuWindowTracker = menuWindowTracker ??
            throw new ArgumentNullException(
                nameof(menuWindowTracker));
        _windowFactory = windowFactory ??
            throw new ArgumentNullException(nameof(windowFactory));
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
        _menuWindowTracker.Changed += MenuWindowsChanged;
    }

    public event EventHandler? Closed;

    public bool IsActive =>
        _primaryWindow is { IsDisposed: false };

    public nint TargetWindow =>
        IsActive
            ? _primaryWindow!.TargetHandle
            : nint.Zero;

    internal int MenuWindowCount => _menuWindows.Count;

    public void Activate(
        nint targetWindow,
        ResolvedVisualEffect primaryEffect,
        ResolvedVisualEffect menuEffect,
        OverlayScope scope)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException(
                "A target window is required.",
                nameof(targetWindow));
        }

        if (!OverlayScopePolicy.IsSupported(scope))
        {
            throw new ArgumentOutOfRangeException(nameof(scope));
        }

        if (IsActive)
        {
            ActivateExisting(
                targetWindow,
                primaryEffect,
                menuEffect,
                scope);
            return;
        }

        ActivateNew(
            targetWindow,
            primaryEffect,
            menuEffect,
            scope);
    }

    public void Disable()
    {
        _menuWindowTracker.Stop();
        CloseMenuWindows();

        var primary = _primaryWindow;
        _primaryWindow = null;
        if (primary is null)
        {
            return;
        }

        primary.Closed -= PrimaryWindowClosed;
        if (!primary.IsDisposed)
        {
            primary.Close();
        }

        primary.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _menuWindowTracker.Changed -= MenuWindowsChanged;
        Disable();
        _menuWindowTracker.Dispose();
        _disposed = true;
    }

    private void ActivateExisting(
        nint targetWindow,
        ResolvedVisualEffect primaryEffect,
        ResolvedVisualEffect menuEffect,
        OverlayScope scope)
    {
        var targetChanged =
            _primaryWindow!.TargetHandle != targetWindow;
        if (targetChanged)
        {
            _menuWindowTracker.Stop();
            CloseMenuWindows();
        }

        _primaryWindow.Retarget(
            targetWindow,
            primaryEffect,
            scope);
        _menuEffect = menuEffect;

        if (!targetChanged)
        {
            TryMenuOperation(
                "Retarget native popup menu overlays",
                RetargetMenuWindows);
        }

        StartMenuTracking(targetWindow);
    }

    private void ActivateNew(
        nint targetWindow,
        ResolvedVisualEffect primaryEffect,
        ResolvedVisualEffect menuEffect,
        OverlayScope scope)
    {
        var primary = _windowFactory.Create(
            targetWindow,
            primaryEffect,
            scope,
            MagnifierOverlayTargetKind.ForegroundWindow);
        primary.Closed += PrimaryWindowClosed;

        try
        {
            primary.SetExcludedWindows([]);
            primary.Show();
            if (primary.IsDisposed)
            {
                throw new InvalidOperationException(
                    "The overlay target became unavailable during activation.");
            }

            _primaryWindow = primary;
            _menuEffect = menuEffect;
            StartMenuTracking(targetWindow);
        }
        catch
        {
            primary.Closed -= PrimaryWindowClosed;
            primary.Dispose();
            throw;
        }
    }

    private void StartMenuTracking(nint targetWindow)
    {
        try
        {
            _menuWindowTracker.Start(targetWindow);
        }
        catch (Exception exception)
        {
            Diagnostics.Report(
                nameof(OverlaySession),
                "Start native menu tracking",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.Recovered,
                "Native menu tracking could not start; the primary correction remains active.",
                exception);
        }
    }

    private void MenuWindowsChanged(
        object? sender,
        Win32MenuWindowsChangedEventArgs eventArgs)
    {
        if (_disposed ||
            !IsActive ||
            _synchronizingMenuWindows)
        {
            return;
        }

        TryMenuOperation(
            "Synchronize native popup menu overlays",
            () => SynchronizeMenuWindows(
                eventArgs.Windows));
    }

    private void SynchronizeMenuWindows(
        IReadOnlyList<nint> menuWindows)
    {
        _synchronizingMenuWindows = true;
        try
        {
            var desiredOrder = menuWindows
                .Where(window =>
                    window != nint.Zero &&
                    _windowApi.IsWindow(window))
                .Distinct()
                .ToArray();
            var desired = desiredOrder.ToHashSet();
            var changed = false;

            foreach (var existing in
                     _menuWindows.Keys.ToArray())
            {
                if (!desired.Contains(existing))
                {
                    RemoveMenuWindow(existing);
                    changed = true;
                }
            }

            var added = new List<IOverlayWindow>();
            foreach (var menuWindow in
                     desiredOrder.Reverse())
            {
                if (_menuWindows.ContainsKey(menuWindow))
                {
                    continue;
                }

                var overlay = _windowFactory.Create(
                    menuWindow,
                    _menuEffect,
                    OverlayScope.Window,
                    MagnifierOverlayTargetKind.TransientPopup);
                overlay.SetOwner(_primaryWindow);
                overlay.Closed += MenuWindowClosed;
                _menuWindows.Add(menuWindow, overlay);
                _menuTargets.Add(overlay, menuWindow);
                added.Add(overlay);
                changed = true;
            }

            if (!changed)
            {
                return;
            }

            ApplyExclusionLists();
            foreach (var overlay in added)
            {
                if (!overlay.IsDisposed && IsActive)
                {
                    overlay.Show();
                }
            }

            ApplyExclusionLists();
        }
        finally
        {
            _synchronizingMenuWindows = false;
        }
    }

    private void RetargetMenuWindows()
    {
        var removed = false;
        foreach (var pair in _menuWindows.ToArray())
        {
            if (pair.Value.IsDisposed ||
                !_windowApi.IsWindow(pair.Key))
            {
                RemoveMenuWindow(pair.Key);
                removed = true;
                continue;
            }

            pair.Value.Retarget(
                pair.Key,
                _menuEffect,
                OverlayScope.Window);
        }

        if (removed)
        {
            ApplyExclusionLists();
        }
    }

    private void ApplyExclusionLists()
    {
        var windows = new List<IOverlayWindow>();
        if (_primaryWindow is { IsDisposed: false })
        {
            windows.Add(_primaryWindow);
        }

        windows.AddRange(
            _menuWindows.Values.Where(
                window => !window.IsDisposed));
        var handles = windows
            .Select(window => window.Handle)
            .Where(handle => handle != nint.Zero)
            .Distinct()
            .ToArray();

        foreach (var window in windows)
        {
            window.SetExcludedWindows(handles);
        }
    }

    private void CloseMenuWindows()
    {
        var previous = _synchronizingMenuWindows;
        _synchronizingMenuWindows = true;
        try
        {
            foreach (var target in
                     _menuWindows.Keys.ToArray())
            {
                RemoveMenuWindow(target);
            }
        }
        finally
        {
            _synchronizingMenuWindows = previous;
        }

        TryResetPrimaryExclusionList();
    }

    private void RemoveMenuWindow(nint targetWindow)
    {
        if (!_menuWindows.Remove(
                targetWindow,
                out var overlay))
        {
            return;
        }

        _menuTargets.Remove(overlay);
        overlay.Closed -= MenuWindowClosed;
        if (!overlay.IsDisposed)
        {
            overlay.Close();
        }

        overlay.Dispose();
    }

    private void TryResetPrimaryExclusionList()
    {
        if (_primaryWindow is not
            { IsDisposed: false } primary)
        {
            return;
        }

        try
        {
            primary.SetExcludedWindows([]);
        }
        catch (Exception exception)
        {
            Diagnostics.Report(
                nameof(OverlaySession),
                "Reset primary overlay filter",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.BestEffort,
                "The primary overlay filter could not be reset.",
                exception);
        }
    }

    private void PrimaryWindowClosed(
        object? sender,
        EventArgs eventArgs)
    {
        if (!ReferenceEquals(sender, _primaryWindow))
        {
            return;
        }

        var primary = _primaryWindow;
        _primaryWindow = null;
        _menuWindowTracker.Stop();
        CloseMenuWindows();

        if (primary is not null)
        {
            primary.Closed -= PrimaryWindowClosed;
            primary.Dispose();
        }

        Closed?.Invoke(this, EventArgs.Empty);
    }

    private void MenuWindowClosed(
        object? sender,
        EventArgs eventArgs)
    {
        if (sender is not IOverlayWindow overlay ||
            !_menuTargets.Remove(
                overlay,
                out var targetWindow))
        {
            return;
        }

        _menuWindows.Remove(targetWindow);
        overlay.Closed -= MenuWindowClosed;
        overlay.Dispose();

        if (!_synchronizingMenuWindows)
        {
            TryMenuOperation(
                "Refresh popup overlay filters",
                ApplyExclusionLists);
        }
    }

    private void TryMenuOperation(
        string operation,
        Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception)
        {
            Diagnostics.Report(
                nameof(OverlaySession),
                operation,
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.Recovered,
                "Transient menu overlays were removed; the primary correction remains active.",
                exception);
            CloseMenuWindows();
        }
    }
}
