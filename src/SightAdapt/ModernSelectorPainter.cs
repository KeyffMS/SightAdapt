using System.Drawing.Drawing2D;

namespace SightAdapt;

internal static class ModernSelectorPainter
{
    public static void Paint(
        Graphics graphics,
        Rectangle bounds,
        string text,
        Font font,
        Color foreground,
        bool selected,
        bool focused,
        bool hovered = false)
    {
        if (bounds.Width <= 2 || bounds.Height <= 2)
        {
            return;
        }

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var selectorPath = DrawingHelpers.CreateRoundedRectangle(bounds, 7);
        using var selectorBrush = new SolidBrush(
            hovered ? AppTheme.SurfaceHover : AppTheme.SurfaceRaised);
        using var selectorPen = new Pen(
            focused || selected ? AppTheme.AccentHover : AppTheme.Border,
            focused ? 1.6f : 1f);
        graphics.FillPath(selectorBrush, selectorPath);
        graphics.DrawPath(selectorPen, selectorPath);

        const int buttonWidth = 32;
        var buttonBounds = new Rectangle(
            bounds.Right - buttonWidth,
            bounds.Top,
            buttonWidth,
            bounds.Height);
        using (var buttonBrush = new SolidBrush(
                   focused || hovered
                       ? AppTheme.SurfaceHover
                       : AppTheme.Surface))
        {
            graphics.FillRectangle(buttonBrush, buttonBounds);
        }
        using (var separatorPen = new Pen(AppTheme.Border))
        {
            graphics.DrawLine(
                separatorPen,
                buttonBounds.Left,
                buttonBounds.Top + 4,
                buttonBounds.Left,
                buttonBounds.Bottom - 4);
        }

        var textBounds = new Rectangle(
            bounds.Left + 10,
            bounds.Top,
            Math.Max(1, bounds.Width - buttonWidth - 14),
            bounds.Height);
        TextRenderer.DrawText(
            graphics,
            text,
            font,
            textBounds,
            foreground,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        var centerX = buttonBounds.Left + buttonBounds.Width / 2;
        var centerY = buttonBounds.Top + buttonBounds.Height / 2;
        using var arrowPen = new Pen(AppTheme.TextSecondary, 1.8f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        graphics.DrawLines(
            arrowPen,
            new Point[]
            {
                new(centerX - 4, centerY - 2),
                new(centerX, centerY + 2),
                new(centerX + 4, centerY - 2),
            });

        if (focused)
        {
            ControlPaint.DrawFocusRectangle(
                graphics,
                Rectangle.Inflate(bounds, -3, -3),
                AppTheme.TextPrimary,
                AppTheme.SurfaceRaised);
        }
    }
}
