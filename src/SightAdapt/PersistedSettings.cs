using System.Text.Json.Serialization;

namespace SightAdapt;

internal sealed class PersistedSightAdaptSettings
{
    public int SchemaVersion { get; set; }

    public bool AutomaticMode { get; set; } = true;

    public List<PersistedApplicationAssignment?>? Applications { get; set; } = [];

    public List<PersistedVisualProfile?>? VisualProfiles { get; set; } =
        PersistedSettingsMapper.CreateDefaultPersistedVisualProfiles();
}

internal sealed class PersistedApplicationAssignment
{
    public string? DisplayName { get; set; }

    public string? ExecutableName { get; set; }

    public string? ExecutablePath { get; set; }

    public bool Enabled { get; set; } = true;

    public string? VisualProfileId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? MenuVisualProfileId { get; set; }

    [JsonPropertyName("overlayScope")]
    public string? OverlayScopeId { get; set; }

    [JsonPropertyName("effect")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyEffect { get; set; }
}

internal sealed class PersistedVisualProfile
{
    public string? Id { get; set; }

    public string? Name { get; set; }

    public string? TransformId { get; set; }

    public float? OutputBlack { get; set; }

    public float? OutputWhite { get; set; }

    public float? Brightness { get; set; }

    public float? Contrast { get; set; }

    public float? Saturation { get; set; }

    public float? HueShiftDegrees { get; set; }
}

internal sealed record SettingsMaterializationResult(
    SightAdaptSettings Settings,
    bool WasMigrated);

internal static class PersistedSettingsMapper
{
    public static SettingsMaterializationResult ToDomain(
        PersistedSightAdaptSettings? persisted)
    {
        if (persisted is null)
        {
            return new SettingsMaterializationResult(
                new SightAdaptSettings(),
                WasMigrated: false);
        }

        var migrated = false;
        var settings = new SightAdaptSettings
        {
            SchemaVersion = persisted.SchemaVersion,
            AutomaticMode = persisted.AutomaticMode,
            Assignments = [],
            VisualProfiles = [],
        };

        if (persisted.VisualProfiles is null)
        {
            migrated = true;
        }
        else
        {
            foreach (var storedProfile in persisted.VisualProfiles)
            {
                if (storedProfile is null)
                {
                    migrated = true;
                    continue;
                }

                settings.VisualProfiles.Add(
                    ToDomain(storedProfile));
            }
        }

        if (persisted.Applications is null)
        {
            migrated = true;
        }
        else
        {
            foreach (var storedAssignment in persisted.Applications)
            {
                if (storedAssignment is null)
                {
                    migrated = true;
                    continue;
                }

                var assignment = ToDomain(storedAssignment);
                if (storedAssignment.LegacyEffect is not null)
                {
                    migrated = true;
                    if (!string.IsNullOrWhiteSpace(
                            storedAssignment.LegacyEffect))
                    {
                        assignment.VisualProfileId =
                            VisualProfileCatalog.DefaultInvertId;
                    }
                }

                settings.Assignments.Add(assignment);
            }
        }

        return new SettingsMaterializationResult(
            settings,
            migrated);
    }

    public static PersistedSightAdaptSettings FromDomain(
        SightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings.EnsureCollections();

        return new PersistedSightAdaptSettings
        {
            SchemaVersion = settings.SchemaVersion,
            AutomaticMode = settings.AutomaticMode,
            Applications = settings.Assignments
                .Where(assignment => assignment is not null)
                .Select(FromDomain)
                .Cast<PersistedApplicationAssignment?>()
                .ToList(),
            VisualProfiles = settings.VisualProfiles
                .Where(profile => profile is not null)
                .Select(FromDomain)
                .Cast<PersistedVisualProfile?>()
                .ToList(),
        };
    }

    private static ApplicationAssignment ToDomain(
        PersistedApplicationAssignment assignment)
    {
        return new ApplicationAssignment
        {
            DisplayName = assignment.DisplayName ?? string.Empty,
            ExecutableName = assignment.ExecutableName ?? string.Empty,
            ExecutablePath = assignment.ExecutablePath ?? string.Empty,
            Enabled = assignment.Enabled,
            VisualProfileId =
                assignment.VisualProfileId ?? string.Empty,
            MenuVisualProfileId = assignment.MenuVisualProfileId,
            OverlayScopeId = assignment.OverlayScopeId ?? string.Empty,
        };
    }

    private static VisualProfile ToDomain(
        PersistedVisualProfile profile)
    {
        return new VisualProfile
        {
            Id = profile.Id ?? string.Empty,
            Name = profile.Name ?? string.Empty,
            TransformId = profile.TransformId ?? string.Empty,
            OutputBlack = profile.OutputBlack ??
                VisualProfileDefaults.SoftOutputBlack,
            OutputWhite = profile.OutputWhite ??
                VisualProfileDefaults.SoftOutputWhite,
            Brightness = profile.Brightness ??
                VisualProfileDefaults.SoftBrightness,
            Contrast = profile.Contrast ??
                VisualProfileDefaults.SoftContrast,
            Saturation = profile.Saturation ??
                VisualProfileDefaults.SoftSaturation,
            HueShiftDegrees = profile.HueShiftDegrees ??
                VisualProfileDefaults.SoftHueShiftDegrees,
        };
    }

    private static PersistedApplicationAssignment FromDomain(
        ApplicationAssignment assignment)
    {
        return new PersistedApplicationAssignment
        {
            DisplayName = assignment.DisplayName,
            ExecutableName = assignment.ExecutableName,
            ExecutablePath = assignment.ExecutablePath,
            Enabled = assignment.Enabled,
            VisualProfileId = assignment.VisualProfileId,
            MenuVisualProfileId = assignment.MenuVisualProfileId,
            OverlayScopeId = assignment.OverlayScopeId,
        };
    }

    internal static List<PersistedVisualProfile?>
        CreateDefaultPersistedVisualProfiles()
    {
        return VisualProfileCatalog.Default
            .CreateBuiltInProfiles()
            .Select(FromDomain)
            .Cast<PersistedVisualProfile?>()
            .ToList();
    }

    internal static PersistedVisualProfile FromDomain(
        VisualProfile profile)
    {
        return new PersistedVisualProfile
        {
            Id = profile.Id,
            Name = profile.Name,
            TransformId = profile.TransformId,
            OutputBlack = profile.OutputBlack,
            OutputWhite = profile.OutputWhite,
            Brightness = profile.Brightness,
            Contrast = profile.Contrast,
            Saturation = profile.Saturation,
            HueShiftDegrees = profile.HueShiftDegrees,
        };
    }
}
