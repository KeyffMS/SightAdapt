using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class ArchitectureComplianceTests
{
    [TestMethod]
    public void AssignmentWritesAreRestrictedToAuthorityAndRecovery()
    {
        AssertPatternRestrictedTo(
            @"(?m)^(?!\s*string\?\s+VisualProfileId\s*=)\s*.*\bVisualProfileId\s*=",
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
    public void UiAndRuntimeUseTransactionalSettingsCoordinator()
    {
        foreach (var fileName in new[]
                 {
                     "ConfigurationForm.cs",
                     "VisualProfileManagerForm.cs",
                     "SightAdaptContext.cs",
                 })
        {
            var source = ReadSource(fileName);
            Assert.IsFalse(
                source.Contains("SettingsStore", StringComparison.Ordinal),
                $"{fileName} must not own settings persistence.");
            StringAssert.Contains(source, "SettingsCoordinator");
        }
    }

    [TestMethod]
    public void SettingsCoordinatorPublishesOnlyAfterSave()
    {
        var source = ReadSource("SettingsCoordinator.cs");
        var saveIndex = source.IndexOf(
            "_store.Save(candidate);",
            StringComparison.Ordinal);
        var publishIndex = source.IndexOf(
            "Current.ReplaceWith(candidate);",
            StringComparison.Ordinal);

        Assert.IsTrue(saveIndex >= 0);
        Assert.IsTrue(publishIndex > saveIndex);
    }

    [TestMethod]
    public void EmergencyStopsOverlayBeforePersistence()
    {
        var source = ReadSource("SightAdaptContext.cs");
        var methodIndex = source.IndexOf(
            "private void EmergencyDisable()",
            StringComparison.Ordinal);
        var disableIndex = source.IndexOf(
            "_overlayController.Disable();",
            methodIndex,
            StringComparison.Ordinal);
        var commitIndex = source.IndexOf(
            "_settingsCoordinator.Commit",
            methodIndex,
            StringComparison.Ordinal);

        Assert.IsTrue(methodIndex >= 0);
        Assert.IsTrue(disableIndex > methodIndex);
        Assert.IsTrue(commitIndex > disableIndex);
    }

    [TestMethod]
    public void RuntimeStateOwnsProfileAndSuppression()
    {
        var source = ReadSource("ApplicationStateController.cs");
        StringAssert.Contains(source, "string? VisualProfileId");
        StringAssert.Contains(source, "AutomaticSuppressedWindow");
        StringAssert.Contains(source, "Fault,");
        StringAssert.Contains(source, "Emergency,");
    }

    [TestMethod]
    public void RuntimeCompositionIsSplitIntoFocusedComponents()
    {
        var context = ReadSource("SightAdaptContext.cs");
        StringAssert.Contains(context, "ForegroundWindowTracker");
        StringAssert.Contains(context, "TrayPresenter");
        Assert.IsFalse(context.Contains("NotifyIcon", StringComparison.Ordinal));
        Assert.IsTrue(SourceExists("ForegroundWindowTracker.cs"));
        Assert.IsTrue(SourceExists("TrayPresenter.cs"));
    }

    [TestMethod]
    public void TransformCapabilitiesComeFromCatalog()
    {
        var model = ReadSource("ApplicationProfile.cs");
        var policy = ReadSource("VisualProfilePolicy.cs");
        var editor = ReadSource("VisualProfileEditorForm.cs");
        var manager = ReadSource("VisualProfileManagerForm.cs");

        StringAssert.Contains(model, "VisualTransformCatalog.SupportsTuning");
        StringAssert.Contains(policy, "VisualTransformCatalog.IsSupported");
        StringAssert.Contains(editor, "VisualTransformCatalog.Default");
        StringAssert.Contains(manager, "VisualTransformCatalog");
        Assert.IsFalse(
            editor.Contains(
                "new SoftInvertVisualTransform",
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProfileEditorUsesDomainLimitsAndFieldSpecificWrites()
    {
        var source = ReadSource("VisualProfileEditorForm.cs");
        StringAssert.Contains(source, "VisualProfileLimits.MinimumOutputBlack");
        StringAssert.Contains(source, "VisualProfileLimits.MaximumHueShift");
        StringAssert.Contains(source, "AttachPercentage(_outputBlackInput");
        StringAssert.Contains(source, "setter((float)(input.Value / 100m));");
        Assert.IsFalse(source.Contains("DecimalPlaces = 0", StringComparison.Ordinal));
    }

    [TestMethod]
    public void StableComboColumnDoesNotHideDataSource()
    {
        var source = ReadSource("VisualProfileComboBoxColumn.cs");
        StringAssert.Contains(source, "StableVisualProfileComboBoxColumn");
        StringAssert.Contains(source, "public void SetProfiles");
        Assert.IsFalse(source.Contains("new object? DataSource", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProductMetadataComesFromAssemblyConfiguration()
    {
        var source = ReadSource("ProductInfo.cs");
        var project = ReadSource("SightAdapt.Demo.csproj");

        StringAssert.Contains(source, "AssemblyProductAttribute");
        StringAssert.Contains(source, "AssemblyMetadataAttribute");
        Assert.IsFalse(source.Contains("0.4 Alpha", StringComparison.Ordinal));
        Assert.IsFalse(
            source.Contains(
                "github.com/KeyffMS/SightAdapt",
                StringComparison.Ordinal));
        StringAssert.Contains(
            project,
            "<AssemblyMetadata Include=\"RepositoryUrl\"");
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
    public void ProgramEnforcesSingleInstance()
    {
        var source = ReadSource("Program.cs");
        StringAssert.Contains(source, "SingleInstanceMutexName");
        StringAssert.Contains(source, "new Mutex");
        StringAssert.Contains(source, "isFirstInstance");
    }

    [TestMethod]
    public void ExpectedFailuresAreNotSilentlySwallowed()
    {
        var violations = Directory
            .EnumerateFiles(SourceDirectory, "*.cs", SearchOption.TopDirectoryOnly)
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                @"catch\s*(?:\([^)]*\))?\s*\{\s*\}",
                RegexOptions.CultureInvariant | RegexOptions.Singleline))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            $"Empty catch blocks found in: {string.Join(", ", violations)}");
    }

    [TestMethod]
    public void LegacyAssignmentMutationServiceWasRemoved()
    {
        Assert.IsFalse(SourceExists("ApplicationProfileToggleService.cs"));
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
            $"Restricted mutation pattern '{pattern}' found in: " +
            string.Join(", ", violations));
    }

    private static bool SourceExists(string fileName)
    {
        return File.Exists(Path.Combine(SourceDirectory, fileName));
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
