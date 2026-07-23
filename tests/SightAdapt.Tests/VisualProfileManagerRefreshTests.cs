using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileManagerRefreshTests
{
    [TestMethod]
    public void LocalCommitRefreshesGridExactlyOnce()
    {
        RunOnSta(() =>
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                var coordinator = CreateCoordinator(directory);
                using var manager =
                    new VisualProfileManagerForm(coordinator);
                var generation = manager.RefreshGeneration;

                manager.Commit(settings =>
                    VisualProfileManagementService.Create(
                        settings,
                        "Reader").Id);

                Assert.AreEqual(
                    generation + 1,
                    manager.RefreshGeneration);
                Assert.IsTrue(coordinator.Current.VisualProfiles.Any(
                    profile => profile.Name == "Reader"));
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        });
    }

    [TestMethod]
    public void ExternalSettingsChangeRefreshesGridExactlyOnce()
    {
        RunOnSta(() =>
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                var coordinator = CreateCoordinator(directory);
                using var manager =
                    new VisualProfileManagerForm(coordinator);
                var generation = manager.RefreshGeneration;

                var result = coordinator.Commit(settings =>
                    VisualProfileManagementService.Create(
                        settings,
                        "Writer").Id);

                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(
                    generation + 1,
                    manager.RefreshGeneration);
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        });
    }

    private static SettingsCoordinator CreateCoordinator(
        string directory)
    {
        return new SettingsCoordinator(
            new SettingsStore(
                Path.Combine(directory, "settings.json")));
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
            "The profile-manager refresh test did not finish in time.");
        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "SightAdapt.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteTemporaryDirectory(
        string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}