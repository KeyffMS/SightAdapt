using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ConfigurationProfileManagerRefreshTests
{
    [TestMethod]
    public void ClosingManagerWithoutChangesDoesNotRefreshConfiguration()
    {
        RunOnSta(() =>
        {
            using var temporaryDirectory =
                new TemporaryDirectory();
            var coordinator = CreateCoordinator(
                temporaryDirectory.Path);
            var managerCalls = 0;
            using var form = new ConfigurationForm(
                coordinator,
                () => null,
                (_, receivedCoordinator) =>
                {
                    Assert.AreSame(
                        coordinator,
                        receivedCoordinator);
                    managerCalls++;
                });
            var generation = form.RefreshGeneration;

            form.ManageVisualProfiles();

            Assert.AreEqual(1, managerCalls);
            Assert.AreEqual(
                generation,
                form.RefreshGeneration);
        });
    }

    [TestMethod]
    public void ManagerMutationRefreshesConfigurationExactlyOnce()
    {
        RunOnSta(() =>
        {
            using var temporaryDirectory =
                new TemporaryDirectory();
            var coordinator = CreateCoordinator(
                temporaryDirectory.Path);
            using var form = new ConfigurationForm(
                coordinator,
                () => null,
                (_, receivedCoordinator) =>
                {
                    var result =
                        receivedCoordinator.Commit(settings =>
                            VisualProfileManagementService.Create(
                                settings,
                                "Reader").Id);
                    Assert.IsTrue(result.Succeeded);
                });
            var generation = form.RefreshGeneration;

            form.ManageVisualProfiles();

            Assert.AreEqual(
                generation + 1,
                form.RefreshGeneration);
            Assert.IsTrue(
                coordinator.Current.VisualProfiles.Any(
                    profile => profile.Name == "Reader"));
        });
    }

    private static SettingsCoordinator CreateCoordinator(
        string directory)
    {
        return new SettingsCoordinator(
            new SettingsStore(
                Path.Combine(
                    directory,
                    "settings.json")));
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(
            thread.Join(TimeSpan.FromSeconds(10)),
            "The configuration refresh test did not finish in time.");
        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
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
                Directory.Delete(
                    Path,
                    recursive: true);
            }
        }
    }
}
