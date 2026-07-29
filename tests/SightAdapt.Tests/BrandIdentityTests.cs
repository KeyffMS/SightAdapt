using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class BrandIdentityTests
{
    private const string CanonicalWebsite =
        "https://aiteracja.pl/sightadapt/";
    private const string Publisher =
        "KeyffMS / aiteracja.pl";
    private const string MarkNotice =
        "SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.";
    private const string CanonicalDescription =
        "SightAdapt is a free, open-source Windows application for per-application visual accessibility and color correction.";

    [TestMethod]
    public void PublicDocumentationUsesCanonicalIdentity()
    {
        var readme = ReadRepositoryFile("README.md");
        var brand = ReadRepositoryFile("docs", "BRAND.md");
        var releasing = ReadRepositoryFile("docs", "RELEASING.md");
        var contributing = ReadRepositoryFile("CONTRIBUTING.md");

        StringAssert.StartsWith(readme, "# SightAdapt™");
        StringAssert.Contains(readme, CanonicalDescription);
        StringAssert.Contains(readme, CanonicalWebsite);
        StringAssert.Contains(readme, Publisher);
        StringAssert.Contains(readme, MarkNotice);

        StringAssert.Contains(brand, CanonicalDescription);
        StringAssert.Contains(brand, CanonicalWebsite);
        StringAssert.Contains(brand, Publisher);
        StringAssert.Contains(brand, MarkNotice);
        StringAssert.Contains(brand, "Never use `®`");

        StringAssert.Contains(releasing, CanonicalDescription);
        StringAssert.Contains(releasing, CanonicalWebsite);
        StringAssert.Contains(releasing, MarkNotice);
        StringAssert.Contains(contributing, "docs/BRAND.md");
    }

    [TestMethod]
    public void RuntimeMetadataUsesCanonicalIdentity()
    {
        Assert.AreEqual("SightAdapt", ProductInfo.ProductName);
        Assert.AreEqual("SightAdapt™", ProductInfo.MarkedProductName);
        Assert.AreEqual(Publisher, ProductInfo.Publisher);
        Assert.AreEqual(CanonicalWebsite, ProductInfo.WebsiteUrl);
        Assert.AreEqual(
            "aiteracja.pl/sightadapt",
            ProductInfo.WebsiteDisplay);
    }

    private static string ReadRepositoryFile(
        params string[] path)
    {
        return File.ReadAllText(Path.Combine(
            new[] { RepositoryLayout.Root }.Concat(path).ToArray()));
    }
}
