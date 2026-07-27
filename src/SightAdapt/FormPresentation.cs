namespace SightAdapt;

internal static class FormPresentation
{
    public static Label CreateHeaderLabel(
        string text,
        float size,
        FontStyle style,
        Color color,
        ContentAlignment alignment,
        bool autoEllipsis = true)
    {
        return new Label
        {
            AutoEllipsis = autoEllipsis,
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = color,
            Font = AppTheme.CreateUiFont(size, style),
            Text = text,
            TextAlign = alignment,
        };
    }

    public static Label CreateCountLabel()
    {
        return new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 18, 0),
            TextAlign = ContentAlignment.MiddleRight,
        };
    }

    public static ModernButton CreateActionButton(
        string text,
        ModernButtonStyle style,
        Action action,
        int minimumWidth = 110,
        Padding? margin = null)
    {
        ArgumentNullException.ThrowIfNull(action);

        var button = new ModernButton
        {
            Text = text,
            VisualStyle = style,
            MinimumSize = new Size(minimumWidth, 40),
            Margin = margin ?? new Padding(0, 0, 8, 0),
        };
        button.Click += (_, _) => action();
        return button;
    }

    public static DataGridViewTextBoxColumn CreateReadOnlyTextColumn(
        string name,
        string header,
        int width,
        bool fill = false,
        int? minimumWidth = null)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            AutoSizeMode = fill
                ? DataGridViewAutoSizeColumnMode.Fill
                : DataGridViewAutoSizeColumnMode.None,
            MinimumWidth = minimumWidth ?? width,
            ReadOnly = true,
            Width = width,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
    }

    public static Control CreateSectionCard(
        string title,
        Control content,
        Padding margin)
    {
        ArgumentNullException.ThrowIfNull(content);

        var host = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        host.ColumnStyles.Add(
            new ColumnStyle(SizeType.Percent, 100));
        host.RowStyles.Add(
            new RowStyle(SizeType.Absolute, 32));
        host.RowStyles.Add(
            new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(CreateHeaderLabel(
            title,
            9.2f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.MiddleLeft), 0, 0);
        host.Controls[0].Padding = new Padding(16, 6, 0, 0);
        host.Controls.Add(content, 0, 1);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = margin,
            Padding = new Padding(1),
        };
        card.Controls.Add(host);
        return card;
    }
}
