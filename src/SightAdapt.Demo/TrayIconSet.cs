using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace SightAdapt.Demo;

internal enum TrayIconState
{
    Inactive,
    Active,
    Emergency,
}

internal sealed class TrayIconSet : IDisposable
{
    private static readonly int[] IconSizes = [16, 20, 24, 32, 40, 48, 64, 128, 256];

    private readonly MemoryStream _inactiveStream;
    private readonly MemoryStream _activeStream;
    private readonly MemoryStream _emergencyStream;
    private bool _disposed;

    public TrayIconSet()
    {
        (_inactiveStream, Inactive) = CreateIcon(TrayIconState.Inactive);
        (_activeStream, Active) = CreateIcon(TrayIconState.Active);
        (_emergencyStream, Emergency) = CreateIcon(TrayIconState.Emergency);
    }

    public Icon Inactive { get; }

    public Icon Active { get; }

    public Icon Emergency { get; }

    public Icon Get(TrayIconState state)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        return state switch
        {
            TrayIconState.Active => Active,
            TrayIconState.Emergency => Emergency,
            _ => Inactive,
        };
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Inactive.Dispose();
        Active.Dispose();
        Emergency.Dispose();
        _inactiveStream.Dispose();
        _activeStream.Dispose();
        _emergencyStream.Dispose();
        _disposed = true;
    }

    private static (MemoryStream Stream, Icon Icon) CreateIcon(TrayIconState state)
    {
        var pngImages = new List<byte[]>(IconSizes.Length);

        foreach (var size in IconSizes)
        {
            using var bitmap = RenderBitmap(state, size);
            using var pngStream = new MemoryStream();
            bitmap.Save(pngStream, ImageFormat.Png);
            pngImages.Add(pngStream.ToArray());
        }

        var iconStream = BuildIconStream(pngImages);
        var icon = new Icon(iconStream);
        return (iconStream, icon);
    }

    private static Bitmap RenderBitmap(TrayIconState state, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        bitmap.SetResolution(96f, 96f);

        using var graphics = Graphics.FromImage(bitmap);
        graphics.Clear(Color.Transparent);
        graphics.CompositingMode = CompositingMode.SourceOver;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.ScaleTransform(size / 256f, size / 256f);

        DrawBackground(graphics);
        DrawEye(graphics);
        DrawIris(graphics, state);
        DrawNotch(graphics, state);

        return bitmap;
    }

    private static void DrawBackground(Graphics graphics)
    {
        using var backgroundPath = CreateRoundedRectangle(new RectangleF(12, 12, 232, 232), 58);
        using var backgroundBrush = new LinearGradientBrush(
            new PointF(28, 24),
            new PointF(226, 232),
            Color.FromArgb(19, 32, 59),
            Color.FromArgb(32, 42, 85));
        graphics.FillPath(backgroundBrush, backgroundPath);

        using var borderPath = CreateRoundedRectangle(new RectangleF(19, 19, 218, 218), 51);
        using var borderPen = new Pen(Color.FromArgb(41, 255, 255, 255), 6);
        graphics.DrawPath(borderPen, borderPath);
    }

    private static void DrawEye(Graphics graphics)
    {
        using var eyePath = new GraphicsPath();
        eyePath.StartFigure();
        eyePath.AddBezier(42, 128, 66, 84, 96, 64, 128, 64);
        eyePath.AddBezier(128, 64, 160, 64, 190, 84, 214, 128);
        eyePath.AddBezier(214, 128, 190, 172, 160, 192, 128, 192);
        eyePath.AddBezier(128, 192, 96, 192, 66, 172, 42, 128);
        eyePath.CloseFigure();

        using (var shadowPath = (GraphicsPath)eyePath.Clone())
        using (var shadowTransform = new Matrix())
        using (var shadowBrush = new SolidBrush(Color.FromArgb(90, 5, 8, 23)))
        {
            shadowTransform.Translate(0, 6);
            shadowPath.Transform(shadowTransform);
            graphics.FillPath(shadowBrush, shadowPath);
        }

        using var eyeBrush = new SolidBrush(Color.FromArgb(247, 250, 255));
        graphics.FillPath(eyeBrush, eyePath);
    }

    private static void DrawIris(Graphics graphics, TrayIconState state)
    {
        var colors = state switch
        {
            TrayIconState.Active => new[]
            {
                Color.FromArgb(66, 232, 255),
                Color.FromArgb(85, 184, 255),
                Color.FromArgb(179, 140, 255),
            },
            TrayIconState.Emergency => new[]
            {
                Color.FromArgb(255, 209, 102),
                Color.FromArgb(255, 159, 67),
                Color.FromArgb(255, 94, 105),
            },
            _ => new[]
            {
                Color.FromArgb(184, 192, 204),
                Color.FromArgb(154, 166, 181),
                Color.FromArgb(126, 137, 152),
            },
        };

        using (var irisBrush = new LinearGradientBrush(
                   new PointF(88, 78),
                   new PointF(170, 176),
                   colors[0],
                   colors[2]))
        {
            irisBrush.InterpolationColors = new ColorBlend
            {
                Colors = colors,
                Positions = [0f, 0.52f, 1f],
            };
            graphics.FillEllipse(irisBrush, 81, 81, 94, 94);
        }

        using (var shadePath = new GraphicsPath())
        using (var shadeBrush = new SolidBrush(Color.FromArgb(77, 22, 48, 77)))
        {
            shadePath.StartFigure();
            shadePath.AddArc(81, 81, 94, 94, 226.7f, -180f);
            shadePath.CloseFigure();
            graphics.FillPath(shadeBrush, shadePath);
        }

        using (var pupilBrush = new SolidBrush(Color.FromArgb(16, 24, 47)))
        {
            graphics.FillEllipse(pupilBrush, 106, 106, 44, 44);
        }

        using var highlightBrush = new SolidBrush(Color.FromArgb(235, 255, 255, 255));
        graphics.FillEllipse(highlightBrush, 106, 103, 18, 18);
    }

    private static void DrawNotch(Graphics graphics, TrayIconState state)
    {
        var color = state switch
        {
            TrayIconState.Active => Color.FromArgb(127, 241, 255),
            TrayIconState.Emergency => Color.FromArgb(255, 240, 179),
            _ => Color.FromArgb(211, 216, 223),
        };

        PointF[] points =
        [
            new(182, 80),
            new(190, 72),
            new(204, 86),
            new(196, 94),
        ];

        using var brush = new SolidBrush(color);
        graphics.FillPolygon(brush, points);
    }

    private static MemoryStream BuildIconStream(IReadOnlyList<byte[]> pngImages)
    {
        var stream = new MemoryStream();
        using var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

        writer.Write((ushort)0);
        writer.Write((ushort)1);
        writer.Write((ushort)pngImages.Count);

        var imageOffset = 6 + (16 * pngImages.Count);

        for (var index = 0; index < pngImages.Count; index++)
        {
            var size = IconSizes[index];
            var image = pngImages[index];

            writer.Write(size == 256 ? (byte)0 : (byte)size);
            writer.Write(size == 256 ? (byte)0 : (byte)size);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((ushort)1);
            writer.Write((ushort)32);
            writer.Write((uint)image.Length);
            writer.Write((uint)imageOffset);

            imageOffset += image.Length;
        }

        foreach (var image in pngImages)
        {
            writer.Write(image);
        }

        writer.Flush();
        stream.Position = 0;
        return stream;
    }

    private static GraphicsPath CreateRoundedRectangle(RectangleF bounds, float radius)
    {
        var diameter = radius * 2;
        var path = new GraphicsPath();
        path.StartFigure();
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        return path;
    }
}
