namespace SightAdapt.Demo;

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
