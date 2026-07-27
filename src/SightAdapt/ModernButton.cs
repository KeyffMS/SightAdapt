using System.Drawing.Drawing2D;

namespace SightAdapt;

internal enum ModernButtonStyle
{
    Primary,
    Secondary,
    Danger,
    Ghost,
}

internal sealed class ModernButton : Button
{
    private bool _hovered;
    private bool _pressed;

    public ModernButton()
    {
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.SupportsTransparentBackColor |
            ControlStyles.UserPaint,
            true);

        AutoSize = true;
        BackColor = Color.Transparent;
        Cursor = Cursors.Hand;
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Font = AppTheme.CreateUiFont(9.5f, FontStyle.Bold);
        MinimumSize = new Size(112, 40);
        Padding = new Padding(16, 0, 16, 0);
        UseVisualStyleBackColor = false;
    }

    public ModernButtonStyle VisualStyle { get; set; } = ModernButtonStyle.Secondary;

    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        base.OnMouseEnter(eventArgs);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        base.OnMouseLeave(eventArgs);
        _hovered = false;
        _pressed = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button == MouseButtons.Left)
        {
            _pressed = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        _pressed = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs eventArgs)
    {
        base.OnEnabledChanged(eventArgs);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? AppTheme.WindowBackground);

        var bounds = new Rectangle(0, 0, Width - 1, Height - 1);
        using var path = DrawingHelpers.CreateRoundedRectangle(bounds, 9);

        var (background, border, foreground) = ResolveColors();
        using var backgroundBrush = new SolidBrush(background);
        using var borderPen = new Pen(border);
        graphics.FillPath(backgroundBrush, path);
        graphics.DrawPath(borderPen, path);

        TextRenderer.DrawText(
            graphics,
            Text,
            Font,
            bounds,
            foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if (Focused && ShowFocusCues)
        {
            var focusBounds = Rectangle.Inflate(bounds, -4, -4);
            ControlPaint.DrawFocusRectangle(graphics, focusBounds, foreground, background);
        }
    }

    private (Color Background, Color Border, Color Foreground) ResolveColors()
    {
        return ResolveColors(
            VisualStyle,
            Enabled,
            _hovered,
            _pressed);
    }

    internal static (
        Color Background,
        Color Border,
        Color Foreground) ResolveColors(
            ModernButtonStyle visualStyle,
            bool enabled,
            bool hovered,
            bool pressed)
    {
        if (!enabled)
        {
            return (
                AppTheme.Surface,
                AppTheme.Border,
                AppTheme.TextMuted);
        }

        return visualStyle switch
        {
            ModernButtonStyle.Primary => (
                pressed
                    ? AppTheme.AccentPressed
                    : hovered
                        ? AppTheme.AccentHover
                        : AppTheme.Accent,
                pressed
                    ? AppTheme.AccentPressed
                    : AppTheme.AccentHover,
                Color.White),
            ModernButtonStyle.Danger => (
                pressed
                    ? AppTheme.DangerPressed
                    : hovered
                        ? AppTheme.DangerHover
                        : AppTheme.DangerSoft,
                hovered
                    ? AppTheme.Danger
                    : AppTheme.DangerBorder,
                hovered
                    ? Color.White
                    : AppTheme.Danger),
            ModernButtonStyle.Ghost => (
                pressed
                    ? AppTheme.SurfaceRaised
                    : hovered
                        ? AppTheme.SurfaceHover
                        : AppTheme.WindowBackground,
                hovered
                    ? AppTheme.Border
                    : AppTheme.WindowBackground,
                AppTheme.TextSecondary),
            _ => (
                pressed || hovered
                    ? AppTheme.SurfaceHover
                    : AppTheme.SurfaceRaised,
                hovered
                    ? AppTheme.Accent
                    : AppTheme.Border,
                AppTheme.TextPrimary),
        };
    }
}
