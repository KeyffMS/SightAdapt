using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileManagementTests
{
    [TestMethod]
    public void CreateAddsIndependentSoftInvertProfile()
    {
        var settings = new SightAdaptSettings();

        var profile = VisualProfileManagementService.Create(
            settings,
            "Reading gray");

        Assert.AreEqual("Reading gray", profile.Name);
        Assert.IsTrue(profile.Id.StartsWith(
            VisualProfilePolicy.UserProfileIdPrefix,
            StringComparison.Ordinal));
        Assert.AreEqual(SoftInvertVisualTransform.TransformId, profile.TransformId);
        Assert.AreEqual(0.08f, profile.OutputBlack);
        Assert.AreEqual(0.92f, profile.OutputWhite);
        Assert.IsFalse(VisualProfileManagementService.IsBuiltIn(profile));
        Assert.AreEqual(3, settings.VisualProfiles.Count);
    }

    [TestMethod]
    public void DuplicateCopiesTuningWithNewIdentity()
    {
        var settings = new SightAdaptSettings();
        var source = settings.VisualProfiles.Single(profile =>
            profile.Id == VisualProfile.DefaultSoftInvertId);
        source.Brightness = 0.12f;
        source.Contrast = 1.35f;
        source.HueShiftDegrees = -20.0f;

        var duplicate = VisualProfileManagementService.Duplicate(
            settings,
            source,
            "Excel muted");

        Assert.AreNotEqual(source.Id, duplicate.Id);
        Assert.AreEqual("Excel muted", duplicate.Name);
        Assert.AreEqual(source.Brightness, duplicate.Brightness);
        Assert.AreEqual(source.Contrast, duplicate.Contrast);
        Assert.AreEqual(source.HueShiftDegrees, duplicate.HueShiftDegrees);
    }

    [TestMethod]
    public void CreateAndRenameRejectDuplicateNamesCaseInsensitively()
    {
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reader");

        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Create(settings, "reader"));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Rename(settings, custom, "soft invert"));
    }

    [TestMethod]
    public void BuiltInProfilesCannotBeRenamedOrDeleted()
    {
        var settings = new SightAdaptSettings();
        var builtIn = settings.VisualProfiles.Single(profile =>
            profile.Id == VisualProfile.DefaultSoftInvertId);

        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Rename(settings, builtIn, "Changed"));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Delete(settings, builtIn));
    }

    [TestMethod]
    public void DetachedProfilesCannotBeMutatedThroughLifecycleAuthority()
    {
        var settings = new SightAdaptSettings();
        var detached = VisualProfile.CreateDefaultSoftInvert();
        detached.Id = "user-detached";
        detached.Name = "Detached";

        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Duplicate(settings, detached, "Copy"));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Rename(settings, detached, "Renamed"));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Delete(settings, detached));
    }

    [TestMethod]
    public void DeleteReassignsApplicationsToFallbackProfile()
    {
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reader");
        settings.Applications =
        [
            CreateApplication("C:\\Apps\\Reader.exe", custom.Id),
            CreateApplication("C:\\Apps\\Notes.exe", custom.Id),
        ];

        var reassigned = VisualProfileManagementService.Delete(settings, custom);

        Assert.AreEqual(2, reassigned);
        Assert.IsFalse(settings.VisualProfiles.Contains(custom));
        Assert.IsTrue(settings.Applications.All(application =>
            application.VisualProfileId == VisualProfilePolicy.DeletionFallbackProfileId));
    }

    [TestMethod]
    public void DeleteRejectsMissingFallbackWithoutChangingSettings()
    {
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reader");
        settings.Applications =
        [
            CreateApplication("C:\\Apps\\Reader.exe", custom.Id),
        ];

        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Delete(settings, custom, "missing"));

        Assert.IsTrue(settings.VisualProfiles.Contains(custom));
        Assert.AreEqual(custom.Id, settings.Applications[0].VisualProfileId);
    }

    [TestMethod]
    public void AssignmentCountsUseStableProfileIdentifier()
    {
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reader");
        settings.Applications =
        [
            CreateApplication("C:\\Apps\\Reader.exe", custom.Id),
            CreateApplication(
                "C:\\Apps\\Notes.exe",
                VisualProfile.DefaultSoftInvertId),
        ];

        var count = VisualProfileManagementService.CountAssignments(settings, custom);

        Assert.AreEqual(1, count);
    }

    [TestMethod]
    public void CustomProfilePersistsAcrossSettingsRoundTrip()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var settingsPath = Path.Combine(temporaryDirectory.Path, "settings.json");
        var store = new SettingsStore(settingsPath);
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reading gray");
        custom.OutputBlack = 0.14f;
        custom.OutputWhite = 0.86f;
        custom.Saturation = 0.45f;
        settings.Applications =
        [
            CreateApplication("C:\\Apps\\Reader.exe", custom.Id),
        ];

        store.Save(settings);
        var reloaded = store.Load();
        var restored = reloaded.VisualProfiles.Single(profile =>
            profile.Id == custom.Id);

        Assert.AreEqual("Reading gray", restored.Name);
        Assert.AreEqual(0.14f, restored.OutputBlack);
        Assert.AreEqual(0.86f, restored.OutputWhite);
        Assert.AreEqual(0.45f, restored.Saturation);
        Assert.AreEqual(custom.Id, reloaded.Applications[0].VisualProfileId);
    }

    [TestMethod]
    public void AvailableNameAddsDeterministicSuffix()
    {
        var settings = new SightAdaptSettings();
        VisualProfileManagementService.Create(settings, "Reading gray");
        VisualProfileManagementService.Create(settings, "Reading gray 2");

        var name = VisualProfileManagementService.CreateAvailableName(
            settings,
            "Reading gray");

        Assert.AreEqual("Reading gray 3", name);
    }

    [TestMethod]
    public void AvailableNameNeverExceedsMaximumLength()
    {
        var settings = new SightAdaptSettings();
        var maximumName = new string('A', VisualProfilePolicy.MaximumNameLength);
        VisualProfileManagementService.Create(settings, maximumName);

        var available = VisualProfileManagementService.CreateAvailableName(
            settings,
            maximumName);

        Assert.IsTrue(available.EndsWith(" 2", StringComparison.Ordinal));
        Assert.AreEqual(VisualProfilePolicy.MaximumNameLength, available.Length);
    }

    [TestMethod]
    public void RepeatedLifecycleKeepsSurvivingProfilesIndependent()
    {
        var settings = new SightAdaptSettings();
        var first = VisualProfileManagementService.Create(settings, "Reader");
        first.Brightness = 0.12f;
        var second = VisualProfileManagementService.Duplicate(
            settings,
            first,
            "Reader copy");
        second.Brightness = -0.18f;
        settings.Applications =
        [
            CreateApplication("C:\\Apps\\Reader.exe", first.Id),
            CreateApplication("C:\\Apps\\Notes.exe", second.Id),
        ];

        VisualProfileManagementService.Rename(settings, second, "Notes muted");
        var reassigned = VisualProfileManagementService.Delete(settings, first);

        Assert.AreEqual(1, reassigned);
        Assert.AreEqual(
            VisualProfilePolicy.DeletionFallbackProfileId,
            settings.Applications[0].VisualProfileId);
        Assert.AreEqual(second.Id, settings.Applications[1].VisualProfileId);
        Assert.AreEqual("Notes muted", second.Name);
        Assert.AreEqual(-0.18f, second.Brightness);
        Assert.IsTrue(settings.VisualProfiles.Contains(second));
    }

    private static ApplicationProfile CreateApplication(
        string path,
        string visualProfileId)
    {
        return new ApplicationProfile
        {
            DisplayName = Path.GetFileNameWithoutExtension(path),
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
