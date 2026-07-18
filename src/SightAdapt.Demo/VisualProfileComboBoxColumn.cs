namespace SightAdapt.Demo;

internal sealed class DataGridViewComboBoxColumn :
    System.Windows.Forms.DataGridViewComboBoxColumn
{
    public DataGridViewComboBoxColumn()
    {
        CellTemplate = new VisualProfileComboBoxCell();
        EnsureProfileBinding();
    }

    public new object? DataSource
    {
        get => base.DataSource;
        set
        {
            base.DataSource = value;
            EnsureProfileBinding();
        }
    }

    internal void EnsureProfileBinding()
    {
        DisplayMember = nameof(VisualProfile.Name);
        ValueMember = nameof(VisualProfile.Id);
    }

    public override object Clone()
    {
        var clone = (DataGridViewComboBoxColumn)base.Clone();
        clone.EnsureProfileBinding();
        return clone;
    }
}

internal sealed class VisualProfileComboBoxCell :
    System.Windows.Forms.DataGridViewComboBoxCell
{
    public override void InitializeEditingControl(
        int rowIndex,
        object? initialFormattedValue,
        DataGridViewCellStyle dataGridViewCellStyle)
    {
        base.InitializeEditingControl(
            rowIndex,
            initialFormattedValue,
            dataGridViewCellStyle);

        if (DataGridView?.EditingControl is DataGridViewComboBoxEditingControl editor)
        {
            editor.DisplayMember = nameof(VisualProfile.Name);
            editor.ValueMember = nameof(VisualProfile.Id);
        }
    }

    public override object Clone()
    {
        return base.Clone();
    }
}
