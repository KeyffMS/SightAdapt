using System.Runtime.InteropServices;
using System.Text;

namespace SightAdapt;

internal static class NativeMethods
{
    public const int WmHotkey = 0x0312;
    public const int WmNcHitTest = 0x0084;
    public const int HtTransparent = -1;

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

    public const string WcMagnifier = "Magnifier";

    public static readonly nint HwndTopMost = new(-1);
    public static readonly nint HwndMessage = new(-3);

    [DllImport("Magnification.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagInitialize();

    [DllImport("Magnification.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagUninitialize();

    [DllImport("Magnification.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowSource(nint hwnd, Rect sourceRect);

    [DllImport("Magnification.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowTransform(nint hwnd, ref MagTransform transform);

    [DllImport("Magnification.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetColorEffect(nint hwnd, ref MagColorEffect effect);

    [DllImport("Magnification.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool MagSetWindowFilterList(
        nint hwnd,
        uint filterMode,
        int count,
        [In] nint[] windows);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern nint CreateWindowEx(
        int extendedStyle,
        string className,
        string windowName,
        int style,
        int x,
        int y,
        int width,
        int height,
        nint parent,
        nint menu,
        nint instance,
        nint parameter);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool DestroyWindow(nint window);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool RegisterHotKey(nint window, int id, uint modifiers, uint key);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool UnregisterHotKey(nint window, int id);

    [DllImport("user32.dll")]
    public static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern nint GetAncestor(nint window, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsIconic(nint window);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(nint window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(nint window, StringBuilder text, int maximumCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(nint window, StringBuilder className, int maximumCount);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(nint window, out Rect rect);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(
        nint window,
        int attribute,
        out Rect value,
        int valueSize);

    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(
        nint window,
        int attribute,
        ref int value,
        int valueSize);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        nint window,
        nint insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetLayeredWindowAttributes(
        nint window,
        uint colorKey,
        byte alpha,
        uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool InvalidateRect(nint window, nint rect, bool erase);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenProcess(
        uint desiredAccess,
        [MarshalAs(UnmanagedType.Bool)] bool inheritHandle,
        uint processId);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool QueryFullProcessImageName(
        nint process,
        uint flags,
        StringBuilder executablePath,
        ref uint pathLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessTimes(
        nint process,
        out NativeFileTime creationTime,
        out NativeFileTime exitTime,
        out NativeFileTime kernelTime,
        out NativeFileTime userTime);

    [DllImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(nint handle);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileTime
    {
        public uint LowDateTime;
        public uint HighDateTime;

        public readonly ulong ToUInt64()
        {
            return ((ulong)HighDateTime << 32) |
                LowDateTime;
        }
    }

    public static bool TryGetVisibleWindowBounds(nint window, out Rect rect)
    {
        if (DwmGetWindowAttribute(
                window,
                DwmwaExtendedFrameBounds,
                out rect,
                Marshal.SizeOf<Rect>()) == 0)
        {
            return true;
        }

        return GetWindowRect(window, out rect);
    }

    public static string GetWindowTitle(nint window)
    {
        var builder = new StringBuilder(512);
        return GetWindowText(window, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    public static string GetWindowClass(nint window)
    {
        var builder = new StringBuilder(256);
        return GetClassName(window, builder, builder.Capacity) > 0
            ? builder.ToString()
            : string.Empty;
    }

    public static bool TryGetProcessIdentityKey(
        nint window,
        out ProcessIdentityKey key)
    {
        key = default;
        GetWindowThreadProcessId(window, out var processId);
        if (processId == 0)
        {
            return false;
        }

        var process = OpenProcess(
            ProcessQueryLimitedInformation,
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
            CloseHandle(process);
        }
    }

    public static bool TryGetProcessPath(
        ProcessIdentityKey expectedKey,
        out string executablePath)
    {
        executablePath = string.Empty;
        if (!expectedKey.IsValid)
        {
            return false;
        }

        var process = OpenProcess(
            ProcessQueryLimitedInformation,
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
            if (!QueryFullProcessImageName(
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
            CloseHandle(process);
        }
    }

    private static bool TryReadProcessIdentityKey(
        uint processId,
        nint process,
        out ProcessIdentityKey key)
    {
        key = default;
        if (!GetProcessTimes(
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

[StructLayout(LayoutKind.Sequential)]
internal struct Rect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;

    public readonly int Width => Right - Left;

    public readonly int Height => Bottom - Top;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MagTransform
{
    public float M00;
    public float M01;
    public float M02;
    public float M10;
    public float M11;
    public float M12;
    public float M20;
    public float M21;
    public float M22;

    public static MagTransform Identity => new()
    {
        M00 = 1.0f,
        M11 = 1.0f,
        M22 = 1.0f,
    };
}

[StructLayout(LayoutKind.Sequential)]
internal struct MagColorEffect
{
    public float M00;
    public float M01;
    public float M02;
    public float M03;
    public float M04;

    public float M10;
    public float M11;
    public float M12;
    public float M13;
    public float M14;

    public float M20;
    public float M21;
    public float M22;
    public float M23;
    public float M24;

    public float M30;
    public float M31;
    public float M32;
    public float M33;
    public float M34;

    public float M40;
    public float M41;
    public float M42;
    public float M43;
    public float M44;

    public static MagColorEffect Invert => new()
    {
        M00 = -1.0f,
        M11 = -1.0f,
        M22 = -1.0f,
        M33 = 1.0f,
        M40 = 1.0f,
        M41 = 1.0f,
        M42 = 1.0f,
        M44 = 1.0f,
    };
}
