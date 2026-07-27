using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ConfigurationGridCommitRegressionTests
{
    [TestMethod]
    public void ProfileSelectionCommitsWithoutRebuildingActiveGrid()
    {
        StaTest.Run(RunProfileSelectionScenario);
    }

    [TestMethod]
    public void ExternalSettingsChangeRebindsGridFromCurrentSettings()
    {
        StaTest.Run(RunExternalChangeScenario);
    }

    [TestMethod]
    public void OverlayScopeSelectionCommitsOnlySelectedApplication()
    {
        StaTest.Run(RunOverlayScopeSelectionScenario);
    }

    private static void RunProfileSelectionScenario()
    {
        using var workspace =
            new TestWorkspace("configuration-grid");
        var coordinator = workspace.CreateSettingsCoordinator();
        var identity = new ApplicationIdentity(
            "Reader",
            "reader.exe",
            @"C:\Apps\reader.exe");
        Assert.IsTrue(coordinator.Commit(settings =>
        {
            ApplicationAssignmentService.AddOrEnable(
                settings,
                identity);
        }).Succeeded);

        using var form = new ConfigurationForm(coordinator, () => null);
        form.Show();
        Application.DoEvents();

        var profilesGrid = FindControl<ApplicationAssignmentsGrid>(form);
        var grid = FindControl<DataGridView>(profilesGrid);
        Assert.AreEqual(1, grid.Rows.Count);
        Assert.AreEqual(identity.ExecutablePath, grid.Rows[0].Tag);

        var profileCell = grid.Rows[0].Cells["VisualProfile"];
        grid.CurrentCell = profileCell;
        grid.Focus();
        Assert.IsTrue(grid.BeginEdit(true));
        Assert.IsInstanceOfType<ModernSelectorEditingControl>(
            grid.EditingControl);

        var editor =
            (ModernSelectorEditingControl)grid.EditingControl;
        var option = ((DataGridViewComboBoxCell)profileCell)
            .Items
            .Cast<object>()
            .OfType<ModernSelectorOption>()
            .Single(candidate =>
                candidate.Id == VisualProfileCatalog.DefaultInvertId);

        editor.SelectOptionFromInput(option);
        WaitFor(() =>
            coordinator.Current.Assignments.Single().VisualProfileId ==
            VisualProfileCatalog.DefaultInvertId);

        Assert.AreEqual(1, grid.Rows.Count);
        Assert.AreEqual(
            VisualProfileCatalog.DefaultInvertId,
            grid.Rows[0].Cells["VisualProfile"].Value);
        Assert.AreEqual(identity.ExecutablePath, grid.Rows[0].Tag);

        grid.CurrentCell = grid.Rows[0].Cells["Application"];
        Application.DoEvents();
        Assert.IsFalse(grid.IsCurrentCellInEditMode);
        Assert.AreEqual(1, grid.Rows.Count);
        form.Close();
    }

    private static void RunExternalChangeScenario()
    {
        using var workspace =
            new TestWorkspace("configuration-grid");
        var coordinator = workspace.CreateSettingsCoordinator();
        Assert.IsTrue(coordinator.Commit(settings =>
        {
            ApplicationAssignmentService.AddOrEnable(
                settings,
                new ApplicationIdentity(
                    "Reader",
                    "reader.exe",
                    @"C:\Apps\reader.exe"));
        }).Succeeded);

        using var form = new ConfigurationForm(coordinator, () => null);
        form.Show();
        Application.DoEvents();

        var grid = FindControl<DataGridView>(
            FindControl<ApplicationAssignmentsGrid>(form));
        Assert.AreEqual(1, grid.Rows.Count);

        Assert.IsTrue(coordinator.Commit(settings =>
        {
            ApplicationAssignmentService.AddOrEnable(
                settings,
                new ApplicationIdentity(
                    "Writer",
                    "writer.exe",
                    @"C:\Apps\writer.exe"));
        }).Succeeded);
        Application.DoEvents();

        Assert.AreEqual(2, grid.Rows.Count);
        CollectionAssert.AreEquivalent(
            new[]
            {
                @"C:\Apps\reader.exe",
                @"C:\Apps\writer.exe",
            },
            grid.Rows
                .Cast<DataGridViewRow>()
                .Select(row => (string)row.Tag!)
                .ToArray());
        form.Close();
    }

    private static void RunOverlayScopeSelectionScenario()
    {
        using var workspace =
            new TestWorkspace("configuration-grid");
        var coordinator = workspace.CreateSettingsCoordinator();
        var reader = new ApplicationIdentity(
            "Reader",
            "reader.exe",
            @"C:\Apps\reader.exe");
        var writer = new ApplicationIdentity(
            "Writer",
            "writer.exe",
            @"C:\Apps\writer.exe");
        Assert.IsTrue(coordinator.Commit(settings =>
        {
            ApplicationAssignmentService.AddOrEnable(settings, reader);
            ApplicationAssignmentService.AddOrEnable(settings, writer);
        }).Succeeded);

        using var form = new ConfigurationForm(coordinator, () => null);
        form.Show();
        Application.DoEvents();

        var grid = FindControl<DataGridView>(
            FindControl<ApplicationAssignmentsGrid>(form));
        var readerRow = grid.Rows
            .Cast<DataGridViewRow>()
            .Single(row => string.Equals(
                row.Tag as string,
                reader.ExecutablePath,
                StringComparison.OrdinalIgnoreCase));
        var scopeCell = readerRow.Cells["OverlayScope"];
        grid.CurrentCell = scopeCell;
        grid.Focus();
        Assert.IsTrue(grid.BeginEdit(true));
        Assert.IsInstanceOfType<ModernSelectorEditingControl>(
            grid.EditingControl);

        var editor = (ModernSelectorEditingControl)grid.EditingControl;
        var option = ((DataGridViewComboBoxCell)scopeCell)
            .Items
            .Cast<object>()
            .OfType<ModernSelectorOption>()
            .Single(candidate => candidate.Id == "screen");
        editor.SelectOptionFromInput(option);

        WaitFor(() => coordinator.Current.Assignments
            .Single(profile => profile.Matches(reader))
            .OverlayScope == OverlayScope.Screen);

        Assert.AreEqual(
            OverlayScope.Screen,
            coordinator.Current.Assignments
                .Single(profile => profile.Matches(reader))
                .OverlayScope);
        Assert.AreEqual(
            OverlayScope.ClientArea,
            coordinator.Current.Assignments
                .Single(profile => profile.Matches(writer))
                .OverlayScope);
        Assert.AreEqual("screen", scopeCell.Value);
        form.Close();
    }

    private static T FindControl<T>(Control root)
        where T : Control
    {
        if (root is T match)
        {
            return match;
        }

        foreach (Control child in root.Controls)
        {
            try
            {
                return FindControl<T>(child);
            }
            catch (InvalidOperationException)
            {
            }
        }

        throw new InvalidOperationException(
            $"Control {typeof(T).Name} was not found.");
    }

    private static void WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
    }
}
