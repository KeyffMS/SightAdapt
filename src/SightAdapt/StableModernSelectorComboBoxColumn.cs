using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed class StableModernSelectorComboBoxColumn :
    DataGridViewComboBoxColumn
{
    private ModernSelectorOption[] _options = [];

    public StableModernSelectorComboBoxColumn()
    {
        CellTemplate = new ModernSelectorComboBoxCell();
        DisplayMember = nameof(ModernSelectorOption.Name);
        ValueMember = nameof(ModernSelectorOption.Id);
        ValueType = typeof(string);
        DisplayStyle = DataGridViewComboBoxDisplayStyle.Nothing;
        FlatStyle = FlatStyle.Flat;
    }

    public void SetProfiles(
        IEnumerable<VisualProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        SetOptions(profiles
            .Where(profile => profile is not null)
            .Select(profile =>
                new ModernSelectorOption(
                    profile.Id,
                    profile.Name)));
    }

    public void SetOptions(
        IEnumerable<ModernSelectorOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var nextOptions = options.ToArray();
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
            (StableModernSelectorComboBoxColumn)
            base.Clone();
        clone._options = _options.ToArray();
        clone.DisplayMember = nameof(ModernSelectorOption.Name);
        clone.ValueMember = nameof(ModernSelectorOption.Id);
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

}
