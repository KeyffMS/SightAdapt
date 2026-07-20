using System.Drawing.Drawing2D;

namespace SightAdapt.Demo;

internal sealed class OutputLimitPreview : Control
{
    public OutputLimitPreview()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.SurfaceRaised;
        MinimumSize = new Size(220, 84);
    }

    public VisualProfile? Profile { get; set; }

    public VisualTransformCatalog TransformCatalog { get; set; } =
        VisualTransformCatalog.Default;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        if (Profile is null || Width < 160 || Height < 60)
        {
            return;
        }

        var effect = TransformCatalog
            .GetRequired(Profile.TransformId)
            .CreateColorEffect(Profile);
        var sourceForeground = Color.Black;
        var sourceBackground = Color.White;
        var outputForeground = ColorEffectPreviewMath.Apply(sourceForeground, effect);
        var outputBackground = ColorEffectPreviewMath.Apply(sourceBackground, effect);

        const int gap = 10;
        var sampleWidth = Math.Max(60, (Width - gap * 3) / 2);
        var sampleHeight = Height - 14;
        DrawSample(
            graphics,
            new Rectangle(gap, 7, sampleWidth, sampleHeight),
            "SOURCE",
            sourceForeground,
            sourceBackground);
        DrawSample(
            graphics,
            new Rectangle(gap * 2 + sampleWidth, 7, sampleWidth, sampleHeight),
            "OUTPUT",
            outputForeground,
            outputBackground);
    }

    private static void DrawSample(
        Graphics graphics,
        Rectangle bounds,
        string caption,
        Color foreground,
        Color background)
    {
        using var backgroundBrush = new SolidBrush(background);
        using var borderPen = new Pen(AppTheme.Border);
        graphics.FillRectangle(backgroundBrush, bounds);
        graphics.DrawRectangle(borderPen, bounds);

        var captionColor = ColorEffectPreviewMath.ContrastText(background);
        TextRenderer.DrawText(
            graphics,
            caption,
            AppTheme.CreateUiFont(7.5f, FontStyle.Bold),
            new Rectangle(bounds.Left + 8, bounds.Top + 5, bounds.Width - 16, 18),
            captionColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);
        TextRenderer.DrawText(
            graphics,
            "SightAdapt",
            AppTheme.CreateUiFont(13f, FontStyle.Bold),
            new Rectangle(bounds.Left + 8, bounds.Top + 24, bounds.Width - 16, bounds.Height - 30),
            foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }
}

internal sealed class ColorProfilePreview : Control
{
    private static readonly string[] Labels =
    [
        "SOURCE GRAY",
        "OUTPUT GRAY",
        "SOURCE HUE",
        "OUTPUT HUE",
    ];

    public ColorProfilePreview()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.SurfaceRaised;
        ForeColor = AppTheme.TextSecondary;
        Font = AppTheme.CreateUiFont(7.5f, FontStyle.Bold);
        MinimumSize = new Size(200, 100);
    }

    public VisualProfile? Profile { get; set; }

    public VisualTransformCatalog TransformCatalog { get; set; } =
        VisualTransformCatalog.Default;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.None;
        eventArgs.Graphics.Clear(BackColor);

        if (Profile is null || Width < 120 || Height < 80)
        {
            return;
        }

        var effect = TransformCatalog
            .GetRequired(Profile.TransformId)
            .CreateColorEffect(Profile);
        const int labelWidth = 96;
        const int gap = 5;
        var stripWidth = Math.Max(1, Width - labelWidth - 8);
        var stripHeight = Math.Max(12, (Height - gap * 3 - 8) / 4);

        for (var row = 0; row < Labels.Length; row++)
        {
            var top = 4 + row * (stripHeight + gap);
            TextRenderer.DrawText(
                eventArgs.Graphics,
                Labels[row],
                Font,
                new Rectangle(4, top, labelWidth - 8, stripHeight),
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);

            for (var x = 0; x < stripWidth; x++)
            {
                var ratio = stripWidth <= 1 ? 0f : (float)x / (stripWidth - 1);
                var source = row < 2
                    ? ColorEffectPreviewMath.CreateGray(ratio)
                    : ColorEffectPreviewMath.CreateHue(ratio * 360f);
                var color = row is 1 or 3
                    ? ColorEffectPreviewMath.Apply(source, effect)
                    : source;
                using var pen = new Pen(color);
                eventArgs.Graphics.DrawLine(
                    pen,
                    labelWidth + x,
                    top,
                    labelWidth + x,
                    top + stripHeight - 1);
            }
        }

        using var border = new Pen(AppTheme.Border);
        eventArgs.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }
}

internal static class ColorEffectPreviewMath
{
    public static Color Apply(Color source, MagColorEffect effect)
    {
        var red = source.R / 255f;
        var green = source.G / 255f;
        var blue = source.B / 255f;
        return Color.FromArgb(
            ToByte(red * effect.M00 + green * effect.M10 + blue * effect.M20 + effect.M40),
            ToByte(red * effect.M01 + green * effect.M11 + blue * effect.M21 + effect.M41),
            ToByte(red * effect.M02 + green * effect.M12 + blue * effect.M22 + effect.M42));
    }

    public static Color CreateGray(float value)
    {
        var channel = ToByte(value);
        return Color.FromArgb(channel, channel, channel);
    }

    public static Color CreateHue(float degrees)
    {
        var hue = (degrees % 360f + 360f) % 360f;
        var sector = hue / 60f;
        var fraction = sector - MathF.Floor(sector);
        var descending = 1f - fraction;
        return (int)MathF.Floor(sector) switch
        {
            0 => Color.FromArgb(255, ToByte(fraction), 0),
            1 => Color.FromArgb(ToByte(descending), 255, 0),
            2 => Color.FromArgb(0, 255, ToByte(fraction)),
            3 => Color.FromArgb(0, ToByte(descending), 255),
            4 => Color.FromArgb(ToByte(fraction), 0, 255),
            _ => Color.FromArgb(255, 0, ToByte(descending)),
        };
    }

    public static Color ContrastText(Color background)
    {
        var luminance =
            0.2126f * background.R +
            0.7152f * background.G +
            0.0722f * background.B;
        return luminance > 145f ? Color.Black : Color.White;
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp(
            (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f),
            0,
            255);
    }
}
