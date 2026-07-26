using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed class ModernSelectorComboBoxCell :
    DataGridViewComboBoxCell
{
    public ModernSelectorComboBoxCell()
    {
        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        FlatStyle = FlatStyle.Flat;
    }

    public override Type EditType =>
        typeof(ModernSelectorEditingControl);

    public override object Clone()
    {
        var clone = (ModernSelectorComboBoxCell)base.Clone();
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
            ModernSelectorEditingControl editingControl)
        {
            return;
        }

        var options = Items
            .Cast<object>()
            .OfType<ModernSelectorOption>()
            .ToArray();
        editingControl.Configure(
            options,
            Value?.ToString(),
            dataGridViewCellStyle,
            OwningColumn?.HeaderText ?? "Selection");
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
            ResolvePaintFont(
                cellStyle.Font,
                DataGridView?.Font),
            foreground,
            selected,
            focused:
                selected &&
                DataGridView?.CurrentCellAddress.X == ColumnIndex &&
                DataGridView.CurrentCellAddress.Y == rowIndex);
    }

    internal static Font ResolvePaintFont(
        Font? cellStyleFont,
        Font? gridFont)
    {
        return cellStyleFont ??
            gridFont ??
            Control.DefaultFont;
    }
}
