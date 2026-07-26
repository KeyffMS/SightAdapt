using System.Drawing.Drawing2D;

namespace SightAdapt;

internal enum MenuItemRole
{
    Default,
    Status,
    Danger,
}

internal static class AppTheme
{
    public static readonly Color WindowBackground = Color.FromArgb(20, 23, 31);
    public static readonly Color HeaderBackground = Color.FromArgb(24, 28, 38);
    public static readonly Color Surface = Color.FromArgb(29, 34, 45);
    public static readonly Color SurfaceAlternate = Color.FromArgb(32, 38, 50);
    public static readonly Color SurfaceRaised = Color.FromArgb(36, 42, 55);
    public static readonly Color SurfaceHover = Color.FromArgb(45, 53, 69);
    public static readonly Color Border = Color.FromArgb(54, 63, 81);
    public static readonly Color TextPrimary = Color.FromArgb(239, 243, 250);
    public static readonly Color TextSecondary = Color.FromArgb(190, 200, 216);
    public static readonly Color TextMuted = Color.FromArgb(151, 164, 184);
    public static readonly Color Accent = Color.FromArgb(112, 139, 255);
    public static readonly Color AccentHover = Color.FromArgb(130, 154, 255);
    public static readonly Color AccentPressed = Color.FromArgb(91, 117, 229);
    public static readonly Color AccentSoft = Color.FromArgb(50, 62, 105);
    public static readonly Color Success = Color.FromArgb(77, 211, 169);
    public static readonly Color SuccessSoft = Color.FromArgb(30, 76, 67);
    public static readonly Color Danger = Color.FromArgb(255, 111, 128);
    public static readonly Color DangerSoft = Color.FromArgb(84, 43, 54);
    public static readonly Color DangerHover = Color.FromArgb(96, 47, 59);
    public static readonly Color DangerPressed = Color.FromArgb(105, 47, 60);
    public static readonly Color DangerBorder = Color.FromArgb(120, 58, 70);
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
        MenuItemRole role = MenuItemRole.Default)
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
            BackColor = SurfaceAlternate,
            ForeColor = TextPrimary,
            SelectionBackColor = Selection,
            SelectionForeColor = TextPrimary,
        };
    }

    private static void EnableDarkTitleBar(nint handle)
    {
        var enabled = 1;
        if (NativeDwmApi.Default.SetWindowAttribute(
                handle,
                NativeConstants.DwmwaUseImmersiveDarkMode,
                ref enabled,
                sizeof(int)) != 0)
        {
            NativeDwmApi.Default.SetWindowAttribute(
                handle,
                NativeConstants.DwmwaUseImmersiveDarkModeBefore20H1,
                ref enabled,
                sizeof(int));
        }
    }
}
