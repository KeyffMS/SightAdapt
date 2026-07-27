namespace SightAdapt;

internal readonly record struct OverlayTargetAvailability(
    bool Exists,
    bool IsAvailable);

internal interface IOverlayTargetAvailability
{
    OverlayTargetAvailability Evaluate(nint targetWindow);
}

internal sealed class ForegroundOverlayTargetAvailability :
    IOverlayTargetAvailability
{
    private readonly INativeWindowApi _windowApi;

    public ForegroundOverlayTargetAvailability()
        : this(NativeWindowApi.Default)
    {
    }

    internal ForegroundOverlayTargetAvailability(
        INativeWindowApi windowApi)
    {
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
    }

    public OverlayTargetAvailability Evaluate(
        nint targetWindow)
    {
        var exists = _windowApi.IsWindow(targetWindow);
        if (!exists ||
            !_windowApi.IsWindowVisible(targetWindow) ||
            _windowApi.IsMinimized(targetWindow))
        {
            return new OverlayTargetAvailability(
                exists,
                IsAvailable: false);
        }

        var foreground = _windowApi.GetRootWindow(
            _windowApi.GetForegroundWindow());
        if (foreground == targetWindow)
        {
            return new OverlayTargetAvailability(
                Exists: true,
                IsAvailable: true);
        }

        if (foreground == nint.Zero ||
            !Win32MenuWindowPolicy.IsPopupMenuClass(
                _windowApi.GetWindowClass(foreground)))
        {
            return new OverlayTargetAvailability(
                Exists: true,
                IsAvailable: false);
        }

        var targetThreadId =
            _windowApi.GetWindowThreadProcessId(
                targetWindow,
                out var targetProcessId);
        var foregroundThreadId =
            _windowApi.GetWindowThreadProcessId(
                foreground,
                out var foregroundProcessId);
        var associated = Win32MenuWindowPolicy
            .IsAssociatedWithTarget(
                targetThreadId,
                targetProcessId,
                foregroundThreadId,
                foregroundProcessId);
        return new OverlayTargetAvailability(
            Exists: true,
            IsAvailable: associated);
    }
}

internal sealed class PopupOverlayTargetAvailability :
    IOverlayTargetAvailability
{
    private readonly INativeWindowApi _windowApi;

    public PopupOverlayTargetAvailability()
        : this(NativeWindowApi.Default)
    {
    }

    internal PopupOverlayTargetAvailability(
        INativeWindowApi windowApi)
    {
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
    }

    public OverlayTargetAvailability Evaluate(
        nint targetWindow)
    {
        var exists = _windowApi.IsWindow(targetWindow);
        return new OverlayTargetAvailability(
            exists,
            exists &&
            _windowApi.IsWindowVisible(targetWindow) &&
            !_windowApi.IsMinimized(targetWindow) &&
            Win32MenuWindowPolicy.IsPopupMenuClass(
                _windowApi.GetWindowClass(targetWindow)));
    }
}

internal interface IOverlayGeometryResolver
{
    bool TryResolve(
        nint targetWindow,
        OverlayScope scope,
        out OverlayGeometry geometry);
}

internal sealed class OverlayGeometryResolver :
    IOverlayGeometryResolver
{
    private OverlayGeometryResolver()
    {
    }

    public static OverlayGeometryResolver Default { get; } = new();

    public bool TryResolve(
        nint targetWindow,
        OverlayScope scope,
        out OverlayGeometry geometry)
    {
        return OverlayBoundsResolver.TryResolve(
            targetWindow,
            scope,
            out geometry);
    }
}

internal readonly record struct MagnifierFrameRequest(
    nint OverlayWindow,
    nint MagnifierWindow,
    nint TargetWindow,
    OverlayScope Scope,
    bool PreservePopupZOrder);

internal sealed class MagnifierFrameRenderer
{
    private readonly INativeWindowApi _windowApi;
    private readonly INativeMagnificationApi _magnificationApi;
    private readonly IOverlayGeometryResolver _geometryResolver;

    public MagnifierFrameRenderer()
        : this(
            NativeWindowApi.Default,
            NativeMagnificationApi.Default,
            OverlayGeometryResolver.Default)
    {
    }

    internal MagnifierFrameRenderer(
        INativeWindowApi windowApi,
        INativeMagnificationApi magnificationApi,
        IOverlayGeometryResolver geometryResolver)
    {
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
        _magnificationApi = magnificationApi ??
            throw new ArgumentNullException(
                nameof(magnificationApi));
        _geometryResolver = geometryResolver ??
            throw new ArgumentNullException(
                nameof(geometryResolver));
    }

    public bool TryRender(
        MagnifierFrameRequest request)
    {
        if (!_geometryResolver.TryResolve(
                request.TargetWindow,
                request.Scope,
                out var geometry))
        {
            return false;
        }

        var destination = geometry.Destination;
        var positionFlags =
            NativeConstants.SwpNoActivate |
            NativeConstants.SwpShowWindow;
        var insertAfter = NativeConstants.HwndTopMost;
        if (request.PreservePopupZOrder)
        {
            positionFlags |= NativeConstants.SwpNoZOrder;
            insertAfter = nint.Zero;
        }

        if (!NativeCall.TryTransient(
                _windowApi.SetWindowPosition(
                    request.OverlayWindow,
                    insertAfter,
                    destination.Left,
                    destination.Top,
                    destination.Width,
                    destination.Height,
                    positionFlags),
                "Position overlay window"))
        {
            return false;
        }

        if (!NativeCall.TryTransient(
                _windowApi.SetWindowPosition(
                    request.MagnifierWindow,
                    nint.Zero,
                    0,
                    0,
                    destination.Width,
                    destination.Height,
                    NativeConstants.SwpNoActivate |
                        NativeConstants.SwpNoZOrder),
                "Resize magnifier control"))
        {
            return false;
        }

        if (!NativeCall.TryTransient(
                _magnificationApi.SetWindowSource(
                    request.MagnifierWindow,
                    geometry.Source),
                "Set magnifier source rectangle"))
        {
            return false;
        }

        _ = _windowApi.Invalidate(
            request.MagnifierWindow,
            erase: true);
        return true;
    }
}
