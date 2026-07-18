using System.Text;
using System.Text.Json;

namespace SightAdapt.Demo;

internal sealed class SettingsStore
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
    };

    public SettingsStore(string? settingsPath = null)
    {
        if (!string.IsNullOrWhiteSpace(settingsPath))
        {
            SettingsPath = Path.GetFullPath(settingsPath);
            return;
        }

        var applicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        SettingsPath = Path.Combine(applicationData, "SightAdapt", "settings.json");
    }

    public string SettingsPath { get; }

    public string? LastLoadWarning { get; private set; }

    public bool SettingsWereMigrated { get; private set; }

    public SightAdaptSettings Load()
    {
        LastLoadWarning = null;
        SettingsWereMigrated = false;

        if (!File.Exists(SettingsPath))
        {
            return new SightAdaptSettings();
        }

        try
        {
            using var stream = File.OpenRead(SettingsPath);
            var settings = JsonSerializer.Deserialize<SightAdaptSettings>(
                stream,
                _serializerOptions) ?? new SightAdaptSettings();

            SettingsWereMigrated = Normalize(settings);
            return settings;
        }
        catch (JsonException exception)
        {
            LastLoadWarning = $"The settings file is invalid and was ignored: {exception.Message}";
            return new SightAdaptSettings();
        }
        catch (IOException exception)
        {
            LastLoadWarning = $"The settings file could not be read: {exception.Message}";
            return new SightAdaptSettings();
        }
        catch (UnauthorizedAccessException exception)
        {
            LastLoadWarning = $"The settings file could not be accessed: {exception.Message}";
            return new SightAdaptSettings();
        }
    }

    public void Save(SightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Normalize(settings);

        var directory = Path.GetDirectoryName(SettingsPath)
            ?? throw new InvalidOperationException("The settings directory could not be resolved.");

        Directory.CreateDirectory(directory);

        var temporaryPath = SettingsPath + ".tmp";

        try
        {
            var json = JsonSerializer.Serialize(settings, _serializerOptions);
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            File.Move(temporaryPath, SettingsPath, true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    internal static bool Normalize(SightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var changed = settings.SchemaVersion != SightAdaptSettings.CurrentSchemaVersion;
        settings.SchemaVersion = SightAdaptSettings.CurrentSchemaVersion;

        settings.VisualProfiles ??= [];
        settings.Applications ??= [];

        var normalizedVisualProfiles = new List<VisualProfile>();
        var visualProfileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var visualProfile in settings.VisualProfiles)
        {
            visualProfile.Id = visualProfile.Id.Trim();
            visualProfile.Name = visualProfile.Name.Trim();
            visualProfile.TransformId = visualProfile.TransformId.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(visualProfile.Id) ||
                !visualProfileIds.Add(visualProfile.Id))
            {
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(visualProfile.Name))
            {
                visualProfile.Name = visualProfile.Id;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(visualProfile.TransformId))
            {
                visualProfile.TransformId = InvertVisualTransform.TransformId;
                changed = true;
            }

            changed |= NormalizeVisualProfile(visualProfile);
            normalizedVisualProfiles.Add(visualProfile);
        }

        if (!visualProfileIds.Contains(VisualProfile.DefaultInvertId))
        {
            normalizedVisualProfiles.Insert(0, VisualProfile.CreateDefaultInvert());
            visualProfileIds.Add(VisualProfile.DefaultInvertId);
            changed = true;
        }

        if (!visualProfileIds.Contains(VisualProfile.DefaultSoftInvertId))
        {
            normalizedVisualProfiles.Add(VisualProfile.CreateDefaultSoftInvert());
            visualProfileIds.Add(VisualProfile.DefaultSoftInvertId);
            changed = true;
        }

        var normalizedApplications = new List<ApplicationProfile>();
        var executablePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var applicationProfile in settings.Applications)
        {
            applicationProfile.DisplayName = applicationProfile.DisplayName.Trim();
            applicationProfile.ExecutableName = applicationProfile.ExecutableName.Trim();
            applicationProfile.ExecutablePath = applicationProfile.ExecutablePath.Trim();
            applicationProfile.VisualProfileId = applicationProfile.VisualProfileId.Trim();

            if (!string.IsNullOrWhiteSpace(applicationProfile.LegacyEffect))
            {
                applicationProfile.VisualProfileId = VisualProfile.DefaultInvertId;
                applicationProfile.LegacyEffect = null;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(applicationProfile.VisualProfileId) ||
                !visualProfileIds.Contains(applicationProfile.VisualProfileId))
            {
                applicationProfile.VisualProfileId = VisualProfile.DefaultInvertId;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(applicationProfile.ExecutablePath) ||
                string.IsNullOrWhiteSpace(applicationProfile.ExecutableName) ||
                !executablePaths.Add(applicationProfile.ExecutablePath))
            {
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(applicationProfile.DisplayName))
            {
                applicationProfile.DisplayName =
                    Path.GetFileNameWithoutExtension(applicationProfile.ExecutableName);
                changed = true;
            }

            normalizedApplications.Add(applicationProfile);
        }

        if (normalizedVisualProfiles.Count != settings.VisualProfiles.Count ||
            normalizedApplications.Count != settings.Applications.Count)
        {
            changed = true;
        }

        settings.VisualProfiles = normalizedVisualProfiles;
        settings.Applications = normalizedApplications;
        return changed;
    }

    private static bool NormalizeVisualProfile(VisualProfile profile)
    {
        if (string.Equals(
                profile.TransformId,
                InvertVisualTransform.TransformId,
                StringComparison.OrdinalIgnoreCase))
        {
            var exactProfileChanged = profile.OutputBlack != 0.0f ||
                profile.OutputWhite != 1.0f ||
                profile.Brightness != 0.0f ||
                profile.Contrast != 1.0f ||
                profile.Saturation != 1.0f ||
                profile.HueShiftDegrees != 0.0f;

            profile.OutputBlack = 0.0f;
            profile.OutputWhite = 1.0f;
            profile.Brightness = 0.0f;
            profile.Contrast = 1.0f;
            profile.Saturation = 1.0f;
            profile.HueShiftDegrees = 0.0f;
            return exactProfileChanged;
        }

        var outputBlack = VisualProfileLimits.ClampFinite(
            profile.OutputBlack,
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack,
            0.08f);
        var outputWhite = VisualProfileLimits.ClampFinite(
            profile.OutputWhite,
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite,
            0.92f);
        var brightness = VisualProfileLimits.ClampFinite(
            profile.Brightness,
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness,
            0.0f);
        var contrast = VisualProfileLimits.ClampFinite(
            profile.Contrast,
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast,
            1.0f);
        var saturation = VisualProfileLimits.ClampFinite(
            profile.Saturation,
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation,
            1.0f);
        var hueShift = VisualProfileLimits.ClampFinite(
            profile.HueShiftDegrees,
            VisualProfileLimits.MinimumHueShift,
            VisualProfileLimits.MaximumHueShift,
            0.0f);

        var changed = profile.OutputBlack != outputBlack ||
            profile.OutputWhite != outputWhite ||
            profile.Brightness != brightness ||
            profile.Contrast != contrast ||
            profile.Saturation != saturation ||
            profile.HueShiftDegrees != hueShift;

        profile.OutputBlack = outputBlack;
        profile.OutputWhite = outputWhite;
        profile.Brightness = brightness;
        profile.Contrast = contrast;
        profile.Saturation = saturation;
        profile.HueShiftDegrees = hueShift;
        return changed;
    }
}
