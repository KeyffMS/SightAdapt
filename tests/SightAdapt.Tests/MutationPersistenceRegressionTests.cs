using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class MutationPersistenceRegressionTests
{
    [TestMethod]
    public void AssignmentTuningAndModeMutationsSurviveRoundTrip()
    {
        using var temporaryDirectory = new TemporaryDirectory();
        var store = new SettingsStore(Path.Combine(
            temporaryDirectory.Path,
            "settings.json"));
        var settings = new SightAdaptSettings();
        var identity = new ApplicationIdentity(
            "Reader",
            "Reader.exe",
            Path.Combine(@"C:\Apps", "Reader.exe"));
        var assignment = ApplicationProfileManagementService
            .AddOrEnable(settings, identity)
            .Profile;
        var profile = VisualProfileManagementService.Create(
            settings,
            "Reader colors");
        var values = profile.CreateWorkingCopy();
        values.Brightness = 0.18f;
        values.Contrast = 1.25f;

        ApplicationProfileManagementService.AssignVisualProfile(
            settings,
            assignment,
            profile.Id);
        VisualProfileManagementService.UpdateTuning(settings, profile, values);
        AutomaticModeManagementService.Disable(settings);
        store.Save(settings);

        var restored = store.Load();
        var restoredProfile = restored.VisualProfiles.Single(
            candidate => candidate.Id == profile.Id);
        var restoredAssignment = restored.Applications.Single();

        Assert.IsFalse(restored.AutomaticMode);
        Assert.AreEqual(profile.Id, restoredAssignment.VisualProfileId);
        Assert.AreEqual(0.18f, restoredProfile.Brightness);
        Assert.AreEqual(1.25f, restoredProfile.Contrast);
    }

    [TestMethod]
    public void RepeatedLifecycleAndAssignmentMutationsRemainConsistent()
    {
        var settings = new SightAdaptSettings();

        for (var index = 0; index < 20; index++)
        {
            var profile = VisualProfileManagementService.Create(
                settings,
                $"Profile {index}");
            var identity = new ApplicationIdentity(
                $"App {index}",
                $"App{index}.exe",
                Path.Combine(@"C:\Apps", $"App{index}.exe"));
            var assignment = ApplicationProfileManagementService
                .AddOrEnable(settings, identity)
                .Profile;
            ApplicationProfileManagementService.AssignVisualProfile(
                settings,
                assignment,
                profile.Id);
            VisualProfileManagementService.Rename(
                settings,
                profile,
                $"Renamed {index}");
            VisualProfileManagementService.Delete(settings, profile);
        }

        Assert.AreEqual(2, settings.VisualProfiles.Count);
        Assert.AreEqual(20, settings.Applications.Count);
        Assert.IsTrue(settings.Applications.All(assignment =>
            assignment.VisualProfileId == VisualProfile.DefaultSoftInvertId));
        Assert.IsFalse(SettingsStore.Normalize(settings));
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
