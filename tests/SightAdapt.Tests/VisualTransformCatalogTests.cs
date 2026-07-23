using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualTransformCatalogTests
{
    [TestMethod]
    public void BuiltInMetadataAndResolutionUseTheSameRegistry()
    {
        var catalog = VisualTransformCatalog.Default;

        Assert.IsTrue(catalog.IsSupported(InvertVisualTransform.TransformId));
        Assert.IsFalse(catalog.SupportsTuning(InvertVisualTransform.TransformId));
        Assert.AreEqual(
            VisualProfileDefaults.ExactInvertName,
            catalog.GetDisplayName(InvertVisualTransform.TransformId));
        Assert.AreEqual(
            InvertVisualTransform.TransformId,
            catalog.GetRequired(InvertVisualTransform.TransformId).Id);

        Assert.IsTrue(catalog.IsSupported(SoftInvertVisualTransform.TransformId));
        Assert.IsTrue(catalog.SupportsTuning(SoftInvertVisualTransform.TransformId));
        Assert.AreEqual(
            VisualProfileDefaults.SoftInvertName,
            catalog.GetDisplayName(SoftInvertVisualTransform.TransformId));
        Assert.AreEqual(
            SoftInvertVisualTransform.TransformId,
            catalog.GetRequired(SoftInvertVisualTransform.TransformId).Id);
    }

    [TestMethod]
    public void UnknownTransformIsRejectedConsistently()
    {
        var catalog = VisualTransformCatalog.Default;

        Assert.IsFalse(catalog.IsSupported("custom-transform"));
        Assert.IsFalse(catalog.SupportsTuning("custom-transform"));
        Assert.AreEqual(
            "custom-transform",
            catalog.GetDisplayName(" custom-transform "));
        Assert.ThrowsException<InvalidOperationException>(() =>
            catalog.GetRequired("custom-transform"));
    }
}