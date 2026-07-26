using System.Runtime.InteropServices;

namespace SightAdapt;

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
internal struct NativePoint(int x, int y)
{
    public int X = x;
    public int Y = y;
}

[StructLayout(LayoutKind.Sequential)]
internal struct NativeFileTime
{
    public uint LowDateTime;
    public uint HighDateTime;

    public readonly ulong ToUInt64()
    {
        return ((ulong)HighDateTime << 32) |
            LowDateTime;
    }
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

internal delegate bool EnumWindowsCallback(
    nint window,
    nint parameter);

internal delegate void WinEventCallback(
    nint hook,
    uint eventType,
    nint window,
    int objectId,
    int childId,
    uint eventThread,
    uint eventTime);
