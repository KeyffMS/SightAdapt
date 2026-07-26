namespace SightAdapt;

internal interface ISettingsNormalizationPass
{
    void Apply(SettingsNormalizationContext context);
}

internal static class SettingsNormalizer
{
    private static readonly ISettingsNormalizationPass[] Passes =
    [
        new SchemaVersionNormalizationPass(),
        new BuiltInVisualProfileNormalizationPass(),
        new UserVisualProfileNormalizationPass(),
        new ApplicationAssignmentNormalizationPass(),
        new ProfileReferenceNormalizationPass(),
    ];

    public static bool Normalize(
        SightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureCollections();

        var context =
            new SettingsNormalizationContext(settings);
        foreach (var pass in Passes)
        {
            pass.Apply(context);
        }

        context.Commit();
        return context.Changed;
    }
}

internal sealed class SettingsNormalizationContext
{
    private readonly SightAdaptSettings _settings;
    private readonly List<VisualProfile> _originalProfiles;
    private readonly List<ApplicationAssignment> _originalAssignments;

    public SettingsNormalizationContext(
        SightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        _settings = settings;
        _originalProfiles = settings.VisualProfiles;
        _originalAssignments = settings.Assignments;
        RemainingProfiles = settings.VisualProfiles
            .OfType<VisualProfile>()
            .ToList();
        SourceAssignments = settings.Assignments
            .OfType<ApplicationAssignment>()
            .ToList();
        Changed = RemainingProfiles.Count !=
                settings.VisualProfiles.Count ||
            SourceAssignments.Count !=
                settings.Assignments.Count;
    }

    public SightAdaptSettings Settings => _settings;

    public bool Changed { get; private set; }

    public List<VisualProfile> RemainingProfiles { get; }

    public List<ApplicationAssignment> SourceAssignments { get; }

    public List<VisualProfile> Profiles { get; } = [];

    public List<ApplicationAssignment> Assignments { get; } = [];

    public HashSet<string> ProfileIds { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public HashSet<string> ExecutablePaths { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public VisualProfile? TakeProfile(string profileId)
    {
        var profile = RemainingProfiles.FirstOrDefault(
            candidate => string.Equals(
                candidate.Id,
                profileId,
                StringComparison.OrdinalIgnoreCase));
        if (profile is not null)
        {
            RemainingProfiles.Remove(profile);
        }

        return profile;
    }

    public void AddProfile(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        Profiles.Add(profile);
        ProfileIds.Add(profile.Id);
    }

    public void MarkChanged()
    {
        Changed = true;
    }

    public void Commit()
    {
        if (!_originalProfiles.SequenceEqual(Profiles) ||
            !_originalAssignments.SequenceEqual(Assignments))
        {
            Changed = true;
        }

        _settings.VisualProfiles = Profiles;
        _settings.Assignments = Assignments;
    }
}

internal sealed class SchemaVersionNormalizationPass :
    ISettingsNormalizationPass
{
    public void Apply(SettingsNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (context.Settings.SchemaVersion ==
            SightAdaptSettings.CurrentSchemaVersion)
        {
            return;
        }

        context.Settings.SchemaVersion =
            SightAdaptSettings.CurrentSchemaVersion;
        context.MarkChanged();
    }
}
