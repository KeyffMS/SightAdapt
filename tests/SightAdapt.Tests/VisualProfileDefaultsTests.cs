using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileDefaultsTests
{
    [TestMethod]
    public void CatalogFactoriesUseCanonicalNamesAndTuning()
    {
        var catalog = VisualProfileCatalog.Default;
        var exact = catalog.CreateBuiltInProfile(
            VisualProfileCatalog.DefaultInvertId);
        var soft = catalog.CreateBuiltInProfile(
            VisualProfileCatalog.DefaultSoftInvertId);

        Assert.AreEqual(
            catalog.GetRequiredBuiltInDefinition(
                VisualProfileCatalog.DefaultInvertId).DisplayName,
            exact.Name);
        Assert.AreEqual(
            VisualProfileDefaults.ExactOutputBlack,
            exact.OutputBlack);
        Assert.AreEqual(
            VisualProfileDefaults.ExactOutputWhite,
            exact.OutputWhite);
        Assert.AreEqual(
            catalog.GetRequiredBuiltInDefinition(
                VisualProfileCatalog.DefaultSoftInvertId).DisplayName,
            soft.Name);
        Assert.AreEqual(
            VisualProfileDefaults.SoftOutputBlack,
            soft.OutputBlack);
        Assert.AreEqual(
            VisualProfileDefaults.SoftOutputWhite,
            soft.OutputWhite);
    }

    [TestMethod]
    public void BuiltInDefinitionRestoresIdentityAndTuning()
    {
        var catalog = VisualProfileCatalog.Default;
        var definition = catalog.GetRequiredBuiltInDefinition(
            VisualProfileCatalog.DefaultInvertId);
        var profile = definition.CreateBuiltInProfile();
        profile.Name = "Broken";
        profile.TransformId =
            SoftInvertVisualTransform.TransformId;
        profile.OutputBlack = 0.2f;

        var changed = definition.CanonicalizeBuiltInProfile(
            profile);

        Assert.IsTrue(changed);
        Assert.AreEqual(
            definition.DisplayName,
            profile.Name);
        Assert.AreEqual(
            InvertVisualTransform.TransformId,
            profile.TransformId);
        Assert.AreEqual(
            VisualProfileDefaults.ExactOutputBlack,
            profile.OutputBlack);
    }

    [TestMethod]
    public void SoftTuningNormalizationUsesCanonicalFallbacks()
    {
        var profile = VisualProfileCatalog.Default
            .CreateBuiltInProfile(
                VisualProfileCatalog.DefaultSoftInvertId);
        profile.OutputBlack = float.NaN;
        profile.OutputWhite = float.PositiveInfinity;

        var tuning = VisualProfileDefaults
            .NormalizeSoftInvertTuning(profile);

        Assert.AreEqual(
            VisualProfileDefaults.SoftOutputBlack,
            tuning.OutputBlack);
        Assert.AreEqual(
            VisualProfileDefaults.SoftOutputWhite,
            tuning.OutputWhite);
    }
}
