from pathlib import Path

path = Path(__file__).resolve().parents[1] / "tests/SightAdapt.Tests/ArchitectureComplianceTests.cs"
text = path.read_text(encoding="utf-8")

start = text.index("    [TestMethod]\n    public void StableComboColumnOwnsModernSelectorAndStatusPainting()")
end = text.index("    [TestMethod]\n    public void ProfileSelectorUsesCustomDarkEditingControl()", start)
text = text[:start] + '''    [TestMethod]
    public void ProfileGridOwnsStatusPaintingAndComboColumnOwnsSelector()
    {
        var selector = ReadSource("VisualProfileComboBoxColumn.cs");
        var grid = ReadSource("ApplicationProfilesGrid.cs");

        StringAssert.Contains(selector, "StableVisualProfileComboBoxColumn");
        StringAssert.Contains(selector, "public void SetProfiles");
        StringAssert.Contains(selector, "ModernVisualProfileComboBoxCell");
        Assert.IsFalse(selector.Contains("GridCellPainting", StringComparison.Ordinal));
        Assert.IsFalse(selector.Contains("new object? DataSource", StringComparison.Ordinal));

        StringAssert.Contains(grid, "internal sealed class ApplicationProfilesGrid");
        StringAssert.Contains(grid, "GridCellPainting");
        StringAssert.Contains(grid, "EnabledColumnName");
        StringAssert.Contains(grid, "AppTheme.Success");
    }

''' + text[end:]

start = text.index("    [TestMethod]\n    public void ConfigurationFormOwnsItsGridRefreshBoundary()")
end = text.index("    private static void AssertPatternRestrictedTo(", start)
text = text[:start] + '''    [TestMethod]
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

''' + text[end:]

path.write_text(text, encoding="utf-8")
