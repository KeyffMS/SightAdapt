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

        var profile = settings.Applications.FirstOrDefault(candidate => candidate.Matches(identity));
        var wasCreated = profile is null;

        if (profile is null)
        {
            profile = new ApplicationProfile
            {
                Enabled = true,
                VisualProfileId = VisualProfile.DefaultInvertId,
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

        if (string.IsNullOrWhiteSpace(profile.VisualProfileId))
        {
            profile.VisualProfileId = VisualProfile.DefaultInvertId;
        }

        return new ApplicationProfileToggleResult(profile, wasCreated, profile.Enabled);
    }
}
