using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class ArchitectureComplianceTests
{
    [TestMethod]
    public void ApplicationAssignmentWritesAreRestrictedToAuthorityAndRecovery()
    {
        AssertPatternRestrictedTo(
            @"\bVisualProfileId\s*=",
            "ApplicationProfile.cs",
            "ApplicationProfileManagementService.cs",
            "SettingsStore.cs",
            "OverlayController.cs");
        AssertPatternRestrictedTo(
            @"\.Applications\.(Add|Remove)\(",
            "ApplicationProfileManagementService.cs",
            "SettingsStore.cs");
    }

    [TestMethod]
    public void AutomaticModeWritesAreRestrictedToAuthority()
    {
        AssertPatternRestrictedTo(
            @"\.AutomaticMode\s*=",
            "AutomaticModeManagementService.cs");
    }

    [TestMethod]
    public void PersistedProfileEditorDoesNotMutateSourceDirectly()
    {
        var source = ReadSource("VisualProfileEditorForm.cs");

        Assert.IsFalse(source.Contains("_sourceProfile", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("CopyTuningFrom", StringComparison.Ordinal));
        StringAssert.Contains(source, "public static VisualProfile? Edit");
    }

    [TestMethod]
    public void ProductDefaultLiteralsAreRestrictedToDefaultsSource()
    {
        AssertPatternRestrictedTo(
            @"\b0\.08f\b|\b0\.92f\b",
            "VisualProfileDefaults.cs");
    }

    [TestMethod]
    public void SettingsNormalizationExposesFocusedStages()
    {
        var source = ReadSource("SettingsStore.cs");

        StringAssert.Contains(source, "CanonicalizeBuiltInProfiles(context);");
        StringAssert.Contains(source, "NormalizeCustomProfiles(context);");
        StringAssert.Contains(source, "NormalizeApplications(context);");
        StringAssert.Contains(source, "RepairProfileReferences(context);");
        StringAssert.Contains(source, "private sealed class SettingsNormalizationContext");
    }

    [TestMethod]
    public void LegacyAssignmentMutationServiceWasRemoved()
    {
        Assert.IsFalse(File.Exists(Path.Combine(
            SourceDirectory,
            "ApplicationProfileToggleService.cs")));
    }

    private static void AssertPatternRestrictedTo(
        string pattern,
        params string[] allowedFiles)
    {
        var allowed = allowedFiles.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var violations = Directory
            .EnumerateFiles(SourceDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => !allowed.Contains(Path.GetFileName(path)))
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
            $"Restricted mutation pattern '{pattern}' found in: {string.Join(", ", violations)}");
    }

    private static string ReadSource(string fileName)
    {
        return File.ReadAllText(Path.Combine(SourceDirectory, fileName));
    }

    private static string SourceDirectory =>
        Path.Combine(RepositoryRoot, "src", "SightAdapt.Demo");

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(
                        directory.FullName,
                        "src",
                        "SightAdapt.Demo")))
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
