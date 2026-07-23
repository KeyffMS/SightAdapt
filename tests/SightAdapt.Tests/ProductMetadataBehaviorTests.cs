using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProductMetadataBehaviorTests
{
    [TestMethod]
    public void AssemblyMetadataProducesCompleteProductInformation()
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.ProductName));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.VersionLabel));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.MilestoneLabel));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.Author));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.License));
        Assert.IsFalse(
            ProductInfo.VersionLabel.Contains(
                '+',
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void RepositoryMetadataIsAnAbsoluteWebAddress()
    {
        Assert.IsTrue(Uri.TryCreate(
            ProductInfo.RepositoryUrl,
            UriKind.Absolute,
            out var repository));
        Assert.IsTrue(
            repository.Scheme is "http" or "https");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(
                ProductInfo.RepositoryDisplay));
    }
}