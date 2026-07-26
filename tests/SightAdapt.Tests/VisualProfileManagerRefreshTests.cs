using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileManagerRefreshTests
{
    [TestMethod]
    public void LocalCommitRefreshesGridExactlyOnce()
    {
        StaTest.Run(() =>
        {
            using var workspace =
                new TestWorkspace("profile-manager-refresh");
            var coordinator = workspace.CreateSettingsCoordinator();
            using var manager =
                new VisualProfileManagerForm(coordinator);
            var generation = manager.RefreshGeneration;

            manager.Commit(useCases =>
                    useCases.Create("Reader"));

            Assert.AreEqual(
                generation + 1,
                manager.RefreshGeneration);
            Assert.IsTrue(coordinator.Current.VisualProfiles.Any(
                    profile => profile.Name == "Reader"));
        });
    }

    [TestMethod]
    public void ExternalSettingsChangeRefreshesGridExactlyOnce()
    {
        StaTest.Run(() =>
        {
            using var workspace =
                new TestWorkspace("profile-manager-refresh");
            var coordinator = workspace.CreateSettingsCoordinator();
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
        });
    }

}
