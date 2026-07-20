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
        Items.AddRange(_options.Cast<object>().ToArray());
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
        }

        base.OnDataGridViewChanged();
        _attachedGrid = DataGridView;

        if (_attachedGrid is not null)
        {
            _attachedGrid.CellPainting += GridCellPainting;
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
}

internal sealed class ModernVisualProfileComboBoxCell :
    DataGridViewComboBoxCell
{
    public ModernVisualProfileComboBoxCell()
    {
        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        FlatStyle = FlatStyle.Flat;
    }

    public override Type EditType =>
        typeof(ModernVisualProfileEditingControl);

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

        if (DataGridView?.EditingControl is not
            ModernVisualProfileEditingControl editingControl)
        {
            return;
        }

        var options = Items
            .Cast<object>()
            .OfType<VisualProfileOption>()
            .ToArray();
        editingControl.Configure(
            options,
            Value?.ToString(),
            dataGridViewCellStyle);
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

        ModernSelectorPainter.Paint(
            graphics,
            Rectangle.Inflate(cellBounds, -7, -6),
            formattedValue?.ToString() ?? string.Empty,
            cellStyle.Font ?? AppTheme.CreateUiFont(9.5f),
            foreground,
            selected,
            focused:
                selected &&
                DataGridView?.CurrentCellAddress.X == ColumnIndex &&
                DataGridView.CurrentCellAddress.Y == rowIndex);
    }
}

internal sealed class ModernVisualProfileEditingControl :
    Control,
    IDataGridViewEditingControl
{
    private readonly ListBox _list;
    private readonly ToolStripDropDown _dropDown;
    private VisualProfileOption[] _options = [];
    private VisualProfileOption? _selected;
    private bool _hovered;

    public ModernVisualProfileEditingControl()
    {
        AccessibleRole = AccessibleRole.ComboBox;
        BackColor = AppTheme.SurfaceRaised;
        Cursor = Cursors.Hand;
        Font = AppTheme.CreateUiFont(9.5f);
        ForeColor = AppTheme.TextPrimary;
        TabStop = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable |
            ControlStyles.UserPaint,
            true);

        _list = new ListBox
        {
            BackColor = AppTheme.SurfaceRaised,
            BorderStyle = BorderStyle.None,
            DrawMode = DrawMode.OwnerDrawFixed,
            Font = AppTheme.CreateUiFont(9.5f),
            ForeColor = AppTheme.TextPrimary,
            IntegralHeight = false,
            ItemHeight = 34,
        };
        _list.DrawItem += DrawListItem;
        _list.MouseClick += (_, _) => CommitListSelection();
        _list.KeyDown += ListKeyDown;

        var host = new ToolStripControlHost(_list)
        {
            AutoSize = false,
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        _dropDown = new ToolStripDropDown
        {
            AutoClose = true,
            BackColor = AppTheme.Border,
            DropShadowEnabled = true,
            Margin = Padding.Empty,
            Padding = new Padding(1),
            Renderer = new DarkMenuRenderer(),
        };
        _dropDown.Items.Add(host);
        _dropDown.Closed += (_, _) => Invalidate();
    }

    public DataGridView? EditingControlDataGridView { get; set; }

    public object EditingControlFormattedValue
    {
        get => _selected?.Id ?? string.Empty;
        set => SelectByValue(value?.ToString());
    }

    public int EditingControlRowIndex { get; set; }

    public bool EditingControlValueChanged { get; set; }

    public Cursor EditingPanelCursor => Cursors.Default;

    public bool RepositionEditingControlOnValueChange => false;

    public void Configure(
        IEnumerable<VisualProfileOption> options,
        string? selectedId,
        DataGridViewCellStyle style)
    {
        _options = options.ToArray();
        Font = style.Font ?? AppTheme.CreateUiFont(9.5f);
        ForeColor = AppTheme.TextPrimary;
        BackColor = AppTheme.SurfaceRaised;
        SelectByValue(selectedId);
        AccessibleName = "Visual profile";
        AccessibleDescription =
            $"Selected profile: {_selected?.Name ?? "none"}.";
        Invalidate();
    }

    public void ApplyCellStyleToEditingControl(
        DataGridViewCellStyle dataGridViewCellStyle)
    {
        Font = dataGridViewCellStyle.Font ?? AppTheme.CreateUiFont(9.5f);
        ForeColor = AppTheme.TextPrimary;
        BackColor = AppTheme.SurfaceRaised;
    }

    public bool EditingControlWantsInputKey(
        Keys keyData,
        bool dataGridViewWantsInputKey)
    {
        return keyData & Keys.KeyCode switch
        {
            Keys.Up or Keys.Down or Keys.Left or Keys.Right or
            Keys.Enter or Keys.Escape or Keys.Space or Keys.F4 => true,
            _ => !dataGridViewWantsInputKey,
        };
    }

    public object GetEditingControlFormattedValue(
        DataGridViewDataErrorContexts context)
    {
        return _selected?.Id ?? string.Empty;
    }

    public void PrepareEditingControlForEdit(bool selectAll)
    {
        Invalidate();
    }

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Up or Keys.Down or Keys.Left or Keys.Right or
            Keys.Enter or Keys.Escape or Keys.Space or Keys.F4 ||
            base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        base.OnKeyDown(eventArgs);

        if (eventArgs.KeyCode == Keys.F4 ||
            eventArgs.KeyCode == Keys.Space ||
            (eventArgs.Alt && eventArgs.KeyCode == Keys.Down))
        {
            ShowDropDown();
        }
        else if (eventArgs.KeyCode is Keys.Down or Keys.Right)
        {
            MoveSelection(1);
        }
        else if (eventArgs.KeyCode is Keys.Up or Keys.Left)
        {
            MoveSelection(-1);
        }
        else if (eventArgs.KeyCode == Keys.Enter)
        {
            if (_dropDown.Visible)
            {
                CommitListSelection();
            }
            else
            {
                ShowDropDown();
            }
        }
        else if (eventArgs.KeyCode == Keys.Escape && _dropDown.Visible)
        {
            _dropDown.Close();
        }
        else
        {
            return;
        }

        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button == MouseButtons.Left)
        {
            Focus();
            ShowDropDown();
        }
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

    protected override void OnGotFocus(EventArgs eventArgs)
    {
        base.OnGotFocus(eventArgs);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs eventArgs)
    {
        base.OnLostFocus(eventArgs);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.Clear(
            EditingControlDataGridView?.EditingPanel.BackColor ??
            AppTheme.Surface);
        ModernSelectorPainter.Paint(
            eventArgs.Graphics,
            Rectangle.Inflate(ClientRectangle, -1, -2),
            _selected?.Name ?? string.Empty,
            Font,
            ForeColor,
            selected: true,
            focused: Focused || _dropDown.Visible,
            hovered: _hovered);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _dropDown.Dispose();
            _list.Dispose();
        }

        base.Dispose(disposing);
    }

    private void ShowDropDown()
    {
        if (_dropDown.Visible || _options.Length == 0)
        {
            return;
        }

        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            _list.Items.AddRange(_options.Cast<object>().ToArray());
            _list.SelectedItem = _selected;
        }
        finally
        {
            _list.EndUpdate();
        }

        var itemCount = Math.Min(Math.Max(_options.Length, 1), 8);
        var width = Math.Max(Width, 220);
        var height = itemCount * _list.ItemHeight + 2;
        _list.Size = new Size(width, height);
        if (_dropDown.Items[0] is ToolStripControlHost host)
        {
            host.Size = _list.Size;
        }

        _dropDown.Show(this, new Point(0, Height));
        _list.Focus();
    }

    private void CommitListSelection()
    {
        if (_list.SelectedItem is VisualProfileOption option)
        {
            SelectOption(option, notifyGrid: true);
        }

        _dropDown.Close();
        Focus();
    }

    private void ListKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.KeyCode == Keys.Enter)
        {
            CommitListSelection();
            eventArgs.Handled = true;
            eventArgs.SuppressKeyPress = true;
        }
        else if (eventArgs.KeyCode == Keys.Escape)
        {
            _dropDown.Close();
            Focus();
            eventArgs.Handled = true;
            eventArgs.SuppressKeyPress = true;
        }
    }

    private void MoveSelection(int direction)
    {
        if (_options.Length == 0)
        {
            return;
        }

        var currentIndex = _selected is null
            ? -1
            : Array.IndexOf(_options, _selected);
        var nextIndex = Math.Clamp(
            currentIndex + direction,
            0,
            _options.Length - 1);
        SelectOption(_options[nextIndex], notifyGrid: true);
    }

    private void SelectByValue(string? value)
    {
        var option = _options.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, value, StringComparison.OrdinalIgnoreCase));
        SelectOption(option ?? _options.FirstOrDefault(), notifyGrid: false);
    }

    private void SelectOption(
        VisualProfileOption? option,
        bool notifyGrid)
    {
        if (Equals(_selected, option))
        {
            return;
        }

        _selected = option;
        AccessibleDescription =
            $"Selected profile: {_selected?.Name ?? "none"}.";
        Invalidate();

        if (!notifyGrid)
        {
            return;
        }

        EditingControlValueChanged = true;
        if (EditingControlDataGridView?.CurrentCell is { } cell)
        {
            cell.Value = _selected?.Id;
            EditingControlDataGridView.NotifyCurrentCellDirty(true);
        }
    }

    private static void DrawListItem(
        object? sender,
        DrawItemEventArgs eventArgs)
    {
        if (sender is not ListBox list || eventArgs.Index < 0)
        {
            return;
        }

        var selected =
            (eventArgs.State & DrawItemState.Selected) != 0;
        using var background = new SolidBrush(
            selected ? AppTheme.Selection : AppTheme.SurfaceRaised);
        eventArgs.Graphics.FillRectangle(background, eventArgs.Bounds);

        var text = list.GetItemText(list.Items[eventArgs.Index]);
        TextRenderer.DrawText(
            eventArgs.Graphics,
            text,
            list.Font,
            Rectangle.Inflate(eventArgs.Bounds, -12, 0),
            AppTheme.TextPrimary,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);

        if ((eventArgs.State & DrawItemState.Focus) != 0)
        {
            ControlPaint.DrawFocusRectangle(
                eventArgs.Graphics,
                Rectangle.Inflate(eventArgs.Bounds, -3, -3),
                AppTheme.TextPrimary,
                selected ? AppTheme.Selection : AppTheme.SurfaceRaised);
        }
    }
}

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
