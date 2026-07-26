using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class RuntimeOverlayContractTests
{
    [TestMethod]
    public void RuntimeOverlayHasOneExplicitActivationMethod()
    {
        var activationMethods = typeof(IRuntimeOverlay)
            .GetMethods()
            .Where(method => method.Name == nameof(IRuntimeOverlay.Activate))
            .ToArray();

        Assert.AreEqual(1, activationMethods.Length);
        CollectionAssert.AreEqual(
            new[] { typeof(OverlayActivationRequest) },
            activationMethods[0]
                .GetParameters()
                .Select(parameter => parameter.ParameterType)
                .ToArray());
    }

    [TestMethod]
    public void ActivationRequestKeepsPrimaryAndMenuProfilesDistinct()
    {
        var primary = VisualProfileCatalog.Default
            .CreateBuiltInProfile(
                VisualProfileCatalog.DefaultNoneId);
        var menu = VisualProfileCatalog.Default
            .CreateBuiltInProfile(
                VisualProfileCatalog.DefaultInvertId);

        var request = new OverlayActivationRequest(
            (nint)42,
            primary,
            menu,
            OverlayScope.Window);

        Assert.AreSame(primary, request.VisualProfile);
        Assert.AreSame(menu, request.MenuVisualProfile);
        Assert.AreEqual((nint)42, request.TargetWindow);
        Assert.AreEqual(OverlayScope.Window, request.OverlayScope);
    }

    [TestMethod]
    public void ActivationRequestRejectsInvalidTargetAndScope()
    {
        var profile = VisualProfileCatalog.Default
            .CreateBuiltInProfile(
                VisualProfileCatalog.DefaultSoftInvertId);

        Assert.ThrowsException<ArgumentException>(() =>
            new OverlayActivationRequest(
                nint.Zero,
                profile,
                profile,
                OverlayScope.ClientArea));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            new OverlayActivationRequest(
                (nint)42,
                profile,
                profile,
                (OverlayScope)999));
    }
}
