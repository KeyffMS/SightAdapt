using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileCatalogTests
{
    [TestMethod]
    public void CanonicalRegistryHasUniqueStableDefinitions()
    {
        var catalog = VisualProfileCatalog.Default;
        var definitions = catalog.Definitions.ToArray();

        CollectionAssert.AreEqual(
            new[]
            {
                VisualProfileCatalog.DefaultInvertId,
                VisualProfileCatalog.DefaultSoftInvertId,
                VisualProfileCatalog.DefaultNoneId,
            },
            definitions.Select(
                definition => definition.ProfileId).ToArray());
        Assert.AreEqual(
            definitions.Length,
            definitions.Select(
                    definition => definition.ProfileId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
        Assert.AreEqual(
            definitions.Length,
            definitions.Select(
                    definition => definition.TransformId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    [TestMethod]
    public void BuiltInMetadataAndTransformResolutionUseOneRegistry()
    {
        var catalog = VisualProfileCatalog.Default;

        foreach (var definition in catalog.Definitions)
        {
            Assert.IsTrue(catalog.IsBuiltInId(
                definition.ProfileId));
            Assert.IsTrue(catalog.IsSupportedTransform(
                definition.TransformId));
            Assert.AreEqual(
                definition.SupportsTuning,
                catalog.SupportsTuning(
                    definition.TransformId));
            Assert.AreEqual(
                definition.DisplayName,
                catalog.GetTransformDisplayName(
                    definition.TransformId));
            Assert.AreSame(
                definition.Transform,
                catalog.GetRequiredTransform(
                    definition.TransformId));
        }
    }

    [TestMethod]
    public void DefaultAndFallbackPolicyReferencesExist()
    {
        var catalog = VisualProfileCatalog.Default;

        Assert.IsTrue(catalog.IsBuiltInId(
            VisualProfilePolicy.NewAssignmentProfileId));
        Assert.IsTrue(catalog.IsBuiltInId(
            VisualProfilePolicy.DeletionFallbackProfileId));
        Assert.IsTrue(catalog.IsBuiltInId(
            VisualProfilePolicy.MissingReferenceFallbackProfileId));
    }

    [TestMethod]
    public void UnknownTransformIsRejectedConsistently()
    {
        var catalog = VisualProfileCatalog.Default;

        Assert.IsFalse(catalog.IsSupportedTransform(
            "custom-transform"));
        Assert.IsFalse(catalog.SupportsTuning(
            "custom-transform"));
        Assert.AreEqual(
            "custom-transform",
            catalog.GetTransformDisplayName(
                " custom-transform "));
        Assert.ThrowsException<InvalidOperationException>(() =>
            catalog.GetRequiredTransform(
                "custom-transform"));
    }
}
