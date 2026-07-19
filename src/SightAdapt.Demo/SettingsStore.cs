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

        var sourceProfiles = settings.VisualProfiles
            .OfType<VisualProfile>()
            .ToList();
        if (sourceProfiles.Count != settings.VisualProfiles.Count)
        {
            changed = true;
        }

        var normalizedProfiles = new List<VisualProfile>();
        var profileIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var exactInvert = sourceProfiles.FirstOrDefault(profile => string.Equals(
            profile.Id,
            VisualProfile.DefaultInvertId,
            StringComparison.OrdinalIgnoreCase));
        if (exactInvert is null)
        {
            exactInvert = VisualProfile.CreateDefaultInvert();
            changed = true;
        }
        else
        {
            sourceProfiles.Remove(exactInvert);
        }

        changed |= NormalizeBuiltInExactInvert(exactInvert);
        normalizedProfiles.Add(exactInvert);
        profileIds.Add(exactInvert.Id);

        var softInvert = sourceProfiles.FirstOrDefault(profile => string.Equals(
            profile.Id,
            VisualProfile.DefaultSoftInvertId,
            StringComparison.OrdinalIgnoreCase));
        if (softInvert is null)
        {
            softInvert = VisualProfile.CreateDefaultSoftInvert();
            changed = true;
        }
        else
        {
            sourceProfiles.Remove(softInvert);
        }

        changed |= NormalizeBuiltInSoftInvert(softInvert);
        normalizedProfiles.Add(softInvert);
        profileIds.Add(softInvert.Id);

        foreach (var profile in sourceProfiles)
        {
            var normalizedId = (profile.Id ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedId) ||
                VisualProfilePolicy.IsBuiltInId(normalizedId) ||
                profileIds.Contains(normalizedId))
            {
                normalizedId = VisualProfilePolicy.CreateUserProfileId(profileIds);
                changed = true;
            }

            if (!string.Equals(profile.Id, normalizedId, StringComparison.Ordinal))
            {
                profile.Id = normalizedId;
                changed = true;
            }

            profileIds.Add(normalizedId);

            var normalizedTransformId = (profile.TransformId ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
            if (!VisualProfilePolicy.IsSupportedTransformId(normalizedTransformId))
            {
                normalizedTransformId = SoftInvertVisualTransform.TransformId;
                changed = true;
            }

            if (!string.Equals(
                    profile.TransformId,
                    normalizedTransformId,
                    StringComparison.Ordinal))
            {
                profile.TransformId = normalizedTransformId;
                changed = true;
            }

            var normalizedName = VisualProfilePolicy.NormalizeNameOrFallback(
                profile.Name,
                "Custom Soft Invert");
            if (normalizedProfiles.Any(candidate => string.Equals(
                    candidate.Name,
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase)))
            {
                normalizedName = VisualProfilePolicy.CreateUniqueName(
                    normalizedProfiles,
                    normalizedName);
                changed = true;
            }

            if (!string.Equals(profile.Name, normalizedName, StringComparison.Ordinal))
            {
                profile.Name = normalizedName;
                changed = true;
            }

            changed |= NormalizeVisualProfile(profile);
            normalizedProfiles.Add(profile);
        }

        var sourceApplications = settings.Applications
            .OfType<ApplicationProfile>()
            .ToList();
        if (sourceApplications.Count != settings.Applications.Count)
        {
            changed = true;
        }

        var normalizedApplications = new List<ApplicationProfile>();
        var executablePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var application in sourceApplications)
        {
            changed |= NormalizeApplicationStrings(application);

            if (!string.IsNullOrWhiteSpace(application.LegacyEffect))
            {
                application.VisualProfileId = VisualProfile.DefaultInvertId;
                application.LegacyEffect = null;
                changed = true;
            }
            else if (application.LegacyEffect is not null)
            {
                application.LegacyEffect = null;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(application.ExecutablePath))
            {
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(application.ExecutableName))
            {
                application.ExecutableName =
                    Path.GetFileName(application.ExecutablePath) ?? string.Empty;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(application.ExecutableName) ||
                !executablePaths.Add(application.ExecutablePath))
            {
                changed = true;
                continue;
            }

            if (string.IsNullOrWhiteSpace(application.DisplayName))
            {
                application.DisplayName = Path.GetFileNameWithoutExtension(
                    application.ExecutableName) ?? string.Empty;
                changed = true;
            }

            if (string.IsNullOrWhiteSpace(application.VisualProfileId) ||
                !profileIds.Contains(application.VisualProfileId))
            {
                application.VisualProfileId =
                    VisualProfilePolicy.MissingReferenceFallbackProfileId;
                changed = true;
            }

            normalizedApplications.Add(application);
        }

        if (!settings.VisualProfiles.SequenceEqual(normalizedProfiles) ||
            !settings.Applications.SequenceEqual(normalizedApplications))
        {
            changed = true;
        }

        settings.VisualProfiles = normalizedProfiles;
        settings.Applications = normalizedApplications;
        return changed;
    }

    private static bool NormalizeBuiltInExactInvert(VisualProfile profile)
    {
        var changed = !string.Equals(
                profile.Id,
                VisualProfile.DefaultInvertId,
                StringComparison.Ordinal) ||
            !string.Equals(profile.Name, "Exact invert", StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                InvertVisualTransform.TransformId,
                StringComparison.Ordinal) ||
            profile.OutputBlack != 0.0f ||
            profile.OutputWhite != 1.0f ||
            profile.Brightness != 0.0f ||
            profile.Contrast != 1.0f ||
            profile.Saturation != 1.0f ||
            profile.HueShiftDegrees != 0.0f;

        profile.Id = VisualProfile.DefaultInvertId;
        profile.Name = "Exact invert";
        profile.TransformId = InvertVisualTransform.TransformId;
        profile.OutputBlack = 0.0f;
        profile.OutputWhite = 1.0f;
        profile.Brightness = 0.0f;
        profile.Contrast = 1.0f;
        profile.Saturation = 1.0f;
        profile.HueShiftDegrees = 0.0f;
        return changed;
    }

    private static bool NormalizeBuiltInSoftInvert(VisualProfile profile)
    {
        var changed = !string.Equals(
                profile.Id,
                VisualProfile.DefaultSoftInvertId,
                StringComparison.Ordinal) ||
            !string.Equals(profile.Name, "Soft invert", StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                SoftInvertVisualTransform.TransformId,
                StringComparison.Ordinal);

        profile.Id = VisualProfile.DefaultSoftInvertId;
        profile.Name = "Soft invert";
        profile.TransformId = SoftInvertVisualTransform.TransformId;
        return NormalizeVisualProfile(profile) || changed;
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

    private static bool NormalizeApplicationStrings(ApplicationProfile application)
    {
        var displayName = (application.DisplayName ?? string.Empty).Trim();
        var executableName = (application.ExecutableName ?? string.Empty).Trim();
        var executablePath = (application.ExecutablePath ?? string.Empty).Trim();
        var visualProfileId = (application.VisualProfileId ?? string.Empty).Trim();

        var changed = !string.Equals(
                application.DisplayName,
                displayName,
                StringComparison.Ordinal) ||
            !string.Equals(
                application.ExecutableName,
                executableName,
                StringComparison.Ordinal) ||
            !string.Equals(
                application.ExecutablePath,
                executablePath,
                StringComparison.Ordinal) ||
            !string.Equals(
                application.VisualProfileId,
                visualProfileId,
                StringComparison.Ordinal);

        application.DisplayName = displayName;
        application.ExecutableName = executableName;
        application.ExecutablePath = executablePath;
        application.VisualProfileId = visualProfileId;
        return changed;
    }
}
