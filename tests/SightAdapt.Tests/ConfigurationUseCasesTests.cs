using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ConfigurationUseCasesTests
{
    [TestMethod]
    public void AssignmentChangeIsCommittedThroughConfigurationAuthority()
    {
        using var directory = new TemporaryDirectory();
        var coordinator = new SettingsCoordinator(
            new SettingsStore(Path.Combine(
                directory.Path,
                "settings.json")));
        var identity = new ApplicationIdentity(
            "Reader",
            "reader.exe",
            @"C:\Apps\reader.exe");
        Assert.IsTrue(coordinator.Commit(settings =>
            ApplicationAssignmentService.AddOrEnable(
                settings,
                identity)).Succeeded);
        var useCases = new ConfigurationUseCases(coordinator);

        var result = useCases.Apply(
            new ApplicationAssignmentChange.VisualProfile(
                identity.ExecutablePath,
                VisualProfileCatalog.DefaultNoneId));

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(
            VisualProfileCatalog.DefaultNoneId,
            coordinator.Current.Assignments.Single().VisualProfileId);
    }

    [TestMethod]
    public void VisualProfileCommandsUseDedicatedUseCaseAuthority()
    {
        using var directory = new TemporaryDirectory();
        var coordinator = new SettingsCoordinator(
            new SettingsStore(Path.Combine(
                directory.Path,
                "settings.json")));
        var useCases = new VisualProfileUseCases(coordinator);

        var created = useCases.Create("Reader colors");
        Assert.IsTrue(created.Succeeded);

        var renamed = useCases.Rename(
            created.Value,
            "Reader contrast");

        Assert.IsTrue(renamed.Succeeded);
        Assert.AreEqual(
            "Reader contrast",
            coordinator.Current.VisualProfiles.Single(
                profile => profile.Id == created.Value).Name);
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
                Directory.Delete(Path, recursive: true);
            }
        }
    }
}
