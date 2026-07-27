using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed class RoundedPanel : Panel
{
    public RoundedPanel()
    {
        BackColor = AppTheme.Surface;
        BorderColor = AppTheme.Border;
        CornerRadius = 12;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    public Color BorderColor { get; set; }

    public int CornerRadius { get; set; }

    protected override void OnResize(EventArgs eventArgs)
    {
        base.OnResize(eventArgs);
        UpdateRegion();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);

        if (Width <= 1 || Height <= 1)
        {
            return;
        }

        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = DrawingHelpers.CreateRoundedRectangle(bounds, CornerRadius);
        using var pen = new Pen(BorderColor);
        eventArgs.Graphics.DrawPath(pen, path);
    }

    private void UpdateRegion()
    {
        if (Width <= 1 || Height <= 1)
        {
            return;
        }

        using var path = DrawingHelpers.CreateRoundedRectangle(
            new Rectangle(0, 0, Width, Height),
            CornerRadius);
        var previous = Region;
        Region = new Region(path);
        previous?.Dispose();
    }
}
