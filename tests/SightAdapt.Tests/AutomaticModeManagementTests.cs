using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class AutomaticModeManagementTests
{
    [TestMethod]
    public void SetReportsWhetherPersistedModeChanged()
    {
        var settings = new SightAdaptSettings
        {
            AutomaticMode = true,
        };

        Assert.IsFalse(AutomaticModeManagementService.Enable(settings));
        Assert.IsTrue(AutomaticModeManagementService.Disable(settings));
        Assert.IsFalse(settings.AutomaticMode);
        Assert.IsFalse(AutomaticModeManagementService.Disable(settings));
    }

    [TestMethod]
    public void EnableAndDisableShareOneMutationBoundary()
    {
        var settings = new SightAdaptSettings
        {
            AutomaticMode = false,
        };

        AutomaticModeManagementService.Enable(settings);
        Assert.IsTrue(settings.AutomaticMode);

        AutomaticModeManagementService.Disable(settings);
        Assert.IsFalse(settings.AutomaticMode);
    }
}
