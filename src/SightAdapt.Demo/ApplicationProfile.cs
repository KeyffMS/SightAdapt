using System.Text.Json.Serialization;

namespace SightAdapt.Demo;

internal sealed class SightAdaptSettings
{
    public const int CurrentSchemaVersion = 3;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public bool AutomaticMode { get; set; } = true;

    public List<ApplicationProfile> Applications { get; set; } = [];

    public List<VisualProfile> VisualProfiles { get; set; } =
    [
        VisualProfile.CreateDefaultInvert(),
        VisualProfile.CreateDefaultSoftInvert(),
    ];
}

internal sealed class ApplicationProfile
{
    public string DisplayName { get; set; } = string.Empty;

    public string ExecutableName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public string VisualProfileId { get; set; } = VisualProfile.DefaultSoftInvertId;

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

    public string Name { get; set; } = "Soft invert";

    public string TransformId { get; set; } = SoftInvertVisualTransform.TransformId;

    public float OutputBlack { get; set; } = 0.08f;

    public float OutputWhite { get; set; } = 0.92f;

    public float Brightness { get; set; }

    public float Contrast { get; set; } = 1.0f;

    public float Saturation { get; set; } = 1.0f;

    public float HueShiftDegrees { get; set; }

    [JsonIgnore]
    public bool SupportsTuning => string.Equals(
        TransformId,
        SoftInvertVisualTransform.TransformId,
        StringComparison.OrdinalIgnoreCase);

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

    public void CopyTuningFrom(VisualProfile source)
    {
        ArgumentNullException.ThrowIfNull(source);

        OutputBlack = source.OutputBlack;
        OutputWhite = source.OutputWhite;
        Brightness = source.Brightness;
        Contrast = source.Contrast;
        Saturation = source.Saturation;
        HueShiftDegrees = source.HueShiftDegrees;
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? Id : Name;
    }

    public static VisualProfile CreateDefaultInvert()
    {
        return new VisualProfile
        {
            Id = DefaultInvertId,
            Name = "Exact invert",
            TransformId = InvertVisualTransform.TransformId,
            OutputBlack = 0.0f,
            OutputWhite = 1.0f,
        };
    }

    public static VisualProfile CreateDefaultSoftInvert()
    {
        return new VisualProfile
        {
            Id = DefaultSoftInvertId,
            Name = "Soft invert",
            TransformId = SoftInvertVisualTransform.TransformId,
            OutputBlack = 0.08f,
            OutputWhite = 0.92f,
            Brightness = 0.0f,
            Contrast = 1.0f,
            Saturation = 1.0f,
            HueShiftDegrees = 0.0f,
        };
    }
}

internal sealed record ApplicationIdentity(
    string DisplayName,
    string ExecutableName,
    string ExecutablePath);
