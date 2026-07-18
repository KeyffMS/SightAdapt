namespace SightAdapt.Demo;

internal static class ProfileResolver
{
    public static ApplicationProfile? FindAssignment(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);

        return settings.Applications.FirstOrDefault(profile => profile.Matches(identity));
    }

    public static ApplicationProfile? FindEnabledAssignment(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        return FindAssignment(settings, identity) is { Enabled: true } assignment
            ? assignment
            : null;
    }

    public static VisualProfile ResolveVisualProfile(
        SightAdaptSettings settings,
        ApplicationProfile? assignment)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var requestedId = assignment?.VisualProfileId;
        var profile = settings.VisualProfiles.FirstOrDefault(
            candidate => string.Equals(
                candidate.Id,
                requestedId,
                StringComparison.OrdinalIgnoreCase));

        return profile ?? settings.VisualProfiles.FirstOrDefault(
            candidate => string.Equals(
                candidate.Id,
                VisualProfile.DefaultInvertId,
                StringComparison.OrdinalIgnoreCase))
            ?? VisualProfile.CreateDefaultInvert();
    }
}
