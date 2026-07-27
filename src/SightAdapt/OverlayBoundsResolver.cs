namespace SightAdapt;

internal readonly record struct OverlayGeometry(
    Rect Destination,
    Rect Source);

internal static class OverlayBoundsResolver
{
    public static bool TryResolve(
        nint targetWindow,
        OverlayScope scope,
        out OverlayGeometry geometry)
    {
        geometry = default;

        if (targetWindow == nint.Zero ||
            !NativeWindowApi.Default.IsWindow(targetWindow) ||
            !OverlayScopePolicy.IsSupported(scope))
        {
            return false;
        }

        return scope switch
        {
            OverlayScope.ClientArea =>
                TryResolveClientArea(targetWindow, out geometry),
            OverlayScope.Window =>
                TryResolveWindow(targetWindow, out geometry),
            OverlayScope.Screen =>
                TryResolveScreen(targetWindow, out geometry),
            OverlayScope.AllScreens =>
                TryResolveAllScreens(out geometry),
            _ => false,
        };
    }

    private static bool TryResolveClientArea(
        nint targetWindow,
        out OverlayGeometry geometry)
    {
        geometry = default;
        return NativeWindowApi.Default.TryGetClientBounds(
                   targetWindow,
                   out var bounds) &&
               TryCreateGeometry(bounds, out geometry);
    }

    private static bool TryResolveWindow(
        nint targetWindow,
        out OverlayGeometry geometry)
    {
        geometry = default;
        return NativeWindowApi.Default.TryGetVisibleWindowBounds(
                   targetWindow,
                   out var bounds) &&
               TryCreateGeometry(bounds, out geometry);
    }

    private static bool TryResolveScreen(
        nint targetWindow,
        out OverlayGeometry geometry)
    {
        return TryCreateGeometry(
            ToRect(Screen.FromHandle(targetWindow).Bounds),
            out geometry);
    }

    private static bool TryResolveAllScreens(
        out OverlayGeometry geometry)
    {
        return TryCreateGeometry(
            ToRect(SystemInformation.VirtualScreen),
            out geometry);
    }

    private static bool TryCreateGeometry(
        Rect bounds,
        out OverlayGeometry geometry)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            geometry = default;
            return false;
        }

        geometry = new OverlayGeometry(bounds, bounds);
        return true;
    }

    private static Rect ToRect(Rectangle rectangle)
    {
        return new Rect
        {
            Left = rectangle.Left,
            Top = rectangle.Top,
            Right = rectangle.Right,
            Bottom = rectangle.Bottom,
        };
    }

}
