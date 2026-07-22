using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ArchitectureAuditRegressionTests
{
    [TestMethod]
    public void RuntimeProfileIdentityHasOneProductTruth()
    {
        var state = ReadSource("ApplicationStateController.cs");
        var overlay = ReadSource("OverlayController.cs");

        StringAssert.Contains(
            state,
            "string? VisualProfileId");
        Assert.IsFalse(
            overlay.Contains(
                "VisualProfileId",
                StringComparison.Ordinal),
            "OverlayController must own only the overlay resource. " +
            "ApplicationStateController.Current owns product runtime profile identity.");
    }

    [TestMethod]
    public void VisualProfileCollectionMutationsStayInLifecycleAuthority()
    {
        var violations = Directory
            .EnumerateFiles(
                SourceDirectory,
                "*.cs",
                SearchOption.TopDirectoryOnly)
            .Where(path =>
                !string.Equals(
                    Path.GetFileName(path),
                    "VisualProfileManagementService.cs",
                    StringComparison.OrdinalIgnoreCase))
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                @"\.VisualProfiles\.(Add|Remove)\(",
                RegexOptions.CultureInvariant))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            "Visual-profile collection mutations found outside " +
            $"VisualProfileManagementService: {string.Join(", ", violations)}");
    }

    private static string ReadSource(string fileName)
    {
        return File.ReadAllText(
            Path.Combine(
                SourceDirectory,
                fileName));
    }

    private static string SourceDirectory =>
        Path.Combine(
            RepositoryRoot,
            "src",
            "SightAdapt");

    private static string RepositoryRoot
    {
        get
        {
            var directory =
                new DirectoryInfo(
                    AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(
                        directory.FullName,
                        "src",
                        "SightAdapt")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "The SightAdapt repository root could not be located.");
        }
    }
}
