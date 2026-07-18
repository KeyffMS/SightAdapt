namespace SightAdapt.Demo;

internal sealed record VisualProfileOption(string Id, string Name)
{
    public override string ToString()
    {
        return Name;
    }
}

internal sealed class DataGridViewComboBoxColumn :
    System.Windows.Forms.DataGridViewComboBoxColumn
{
    private VisualProfileOption[] _options = [];

    public DataGridViewComboBoxColumn()
    {
        DisplayMember = nameof(VisualProfileOption.Name);
        ValueMember = nameof(VisualProfileOption.Id);
        ValueType = typeof(string);
    }

    public new object? DataSource
    {
        get => null;
        set
        {
            // ConfigurationForm previously cleared and rebound DataSource while the
            // combo box was committing an edit. WinForms can throw inside
            // ItemFromComboBoxDataSource in that situation. Keep the column
            // permanently unbound and update its Items collection only when the
            // actual profile options change.
            if (value is null)
            {
                return;
            }

            if (value is not IEnumerable<VisualProfile> profiles)
            {
                throw new ArgumentException(
                    "The visual profile column accepts VisualProfile collections only.",
                    nameof(value));
            }

            var nextOptions = profiles
                .Select(profile => new VisualProfileOption(profile.Id, profile.Name))
                .ToArray();

            if (_options.SequenceEqual(nextOptions))
            {
                return;
            }

            _options = nextOptions;
            Items.Clear();
            Items.AddRange(_options.Cast<object>().ToArray());
        }
    }

    public override object Clone()
    {
        var clone = (DataGridViewComboBoxColumn)base.Clone();
        clone._options = _options.ToArray();
        clone.DisplayMember = nameof(VisualProfileOption.Name);
        clone.ValueMember = nameof(VisualProfileOption.Id);
        clone.ValueType = typeof(string);
        clone.Items.Clear();
        clone.Items.AddRange(clone._options.Cast<object>().ToArray());
        return clone;
    }
}
