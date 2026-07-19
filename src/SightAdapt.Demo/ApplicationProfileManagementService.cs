namespace SightAdapt.Demo;

internal sealed record ApplicationProfileToggleResult(
    ApplicationProfile Profile,
    bool WasCreated,
    bool IsEnabled);

internal static class ApplicationProfileManagementService
{
    public static ApplicationProfileToggleResult AddOrEnable(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);
        EnsureCollections(settings);

        var profile = ProfileResolver.FindAssignment(settings, identity);
        var wasCreated = profile is null;

        if (profile is null)
        {
            profile = new ApplicationProfile
            {
                VisualProfileId = VisualProfilePolicy.NewAssignmentProfileId,
            };
            settings.Applications.Add(profile);
        }

        UpdateIdentity(profile, identity);
        profile.Enabled = true;
        profile.LegacyEffect = null;
        EnsureValidProfileReference(settings, profile);

        return new ApplicationProfileToggleResult(profile, wasCreated, profile.Enabled);
    }

    public static ApplicationProfileToggleResult Toggle(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);
        EnsureCollections(settings);

        var profile = ProfileResolver.FindAssignment(settings, identity);
        var wasCreated = profile is null;

        if (profile is null)
        {
            profile = new ApplicationProfile
            {
                Enabled = true,
                VisualProfileId = VisualProfilePolicy.NewAssignmentProfileId,
            };
            settings.Applications.Add(profile);
        }
        else
        {
            profile.Enabled = !profile.Enabled;
        }

        UpdateIdentity(profile, identity);
        profile.LegacyEffect = null;
        EnsureValidProfileReference(settings, profile);

        return new ApplicationProfileToggleResult(profile, wasCreated, profile.Enabled);
    }

    public static void SetEnabled(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureCollections(settings);
        EnsureMember(settings, profile);
        profile.Enabled = enabled;
    }

    public static void AssignVisualProfile(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        string visualProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureCollections(settings);
        EnsureMember(settings, profile);

        var visualProfile = ProfileResolver.FindVisualProfile(settings, visualProfileId)
            ?? throw new InvalidOperationException(
                $"The visual profile '{visualProfileId}' does not exist.");

        profile.VisualProfileId = visualProfile.Id;
        profile.LegacyEffect = null;
    }

    public static void Remove(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureCollections(settings);
        EnsureMember(settings, profile);
        settings.Applications.Remove(profile);
    }

    public static int ReassignVisualProfile(
        SightAdaptSettings settings,
        string sourceProfileId,
        string targetProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        EnsureCollections(settings);

        var target = ProfileResolver.FindVisualProfile(settings, targetProfileId)
            ?? throw new InvalidOperationException(
                $"The fallback visual profile '{targetProfileId}' does not exist.");

        var assignments = settings.Applications
            .Where(assignment => assignment is not null && string.Equals(
                assignment.VisualProfileId,
                sourceProfileId,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var assignment in assignments)
        {
            assignment.VisualProfileId = target.Id;
            assignment.LegacyEffect = null;
        }

        return assignments.Length;
    }

    public static int CountAssignments(
        SightAdaptSettings settings,
        string visualProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        EnsureCollections(settings);

        return settings.Applications.Count(assignment =>
            assignment is not null && string.Equals(
                assignment.VisualProfileId,
                visualProfileId,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureCollections(SightAdaptSettings settings)
    {
        settings.Applications ??= [];
        settings.VisualProfiles ??= [];
    }

    private static void EnsureMember(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        if (!settings.Applications.Contains(profile))
        {
            throw new InvalidOperationException(
                "The application assignment is not part of the current settings.");
        }
    }

    private static void EnsureValidProfileReference(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        if (ProfileResolver.FindVisualProfile(settings, profile.VisualProfileId) is null)
        {
            profile.VisualProfileId = VisualProfilePolicy.NewAssignmentProfileId;
        }
    }

    private static void UpdateIdentity(
        ApplicationProfile profile,
        ApplicationIdentity identity)
    {
        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
    }
}
