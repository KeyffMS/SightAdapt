using System.Runtime.InteropServices;
using System.Text;

namespace SightAdapt;

internal static class NativeConstants
{
    public const int WmHotkey = 0x0312;
    public const int WmNcHitTest = 0x0084;
    public const int HtTransparent = -1;
    public const int WmApp = 0x8000;

    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;
    public const uint ModShift = 0x0004;
    public const uint ModWin = 0x0008;
    public const uint ModNoRepeat = 0x4000;

    public const int WsChild = 0x40000000;
    public const int WsVisible = 0x10000000;
    public const int WsExTransparent = 0x00000020;
    public const int WsExToolWindow = 0x00000080;
    public const int WsExLayered = 0x00080000;
    public const int WsExNoActivate = 0x08000000;

    public const uint LwaAlpha = 0x00000002;
    public const uint MwFilterModeExclude = 0;
    public const uint SwpNoZOrder = 0x0004;
    public const uint SwpNoActivate = 0x0010;
    public const uint SwpShowWindow = 0x0040;

    public const int SwHide = 0;
    public const uint GaRoot = 2;
    public const int DwmwaExtendedFrameBounds = 9;
    public const int DwmwaUseImmersiveDarkModeBefore20H1 = 19;
    public const int DwmwaUseImmersiveDarkMode = 20;
    public const uint ProcessQueryLimitedInformation = 0x1000;

    public const uint EventSystemMenuStart = 0x0004;
    public const uint EventSystemMenuPopupEnd = 0x0007;
    public const uint WinEventOutOfContext = 0x0000;
    public const uint WinEventSkipOwnProcess = 0x0002;

    public const string WcMagnifier = "Magnifier";

    public static readonly nint HwndTopMost = new(-1);
    public static readonly nint HwndMessage = new(-3);
}

internal interface INativeWindowApi
{
    nint GetForegroundWindow();
    nint GetRootWindow(nint window);
    bool IsWindow(nint window);
    bool IsWindowVisible(nint window);
    bool IsMinimized(nint window);
    uint GetWindowThreadProcessId(nint window, out uint processId);
    string GetWindowTitle(nint window);
    string GetWindowClass(nint window);
    bool TryGetVisibleWindowBounds(nint window, out Rect rect);
    bool TryGetClientBounds(nint window, out Rect rect);
    nint CreateWindow(
        int extendedStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        nint parent);
    bool DestroyWindow(nint window);
    bool SetWindowPosition(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
    bool SetLayeredOpacity(nint window, byte alpha);
    bool ShowWindow(nint window, int command);
    bool Invalidate(nint window, bool erase);
    bool RegisterHotKey(nint window, int id, uint modifiers, uint key);
    bool UnregisterHotKey(nint window, int id);
}

internal sealed class NativeWindowApi : INativeWindowApi
{
    private NativeWindowApi()
    {
    }

    public static NativeWindowApi Default { get; } = new();

    public nint GetForegroundWindow() =>
        NativeInterop.User32.GetForegroundWindow();

    public nint GetRootWindow(nint window) =>
        NativeInterop.User32.GetAncestor(
            window,
            NativeConstants.GaRoot);

    public bool IsWindow(nint window) =>
        NativeInterop.User32.IsWindow(window);

    public bool IsWindowVisible(nint window) =>
        NativeInterop.User32.IsWindowVisible(window);

    public bool IsMinimized(nint window) =>
        NativeInterop.User32.IsIconic(window);

    public uint GetWindowThreadProcessId(
        nint window,
        out uint processId)
    {
        return NativeInterop.User32.GetWindowThreadProcessId(
            window,
            out processId);
    }

    public string GetWindowTitle(nint window)
    {
        var builder = new StringBuilder(512);
        return NativeInterop.User32.GetWindowText(
                    window,
                    builder,
                    builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    public string GetWindowClass(nint window)
    {
        var builder = new StringBuilder(256);
        return NativeInterop.User32.GetClassName(
                    window,
                    builder,
                    builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    public bool TryGetVisibleWindowBounds(
        nint window,
        out Rect rect)
    {
        if (NativeInterop.Dwm.DwmGetWindowAttribute(
                window,
                NativeConstants.DwmwaExtendedFrameBounds,
                out rect,
                Marshal.SizeOf<Rect>()) == 0)
        {
            return true;
        }

        return NativeInterop.User32.GetWindowRect(
            window,
            out rect);
    }

    public bool TryGetClientBounds(
        nint window,
        out Rect rect)
    {
        rect = default;
        if (!NativeInterop.User32.GetClientRect(
                window,
                out var client))
        {
            return false;
        }

        var topLeft = new NativePoint(
            client.Left,
            client.Top);
        var bottomRight = new NativePoint(
            client.Right,
            client.Bottom);
        if (!NativeInterop.User32.ClientToScreen(
                window,
                ref topLeft) ||
            !NativeInterop.User32.ClientToScreen(
                window,
                ref bottomRight))
        {
            return false;
        }

        rect = new Rect
        {
            Left = topLeft.X,
            Top = topLeft.Y,
            Right = bottomRight.X,
            Bottom = bottomRight.Y,
        };
        return rect.Width > 0 && rect.Height > 0;
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
        return NativeInterop.User32.CreateWindowEx(
            extendedStyle,
            className,
            windowName,
            style,
            x,
            y,
            width,
            height,
            parent,
            nint.Zero,
            nint.Zero,
            nint.Zero);
    }

    public bool DestroyWindow(nint window) =>
        NativeInterop.User32.DestroyWindow(window);

    public bool SetWindowPosition(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags)
    {
        return NativeInterop.User32.SetWindowPos(
            window,
            insertAfter,
            x,
            y,
            width,
            height,
            flags);
    }

    public bool SetLayeredOpacity(
        nint window,
        byte alpha)
    {
        return NativeInterop.User32.SetLayeredWindowAttributes(
            window,
            0,
            alpha,
            NativeConstants.LwaAlpha);
    }

    public bool ShowWindow(nint window, int command) =>
        NativeInterop.User32.ShowWindow(window, command);

    public bool Invalidate(nint window, bool erase) =>
        NativeInterop.User32.InvalidateRect(
            window,
            nint.Zero,
            erase);

    public bool RegisterHotKey(
        nint window,
        int id,
        uint modifiers,
        uint key)
    {
        return NativeInterop.User32.RegisterHotKey(
            window,
            id,
            modifiers,
            key);
    }

    public bool UnregisterHotKey(nint window, int id) =>
        NativeInterop.User32.UnregisterHotKey(window, id);
}

internal interface INativeMagnificationApi
{
    bool Initialize();
    bool Uninitialize();
    bool SetWindowSource(nint window, Rect source);
    bool SetWindowTransform(nint window, ref MagTransform transform);
    bool SetColorEffect(nint window, ref MagColorEffect effect);
    bool SetWindowFilterList(
        nint window,
        uint filterMode,
        int count,
        nint[] windows);
}

internal sealed class NativeMagnificationApi : INativeMagnificationApi
{
    private NativeMagnificationApi()
    {
    }

    public static NativeMagnificationApi Default { get; } = new();

    public bool Initialize() =>
        NativeInterop.Magnification.MagInitialize();

    public bool Uninitialize() =>
        NativeInterop.Magnification.MagUninitialize();

    public bool SetWindowSource(nint window, Rect source) =>
        NativeInterop.Magnification.MagSetWindowSource(
            window,
            source);

    public bool SetWindowTransform(
        nint window,
        ref MagTransform transform)
    {
        return NativeInterop.Magnification.MagSetWindowTransform(
            window,
            ref transform);
    }

    public bool SetColorEffect(
        nint window,
        ref MagColorEffect effect)
    {
        return NativeInterop.Magnification.MagSetColorEffect(
            window,
            ref effect);
    }

    public bool SetWindowFilterList(
        nint window,
        uint filterMode,
        int count,
        nint[] windows)
    {
        return NativeInterop.Magnification.MagSetWindowFilterList(
            window,
            filterMode,
            count,
            windows);
    }
}

internal interface INativeDwmApi
{
    int SetWindowAttribute(
        nint window,
        int attribute,
        ref int value,
        int valueSize);
}

internal sealed class NativeDwmApi : INativeDwmApi
{
    private NativeDwmApi()
    {
    }

    public static NativeDwmApi Default { get; } = new();

    public int SetWindowAttribute(
        nint window,
        int attribute,
        ref int value,
        int valueSize)
    {
        return NativeInterop.Dwm.DwmSetWindowAttribute(
            window,
            attribute,
            ref value,
            valueSize);
    }
}

internal interface INativeProcessApi
{
    bool TryGetProcessIdentityKey(
        nint window,
        out ProcessIdentityKey key);
    bool TryGetProcessPath(
        ProcessIdentityKey expectedKey,
        out string executablePath);
}

internal sealed class NativeProcessApi : INativeProcessApi
{
    private NativeProcessApi()
    {
    }

    public static NativeProcessApi Default { get; } = new();

    public bool TryGetProcessIdentityKey(
        nint window,
        out ProcessIdentityKey key)
    {
        key = default;
        NativeInterop.User32.GetWindowThreadProcessId(
            window,
            out var processId);
        if (processId == 0)
        {
            return false;
        }

        var process = NativeInterop.Kernel32.OpenProcess(
            NativeConstants.ProcessQueryLimitedInformation,
            false,
            processId);
        if (process == nint.Zero)
        {
            return false;
        }

        try
        {
            return TryReadProcessIdentityKey(
                processId,
                process,
                out key);
        }
        finally
        {
            _ = NativeInterop.Kernel32.CloseHandle(process);
        }
    }

    public bool TryGetProcessPath(
        ProcessIdentityKey expectedKey,
        out string executablePath)
    {
        executablePath = string.Empty;
        if (!expectedKey.IsValid)
        {
            return false;
        }

        var process = NativeInterop.Kernel32.OpenProcess(
            NativeConstants.ProcessQueryLimitedInformation,
            false,
            expectedKey.ProcessId);
        if (process == nint.Zero)
        {
            return false;
        }

        try
        {
            if (!TryReadProcessIdentityKey(
                    expectedKey.ProcessId,
                    process,
                    out var currentKey) ||
                currentKey != expectedKey)
            {
                return false;
            }

            var builder = new StringBuilder(32768);
            var length = (uint)builder.Capacity;
            if (!NativeInterop.Kernel32.QueryFullProcessImageName(
                    process,
                    0,
                    builder,
                    ref length))
            {
                return false;
            }

            executablePath = builder.ToString();
            return !string.IsNullOrWhiteSpace(executablePath);
        }
        finally
        {
            _ = NativeInterop.Kernel32.CloseHandle(process);
        }
    }

    private static bool TryReadProcessIdentityKey(
        uint processId,
        nint process,
        out ProcessIdentityKey key)
    {
        key = default;
        if (!NativeInterop.Kernel32.GetProcessTimes(
                process,
                out var creationTime,
                out _,
                out _,
                out _))
        {
            return false;
        }

        var creationTimeValue = creationTime.ToUInt64();
        if (creationTimeValue == 0)
        {
            return false;
        }

        key = new ProcessIdentityKey(
            processId,
            creationTimeValue);
        return true;
    }
}

internal interface INativeMenuEventApi
{
    bool EnumerateWindows(EnumWindowsCallback callback);
    nint InstallHook(WinEventCallback callback);
    bool RemoveHook(nint hook);
    bool PostMessage(nint window, int message);
}

internal sealed class NativeMenuEventApi : INativeMenuEventApi
{
    private NativeMenuEventApi()
    {
    }

    public static NativeMenuEventApi Default { get; } = new();

    public bool EnumerateWindows(EnumWindowsCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return NativeInterop.User32.EnumWindows(
            callback,
            nint.Zero);
    }

    public nint InstallHook(WinEventCallback callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return NativeInterop.User32.SetWinEventHook(
            NativeConstants.EventSystemMenuStart,
            NativeConstants.EventSystemMenuPopupEnd,
            nint.Zero,
            callback,
            0,
            0,
            NativeConstants.WinEventOutOfContext |
                NativeConstants.WinEventSkipOwnProcess);
    }

    public bool RemoveHook(nint hook) =>
        NativeInterop.User32.UnhookWinEvent(hook);

    public bool PostMessage(nint window, int message) =>
        NativeInterop.User32.PostMessage(
            window,
            message,
            nint.Zero,
            nint.Zero);
}
