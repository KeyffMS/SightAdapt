using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace SightAdapt;

internal sealed class SettingsStore
{
    private readonly JsonSerializerOptions _serializerOptions =
        new()
        {
            PropertyNamingPolicy =
                JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
        };

    public SettingsStore(string? settingsPath = null)
    {
        if (!string.IsNullOrWhiteSpace(settingsPath))
        {
            SettingsPath =
                Path.GetFullPath(settingsPath);
            return;
        }

        var applicationData =
            Environment.GetFolderPath(
                Environment.SpecialFolder
                    .LocalApplicationData);
        SettingsPath = Path.Combine(
            applicationData,
            "SightAdapt",
            "settings.json");
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
            using var stream =
                File.OpenRead(SettingsPath);
            var settings =
                JsonSerializer
                    .Deserialize<SightAdaptSettings>(
                        stream,
                        _serializerOptions) ??
                new SightAdaptSettings();

            SettingsWereMigrated =
                SettingsNormalizer.Normalize(settings);
            return settings;
        }
        catch (JsonException exception)
        {
            LastLoadWarning =
                "The settings file is invalid and " +
                $"was ignored: {exception.Message}";
            return new SightAdaptSettings();
        }
        catch (IOException exception)
        {
            LastLoadWarning =
                "The settings file could not be read: " +
                exception.Message;
            return new SightAdaptSettings();
        }
        catch (UnauthorizedAccessException exception)
        {
            LastLoadWarning =
                "The settings file could not be accessed: " +
                exception.Message;
            return new SightAdaptSettings();
        }
    }

    public void Save(SightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        SettingsNormalizer.Normalize(settings);

        var directory =
            Path.GetDirectoryName(SettingsPath) ??
            throw new InvalidOperationException(
                "The settings directory could not be resolved.");
        Directory.CreateDirectory(directory);

        var temporaryPath =
            SettingsPath + ".tmp";
        Exception? primaryException = null;

        try
        {
            var json =
                JsonSerializer.Serialize(
                    settings,
                    _serializerOptions);
            File.WriteAllText(
                temporaryPath,
                json,
                new UTF8Encoding(false));
            File.Move(
                temporaryPath,
                SettingsPath,
                true);
        }
        catch (Exception exception)
        {
            primaryException = exception;
            throw;
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch (Exception cleanupException)
                when (primaryException is not null)
            {
                Debug.WriteLine(
                    "SightAdapt could not remove the temporary settings " +
                    $"file after a save failure: {cleanupException}");
            }
        }
    }

    internal static bool Normalize(
        SightAdaptSettings settings)
    {
        return SettingsNormalizer.Normalize(settings);
    }
}
