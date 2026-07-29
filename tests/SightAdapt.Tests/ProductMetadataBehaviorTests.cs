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
        Assert.AreEqual(
            $"{ProductInfo.ProductName}™",
            ProductInfo.MarkedProductName);
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.VersionLabel));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.MilestoneLabel));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.Publisher));
        Assert.AreEqual(
            ProductInfo.Publisher,
            ProductInfo.Author);
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.License));
        Assert.IsFalse(
            ProductInfo.VersionLabel.Contains(
                '+',
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void PublicWebMetadataUsesAbsoluteAddresses()
    {
        AssertAbsoluteWebAddress(
            ProductInfo.WebsiteUrl,
            ProductInfo.WebsiteDisplay);
        AssertAbsoluteWebAddress(
            ProductInfo.RepositoryUrl,
            ProductInfo.RepositoryDisplay);
    }

    private static void AssertAbsoluteWebAddress(
        string url,
        string display)
    {
        Assert.IsTrue(Uri.TryCreate(
            url,
            UriKind.Absolute,
            out var address));
        Assert.IsTrue(
            address.Scheme is "http" or "https");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(display));
    }
}
