using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

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

        StringAssert.Contains(model, "VisualTransformCatalog.Default.SupportsTuning");
        StringAssert.Contains(policy, "VisualTransformCatalog.Default.IsSupported");
        StringAssert.Contains(editor, "VisualTransformCatalog.Default");
        StringAssert.Contains(manager, "VisualTransformCatalog");
        Assert.IsFalse(
            editor.Contains(
                "new SoftInvertVisualTransform",
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProfileEditorUsesDomainLimitsAndFieldSpecificSliders()
    {
        var source = ReadSource("VisualProfileEditorForm.cs");
        StringAssert.Contains(source, "VisualProfileLimits.MinimumOutputBlack");
        StringAssert.Contains(source, "VisualProfileLimits.MaximumHueShift");
        StringAssert.Contains(source, "ModernProfileSlider");
        StringAssert.Contains(source, "AttachPercentage(");
        StringAssert.Contains(source, "value => _workingProfile.OutputBlack = value");
        StringAssert.Contains(source, "value => _workingProfile.HueShiftDegrees = value");
        StringAssert.Contains(source, "OutputLimitPreview");
        Assert.IsFalse(source.Contains("NumericUpDown", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProfileSliderSupportsSynchronizedNumericEntry()
    {
        var source = ReadSource("ProfileSliderControl.cs");
        StringAssert.Contains(source, "private readonly TextBox _valueInput;");
        StringAssert.Contains(source, "private void CommitInput()");
        StringAssert.Contains(source, "NormalizeDecimalSeparator");
        StringAssert.Contains(source, "Value = SnapToStep(value);");
        StringAssert.Contains(source, "_valueInput.Validating");
        Assert.IsFalse(source.Contains("NumericUpDown", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProfileGridOwnsStatusPaintingAndComboColumnOwnsSelector()
    {
        var selector = ReadSource("ModernSelectorComboBoxColumn.cs");
        var grid = ReadSource("ApplicationProfilesGrid.cs");

        StringAssert.Contains(selector, "StableModernSelectorComboBoxColumn");
        StringAssert.Contains(selector, "public void SetProfiles");
        StringAssert.Contains(selector, "ModernSelectorComboBoxCell");
        Assert.IsFalse(selector.Contains("GridCellPainting", StringComparison.Ordinal));
        Assert.IsFalse(selector.Contains("new object? DataSource", StringComparison.Ordinal));

        StringAssert.Contains(grid, "internal sealed class ApplicationProfilesGrid");
        StringAssert.Contains(grid, "GridCellPainting");
        StringAssert.Contains(grid, "EnabledColumnName");
        StringAssert.Contains(grid, "AppTheme.Success");
    }

    [TestMethod]
    public void ProfileSelectorUsesCustomDarkEditingControl()
    {
        var source = ReadSource("ModernSelectorComboBoxColumn.cs");
        StringAssert.Contains(source, "ModernSelectorEditingControl");
        StringAssert.Contains(source, "IDataGridViewEditingControl");
        StringAssert.Contains(source, "ToolStripDropDown");
        StringAssert.Contains(source, "ListBox");
        StringAssert.Contains(source, "public override Type EditType");
        Assert.IsFalse(
            source.Contains(
                "DataGridViewComboBoxEditingControl",
                StringComparison.Ordinal));
        Assert.IsFalse(
            source.Contains(
                "EditingControlShowing",
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void TrayUsesOneMenuForLeftClickAndAbout()
    {
        var source = ReadSource("TrayPresenter.cs");
        StringAssert.Contains(source, "private readonly ContextMenuStrip _menu;");
        StringAssert.Contains(source, "_notifyIcon.MouseClick += NotifyIconMouseClick;");
        StringAssert.Contains(source, "eventArgs.Button == MouseButtons.Left");
        StringAssert.Contains(source, "_menu.Show(Cursor.Position);");
        StringAssert.Contains(source, "new AboutForm(_icons.Inactive)");
        Assert.IsTrue(SourceExists("AboutForm.cs"));
    }

    [TestMethod]
    public void ProductMetadataComesFromAssemblyConfiguration()
    {
        var source = ReadSource("ProductInfo.cs");
        var project = ReadSource("SightAdapt.csproj");

        StringAssert.Contains(source, "AssemblyProductAttribute");
        StringAssert.Contains(source, "AssemblyMetadataAttribute");
        StringAssert.Contains(source, "GetMetadata(\"Milestone\"");
        Assert.IsFalse(source.Contains("0.4 Alpha", StringComparison.Ordinal));
        Assert.IsFalse(
            source.Contains(
                "github.com/KeyffMS/SightAdapt",
                StringComparison.Ordinal));
        StringAssert.Contains(
            project,
            "<AssemblyMetadata Include=\"RepositoryUrl\"");
        StringAssert.Contains(
            project,
            "<AssemblyMetadata Include=\"Milestone\"");
        Assert.IsFalse(
            project.Contains(
                "<Title>SightAdapt 0.4 Alpha</Title>",
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void AboutDialogUsesCanonicalProductMetadata()
    {
        var source = ReadSource("AboutForm.cs");
        StringAssert.Contains(source, "ProductInfo.ProductName");
        StringAssert.Contains(source, "ProductInfo.MilestoneLabel");
        StringAssert.Contains(source, "ProductInfo.VersionLabel");
        StringAssert.Contains(source, "ProductInfo.Author");
        StringAssert.Contains(source, "ProductInfo.License");
        StringAssert.Contains(source, "Icon sourceIcon");
        Assert.IsFalse(source.Contains("Alpha 0.4", StringComparison.Ordinal));
    }

    [TestMethod]
    public void AboutDialogExposesRepositoryWithoutClippingMetadata()
    {
        var source = ReadSource("AboutForm.cs");
        StringAssert.Contains(source, "ProductInfo.RepositoryDisplay");
        StringAssert.Contains(source, "ProductInfo.RepositoryUrl");
        StringAssert.Contains(source, "new Size(720, 470)");
        StringAssert.Contains(source, "AutoEllipsis = false");
        StringAssert.Contains(source, "OpenRepository");
    }

    [TestMethod]
    public void DarkThemeSecondaryTextUsesReadableContrast()
    {
        var source = ReadSource("ModernTheme.cs");
        StringAssert.Contains(
            source,
            "TextSecondary = Color.FromArgb(190, 200, 216)");
        StringAssert.Contains(
            source,
            "TextMuted = Color.FromArgb(151, 164, 184)");
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

    [TestMethod]
    public void SettingsCoordinatorDoesNotOwnWinFormsLifecycle()
    {
        var source = ReadSource("SettingsCoordinator.cs");
        StringAssert.Contains(source, "Changed?.Invoke(this, EventArgs.Empty);");
        Assert.IsFalse(source.Contains("DataGridView", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Control control", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("System.Windows.Forms.Timer", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProfileSelectorUsesEditingControlContract()
    {
        var source = ReadSource("ModernSelectorComboBoxColumn.cs");
        StringAssert.Contains(source, "EditingControlValueChanged = true;");
        StringAssert.Contains(source, "NotifyCurrentCellDirty(true);");
        Assert.IsFalse(source.Contains("cell.Value = _selected?.Id", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("QueueGridEditCompletion", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ConfigurationFormOwnsTransactionsAndGridOwnsPresentation()
    {
        var form = ReadSource("ConfigurationForm.cs");
        var grid = ReadSource("ApplicationProfilesGrid.cs");

        StringAssert.Contains(form, "private bool _committingGridValue;");
        StringAssert.Contains(form, "if (_committingGridValue)");
        StringAssert.Contains(form, "_profilesGrid.UpdateApplication");
        StringAssert.Contains(form, "_profilesGrid.RestoreValue");
        Assert.IsFalse(form.Contains("Rows.Clear()", StringComparison.Ordinal));

        StringAssert.Contains(grid, "_grid.Rows.Clear();");
        StringAssert.Contains(grid, "row.Tag = application.ExecutablePath;");
        StringAssert.Contains(grid, "public string? SelectedExecutablePath");
        Assert.IsFalse(grid.Contains("SettingsCoordinator", StringComparison.Ordinal));
    }

    [TestMethod]
    public void OverlayScopeHasOneModelAndOneGeometryAuthority()
    {
        var model = ReadSource("ApplicationProfile.cs");
        var grid = ReadSource("ApplicationProfilesGrid.cs");
        var resolver = ReadSource("OverlayBoundsResolver.cs");
        var form = ReadSource("ConfigurationForm.cs");

        StringAssert.Contains(model, "OverlayScopeId");
        StringAssert.Contains(model, "public OverlayScope OverlayScope");
        StringAssert.Contains(grid, "OverlayScopeColumnName");
        StringAssert.Contains(form, "SetOverlayScope");
        StringAssert.Contains(resolver, "OverlayScope.ClientArea");
        StringAssert.Contains(resolver, "OverlayScope.AllScreens");
        Assert.IsFalse(grid.Contains("SettingsCoordinator", StringComparison.Ordinal));
        Assert.IsFalse(resolver.Contains("SettingsCoordinator", StringComparison.Ordinal));
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
        Path.Combine(RepositoryRoot, "src", "SightAdapt");

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
