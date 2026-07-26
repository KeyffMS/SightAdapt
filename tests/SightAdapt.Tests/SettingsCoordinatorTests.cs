using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class SettingsCoordinatorTests
{
    [TestMethod]
    public void SuccessfulCommitPublishesPersistedSnapshot()
    {
        using var temporaryDirectory =
            new TestWorkspace();
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
    public void CurrentReturnsDefensiveSnapshot()
    {
        using var temporaryDirectory =
            new TestWorkspace();
        var coordinator =
            new SettingsCoordinator(
                new SettingsStore(Path.Combine(
                    temporaryDirectory.Path,
                    "settings.json")));
        var identity = new ApplicationIdentity(
            "Reader",
            "reader.exe",
            @"C:\Apps\reader.exe");
        var result = coordinator.Commit(settings =>
            ApplicationAssignmentService.AddOrEnable(
                settings,
                identity));
        Assert.IsTrue(result.Succeeded);

        var changedEvents = 0;
        coordinator.Changed += (_, _) => changedEvents++;
        var snapshot = coordinator.Current;
        snapshot.AutomaticMode = false;
        snapshot.Assignments[0].Enabled = false;
        snapshot.Assignments.Clear();
        snapshot.VisualProfiles.Clear();

        var current = coordinator.Current;
        Assert.IsTrue(current.AutomaticMode);
        Assert.AreEqual(1, current.Assignments.Count);
        Assert.IsTrue(current.Assignments[0].Enabled);
        Assert.AreEqual(3, current.VisualProfiles.Count);
        Assert.AreEqual(0, changedEvents);
    }

    [TestMethod]
    public void FailedPersistenceDoesNotPublishCandidateState()
    {
        using var temporaryDirectory =
            new TestWorkspace();
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
            new TestWorkspace();
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
                ApplicationAssignmentService
                    .AssignVisualProfile(
                        settings,
                        new ApplicationAssignment(),
                        "missing");
            });

        Assert.IsFalse(result.Succeeded);
        Assert.IsTrue(
            coordinator.Current.AutomaticMode);
        Assert.AreEqual(
            0,
            coordinator.Current.Assignments.Count);
    }

    [TestMethod]
    public void ValidationFailureDoesNotExposeGenericValue()
    {
        using var temporaryDirectory =
            new TestWorkspace();
        var coordinator =
            new SettingsCoordinator(
                new SettingsStore(Path.Combine(
                    temporaryDirectory.Path,
                    "settings.json")));

        var result = coordinator.Commit<string>(_ =>
            throw new SettingsValidationException(
                "The requested change is invalid."));

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(
            "The requested change is invalid.",
            result.ErrorMessage);
        Assert.IsFalse(result.TryGetValue(out _));
        Assert.ThrowsException<InvalidOperationException>(
            () => _ = result.Value);
    }

    [TestMethod]
    public void UnexpectedMutationFailureIsReportedAndRethrown()
    {
        using var temporaryDirectory =
            new TestWorkspace();
        Exception? reported = null;
        var coordinator =
            new SettingsCoordinator(
                new SettingsStore(Path.Combine(
                    temporaryDirectory.Path,
                    "settings.json")),
                exception => reported = exception);
        var changedEvents = 0;
        coordinator.Changed += (_, _) => changedEvents++;

        var thrown = Assert.ThrowsException<InvalidOperationException>(
            () => coordinator.Commit(settings =>
            {
                settings.AutomaticMode = false;
                throw new InvalidOperationException(
                    "programming failure");
            }));

        Assert.AreSame(thrown, reported);
        Assert.IsTrue(coordinator.Current.AutomaticMode);
        Assert.AreEqual(0, changedEvents);
    }

    [TestMethod]
    public void CommitReturnsValueFromPublishedCandidate()
    {
        using var temporaryDirectory =
            new TestWorkspace();
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
        Assert.IsTrue(result.TryGetValue(out var profileId));
        Assert.AreEqual(result.Value, profileId);
        Assert.IsTrue(
            coordinator.Current.VisualProfiles.Any(
                profile =>
                    profile.Id == profileId));
    }

}
