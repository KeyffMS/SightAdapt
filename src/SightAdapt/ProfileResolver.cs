namespace SightAdapt;

internal static class ProfileResolver
{
    public static ApplicationAssignment? FindAssignment(
        IReadOnlySightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);

        return settings.Assignments.FirstOrDefault(profile =>
            profile is not null && profile.Matches(identity));
    }

    public static ApplicationAssignment? FindAssignmentByExecutablePath(
        IReadOnlySightAdaptSettings settings,
        string? executablePath)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        var normalizedPath = executablePath.Trim();
        return settings.Assignments.FirstOrDefault(profile =>
            profile is not null && string.Equals(
                profile.ExecutablePath,
                normalizedPath,
                StringComparison.OrdinalIgnoreCase));
    }

    public static ApplicationAssignment RequireAssignmentByExecutablePath(
        IReadOnlySightAdaptSettings settings,
        string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        return FindAssignmentByExecutablePath(
                settings,
                executablePath) ??
            throw new SettingsValidationException(
                "The selected application assignment no longer exists.");
    }

    public static ApplicationAssignment? FindEnabledAssignment(
        IReadOnlySightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        return FindAssignment(settings, identity) is
            { Enabled: true } assignment
                ? assignment
                : null;
    }

    public static VisualProfile? FindVisualProfile(
        IReadOnlySightAdaptSettings settings,
        string? profileId)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(profileId))
        {
            return null;
        }

        return settings.VisualProfiles.FirstOrDefault(candidate =>
            candidate is not null && string.Equals(
                candidate.Id,
                profileId.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public static VisualProfile RequireVisualProfile(
        IReadOnlySightAdaptSettings settings,
        string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        return FindVisualProfile(settings, profileId) ??
            throw new SettingsValidationException(
                "The selected visual profile no longer exists.");
    }

    public static string ResolveVisualProfileName(
        IReadOnlySightAdaptSettings settings,
        string? profileId,
        string fallback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

        var name = FindVisualProfile(settings, profileId)?.Name;
        return string.IsNullOrWhiteSpace(name)
            ? fallback
            : name;
    }

    public static VisualProfile ResolveMenuVisualProfile(
        IReadOnlySightAdaptSettings settings,
        ApplicationAssignment? assignment)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return FindVisualProfile(
                settings,
                assignment?.MenuVisualProfileId)
            ?? ResolveVisualProfile(
                settings,
                assignment);
    }

    public static VisualProfile ResolveVisualProfile(
        IReadOnlySightAdaptSettings settings,
        ApplicationAssignment? assignment)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return FindVisualProfile(
                settings,
                assignment?.VisualProfileId)
            ?? FindVisualProfile(
                settings,
                VisualProfilePolicy
                    .MissingReferenceFallbackProfileId)
            ?? VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultInvertId);
    }
}
