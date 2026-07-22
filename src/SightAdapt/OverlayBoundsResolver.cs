using System.ComponentModel;
using System.Runtime.InteropServices;

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
            !NativeMethods.IsWindow(targetWindow) ||
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
        if (!GetClientRect(targetWindow, out var clientRect))
        {
            return false;
        }

        var topLeft = new NativePoint(
            clientRect.Left,
            clientRect.Top);
        var bottomRight = new NativePoint(
            clientRect.Right,
            clientRect.Bottom);

        if (!ClientToScreen(targetWindow, ref topLeft) ||
            !ClientToScreen(targetWindow, ref bottomRight))
        {
            return false;
        }

        return TryCreateGeometry(
            new Rect
            {
                Left = topLeft.X,
                Top = topLeft.Y,
                Right = bottomRight.X,
                Bottom = bottomRight.Y,
            },
            out geometry);
    }

    private static bool TryResolveWindow(
        nint targetWindow,
        out OverlayGeometry geometry)
    {
        geometry = default;
        return NativeMethods.TryGetVisibleWindowBounds(
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

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(
        nint window,
        out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(
        nint window,
        ref NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }
}
