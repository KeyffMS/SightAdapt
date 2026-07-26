using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed class ToggleSwitch : CheckBox
{
    private bool _hovered;

    public ToggleSwitch()
    {
        AutoSize = false;
        Cursor = Cursors.Hand;
        Size = new Size(50, 28);
        Text = string.Empty;
        AccessibleRole = AccessibleRole.CheckButton;

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.UserPaint,
            true);
    }

    protected override void OnCheckedChanged(EventArgs eventArgs)
    {
        base.OnCheckedChanged(eventArgs);
        Invalidate();
    }

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
        graphics.Clear(Parent?.BackColor ?? AppTheme.Surface);

        var track = new Rectangle(1, 4, Width - 2, Height - 8);
        var trackColor = !Enabled
            ? AppTheme.Border
            : Checked
                ? (_hovered ? AppTheme.AccentHover : AppTheme.Accent)
                : (_hovered ? AppTheme.SurfaceHover : AppTheme.Border);

        using (var trackPath = DrawingHelpers.CreateRoundedRectangle(track, track.Height / 2))
        using (var trackBrush = new SolidBrush(trackColor))
        {
            graphics.FillPath(trackBrush, trackPath);
        }

        const int thumbSize = 20;
        var thumbX = Checked ? Width - thumbSize - 4 : 4;
        var thumb = new Rectangle(thumbX, (Height - thumbSize) / 2, thumbSize, thumbSize);
        using var thumbBrush = new SolidBrush(Enabled ? Color.White : AppTheme.TextMuted);
        graphics.FillEllipse(thumbBrush, thumb);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(graphics, ClientRectangle);
        }
    }
}
