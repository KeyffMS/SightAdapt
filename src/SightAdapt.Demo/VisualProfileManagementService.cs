namespace SightAdapt.Demo;

internal static class VisualProfileManagementService
{
    public static VisualProfile Create(
        SightAdaptSettings settings,
        string name)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureCollections();

        var normalizedName =
            VisualProfilePolicy.ValidateUserName(
                settings.VisualProfiles,
                name);
        var profile = CreateUserProfile(
            settings,
            normalizedName);
        settings.VisualProfiles.Add(profile);
        return profile;
    }

    public static VisualProfile Duplicate(
        SightAdaptSettings settings,
        VisualProfile source,
        string name)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(source);
        settings.EnsureCollections();
        EnsureMember(settings, source);

        if (!source.SupportsTuning)
        {
            throw new InvalidOperationException(
                "Only editable visual profiles can be duplicated.");
        }

        var normalizedName =
            VisualProfilePolicy.ValidateUserName(
                settings.VisualProfiles,
                name);
        var profile = source.CreateWorkingCopy();
        profile.Id =
            CreateAvailableUserProfileId(settings);
        profile.Name = normalizedName;
        settings.VisualProfiles.Add(profile);
        return profile;
    }

    public static void Rename(
        SightAdaptSettings settings,
        VisualProfile profile,
        string name)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        settings.EnsureCollections();
        EnsureMember(settings, profile);
        EnsureUserDefined(profile, "renamed");

        profile.Name =
            VisualProfilePolicy.ValidateUserName(
                settings.VisualProfiles,
                name,
                profile);
    }

    public static void UpdateTuning(
        SightAdaptSettings settings,
        VisualProfile profile,
        VisualProfile values)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(values);
        settings.EnsureCollections();
        EnsureMember(settings, profile);

        if (!profile.SupportsTuning)
        {
            throw new InvalidOperationException(
                "Only editable visual profiles can be tuned.");
        }

        VisualProfileDefaults.ApplyTuning(
            profile,
            VisualProfileDefaults
                .NormalizeSoftInvertTuning(values));
    }

    public static int Delete(
        SightAdaptSettings settings,
        VisualProfile profile,
        string fallbackProfileId =
            VisualProfilePolicy.DeletionFallbackProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        settings.EnsureCollections();
        EnsureMember(settings, profile);
        EnsureUserDefined(profile, "deleted");

        var fallback =
            ProfileResolver.FindVisualProfile(
                settings,
                fallbackProfileId);

        if (fallback is null ||
            ReferenceEquals(fallback, profile))
        {
            throw new InvalidOperationException(
                "A valid fallback visual profile is " +
                "required before deletion.");
        }

        var reassigned =
            ApplicationProfileManagementService
                .ReassignVisualProfile(
                    settings,
                    profile.Id,
                    fallback.Id);

        settings.VisualProfiles.Remove(profile);
        return reassigned;
    }

    public static int CountAssignments(
        SightAdaptSettings settings,
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        settings.EnsureCollections();

        return ApplicationProfileManagementService
            .CountAssignments(
                settings,
                profile.Id);
    }

    public static bool IsBuiltIn(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return VisualProfilePolicy.IsBuiltInId(
            profile.Id);
    }

    public static string CreateAvailableName(
        SightAdaptSettings settings,
        string baseName)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureCollections();

        return VisualProfilePolicy.CreateUniqueName(
            settings.VisualProfiles,
            baseName);
    }

    private static VisualProfile CreateUserProfile(
        SightAdaptSettings settings,
        string name)
    {
        var profile =
            VisualProfile.CreateDefaultSoftInvert();
        profile.Id =
            CreateAvailableUserProfileId(settings);
        profile.Name = name;
        return profile;
    }

    private static string CreateAvailableUserProfileId(
        SightAdaptSettings settings)
    {
        var reservedIds = settings.VisualProfiles
            .Where(profile => profile is not null)
            .Select(profile =>
                profile.Id ?? string.Empty)
            .ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        return VisualProfilePolicy
            .CreateUserProfileId(reservedIds);
    }

    private static void EnsureMember(
        SightAdaptSettings settings,
        VisualProfile profile)
    {
        if (!settings.VisualProfiles.Contains(profile))
        {
            throw new InvalidOperationException(
                "The visual profile is not part " +
                "of the current settings.");
        }
    }

    private static void EnsureUserDefined(
        VisualProfile profile,
        string operation)
    {
        if (IsBuiltIn(profile))
        {
            throw new InvalidOperationException(
                $"Built-in visual profiles cannot be " +
                $"{operation}.");
        }
    }
}
