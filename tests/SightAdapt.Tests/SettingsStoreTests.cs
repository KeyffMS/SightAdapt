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
        Assert.IsTrue(settings.VisualProfiles.Any(
            profile => profile.Id == VisualProfile.DefaultSoftInvertId));
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
                    VisualProfileId = VisualProfile.DefaultSoftInvertId,
                },
            ],
        };

        store.Save(settings);
        var json = File.ReadAllText(settingsPath);
        var reloaded = store.Load();

        StringAssert.Contains(json, "\"schemaVersion\": 3");
        StringAssert.Contains(json, "\"visualProfileId\": \"default-soft-invert\"");
        StringAssert.Contains(json, "\"outputBlack\": 0.08");
        StringAssert.Contains(json, "\"outputWhite\": 0.92");
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

    [TestMethod]
    public void NormalizeClampsSoftProfileParameters()
    {
        var profile = VisualProfile.CreateDefaultSoftInvert();
        profile.OutputBlack = -3.0f;
        profile.OutputWhite = 4.0f;
        profile.Brightness = float.NaN;
        profile.Contrast = 9.0f;
        profile.Saturation = -2.0f;
        profile.HueShiftDegrees = 900.0f;
        var settings = new SightAdaptSettings
        {
            VisualProfiles =
            [
                VisualProfile.CreateDefaultInvert(),
                profile,
            ],
        };

        var changed = SettingsStore.Normalize(settings);

        Assert.IsTrue(changed);
        Assert.AreEqual(0.0f, profile.OutputBlack);
        Assert.AreEqual(1.0f, profile.OutputWhite);
        Assert.AreEqual(0.0f, profile.Brightness);
        Assert.AreEqual(2.0f, profile.Contrast);
        Assert.AreEqual(0.0f, profile.Saturation);
        Assert.AreEqual(180.0f, profile.HueShiftDegrees);
    }

    [TestMethod]
    public void SchemaTwoSettingsGainSoftInvertWithoutChangingExistingAssignment()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 2,
              "automaticMode": true,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "Reader.exe",
                  "executablePath": "C:\\Apps\\Reader.exe",
                  "enabled": true,
                  "visualProfileId": "default-invert"
                }
              ],
              "visualProfiles": [
                {
                  "id": "default-invert",
                  "name": "Invert",
                  "transformId": "invert"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual(VisualProfile.DefaultInvertId, settings.Applications[0].VisualProfileId);
        Assert.IsTrue(settings.VisualProfiles.Any(
            profile => profile.Id == VisualProfile.DefaultSoftInvertId));
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
