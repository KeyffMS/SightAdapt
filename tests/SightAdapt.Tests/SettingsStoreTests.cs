using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class SettingsStoreTests
{
    [TestMethod]
    public void MigratesLegacyEffectToVisualProfileAssignment()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "automaticMode": true,
              "applications": [
                {
                  "displayName": "Notepad",
                  "executableName": "notepad.exe",
                  "executablePath": "C:\\Windows\\System32\\notepad.exe",
                  "enabled": true,
                  "effect": "invert"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual(SightAdaptSettings.CurrentSchemaVersion, settings.SchemaVersion);
        Assert.AreEqual(1, settings.Applications.Count);
        Assert.AreEqual(
            VisualProfile.DefaultInvertId,
            settings.Applications[0].VisualProfileId);
        Assert.IsNull(settings.Applications[0].LegacyEffect);
        Assert.IsTrue(settings.VisualProfiles.Any(
            profile => profile.Id == VisualProfile.DefaultInvertId));
    }

    [TestMethod]
    public void SaveWritesCurrentSchemaWithoutLegacyEffect()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        var store = new SettingsStore(settingsPath);
        var settings = new SightAdaptSettings
        {
            Applications =
            [
                new ApplicationProfile
                {
                    DisplayName = "Notepad",
                    ExecutableName = "notepad.exe",
                    ExecutablePath = "C:\\Windows\\System32\\notepad.exe",
                    VisualProfileId = VisualProfile.DefaultInvertId,
                },
            ],
        };

        store.Save(settings);
        var json = File.ReadAllText(settingsPath);
        var reloaded = store.Load();

        StringAssert.Contains(json, "\"schemaVersion\": 2");
        StringAssert.Contains(json, "\"visualProfileId\": \"default-invert\"");
        Assert.IsFalse(json.Contains("\"effect\"", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(1, reloaded.Applications.Count);
        Assert.IsFalse(store.SettingsWereMigrated);
    }

    [TestMethod]
    public void NormalizeRemovesDuplicateApplicationPaths()
    {
        var settings = new SightAdaptSettings
        {
            Applications =
            [
                CreateApplication("C:\\Apps\\Reader.exe"),
                CreateApplication("c:\\apps\\reader.exe"),
            ],
        };

        var changed = SettingsStore.Normalize(settings);

        Assert.IsTrue(changed);
        Assert.AreEqual(1, settings.Applications.Count);
    }

    private static ApplicationProfile CreateApplication(string path)
    {
        return new ApplicationProfile
        {
            DisplayName = "Reader",
            ExecutableName = "Reader.exe",
            ExecutablePath = path,
        };
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "SightAdapt.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
