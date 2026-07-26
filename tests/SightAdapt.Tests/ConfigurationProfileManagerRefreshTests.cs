using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ConfigurationProfileManagerRefreshTests
{
    [TestMethod]
    public void ClosingManagerWithoutChangesDoesNotRefreshConfiguration()
    {
        StaTest.Run(() =>
        {
            using var workspace =
                new TestWorkspace("configuration-manager-refresh");
            var coordinator = workspace.CreateSettingsCoordinator();
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
        StaTest.Run(() =>
        {
            using var workspace =
                new TestWorkspace("configuration-manager-refresh");
            var coordinator = workspace.CreateSettingsCoordinator();
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

}
