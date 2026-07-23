namespace SightAdapt;

internal static class ProfileResolver
{
    public static ApplicationProfile? FindAssignment(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);

        return settings.Applications?.FirstOrDefault(profile =>
            profile is not null && profile.Matches(identity));
    }

    public static ApplicationProfile? FindAssignmentByExecutablePath(
        SightAdaptSettings settings,
        string? executablePath)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        var normalizedPath = executablePath.Trim();
        return settings.Applications?.FirstOrDefault(profile =>
            profile is not null && string.Equals(
                profile.ExecutablePath,
                normalizedPath,
                StringComparison.OrdinalIgnoreCase));
    }

    public static ApplicationProfile RequireAssignmentByExecutablePath(
        SightAdaptSettings settings,
        string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        return FindAssignmentByExecutablePath(
                settings,
                executablePath) ??
            throw new InvalidOperationException(
                "The selected application assignment no longer exists.");
    }

    public static ApplicationProfile? FindEnabledAssignment(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        return FindAssignment(settings, identity) is { Enabled: true } assignment
            ? assignment
            : null;
    }

    public static VisualProfile? FindVisualProfile(
        SightAdaptSettings settings,
        string? profileId)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(profileId))
        {
            return null;
        }

        return settings.VisualProfiles?.FirstOrDefault(candidate =>
            candidate is not null && string.Equals(
                candidate.Id,
                profileId.Trim(),
                StringComparison.OrdinalIgnoreCase));
    }

    public static VisualProfile RequireVisualProfile(
        SightAdaptSettings settings,
        string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        return FindVisualProfile(settings, profileId) ??
            throw new InvalidOperationException(
                "The selected visual profile no longer exists.");
    }

    public static string ResolveVisualProfileName(
        SightAdaptSettings settings,
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

    public static VisualProfile ResolveVisualProfile(
        SightAdaptSettings settings,
        ApplicationProfile? assignment)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return FindVisualProfile(settings, assignment?.VisualProfileId)
            ?? FindVisualProfile(
                settings,
                VisualProfilePolicy.MissingReferenceFallbackProfileId)
            ?? VisualProfile.CreateDefaultInvert();
    }
}
