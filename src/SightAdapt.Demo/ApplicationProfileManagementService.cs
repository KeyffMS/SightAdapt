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
        return MutateAssignment(
            settings,
            identity,
            existingEnabled => true);
    }

    public static ApplicationProfileToggleResult Toggle(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        return MutateAssignment(
            settings,
            identity,
            existingEnabled => !existingEnabled);
    }

    public static void SetEnabled(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        settings.EnsureCollections();
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
        settings.EnsureCollections();
        EnsureMember(settings, profile);

        var visualProfile =
            ProfileResolver.FindVisualProfile(
                settings,
                visualProfileId) ??
            throw new InvalidOperationException(
                $"The visual profile " +
                $"'{visualProfileId}' does not exist.");

        profile.VisualProfileId =
            visualProfile.Id;
        profile.LegacyEffect = null;
    }

    public static void SetOverlayScope(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        OverlayScope overlayScope)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        settings.EnsureCollections();
        EnsureMember(settings, profile);

        if (!OverlayScopePolicy.IsSupported(overlayScope))
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");
        }

        profile.OverlayScopeId =
            OverlayScopePolicy.ToId(overlayScope);
    }

    public static void Remove(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        settings.EnsureCollections();
        EnsureMember(settings, profile);

        settings.Applications.Remove(profile);
    }

    public static int ReassignVisualProfile(
        SightAdaptSettings settings,
        string sourceProfileId,
        string targetProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureCollections();

        var target =
            ProfileResolver.FindVisualProfile(
                settings,
                targetProfileId) ??
            throw new InvalidOperationException(
                $"The fallback visual profile " +
                $"'{targetProfileId}' does not exist.");

        var assignments = settings.Applications
            .Where(assignment =>
                assignment is not null &&
                string.Equals(
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
        settings.EnsureCollections();

        return settings.Applications.Count(
            assignment =>
                assignment is not null &&
                string.Equals(
                    assignment.VisualProfileId,
                    visualProfileId,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationProfileToggleResult
        MutateAssignment(
            SightAdaptSettings settings,
            ApplicationIdentity identity,
            Func<bool, bool> selectEnabled)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(selectEnabled);
        settings.EnsureCollections();

        var (profile, wasCreated) =
            GetOrCreate(settings, identity);

        profile.Enabled = wasCreated
            ? true
            : selectEnabled(profile.Enabled);

        FinalizeAssignment(
            settings,
            profile,
            identity,
            wasCreated);

        return new ApplicationProfileToggleResult(
            profile,
            wasCreated,
            profile.Enabled);
    }

    private static (
        ApplicationProfile Profile,
        bool WasCreated) GetOrCreate(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        var existing =
            ProfileResolver.FindAssignment(
                settings,
                identity);

        if (existing is not null)
        {
            return (existing, false);
        }

        var created = new ApplicationProfile
        {
            Enabled = true,
            VisualProfileId =
                VisualProfilePolicy.NewAssignmentProfileId,
        };
        settings.Applications.Add(created);
        return (created, true);
    }

    private static void FinalizeAssignment(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        ApplicationIdentity identity,
        bool wasCreated)
    {
        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
        profile.LegacyEffect = null;

        if (ProfileResolver.FindVisualProfile(
                settings,
                profile.VisualProfileId) is not null)
        {
            return;
        }

        profile.VisualProfileId = wasCreated
            ? VisualProfilePolicy.NewAssignmentProfileId
            : VisualProfilePolicy
                .MissingReferenceFallbackProfileId;
    }

    private static void EnsureMember(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        if (!settings.Applications.Contains(profile))
        {
            throw new InvalidOperationException(
                "The application assignment is not part " +
                "of the current settings.");
        }
    }
}
