namespace SightAdapt.Demo;

internal sealed record ApplicationProfileToggleResult(
    ApplicationProfile Profile,
    bool WasCreated,
    bool IsEnabled);

internal static class ApplicationProfileToggleService
{
    public static ApplicationProfileToggleResult Toggle(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);

        settings.Applications ??= [];
        settings.VisualProfiles ??= [];

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

        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
        profile.LegacyEffect = null;

        if (ProfileResolver.FindVisualProfile(settings, profile.VisualProfileId) is null)
        {
            profile.VisualProfileId = VisualProfilePolicy.MissingReferenceFallbackProfileId;
        }

        return new ApplicationProfileToggleResult(profile, wasCreated, profile.Enabled);
    }
}
