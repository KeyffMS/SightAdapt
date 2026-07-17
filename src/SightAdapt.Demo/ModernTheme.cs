using System.Drawing.Drawing2D;

namespace SightAdapt.Demo;

internal static class AppTheme
{
    public static readonly Color WindowBackground = Color.FromArgb(20, 23, 31);
    public static readonly Color HeaderBackground = Color.FromArgb(24, 28, 38);
    public static readonly Color Surface = Color.FromArgb(29, 34, 45);
    public static readonly Color SurfaceRaised = Color.FromArgb(36, 42, 55);
    public static readonly Color SurfaceHover = Color.FromArgb(45, 53, 69);
    public static readonly Color Border = Color.FromArgb(54, 63, 81);
    public static readonly Color TextPrimary = Color.FromArgb(239, 243, 250);
    public static readonly Color TextSecondary = Color.FromArgb(158, 169, 187);
    public static readonly Color TextMuted = Color.FromArgb(112, 122, 141);
    public static readonly Color Accent = Color.FromArgb(112, 139, 255);
    public static readonly Color AccentHover = Color.FromArgb(130, 154, 255);
    public static readonly Color AccentPressed = Color.FromArgb(91, 117, 229);
    public static readonly Color AccentSoft = Color.FromArgb(50, 62, 105);
    public static readonly Color Success = Color.FromArgb(77, 211, 169);
    public static readonly Color SuccessSoft = Color.FromArgb(30, 76, 67);
    public static readonly Color Danger = Color.FromArgb(255, 111, 128);
    public static readonly Color DangerSoft = Color.FromArgb(84, 43, 54);
    public static readonly Color Selection = Color.FromArgb(52, 67, 105);

    public static Font CreateUiFont(float size = 9.5f, FontStyle style = FontStyle.Regular)
    {
        return new Font("Segoe UI", size, style, GraphicsUnit.Point);
    }

    public static void ApplyTo(Form form)
    {
        form.AutoScaleMode = AutoScaleMode.Dpi;
        form.BackColor = WindowBackground;
        form.ForeColor = TextPrimary;
        form.Font = CreateUiFont();
        form.HandleCreated += (_, _) => EnableDarkTitleBar(form.Handle);
    }

    public static ContextMenuStrip CreateContextMenu()
    {
        return new ContextMenuStrip
        {
            AutoSize = true,
            BackColor = Surface,
            ForeColor = TextPrimary,
            Font = CreateUiFont(10f),
            MinimumSize = new Size(320, 0),
            Padding = new Padding(8),
            Renderer = new DarkMenuRenderer(),
            ShowCheckMargin = true,
            ShowImageMargin = false,
        };
    }

    public static void StyleMenuItem(
        ToolStripItem item,
        Color? foreground = null,
        FontStyle fontStyle = FontStyle.Regular,
        string? role = null)
    {
        item.ForeColor = foreground ?? TextPrimary;
        item.Font = CreateUiFont(10f, fontStyle);
        item.Padding = new Padding(10, 6, 10, 6);
        item.Tag = role;
    }

    public static void StyleGrid(DataGridView grid)
    {
        grid.BackgroundColor = Surface;
        grid.BorderStyle = BorderStyle.None;
        grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
        grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
        grid.EnableHeadersVisualStyles = false;
        grid.GridColor = Border;
        grid.RowHeadersVisible = false;
        grid.RowTemplate.Height = 42;
        grid.ColumnHeadersHeight = 44;
        grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

        grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            BackColor = SurfaceRaised,
            ForeColor = TextSecondary,
            Font = CreateUiFont(9f, FontStyle.Bold),
            Padding = new Padding(10, 0, 10, 0),
            SelectionBackColor = SurfaceRaised,
            SelectionForeColor = TextSecondary,
        };

        grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            Alignment = DataGridViewContentAlignment.MiddleLeft,
            BackColor = Surface,
            ForeColor = TextPrimary,
            Font = CreateUiFont(9.5f),
            Padding = new Padding(10, 0, 10, 0),
            SelectionBackColor = Selection,
            SelectionForeColor = TextPrimary,
        };

        grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = Color.FromArgb(32, 38, 50),
            ForeColor = TextPrimary,
            SelectionBackColor = Selection,
            SelectionForeColor = TextPrimary,
        };
    }

    private static void EnableDarkTitleBar(nint handle)
    {
        var enabled = 1;
        if (NativeMethods.DwmSetWindowAttribute(
                handle,
                NativeMethods.DwmwaUseImmersiveDarkMode,
                ref enabled,
                sizeof(int)) != 0)
        {
            NativeMethods.DwmSetWindowAttribute(
                handle,
                NativeMethods.DwmwaUseImmersiveDarkModeBefore20H1,
                ref enabled,
                sizeof(int));
        }
    }
}

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
        if (!Enabled)
        {
            return (AppTheme.Surface, AppTheme.Border, AppTheme.TextMuted);
        }

        return VisualStyle switch
        {
            ModernButtonStyle.Primary => (
                _pressed ? AppTheme.AccentPressed : _hovered ? AppTheme.AccentHover : AppTheme.Accent,
                _pressed ? AppTheme.AccentPressed : AppTheme.AccentHover,
                Color.White),
            ModernButtonStyle.Danger => (
                _pressed ? Color.FromArgb(105, 47, 60) : _hovered ? Color.FromArgb(96, 47, 59) : AppTheme.DangerSoft,
                _hovered ? AppTheme.Danger : Color.FromArgb(120, 58, 70),
                _hovered ? Color.White : AppTheme.Danger),
            ModernButtonStyle.Ghost => (
                _pressed ? AppTheme.SurfaceRaised : _hovered ? AppTheme.SurfaceHover : AppTheme.WindowBackground,
                _hovered ? AppTheme.Border : AppTheme.WindowBackground,
                AppTheme.TextSecondary),
            _ => (
                _pressed ? AppTheme.SurfaceHover : _hovered ? AppTheme.SurfaceHover : AppTheme.SurfaceRaised,
                _hovered ? AppTheme.Accent : AppTheme.Border,
                AppTheme.TextPrimary),
        };
    }
}

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
        var textColor = !eventArgs.Item.Enabled
            ? AppTheme.TextSecondary
            : eventArgs.Item.Text.StartsWith("Emergency", StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Danger
                : eventArgs.Item.Checked
                    ? AppTheme.AccentHover
                    : AppTheme.TextPrimary;

        TextRenderer.DrawText(
            eventArgs.Graphics,
            eventArgs.Text,
            eventArgs.TextFont,
            eventArgs.TextRectangle,
            textColor,
            eventArgs.TextFormat);
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

internal static class DrawingHelpers
{
    public static GraphicsPath CreateRoundedRectangle(Rectangle bounds, int radius)
    {
        var diameter = Math.Max(1, radius * 2);
        var path = new GraphicsPath();

        if (bounds.Width <= diameter || bounds.Height <= diameter)
        {
            path.AddRectangle(bounds);
            path.CloseFigure();
            return path;
        }

        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}
