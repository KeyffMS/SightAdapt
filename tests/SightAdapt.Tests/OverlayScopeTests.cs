using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

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
                @"C:\Apps\reader.exe"));

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
                @"C:\Apps\reader.exe"))
            .Profile;

        ApplicationProfileManagementService.SetOverlayScope(
            settings,
            profile,
            OverlayScope.AllScreens);

        Assert.AreEqual(OverlayScope.AllScreens, profile.OverlayScope);
        Assert.AreEqual("all-screens", profile.OverlayScopeId);
    }

    [TestMethod]
    public void ScopeMetadataRoundTripsEveryIdentifier()
    {
        CollectionAssert.AreEqual(
            OverlayScopePolicy.Definitions
                .Select(definition => definition.Scope)
                .ToArray(),
            OverlayScopePolicy.All.ToArray());

        foreach (var definition in OverlayScopePolicy.Definitions)
        {
            Assert.IsTrue(
                OverlayScopePolicy.IsSupported(definition.Scope));
            Assert.AreEqual(
                definition.Id,
                OverlayScopePolicy.ToId(definition.Scope));
            Assert.AreEqual(
                definition.DisplayName,
                OverlayScopePolicy.GetDisplayName(definition.Scope));

            foreach (var identifier in definition.Identifiers)
            {
                Assert.IsTrue(
                    OverlayScopePolicy.TryParseId(
                        $"  {identifier.ToUpperInvariant()}  ",
                        out var parsed));
                Assert.AreEqual(definition.Scope, parsed);
            }
        }
    }

    [TestMethod]
    public void UnknownScopeIdentifierIsRejectedConsistently()
    {
        Assert.IsFalse(
            OverlayScopePolicy.TryParseId(
                "unknown-scope",
                out var parsed));
        Assert.AreEqual(OverlayScopePolicy.Default, parsed);
        Assert.ThrowsException<ArgumentException>(() =>
            OverlayScopePolicy.ParseRequired("unknown-scope"));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            OverlayScopePolicy.ToId((OverlayScope)999));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            OverlayScopePolicy.GetDisplayName((OverlayScope)999));
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
