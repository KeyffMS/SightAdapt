using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class OverlayScopeTests
{
    [TestMethod]
    public void NewApplicationDefaultsToClientArea()
    {
        var settings = new SightAdaptSettings();
        var result = ApplicationProfileManagementService.AddOrEnable(
            settings,
            new ApplicationIdentity(
                "Reader",
                "reader.exe",
                @"C:\Appseader.exe"));

        Assert.IsTrue(result.WasCreated);
        Assert.AreEqual(OverlayScope.ClientArea, result.Profile.OverlayScope);
        Assert.AreEqual("client-area", result.Profile.OverlayScopeId);
    }

    [TestMethod]
    public void ApplicationScopeMutationUsesAssignmentAuthority()
    {
        var settings = new SightAdaptSettings();
        var profile = ApplicationProfileManagementService.AddOrEnable(
            settings,
            new ApplicationIdentity(
                "Reader",
                "reader.exe",
                @"C:\Appseader.exe"))
            .Profile;

        ApplicationProfileManagementService.SetOverlayScope(
            settings,
            profile,
            OverlayScope.AllScreens);

        Assert.AreEqual(OverlayScope.AllScreens, profile.OverlayScope);
        Assert.AreEqual("all-screens", profile.OverlayScopeId);
    }

    [TestMethod]
    public void ScopeIdentifiersRoundTrip()
    {
        foreach (var scope in OverlayScopePolicy.All)
        {
            var id = OverlayScopePolicy.ToId(scope);
            Assert.IsTrue(OverlayScopePolicy.TryParseId(id, out var parsed));
            Assert.AreEqual(scope, parsed);
        }
    }

    [TestMethod]
    public void InvalidPersistedScopeRecoversToDefault()
    {
        var profile = new ApplicationProfile
        {
            OverlayScopeId = "unknown-scope",
        };

        Assert.AreEqual(OverlayScope.ClientArea, profile.OverlayScope);
        Assert.AreEqual("client-area", profile.OverlayScopeId);
    }

    [TestMethod]
    public void WorkingCopyPreservesPerApplicationScope()
    {
        var original = new ApplicationProfile
        {
            OverlayScopeId = "screen",
        };

        var copy = original.CreateWorkingCopy();

        Assert.AreEqual(OverlayScope.Screen, copy.OverlayScope);
        Assert.AreEqual("screen", copy.OverlayScopeId);
    }
}
