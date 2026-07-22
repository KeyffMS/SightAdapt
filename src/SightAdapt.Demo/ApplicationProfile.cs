using System.Text.Json.Serialization;

namespace SightAdapt.Demo;

internal sealed class SightAdaptSettings
{
    public const int CurrentSchemaVersion = 4;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public bool AutomaticMode { get; set; } = true;

    public List<ApplicationProfile> Applications { get; set; } = [];

    public List<VisualProfile> VisualProfiles { get; set; } =
    [
        VisualProfile.CreateDefaultInvert(),
        VisualProfile.CreateDefaultSoftInvert(),
    ];

    public SightAdaptSettings CreateWorkingCopy()
    {
        EnsureCollections();

        return new SightAdaptSettings
        {
            SchemaVersion = SchemaVersion,
            AutomaticMode = AutomaticMode,
            Applications = Applications
                .Where(profile => profile is not null)
                .Select(profile => profile.CreateWorkingCopy())
                .ToList(),
            VisualProfiles = VisualProfiles
                .Where(profile => profile is not null)
                .Select(profile => profile.CreateWorkingCopy())
                .ToList(),
        };
    }

    public void ReplaceWith(SightAdaptSettings source)
    {
        ArgumentNullException.ThrowIfNull(source);
        source.EnsureCollections();

        SchemaVersion = source.SchemaVersion;
        AutomaticMode = source.AutomaticMode;
        Applications = source.Applications;
        VisualProfiles = source.VisualProfiles;
    }

    public void EnsureCollections()
    {
        Applications ??= [];
        VisualProfiles ??= [];
    }
}

internal sealed class ApplicationProfile
{
    public string DisplayName { get; set; } = string.Empty;

    public string ExecutableName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public string VisualProfileId { get; set; } =
        VisualProfilePolicy.NewAssignmentProfileId;

    private string _overlayScopeId =
        OverlayScopePolicy.ToId(OverlayScopePolicy.Default);

    [JsonPropertyName("overlayScope")]
    public string OverlayScopeId
    {
        get => _overlayScopeId;
        set
        {
            OverlayScopePolicy.TryParseId(value, out var scope);
            _overlayScopeId = OverlayScopePolicy.ToId(scope);
        }
    }

    [JsonIgnore]
    public OverlayScope OverlayScope =>
        OverlayScopePolicy.ParseRequired(OverlayScopeId);

    [JsonPropertyName("effect")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyEffect { get; set; }

    [JsonIgnore]
    public string Effect
    {
        get => InvertVisualTransform.TransformId;
        set
        {
            VisualProfileId = VisualProfile.DefaultInvertId;
            LegacyEffect = null;
        }
    }

    public ApplicationProfile CreateWorkingCopy()
    {
        return new ApplicationProfile
        {
            DisplayName = DisplayName,
            ExecutableName = ExecutableName,
            ExecutablePath = ExecutablePath,
            Enabled = Enabled,
            VisualProfileId = VisualProfileId,
            OverlayScopeId = OverlayScopeId,
            LegacyEffect = LegacyEffect,
        };
    }

    public bool Matches(ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        if (!string.IsNullOrWhiteSpace(ExecutablePath) &&
            !string.IsNullOrWhiteSpace(identity.ExecutablePath))
        {
            return string.Equals(
                ExecutablePath,
                identity.ExecutablePath,
                StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            ExecutableName,
            identity.ExecutableName,
            StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed class VisualProfile
{
    public const string DefaultInvertId = "default-invert";
    public const string DefaultSoftInvertId = "default-soft-invert";

    public string Id { get; set; } = DefaultSoftInvertId;

    public string Name { get; set; } = VisualProfileDefaults.SoftInvertName;

    public string TransformId { get; set; } = SoftInvertVisualTransform.TransformId;

    public float OutputBlack { get; set; } = VisualProfileDefaults.SoftOutputBlack;

    public float OutputWhite { get; set; } = VisualProfileDefaults.SoftOutputWhite;

    public float Brightness { get; set; } = VisualProfileDefaults.SoftBrightness;

    public float Contrast { get; set; } = VisualProfileDefaults.SoftContrast;

    public float Saturation { get; set; } = VisualProfileDefaults.SoftSaturation;

    public float HueShiftDegrees { get; set; } =
        VisualProfileDefaults.SoftHueShiftDegrees;

    [JsonIgnore]
    public bool SupportsTuning =>
        VisualTransformCatalog.SupportsTuning(TransformId);

    public VisualProfile CreateWorkingCopy()
    {
        return new VisualProfile
        {
            Id = Id,
            Name = Name,
            TransformId = TransformId,
            OutputBlack = OutputBlack,
            OutputWhite = OutputWhite,
            Brightness = Brightness,
            Contrast = Contrast,
            Saturation = Saturation,
            HueShiftDegrees = HueShiftDegrees,
        };
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? Id : Name;
    }

    public static VisualProfile CreateDefaultInvert()
    {
        return VisualProfileDefaults.CreateExactInvert();
    }

    public static VisualProfile CreateDefaultSoftInvert()
    {
        return VisualProfileDefaults.CreateSoftInvert();
    }
}

internal sealed record ApplicationIdentity(
    string DisplayName,
    string ExecutableName,
    string ExecutablePath);
