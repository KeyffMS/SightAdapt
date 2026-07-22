using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

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

        StringAssert.Contains(json, "\"schemaVersion\": 4");
        StringAssert.Contains(json, "\"visualProfileId\": \"default-soft-invert\"");
        StringAssert.Contains(json, "\"overlayScope\": \"client-area\"");
        StringAssert.Contains(json, "\"outputBlack\": 0.08");
        StringAssert.Contains(json, "\"outputWhite\": 0.92");
        Assert.IsFalse(json.Contains("\"effect\"", StringComparison.OrdinalIgnoreCase));
        Assert.AreEqual(1, reloaded.Applications.Count);
        Assert.IsFalse(store.SettingsWereMigrated);
    }

    [TestMethod]
    public void LoadKeepsCanonicalOverlayScopeWithoutMigration()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 4,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "reader.exe",
                  "executablePath": "C:\\Apps\\reader.exe",
                  "visualProfileId": "default-soft-invert",
                  "overlayScope": "screen"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsFalse(store.SettingsWereMigrated);
        Assert.AreEqual("screen", settings.Applications[0].OverlayScopeId);
        Assert.AreEqual(OverlayScope.Screen, settings.Applications[0].OverlayScope);
    }

    [TestMethod]
    public void LoadCanonicalizesOverlayScopeAliasAndReportsMigration()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 4,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "reader.exe",
                  "executablePath": "C:\\Apps\\reader.exe",
                  "visualProfileId": "default-soft-invert",
                  "overlayScope": "full-window"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual("window", settings.Applications[0].OverlayScopeId);
        Assert.AreEqual(OverlayScope.Window, settings.Applications[0].OverlayScope);

        store.Save(settings);
        StringAssert.Contains(
            File.ReadAllText(settingsPath),
            "\"overlayScope\": \"window\"");
    }

    [TestMethod]
    public void LoadRepairsInvalidOverlayScopeAndReportsMigration()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 4,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "reader.exe",
                  "executablePath": "C:\\Apps\\reader.exe",
                  "visualProfileId": "default-soft-invert",
                  "overlayScope": "unknown-scope"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual("client-area", settings.Applications[0].OverlayScopeId);
        Assert.AreEqual(OverlayScope.ClientArea, settings.Applications[0].OverlayScope);

        store.Save(settings);
        StringAssert.Contains(
            File.ReadAllText(settingsPath),
            "\"overlayScope\": \"client-area\"");
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

    [TestMethod]
    public void LoadRepairsNullValuesAndMissingExecutableMetadata()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 3,
              "visualProfiles": [
                null,
                {
                  "id": null,
                  "name": null,
                  "transformId": null,
                  "outputBlack": 0.12,
                  "outputWhite": 0.88
                }
              ],
              "applications": [
                null,
                {
                  "displayName": null,
                  "executableName": null,
                  "executablePath": " C:\\Apps\\Reader.exe ",
                  "visualProfileId": null
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual(3, settings.VisualProfiles.Count);
        var recovered = settings.VisualProfiles.Single(profile =>
            !VisualProfileManagementService.IsBuiltIn(profile));
        Assert.IsTrue(recovered.Id.StartsWith(
            VisualProfilePolicy.UserProfileIdPrefix,
            StringComparison.Ordinal));
        Assert.AreEqual("Custom Soft Invert", recovered.Name);
        Assert.AreEqual(SoftInvertVisualTransform.TransformId, recovered.TransformId);
        Assert.AreEqual(0.12f, recovered.OutputBlack);
        Assert.AreEqual(0.88f, recovered.OutputWhite);

        Assert.AreEqual(1, settings.Applications.Count);
        Assert.AreEqual("Reader.exe", settings.Applications[0].ExecutableName);
        Assert.AreEqual("Reader", settings.Applications[0].DisplayName);
        Assert.AreEqual(
            VisualProfilePolicy.MissingReferenceFallbackProfileId,
            settings.Applications[0].VisualProfileId);
    }

    [TestMethod]
    public void NormalizeCanonicalizesBuiltInsAndPreservesSoftTuning()
    {
        var exact = new VisualProfile
        {
            Id = VisualProfile.DefaultInvertId,
            Name = "Broken exact",
            TransformId = SoftInvertVisualTransform.TransformId,
            OutputBlack = 0.2f,
            OutputWhite = 0.8f,
        };
        var soft = new VisualProfile
        {
            Id = VisualProfile.DefaultSoftInvertId,
            Name = "Broken soft",
            TransformId = InvertVisualTransform.TransformId,
            OutputBlack = 0.17f,
            OutputWhite = 0.83f,
            Brightness = 0.11f,
        };
        var settings = new SightAdaptSettings
        {
            VisualProfiles = [soft, exact],
        };

        var changed = SettingsStore.Normalize(settings);

        Assert.IsTrue(changed);
        Assert.AreSame(exact, settings.VisualProfiles[0]);
        Assert.AreEqual("Exact invert", exact.Name);
        Assert.AreEqual(InvertVisualTransform.TransformId, exact.TransformId);
        Assert.AreEqual(0.0f, exact.OutputBlack);
        Assert.AreEqual(1.0f, exact.OutputWhite);

        Assert.AreSame(soft, settings.VisualProfiles[1]);
        Assert.AreEqual("Soft invert", soft.Name);
        Assert.AreEqual(SoftInvertVisualTransform.TransformId, soft.TransformId);
        Assert.AreEqual(0.17f, soft.OutputBlack);
        Assert.AreEqual(0.83f, soft.OutputWhite);
        Assert.AreEqual(0.11f, soft.Brightness);
    }

    [TestMethod]
    public void NormalizeReidentifiesDuplicateProfilesWithoutDroppingThem()
    {
        var first = CreateCustomProfile("shared-id", "Reader");
        var second = CreateCustomProfile("shared-id", "Notes");
        var reservedDuplicate = CreateCustomProfile(
            VisualProfile.DefaultSoftInvertId,
            "Reserved copy");
        var settings = new SightAdaptSettings
        {
            VisualProfiles =
            [
                VisualProfile.CreateDefaultInvert(),
                VisualProfile.CreateDefaultSoftInvert(),
                first,
                second,
                reservedDuplicate,
            ],
        };

        var changed = SettingsStore.Normalize(settings);

        Assert.IsTrue(changed);
        Assert.AreEqual(5, settings.VisualProfiles.Count);
        Assert.AreEqual(5, settings.VisualProfiles
            .Select(profile => profile.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count());
        Assert.AreEqual("shared-id", first.Id);
        Assert.IsTrue(second.Id.StartsWith(
            VisualProfilePolicy.UserProfileIdPrefix,
            StringComparison.Ordinal));
        Assert.IsTrue(reservedDuplicate.Id.StartsWith(
            VisualProfilePolicy.UserProfileIdPrefix,
            StringComparison.Ordinal));
    }

    [TestMethod]
    public void NormalizeRepairsUnknownTransformsAndDuplicateNames()
    {
        var first = CreateCustomProfile("user-a", "Reading gray");
        first.TransformId = "missing-transform";
        var second = CreateCustomProfile("user-b", "reading gray");
        var settings = new SightAdaptSettings
        {
            VisualProfiles =
            [
                VisualProfile.CreateDefaultInvert(),
                VisualProfile.CreateDefaultSoftInvert(),
                first,
                second,
            ],
        };

        var changed = SettingsStore.Normalize(settings);

        Assert.IsTrue(changed);
        Assert.AreEqual(SoftInvertVisualTransform.TransformId, first.TransformId);
        Assert.AreEqual("Reading gray", first.Name);
        Assert.AreEqual("reading gray 2", second.Name);
    }

    [TestMethod]
    public void RecoveredSettingsAreIdempotent()
    {
        var settings = new SightAdaptSettings
        {
            VisualProfiles =
            [
                CreateCustomProfile(string.Empty, string.Empty),
                CreateCustomProfile(string.Empty, string.Empty),
            ],
            Applications =
            [
                new ApplicationProfile
                {
                    ExecutablePath = " C:\\Apps\\Reader.exe ",
                    VisualProfileId = "missing",
                },
            ],
        };

        Assert.IsTrue(SettingsStore.Normalize(settings));
        Assert.IsFalse(SettingsStore.Normalize(settings));
    }

    [TestMethod]
    public void MultipleCustomProfilesAndAssignmentsRoundTripIndependently()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        var store = new SettingsStore(settingsPath);
        var settings = new SightAdaptSettings();
        var reading = VisualProfileManagementService.Create(settings, "Reading gray");
        reading.Brightness = 0.08f;
        var spreadsheet = VisualProfileManagementService.Create(settings, "Spreadsheet muted");
        spreadsheet.Saturation = 0.35f;
        settings.Applications =
        [
            CreateApplication("C:\\Apps\\Reader.exe", reading.Id),
            CreateApplication("C:\\Apps\\Sheet.exe", spreadsheet.Id),
        ];

        store.Save(settings);
        var reloaded = store.Load();

        Assert.IsFalse(store.SettingsWereMigrated);
        Assert.AreEqual(4, reloaded.VisualProfiles.Count);
        Assert.AreEqual(reading.Id, reloaded.Applications[0].VisualProfileId);
        Assert.AreEqual(spreadsheet.Id, reloaded.Applications[1].VisualProfileId);
        Assert.AreEqual(0.08f, reloaded.VisualProfiles.Single(profile =>
            profile.Id == reading.Id).Brightness);
        Assert.AreEqual(0.35f, reloaded.VisualProfiles.Single(profile =>
            profile.Id == spreadsheet.Id).Saturation);
    }

    private static VisualProfile CreateCustomProfile(string id, string name)
    {
        var profile = VisualProfile.CreateDefaultSoftInvert();
        profile.Id = id;
        profile.Name = name;
        return profile;
    }

    private static ApplicationProfile CreateApplication(
        string path,
        string visualProfileId = VisualProfile.DefaultSoftInvertId)
    {
        return new ApplicationProfile
        {
            DisplayName = "Reader",
            ExecutableName = Path.GetFileName(path),
            ExecutablePath = path,
            VisualProfileId = visualProfileId,
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
