using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProductInfoVersionTests
{
    [DataTestMethod]
    [DataRow("0.5.0.22+abcdef", "0.5.0.22")]
    [DataRow("0.5.0.22", "0.5.0.22")]
    [DataRow(" 0.5.0.22+abcdef ", "0.5.0.22")]
    [DataRow("", "Unknown")]
    public void DisplayVersionOmitsBuildMetadata(
        string input,
        string expected)
    {
        Assert.AreEqual(
            expected,
            ProductInfo.CreateVersionLabel(input));
    }

    [TestMethod]
    public void RuntimeVersionLabelIsVisibleAndCompact()
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(
                ProductInfo.VersionLabel));
        Assert.IsFalse(
            ProductInfo.VersionLabel.Contains(
                '+',
                StringComparison.Ordinal));
    }
}