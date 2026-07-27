namespace SightAdapt;

internal sealed class VisualProfileDefinition
{
    private readonly Func<VisualProfile, VisualProfileTuning>
        _normalizeTuning;

    public VisualProfileDefinition(
        string profileId,
        string transformId,
        string displayName,
        bool supportsTuning,
        IVisualTransform transform,
        Func<VisualProfile, VisualProfileTuning> normalizeTuning)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(transformId);
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        ArgumentNullException.ThrowIfNull(transform);
        ArgumentNullException.ThrowIfNull(normalizeTuning);

        ProfileId = profileId;
        TransformId = transformId;
        DisplayName = displayName;
        SupportsTuning = supportsTuning;
        Transform = transform;
        _normalizeTuning = normalizeTuning;
    }

    public string ProfileId { get; }

    public string TransformId { get; }

    public string DisplayName { get; }

    public bool SupportsTuning { get; }

    public IVisualTransform Transform { get; }

    public VisualProfile CreateBuiltInProfile()
    {
        var profile = new VisualProfile
        {
            Id = ProfileId,
            Name = DisplayName,
            TransformId = TransformId,
        };
        VisualProfileDefaults.ApplyTuning(
            profile,
            _normalizeTuning(profile));
        return profile;
    }

    public bool CanonicalizeBuiltInProfile(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var changed = !string.Equals(
                profile.Id,
                ProfileId,
                StringComparison.Ordinal) ||
            !string.Equals(
                profile.Name,
                DisplayName,
                StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                TransformId,
                StringComparison.Ordinal);

        profile.Id = ProfileId;
        profile.Name = DisplayName;
        profile.TransformId = TransformId;
        return VisualProfileDefaults.ApplyTuningIfChanged(
                profile,
                _normalizeTuning(profile)) ||
            changed;
    }

    public bool NormalizeTuning(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return VisualProfileDefaults.ApplyTuningIfChanged(
            profile,
            _normalizeTuning(profile));
    }
}

internal sealed class VisualProfileCatalog
{
    public const string DefaultNoneId = "default-none";
    public const string DefaultInvertId = "default-invert";
    public const string DefaultSoftInvertId = "default-soft-invert";
    public const string DefaultUserProfileBaseName =
        "Custom Soft Invert";

    private static readonly VisualProfileDefinition[]
        CanonicalDefinitions =
        [
            new(
                DefaultInvertId,
                InvertVisualTransform.TransformId,
                "Exact invert",
                supportsTuning: false,
                new InvertVisualTransform(),
                _ => VisualProfileDefaults.ExactInvertTuning),
            new(
                DefaultSoftInvertId,
                SoftInvertVisualTransform.TransformId,
                "Soft invert",
                supportsTuning: true,
                new SoftInvertVisualTransform(),
                VisualProfileDefaults.NormalizeSoftInvertTuning),
            new(
                DefaultNoneId,
                NoneVisualTransform.TransformId,
                "None",
                supportsTuning: false,
                new NoneVisualTransform(),
                _ => VisualProfileDefaults.ExactInvertTuning),
        ];

    private static readonly IReadOnlyList<VisualProfileDefinition>
        ReadOnlyDefinitions = Array.AsReadOnly(
            CanonicalDefinitions);

    private readonly IReadOnlyDictionary<
        string,
        VisualProfileDefinition> _definitionsByProfileId;
    private readonly IReadOnlyDictionary<
        string,
        VisualProfileDefinition> _definitionsByTransformId;

    private VisualProfileCatalog()
    {
        _definitionsByProfileId =
            CanonicalDefinitions.ToDictionary(
                definition => definition.ProfileId,
                StringComparer.OrdinalIgnoreCase);
        _definitionsByTransformId =
            CanonicalDefinitions.ToDictionary(
                definition => definition.TransformId,
                StringComparer.OrdinalIgnoreCase);
    }

    public static VisualProfileCatalog Default { get; } = new();

    public IReadOnlyList<VisualProfileDefinition> Definitions =>
        ReadOnlyDefinitions;

    public IEnumerable<VisualProfile> CreateBuiltInProfiles()
    {
        return CanonicalDefinitions.Select(
            definition => definition.CreateBuiltInProfile());
    }

    public VisualProfile CreateBuiltInProfile(
        string profileId)
    {
        return GetRequiredBuiltInDefinition(profileId)
            .CreateBuiltInProfile();
    }

    public bool IsBuiltInId(string? profileId)
    {
        return !string.IsNullOrWhiteSpace(profileId) &&
            _definitionsByProfileId.ContainsKey(profileId.Trim());
    }

    public bool TryGetBuiltInDefinition(
        string? profileId,
        out VisualProfileDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(profileId) &&
            _definitionsByProfileId.TryGetValue(
                profileId.Trim(),
                out var found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }

    public VisualProfileDefinition GetRequiredBuiltInDefinition(
        string profileId)
    {
        if (TryGetBuiltInDefinition(
                profileId,
                out var definition))
        {
            return definition;
        }

        throw new InvalidOperationException(
            $"The built-in visual profile '{profileId}' is not registered.");
    }

    public bool IsSupportedTransform(string? transformId)
    {
        return TryGetTransformDefinition(
            transformId,
            out _);
    }

    public bool SupportsTuning(string? transformId)
    {
        return TryGetTransformDefinition(
                transformId,
                out var definition) &&
            definition.SupportsTuning;
    }

    public string GetTransformDisplayName(
        string? transformId)
    {
        return TryGetTransformDefinition(
                transformId,
                out var definition)
            ? definition.DisplayName
            : transformId?.Trim() ?? string.Empty;
    }

    public IVisualTransform GetRequiredTransform(
        string transformId)
    {
        if (TryGetTransformDefinition(
                transformId,
                out var definition))
        {
            return definition.Transform;
        }

        throw new InvalidOperationException(
            $"The visual transform '{transformId}' is not registered.");
    }

    public bool NormalizeTuningForTransform(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return TryGetTransformDefinition(
                profile.TransformId,
                out var definition)
            ? definition.NormalizeTuning(profile)
            : throw new InvalidOperationException(
                $"The visual transform '{profile.TransformId}' is not registered.");
    }

    private bool TryGetTransformDefinition(
        string? transformId,
        out VisualProfileDefinition definition)
    {
        if (!string.IsNullOrWhiteSpace(transformId) &&
            _definitionsByTransformId.TryGetValue(
                transformId.Trim(),
                out var found))
        {
            definition = found;
            return true;
        }

        definition = null!;
        return false;
    }
}
