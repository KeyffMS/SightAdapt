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

    public SettingsStore()
    {
        var applicationData = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        SettingsPath = Path.Combine(applicationData, "SightAdapt", "settings.json");
    }

    public string SettingsPath { get; }

    public string? LastLoadWarning { get; private set; }

    public SightAdaptSettings Load()
    {
        LastLoadWarning = null;

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

            Normalize(settings);
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

    private static void Normalize(SightAdaptSettings settings)
    {
        settings.Applications ??= [];

        var normalized = new List<ApplicationProfile>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var profile in settings.Applications)
        {
            profile.DisplayName = profile.DisplayName.Trim();
            profile.ExecutableName = profile.ExecutableName.Trim();
            profile.ExecutablePath = profile.ExecutablePath.Trim();
            profile.Effect = string.IsNullOrWhiteSpace(profile.Effect)
                ? "invert"
                : profile.Effect.Trim().ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(profile.ExecutablePath) ||
                string.IsNullOrWhiteSpace(profile.ExecutableName) ||
                !paths.Add(profile.ExecutablePath))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(profile.DisplayName))
            {
                profile.DisplayName = Path.GetFileNameWithoutExtension(profile.ExecutableName);
            }

            normalized.Add(profile);
        }

        settings.Applications = normalized;
    }
}
