namespace SightAdapt.Demo;

internal static class VisualProfileManagementService
{
    private const string UserProfileIdPrefix = "user-";

    public static VisualProfile Create(
        SightAdaptSettings settings,
        string name)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalizedName = ValidateName(settings, name, exceptProfile: null);
        var profile = CreateUserProfile(normalizedName);
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

        if (!source.SupportsTuning)
        {
            throw new InvalidOperationException(
                "Only editable Soft Invert profiles can be duplicated.");
        }

        var normalizedName = ValidateName(settings, name, exceptProfile: null);
        var profile = source.CreateWorkingCopy();
        profile.Id = CreateUserProfileId();
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
        EnsureUserDefined(profile, "renamed");

        profile.Name = ValidateName(settings, name, profile);
    }

    public static int Delete(
        SightAdaptSettings settings,
        VisualProfile profile,
        string fallbackProfileId = VisualProfile.DefaultSoftInvertId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureUserDefined(profile, "deleted");

        var fallback = settings.VisualProfiles.FirstOrDefault(candidate =>
            string.Equals(
                candidate.Id,
                fallbackProfileId,
                StringComparison.OrdinalIgnoreCase));

        if (fallback is null || ReferenceEquals(fallback, profile))
        {
            throw new InvalidOperationException(
                "A valid fallback visual profile is required before deletion.");
        }

        var assignments = settings.Applications
            .Where(assignment => string.Equals(
                assignment.VisualProfileId,
                profile.Id,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var assignment in assignments)
        {
            assignment.VisualProfileId = fallback.Id;
        }

        if (!settings.VisualProfiles.Remove(profile))
        {
            throw new InvalidOperationException(
                "The visual profile is not part of the current settings.");
        }

        return assignments.Length;
    }

    public static int CountAssignments(
        SightAdaptSettings settings,
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);

        return settings.Applications.Count(assignment => string.Equals(
            assignment.VisualProfileId,
            profile.Id,
            StringComparison.OrdinalIgnoreCase));
    }

    public static bool IsBuiltIn(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return string.Equals(
                   profile.Id,
                   VisualProfile.DefaultInvertId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   profile.Id,
                   VisualProfile.DefaultSoftInvertId,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static string CreateAvailableName(
        SightAdaptSettings settings,
        string baseName)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var normalizedBase = string.IsNullOrWhiteSpace(baseName)
            ? "Custom Soft Invert"
            : baseName.Trim();

        if (!NameExists(settings, normalizedBase, exceptProfile: null))
        {
            return normalizedBase;
        }

        for (var suffix = 2; suffix < int.MaxValue; suffix++)
        {
            var candidate = $"{normalizedBase} {suffix}";
            if (!NameExists(settings, candidate, exceptProfile: null))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("A unique profile name could not be generated.");
    }

    private static VisualProfile CreateUserProfile(string name)
    {
        var defaults = VisualProfile.CreateDefaultSoftInvert();
        defaults.Id = CreateUserProfileId();
        defaults.Name = name;
        return defaults;
    }

    private static string CreateUserProfileId()
    {
        return UserProfileIdPrefix + Guid.NewGuid().ToString("N");
    }

    private static string ValidateName(
        SightAdaptSettings settings,
        string name,
        VisualProfile? exceptProfile)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException(
                "The visual profile name cannot be empty.",
                nameof(name));
        }

        if (normalizedName.Length > 80)
        {
            throw new ArgumentException(
                "The visual profile name cannot exceed 80 characters.",
                nameof(name));
        }

        if (NameExists(settings, normalizedName, exceptProfile))
        {
            throw new InvalidOperationException(
                $"A visual profile named '{normalizedName}' already exists.");
        }

        return normalizedName;
    }

    private static bool NameExists(
        SightAdaptSettings settings,
        string name,
        VisualProfile? exceptProfile)
    {
        return settings.VisualProfiles.Any(candidate =>
            !ReferenceEquals(candidate, exceptProfile) &&
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureUserDefined(
        VisualProfile profile,
        string operation)
    {
        if (IsBuiltIn(profile))
        {
            throw new InvalidOperationException(
                $"Built-in visual profiles cannot be {operation}.");
        }
    }
}
