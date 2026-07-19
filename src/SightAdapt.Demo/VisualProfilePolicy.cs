namespace SightAdapt.Demo;

internal static class VisualProfilePolicy
{
    public const string UserProfileIdPrefix = "user-";
    public const int MaximumNameLength = 80;

    public const string NewAssignmentProfileId = VisualProfile.DefaultSoftInvertId;
    public const string DeletionFallbackProfileId = VisualProfile.DefaultSoftInvertId;
    public const string MissingReferenceFallbackProfileId = VisualProfile.DefaultInvertId;

    public static bool IsBuiltInId(string? profileId)
    {
        return string.Equals(
                   profileId,
                   VisualProfile.DefaultInvertId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   profileId,
                   VisualProfile.DefaultSoftInvertId,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsSupportedTransformId(string? transformId)
    {
        return string.Equals(
                   transformId,
                   InvertVisualTransform.TransformId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   transformId,
                   SoftInvertVisualTransform.TransformId,
                   StringComparison.OrdinalIgnoreCase);
    }

    public static string CreateUserProfileId(ISet<string>? reservedIds = null)
    {
        while (true)
        {
            var candidate = UserProfileIdPrefix + Guid.NewGuid().ToString("N");
            if (reservedIds is null || !reservedIds.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    public static string CreateUniqueName(
        IEnumerable<VisualProfile> profiles,
        string? requestedBaseName,
        VisualProfile? exceptProfile = null)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var baseName = NormalizeNameOrFallback(requestedBaseName, "Custom Soft Invert");
        if (!NameExists(profiles, baseName, exceptProfile))
        {
            return baseName;
        }

        for (var suffix = 2; suffix < int.MaxValue; suffix++)
        {
            var suffixText = $" {suffix}";
            var maximumBaseLength = MaximumNameLength - suffixText.Length;
            var shortenedBase = baseName.Length <= maximumBaseLength
                ? baseName
                : baseName[..maximumBaseLength].TrimEnd();
            var candidate = shortenedBase + suffixText;

            if (!NameExists(profiles, candidate, exceptProfile))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("A unique visual profile name could not be generated.");
    }

    public static string ValidateUserName(
        IEnumerable<VisualProfile> profiles,
        string? name,
        VisualProfile? exceptProfile = null)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        var normalizedName = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException(
                "The visual profile name cannot be empty.",
                nameof(name));
        }

        if (normalizedName.Length > MaximumNameLength)
        {
            throw new ArgumentException(
                $"The visual profile name cannot exceed {MaximumNameLength} characters.",
                nameof(name));
        }

        if (NameExists(profiles, normalizedName, exceptProfile))
        {
            throw new InvalidOperationException(
                $"A visual profile named '{normalizedName}' already exists.");
        }

        return normalizedName;
    }

    public static string NormalizeNameOrFallback(string? name, string fallback)
    {
        var normalized = (name ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            normalized = fallback;
        }

        return normalized.Length <= MaximumNameLength
            ? normalized
            : normalized[..MaximumNameLength].TrimEnd();
    }

    private static bool NameExists(
        IEnumerable<VisualProfile> profiles,
        string name,
        VisualProfile? exceptProfile)
    {
        return profiles.Any(candidate =>
            candidate is not null &&
            !ReferenceEquals(candidate, exceptProfile) &&
            string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}
