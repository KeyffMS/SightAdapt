using System.Drawing.Drawing2D;

namespace SightAdapt.Demo;

internal sealed record VisualProfileOption(
    string Id,
    string Name)
{
    public override string ToString()
    {
        return Name;
    }
}

internal sealed class StableVisualProfileComboBoxColumn :
    DataGridViewComboBoxColumn
{
    private VisualProfileOption[] _options = [];
    private DataGridView? _attachedGrid;

    public StableVisualProfileComboBoxColumn()
    {
        CellTemplate = new ModernVisualProfileComboBoxCell();
        DisplayMember = nameof(VisualProfileOption.Name);
        ValueMember = nameof(VisualProfileOption.Id);
        ValueType = typeof(string);
        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        FlatStyle = FlatStyle.Flat;
    }

    public void SetProfiles(
        IEnumerable<VisualProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var nextOptions = profiles
            .Where(profile => profile is not null)
            .Select(profile =>
                new VisualProfileOption(
                    profile.Id,
                    profile.Name))
            .ToArray();

        if (_options.SequenceEqual(nextOptions))
        {
            return;
        }

        _options = nextOptions;
        Items.Clear();
        Items.AddRange(
            _options.Cast<object>().ToArray());
    }

    public override object Clone()
    {
        var clone =
            (StableVisualProfileComboBoxColumn)
            base.Clone();
        clone._options = _options.ToArray();
        clone._attachedGrid = null;
        clone.DisplayMember = nameof(VisualProfileOption.Name);
        clone.ValueMember = nameof(VisualProfileOption.Id);
        clone.ValueType = typeof(string);
        clone.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        clone.FlatStyle = FlatStyle.Flat;
        clone.Items.Clear();
        clone.Items.AddRange(
            clone._options
                .Cast<object>()
                .ToArray());
        return clone;
    }

    protected override void OnDataGridViewChanged()
    {
        if (_attachedGrid is not null)
        {
            _attachedGrid.CellPainting -= GridCellPainting;
            _attachedGrid.EditingControlShowing -= GridEditingControlShowing;
        }

        base.OnDataGridViewChanged();
        _attachedGrid = DataGridView;

        if (_attachedGrid is not null)
        {
            _attachedGrid.CellPainting += GridCellPainting;
            _attachedGrid.EditingControlShowing += GridEditingControlShowing;
        }
    }

    private static void GridCellPainting(
        object? sender,
        DataGridViewCellPaintingEventArgs eventArgs)
    {
        if (sender is not DataGridView grid ||
            eventArgs.RowIndex < 0 ||
            eventArgs.ColumnIndex < 0 ||
            !string.Equals(
                grid.Columns[eventArgs.ColumnIndex].Name,
                "Enabled",
                StringComparison.Ordinal))
        {
            return;
        }

        var graphics = eventArgs.Graphics;
        if (graphics is null)
        {
            return;
        }

        eventArgs.PaintBackground(
            eventArgs.CellBounds,
            (eventArgs.State & DataGridViewElementStates.Selected) != 0);

        var enabled = eventArgs.FormattedValue is true;
        const int diameter = 15;
        var bounds = new Rectangle(
            eventArgs.CellBounds.Left +
                (eventArgs.CellBounds.Width - diameter) / 2,
            eventArgs.CellBounds.Top +
                (eventArgs.CellBounds.Height - diameter) / 2,
            diameter,
            diameter);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(
            enabled ? AppTheme.Success : AppTheme.Surface);
        using var border = new Pen(
            enabled ? AppTheme.Success : AppTheme.TextMuted,
            enabled ? 1.5f : 1.2f);
        graphics.FillEllipse(fill, bounds);
        graphics.DrawEllipse(border, bounds);

        if ((eventArgs.State & DataGridViewElementStates.Selected) != 0 &&
            grid.CurrentCellAddress.X == eventArgs.ColumnIndex &&
            grid.CurrentCellAddress.Y == eventArgs.RowIndex)
        {
            var focusBounds = Rectangle.Inflate(bounds, 5, 5);
            ControlPaint.DrawFocusRectangle(
                graphics,
                focusBounds,
                AppTheme.TextPrimary,
                AppTheme.Selection);
        }

        eventArgs.Handled = true;
    }

    private static void GridEditingControlShowing(
        object? sender,
        DataGridViewEditingControlShowingEventArgs eventArgs)
    {
        if (eventArgs.Control is not ComboBox comboBox)
        {
            return;
        }

        comboBox.BackColor = AppTheme.SurfaceRaised;
        comboBox.ForeColor = AppTheme.TextPrimary;
        comboBox.Font = AppTheme.CreateUiFont(9.5f);
        comboBox.FlatStyle = FlatStyle.Flat;
        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        comboBox.DrawMode = DrawMode.OwnerDrawFixed;
        comboBox.ItemHeight = 30;
        comboBox.DrawItem -= DrawComboItem;
        comboBox.DrawItem += DrawComboItem;
    }

    private static void DrawComboItem(
        object? sender,
        DrawItemEventArgs eventArgs)
    {
        if (sender is not ComboBox comboBox || eventArgs.Index < 0)
        {
            return;
        }

        var selected =
            (eventArgs.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(
            selected ? AppTheme.Selection : AppTheme.SurfaceRaised);
        eventArgs.Graphics.FillRectangle(background, eventArgs.Bounds);

        var text = comboBox.GetItemText(
            comboBox.Items[eventArgs.Index]);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            text,
            comboBox.Font,
            Rectangle.Inflate(eventArgs.Bounds, -10, 0),
            AppTheme.TextPrimary,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if ((eventArgs.State & DrawItemState.Focus) != 0)
        {
            eventArgs.DrawFocusRectangle();
        }
    }
}

internal sealed class ModernVisualProfileComboBoxCell :
    DataGridViewComboBoxCell
{
    public ModernVisualProfileComboBoxCell()
    {
        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        FlatStyle = FlatStyle.Flat;
    }

    public override object Clone()
    {
        var clone = (ModernVisualProfileComboBoxCell)base.Clone();
        clone.DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        clone.FlatStyle = FlatStyle.Flat;
        return clone;
    }

    public override void InitializeEditingControl(
        int rowIndex,
        object? initialFormattedValue,
        DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(
            rowIndex,
            initialFormattedValue,
            dataGridViewCellStyle);

        if (DataGridView?.EditingControl is ComboBox comboBox)
        {
            comboBox.BackColor = AppTheme.SurfaceRaised;
            comboBox.ForeColor = AppTheme.TextPrimary;
            comboBox.FlatStyle = FlatStyle.Flat;
            comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        }
    }

    protected override void Paint(
        Graphics graphics,
        Rectangle clipBounds,
        Rectangle cellBounds,
        int rowIndex,
        DataGridViewElementStates cellState,
        object? value,
        object? formattedValue,
        string? errorText,
        DataGridViewCellStyle cellStyle,
        DataGridViewAdvancedBorderStyle advancedBorderStyle,
        DataGridViewPaintParts paintParts)
    {
        var selected =
            (cellState & DataGridViewElementStates.Selected) != 0;
        var background = selected
            ? cellStyle.SelectionBackColor
            : cellStyle.BackColor;
        var foreground = selected
            ? cellStyle.SelectionForeColor
            : cellStyle.ForeColor;

        using (var backgroundBrush = new SolidBrush(background))
        {
            graphics.FillRectangle(backgroundBrush, cellBounds);
        }

        var selectorBounds = Rectangle.Inflate(cellBounds, -7, -6);
        selectorBounds.Width = Math.Max(1, selectorBounds.Width);
        selectorBounds.Height = Math.Max(1, selectorBounds.Height);
        using var selectorPath = DrawingHelpers.CreateRoundedRectangle(
            selectorBounds,
            7);
        using var selectorBrush = new SolidBrush(AppTheme.SurfaceRaised);
        using var selectorPen = new Pen(
            selected ? AppTheme.AccentHover : AppTheme.Border,
            selected ? 1.4f : 1f);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.FillPath(selectorBrush, selectorPath);
        graphics.DrawPath(selectorPen, selectorPath);

        const int buttonWidth = 30;
        var buttonBounds = new Rectangle(
            selectorBounds.Right - buttonWidth,
            selectorBounds.Top,
            buttonWidth,
            selectorBounds.Height);
        using (var buttonBrush = new SolidBrush(
                   selected ? AppTheme.SurfaceHover : AppTheme.Surface))
        {
            graphics.FillRectangle(buttonBrush, buttonBounds);
        }
        using (var separatorPen = new Pen(AppTheme.Border))
        {
            graphics.DrawLine(
                separatorPen,
                buttonBounds.Left,
                buttonBounds.Top + 3,
                buttonBounds.Left,
                buttonBounds.Bottom - 3);
        }

        var textBounds = new Rectangle(
            selectorBounds.Left + 10,
            selectorBounds.Top,
            Math.Max(1, selectorBounds.Width - buttonWidth - 14),
            selectorBounds.Height);
        TextRenderer.DrawText(
            graphics,
            formattedValue?.ToString() ?? string.Empty,
            cellStyle.Font,
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

        if ((cellState & DataGridViewElementStates.Selected) != 0 &&
            DataGridView?.CurrentCellAddress.X == ColumnIndex &&
            DataGridView.CurrentCellAddress.Y == rowIndex)
        {
            var focusBounds = Rectangle.Inflate(selectorBounds, -3, -3);
            ControlPaint.DrawFocusRectangle(
                graphics,
                focusBounds,
                AppTheme.TextPrimary,
                AppTheme.SurfaceRaised);
        }
    }
}
