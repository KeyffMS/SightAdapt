using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class SettingsCoordinatorTests
{
    [TestMethod]
    public void SuccessfulCommitPublishesPersistedSnapshot()
    {
        using var temporaryDirectory =
            new TemporaryDirectory();
        var settingsPath = Path.Combine(
            temporaryDirectory.Path,
            "settings.json");
        var coordinator =
            new SettingsCoordinator(
                new SettingsStore(settingsPath));
        var changedEvents = 0;
        var fileExistedWhenPublished = false;
        coordinator.Changed += (_, _) =>
        {
            changedEvents++;
            fileExistedWhenPublished =
                File.Exists(settingsPath);
        };

        var result = coordinator.Commit(
            settings =>
                AutomaticModeManagementService
                    .Disable(settings));

        Assert.IsTrue(result.Succeeded);
        Assert.IsFalse(
            coordinator.Current.AutomaticMode);
        Assert.AreEqual(1, changedEvents);
        Assert.IsTrue(
            fileExistedWhenPublished);
    }

    [TestMethod]
    public void FailedPersistenceDoesNotPublishCandidateState()
    {
        using var temporaryDirectory =
            new TemporaryDirectory();
        var blockingFile = Path.Combine(
            temporaryDirectory.Path,
            "not-a-directory");
        File.WriteAllText(
            blockingFile,
            "block");
        var settingsPath = Path.Combine(
            blockingFile,
            "settings.json");
        var coordinator =
            new SettingsCoordinator(
                new SettingsStore(settingsPath));
        var changedEvents = 0;
        coordinator.Changed +=
            (_, _) => changedEvents++;

        var result = coordinator.Commit(
            settings =>
                AutomaticModeManagementService
                    .Disable(settings));

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(
            coordinator.Current.AutomaticMode);
        Assert.AreEqual(0, changedEvents);
    }

    [TestMethod]
    public void FailedDomainMutationDoesNotPublishPartialChanges()
    {
        using var temporaryDirectory =
            new TemporaryDirectory();
        var coordinator =
            new SettingsCoordinator(
                new SettingsStore(Path.Combine(
                    temporaryDirectory.Path,
                    "settings.json")));

        var result = coordinator.Commit(
            settings =>
            {
                AutomaticModeManagementService
                    .Disable(settings);
                ApplicationProfileManagementService
                    .AssignVisualProfile(
                        settings,
                        new ApplicationProfile(),
                        "missing");
            });

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(
            coordinator.Current.AutomaticMode);
        Assert.AreEqual(
            0,
            coordinator.Current.Applications.Count);
    }

    [TestMethod]
    public void CommitReturnsValueFromPublishedCandidate()
    {
        using var temporaryDirectory =
            new TemporaryDirectory();
        var coordinator =
            new SettingsCoordinator(
                new SettingsStore(Path.Combine(
                    temporaryDirectory.Path,
                    "settings.json")));

        var result = coordinator.Commit(
            settings =>
                VisualProfileManagementService.Create(
                    settings,
                    "Reader").Id);

        Assert.IsTrue(result.Succeeded);
        Assert.IsTrue(
            coordinator.Current.VisualProfiles.Any(
                profile =>
                    profile.Id == result.Value));
    }

    private sealed class TemporaryDirectory :
        IDisposable
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
                Directory.Delete(
                    Path,
                    true);
            }
        }
    }
}
