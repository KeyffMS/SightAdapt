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

            normalizedVisualProfiles.Add(visualProfile);
        }

        if (!visualProfileIds.Contains(VisualProfile.DefaultInvertId))
        {
            normalizedVisualProfiles.Insert(0, VisualProfile.CreateDefaultInvert());
            visualProfileIds.Add(VisualProfile.DefaultInvertId);
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
}
