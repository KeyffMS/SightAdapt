namespace SightAdapt;

internal sealed record ApplicationAssignmentToggleResult(
    ApplicationAssignment Assignment,
    bool WasCreated,
    bool IsEnabled);

internal static class ApplicationAssignmentService
{
    public static ApplicationAssignmentToggleResult AddOrEnable(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        return MutateAssignment(
            settings,
            identity,
            _ => true);
    }

    public static ApplicationAssignmentToggleResult Toggle(
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
        ApplicationAssignment assignment,
        bool enabled)
    {
        EnsureMutableAssignment(
            settings,
            assignment);
        assignment.Enabled = enabled;
    }

    public static void AssignVisualProfile(
        SightAdaptSettings settings,
        ApplicationAssignment assignment,
        string visualProfileId)
    {
        EnsureMutableAssignment(
            settings,
            assignment);

        assignment.VisualProfileId =
            ProfileResolver.RequireVisualProfile(
                settings,
                visualProfileId).Id;
    }

    public static void AssignMenuVisualProfile(
        SightAdaptSettings settings,
        ApplicationAssignment assignment,
        string? menuVisualProfileId)
    {
        EnsureMutableAssignment(
            settings,
            assignment);

        var normalizedId =
            ApplicationMenuProfilePolicy.FromSelectorId(
                menuVisualProfileId);
        assignment.MenuVisualProfileId = normalizedId is null
            ? null
            : ProfileResolver.RequireVisualProfile(
                settings,
                normalizedId).Id;
    }

    public static void SetOverlayScope(
        SightAdaptSettings settings,
        ApplicationAssignment assignment,
        OverlayScope overlayScope)
    {
        EnsureMutableAssignment(
            settings,
            assignment);

        if (!OverlayScopePolicy.IsSupported(overlayScope))
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");
        }

        assignment.OverlayScopeId =
            OverlayScopePolicy.ToId(overlayScope);
    }

    public static void Remove(
        SightAdaptSettings settings,
        ApplicationAssignment assignment)
    {
        EnsureMutableAssignment(
            settings,
            assignment);
        settings.Assignments.Remove(assignment);
    }

    public static int ReassignVisualProfile(
        SightAdaptSettings settings,
        string sourceProfileId,
        string targetProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureCollections();

        var target = ProfileResolver.RequireVisualProfile(
            settings,
            targetProfileId);
        var changed = 0;

        foreach (var assignment in settings.Assignments)
        {
            var assignmentChanged = false;

            if (string.Equals(
                    assignment.VisualProfileId,
                    sourceProfileId,
                    StringComparison.OrdinalIgnoreCase))
            {
                assignment.VisualProfileId = target.Id;
                assignmentChanged = true;
            }

            if (string.Equals(
                    assignment.MenuVisualProfileId,
                    sourceProfileId,
                    StringComparison.OrdinalIgnoreCase))
            {
                assignment.MenuVisualProfileId = target.Id;
                assignmentChanged = true;
            }

            if (assignmentChanged)
            {
                changed++;
            }
        }

        return changed;
    }

    public static int CountAssignments(
        IReadOnlySightAdaptSettings settings,
        string visualProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return settings.Assignments.Count(
            assignment =>
                string.Equals(
                    assignment.VisualProfileId,
                    visualProfileId,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    assignment.MenuVisualProfileId,
                    visualProfileId,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static ApplicationAssignmentToggleResult
        MutateAssignment(
            SightAdaptSettings settings,
            ApplicationIdentity identity,
            Func<bool, bool> selectEnabled)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(selectEnabled);
        settings.EnsureCollections();

        var (assignment, wasCreated) =
            GetOrCreate(settings, identity);
        assignment.Enabled = wasCreated
            ? true
            : selectEnabled(assignment.Enabled);
        SynchronizeIdentityAndReferences(
            settings,
            assignment,
            identity,
            wasCreated);

        return new ApplicationAssignmentToggleResult(
            assignment,
            wasCreated,
            assignment.Enabled);
    }

    private static (
        ApplicationAssignment Assignment,
        bool WasCreated) GetOrCreate(
            SightAdaptSettings settings,
            ApplicationIdentity identity)
    {
        var existing = ProfileResolver.FindAssignment(
            settings,
            identity);
        if (existing is not null)
        {
            return (existing, false);
        }

        var created = new ApplicationAssignment
        {
            Enabled = true,
            VisualProfileId =
                VisualProfilePolicy.NewAssignmentProfileId,
        };
        settings.Assignments.Add(created);
        return (created, true);
    }

    private static void SynchronizeIdentityAndReferences(
        SightAdaptSettings settings,
        ApplicationAssignment assignment,
        ApplicationIdentity identity,
        bool wasCreated)
    {
        assignment.DisplayName = identity.DisplayName;
        assignment.ExecutableName = identity.ExecutableName;
        assignment.ExecutablePath = identity.ExecutablePath;

        if (ProfileResolver.FindVisualProfile(
                settings,
                assignment.VisualProfileId) is null)
        {
            assignment.VisualProfileId = wasCreated
                ? VisualProfilePolicy.NewAssignmentProfileId
                : VisualProfilePolicy
                    .MissingReferenceFallbackProfileId;
        }

        var menuProfileId =
            ApplicationMenuProfilePolicy.FromSelectorId(
                assignment.MenuVisualProfileId);
        assignment.MenuVisualProfileId = menuProfileId is null
            ? null
            : ProfileResolver.FindVisualProfile(
                settings,
                menuProfileId)?.Id;
    }

    private static void EnsureMutableAssignment(
        SightAdaptSettings settings,
        ApplicationAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(assignment);
        settings.EnsureCollections();

        if (!settings.Assignments.Contains(assignment))
        {
            throw new SettingsValidationException(
                "The application assignment is not part " +
                "of the current settings.");
        }
    }
}
