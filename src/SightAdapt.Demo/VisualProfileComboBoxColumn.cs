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
    System.Windows.Forms.DataGridViewComboBoxColumn
{
    private VisualProfileOption[] _options = [];

    public StableVisualProfileComboBoxColumn()
    {
        DisplayMember =
            nameof(VisualProfileOption.Name);
        ValueMember =
            nameof(VisualProfileOption.Id);
        ValueType = typeof(string);
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
        clone.DisplayMember =
            nameof(VisualProfileOption.Name);
        clone.ValueMember =
            nameof(VisualProfileOption.Id);
        clone.ValueType = typeof(string);
        clone.Items.Clear();
        clone.Items.AddRange(
            clone._options
                .Cast<object>()
                .ToArray());
        return clone;
    }
}
