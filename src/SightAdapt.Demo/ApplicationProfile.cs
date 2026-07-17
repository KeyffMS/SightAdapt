using System.Text.Json.Serialization;

namespace SightAdapt.Demo;

internal sealed class SightAdaptSettings
{
    public const int CurrentSchemaVersion = 2;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;

    public bool AutomaticMode { get; set; } = true;

    public List<ApplicationProfile> Applications { get; set; } = [];

    public List<VisualProfile> VisualProfiles { get; set; } =
        [VisualProfile.CreateDefaultInvert()];
}

internal sealed class ApplicationProfile
{
    public string DisplayName { get; set; } = string.Empty;

    public string ExecutableName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public string VisualProfileId { get; set; } = VisualProfile.DefaultInvertId;

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

    public string Id { get; set; } = DefaultInvertId;

    public string Name { get; set; } = "Invert";

    public string TransformId { get; set; } = InvertVisualTransform.TransformId;

    public static VisualProfile CreateDefaultInvert()
    {
        return new VisualProfile
        {
            Id = DefaultInvertId,
            Name = "Invert",
            TransformId = InvertVisualTransform.TransformId,
        };
    }
}

internal sealed record ApplicationIdentity(
    string DisplayName,
    string ExecutableName,
    string ExecutablePath);
