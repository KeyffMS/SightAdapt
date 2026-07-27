using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed class ModernSelectorEditingControl :
    Control,
    IDataGridViewEditingControl
{
    private readonly Font _defaultFont;
    private readonly ListBox _list;
    private readonly ToolStripDropDown _dropDown;
    private ModernSelectorOption[] _options = [];
    private ModernSelectorOption? _selected;
    private bool _hovered;

    public ModernSelectorEditingControl()
    {
        _defaultFont = AppTheme.CreateUiFont(9.5f);

        AccessibleRole = AccessibleRole.ComboBox;
        BackColor = AppTheme.SurfaceRaised;
        Cursor = Cursors.Hand;
        Font = _defaultFont;
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
            Font = _defaultFont,
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
        get => _selected?.Name ?? string.Empty;
        set => SelectByValue(value?.ToString());
    }

    public int EditingControlRowIndex { get; set; }

    public bool EditingControlValueChanged { get; set; }

    public Cursor EditingPanelCursor => Cursors.Default;

    public bool RepositionEditingControlOnValueChange => false;

    public void Configure(
        IEnumerable<ModernSelectorOption> options,
        string? selectedId,
        DataGridViewCellStyle style,
        string accessibleName)
    {
        _options = options.ToArray();
        EditingControlValueChanged = false;
        Font = style.Font ?? _defaultFont;
        ForeColor = AppTheme.TextPrimary;
        BackColor = AppTheme.SurfaceRaised;
        SelectByValue(selectedId);
        AccessibleName = accessibleName;
        AccessibleDescription =
            $"Selected option: {_selected?.Name ?? "none"}.";
        Invalidate();
    }

    public void ApplyCellStyleToEditingControl(
        DataGridViewCellStyle dataGridViewCellStyle)
    {
        Font = dataGridViewCellStyle.Font ?? _defaultFont;
        ForeColor = AppTheme.TextPrimary;
        BackColor = AppTheme.SurfaceRaised;
    }

    public bool EditingControlWantsInputKey(
        Keys keyData,
        bool dataGridViewWantsInputKey)
    {
        return (keyData & Keys.KeyCode) switch
        {
            Keys.Up or Keys.Down or Keys.Left or Keys.Right or
            Keys.Enter or Keys.Escape or Keys.Space or Keys.F4 => true,
            _ => !dataGridViewWantsInputKey,
        };
    }

    public object GetEditingControlFormattedValue(
        DataGridViewDataErrorContexts context)
    {
        return _selected?.Name ?? string.Empty;
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
            _defaultFont.Dispose();
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
        if (_list.SelectedItem is ModernSelectorOption option)
        {
            SelectOptionFromInput(option);
        }

        _dropDown.Close();
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
        SelectOptionFromInput(_options[nextIndex]);
    }

    internal void SelectOptionFromInput(ModernSelectorOption option)
    {
        ArgumentNullException.ThrowIfNull(option);
        SelectOption(option, notifyGrid: true);
    }

    private void SelectByValue(string? value)
    {
        var option = _options.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, value, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(candidate.Name, value, StringComparison.OrdinalIgnoreCase));
        SelectOption(option ?? _options.FirstOrDefault(), notifyGrid: false);
    }

    private void SelectOption(
        ModernSelectorOption? option,
        bool notifyGrid)
    {
        if (Equals(_selected, option))
        {
            return;
        }

        _selected = option;
        AccessibleDescription =
            $"Selected option: {_selected?.Name ?? "none"}.";
        Invalidate();

        if (!notifyGrid)
        {
            return;
        }

        EditingControlValueChanged = true;
        EditingControlDataGridView?.NotifyCurrentCellDirty(true);
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
