using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ArchitectureComplianceTests
{
    [TestMethod]
    public void ApplicationAssignmentWritesStayInAuthorities()
    {
        // Intentional exhaustive source scan: a finite runtime scenario cannot
        // prove that no other top-level production file writes these collections.
        AssertPatternRestrictedTo(
            @"(?m)^(?!\s*string\?\s+VisualProfileId\s*=)\s*.*\bVisualProfileId\s*=",
            "ApplicationProfile.cs",
            "ApplicationProfileManagementService.cs",
            "SettingsNormalizer.cs",
            "OverlayController.cs");
        AssertPatternRestrictedTo(
            @"\.Applications\.(Add|Remove)\(",
            "ApplicationProfileManagementService.cs",
            "SettingsNormalizer.cs");
    }

    [TestMethod]
    public void AutomaticModeWritesStayInAuthority()
    {
        // Intentional exhaustive source scan for the single mutation authority.
        AssertPatternRestrictedTo(
            @"\.AutomaticMode\s*=",
            "AutomaticModeManagementService.cs");
    }

    [TestMethod]
    public void VisualProfileCollectionWritesStayInLifecycleAuthority()
    {
        // Intentional exhaustive source scan for collection ownership.
        AssertPatternRestrictedTo(
            @"\.VisualProfiles\.(Add|Remove)\(",
            "VisualProfileManagementService.cs");
    }

    [TestMethod]
    public void UiAndRuntimeDoNotPersistSettingsDirectly()
    {
        // This is a dependency-boundary check rather than an implementation-
        // spelling check: these components must use SettingsCoordinator.
        foreach (var fileName in new[]
                 {
                     "ConfigurationForm.cs",
                     "VisualProfileManagerForm.cs",
                     "SightAdaptContext.cs",
                     "RuntimeCoordinator.cs",
                 })
        {
            var source = ReadSource(fileName);
            Assert.IsFalse(
                source.Contains(
                    "SettingsStore",
                    StringComparison.Ordinal),
                $"{fileName} must not own settings persistence.");
        }
    }

    [TestMethod]
    public void ExpectedFailuresAreNotSilentlySwallowed()
    {
        // Intentional exhaustive scan: empty catch blocks have no observable
        // behavior and therefore cannot be covered reliably by runtime tests.
        var violations = Directory
            .EnumerateFiles(
                SourceDirectory,
                "*.cs",
                SearchOption.TopDirectoryOnly)
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                @"catch\s*(?:\([^)]*\))?\s*\{\s*\}",
                RegexOptions.CultureInvariant |
                RegexOptions.Singleline))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            $"Empty catch blocks found in: {string.Join(", ", violations)}");
    }

    [TestMethod]
    public void RemovedLegacyMutationServiceDoesNotReturn()
    {
        Assert.IsFalse(
            File.Exists(Path.Combine(
                SourceDirectory,
                "ApplicationProfileToggleService.cs")));
    }

    private static void AssertPatternRestrictedTo(
        string pattern,
        params string[] allowedFiles)
    {
        var allowed = allowedFiles.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        var violations = Directory
            .EnumerateFiles(
                SourceDirectory,
                "*.cs",
                SearchOption.TopDirectoryOnly)
            .Where(path => !allowed.Contains(
                Path.GetFileName(path)))
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                pattern,
                RegexOptions.CultureInvariant))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            $"Restricted mutation pattern '{pattern}' found in: " +
            string.Join(", ", violations));
    }

    private static string ReadSource(string fileName)
    {
        return File.ReadAllText(
            Path.Combine(SourceDirectory, fileName));
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
                new DirectoryInfo(AppContext.BaseDirectory);
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
