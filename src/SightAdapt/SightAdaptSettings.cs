namespace SightAdapt;

internal interface IReadOnlySightAdaptSettings
{
    int SchemaVersion { get; }

    bool AutomaticMode { get; }

    IReadOnlyList<ApplicationAssignment> Assignments { get; }

    IReadOnlyList<VisualProfile> VisualProfiles { get; }
}

internal sealed class SightAdaptSettings : IReadOnlySightAdaptSettings
{
    public static int CurrentSchemaVersion =>
        ProductInfo.SettingsSchemaVersion;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public bool AutomaticMode { get; set; } = true;

    public List<ApplicationAssignment> Assignments { get; set; } = [];

    public List<VisualProfile> VisualProfiles { get; set; } =
        VisualProfileCatalog.Default
            .CreateBuiltInProfiles()
            .ToList();

    IReadOnlyList<ApplicationAssignment>
        IReadOnlySightAdaptSettings.Assignments => Assignments;

    IReadOnlyList<VisualProfile>
        IReadOnlySightAdaptSettings.VisualProfiles => VisualProfiles;

    public SightAdaptSettings CreateWorkingCopy()
    {
        EnsureCollections();

        return new SightAdaptSettings
        {
            SchemaVersion = SchemaVersion,
            AutomaticMode = AutomaticMode,
            Assignments = Assignments
                .Where(assignment => assignment is not null)
                .Select(assignment => assignment.CreateWorkingCopy())
                .ToList(),
            VisualProfiles = VisualProfiles
                .Where(profile => profile is not null)
                .Select(profile => profile.CreateWorkingCopy())
                .ToList(),
        };
    }

    public void ReplaceWith(SightAdaptSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.EnsureCollections();

        SchemaVersion = source.SchemaVersion;
        AutomaticMode = source.AutomaticMode;
        Assignments = source.Assignments;
        VisualProfiles = source.VisualProfiles;
    }

    public void EnsureCollections()
    {
        Assignments ??= [];
        VisualProfiles ??= [];
    }
}
