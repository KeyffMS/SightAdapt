using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    public DarkMenuRenderer()
        : base(new DarkMenuColorTable())
    {
        RoundedEdges = true;
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs eventArgs)
    {
        eventArgs.Graphics.Clear(AppTheme.Surface);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs eventArgs)
    {
        using var pen = new Pen(AppTheme.Border);
        var bounds = new Rectangle(
            0,
            0,
            eventArgs.ToolStrip.Width - 1,
            eventArgs.ToolStrip.Height - 1);
        eventArgs.Graphics.DrawRectangle(pen, bounds);
    }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs eventArgs)
    {
        if (!eventArgs.Item.Selected)
        {
            return;
        }

        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var bounds = new Rectangle(4, 2, eventArgs.Item.Width - 8, eventArgs.Item.Height - 4);
        using var path = DrawingHelpers.CreateRoundedRectangle(bounds, 7);
        using var brush = new SolidBrush(AppTheme.SurfaceHover);
        eventArgs.Graphics.FillPath(brush, path);

        using var accentBrush = new SolidBrush(AppTheme.Accent);
        eventArgs.Graphics.FillRectangle(accentBrush, bounds.Left, bounds.Top + 5, 3, bounds.Height - 10);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs eventArgs)
    {
        using var pen = new Pen(AppTheme.Border);
        var y = eventArgs.Item.Height / 2;
        eventArgs.Graphics.DrawLine(pen, 12, y, eventArgs.Item.Width - 12, y);
    }

    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs eventArgs)
    {
        var textColor = ResolveItemTextColor(eventArgs.Item);

        TextRenderer.DrawText(
            eventArgs.Graphics,
            eventArgs.Text ?? string.Empty,
            eventArgs.TextFont,
            eventArgs.TextRectangle,
            textColor,
            eventArgs.TextFormat);
    }

    internal static Color ResolveItemTextColor(
        ToolStripItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.Enabled)
        {
            return AppTheme.TextSecondary;
        }

        if (item.Tag is MenuItemRole role &&
            role == MenuItemRole.Danger)
        {
            return AppTheme.Danger;
        }

        return item is ToolStripMenuItem { Checked: true }
            ? AppTheme.AccentHover
            : AppTheme.TextPrimary;
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs eventArgs)
    {
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        const int size = 18;
        var bounds = new Rectangle(
            eventArgs.ImageRectangle.X + 2,
            eventArgs.Item.ContentRectangle.Top + (eventArgs.Item.ContentRectangle.Height - size) / 2,
            size,
            size);

        using (var path = DrawingHelpers.CreateRoundedRectangle(bounds, 5))
        using (var brush = new SolidBrush(AppTheme.Accent))
        {
            eventArgs.Graphics.FillPath(brush, path);
        }

        using var pen = new Pen(Color.White, 2f)
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };
        eventArgs.Graphics.DrawLines(
            pen,
            new[]
            {
                new Point(bounds.Left + 4, bounds.Top + 9),
                new Point(bounds.Left + 8, bounds.Top + 13),
                new Point(bounds.Left + 14, bounds.Top + 5),
            });
    }
}

internal sealed class DarkMenuColorTable : ProfessionalColorTable
{
    public override Color MenuBorder => AppTheme.Border;
    public override Color MenuItemBorder => AppTheme.SurfaceHover;
    public override Color MenuItemSelected => AppTheme.SurfaceHover;
    public override Color MenuItemSelectedGradientBegin => AppTheme.SurfaceHover;
    public override Color MenuItemSelectedGradientEnd => AppTheme.SurfaceHover;
    public override Color MenuItemPressedGradientBegin => AppTheme.SurfaceHover;
    public override Color MenuItemPressedGradientEnd => AppTheme.SurfaceHover;
    public override Color ToolStripDropDownBackground => AppTheme.Surface;
    public override Color ImageMarginGradientBegin => AppTheme.Surface;
    public override Color ImageMarginGradientMiddle => AppTheme.Surface;
    public override Color ImageMarginGradientEnd => AppTheme.Surface;
    public override Color SeparatorDark => AppTheme.Border;
    public override Color SeparatorLight => AppTheme.Border;
}
