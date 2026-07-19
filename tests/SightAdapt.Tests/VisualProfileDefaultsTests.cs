using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileDefaultsTests
{
    [TestMethod]
    public void FactoriesUseCanonicalNamesAndTuning()
    {
        var exact = VisualProfile.CreateDefaultInvert();
        var soft = VisualProfile.CreateDefaultSoftInvert();

        Assert.AreEqual(VisualProfileDefaults.ExactInvertName, exact.Name);
        Assert.AreEqual(VisualProfileDefaults.ExactOutputBlack, exact.OutputBlack);
        Assert.AreEqual(VisualProfileDefaults.ExactOutputWhite, exact.OutputWhite);
        Assert.AreEqual(VisualProfileDefaults.SoftInvertName, soft.Name);
        Assert.AreEqual(VisualProfileDefaults.SoftOutputBlack, soft.OutputBlack);
        Assert.AreEqual(VisualProfileDefaults.SoftOutputWhite, soft.OutputWhite);
    }

    [TestMethod]
    public void CanonicalExactInvertRestoresIdentityAndTuning()
    {
        var profile = VisualProfile.CreateDefaultInvert();
        profile.Name = "Broken";
        profile.TransformId = SoftInvertVisualTransform.TransformId;
        profile.OutputBlack = 0.2f;

        var changed = VisualProfileDefaults.CanonicalizeExactInvert(profile);

        Assert.IsTrue(changed);
        Assert.AreEqual(VisualProfileDefaults.ExactInvertName, profile.Name);
        Assert.AreEqual(InvertVisualTransform.TransformId, profile.TransformId);
        Assert.AreEqual(VisualProfileDefaults.ExactOutputBlack, profile.OutputBlack);
    }

    [TestMethod]
    public void SoftTuningNormalizationUsesCanonicalFallbacks()
    {
        var profile = VisualProfile.CreateDefaultSoftInvert();
        profile.OutputBlack = float.NaN;
        profile.OutputWhite = float.PositiveInfinity;

        var tuning = VisualProfileDefaults.NormalizeSoftInvertTuning(profile);

        Assert.AreEqual(VisualProfileDefaults.SoftOutputBlack, tuning.OutputBlack);
        Assert.AreEqual(VisualProfileDefaults.SoftOutputWhite, tuning.OutputWhite);
    }
}
