using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class ConfigurationGridCommitRegressionTests
{
    [TestMethod]
    public void ProfileSelectionCommitsWithoutRebuildingActiveGrid()
    {
        RunOnSta(RunProfileSelectionScenario);
    }

    [TestMethod]
    public void ExternalSettingsChangeRebindsGridFromCurrentSettings()
    {
        RunOnSta(RunExternalChangeScenario);
    }

    [TestMethod]
    public void OverlayScopeSelectionCommitsOnlySelectedApplication()
    {
        RunOnSta(RunOverlayScopeSelectionScenario);
    }

    private static void RunOnSta(Action scenario)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                scenario();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(
            thread.Join(TimeSpan.FromSeconds(10)),
            "The configuration-grid regression did not finish in time.");

        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }

    private static void RunProfileSelectionScenario()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var coordinator = CreateCoordinator(directory);
            var identity = new ApplicationIdentity(
                "Reader",
                "reader.exe",
                @"C:\Apps\reader.exe");
            Assert.IsTrue(coordinator.Commit(settings =>
            {
                ApplicationProfileManagementService.AddOrEnable(
                    settings,
                    identity);
            }).Succeeded);

            using var form = new ConfigurationForm(coordinator, () => null);
            form.Show();
            Application.DoEvents();

            var profilesGrid = FindControl<ApplicationProfilesGrid>(form);
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
                    candidate.Id == VisualProfile.DefaultInvertId);

            editor.SelectOptionFromInput(option);
            WaitFor(() =>
                coordinator.Current.Applications.Single().VisualProfileId ==
                VisualProfile.DefaultInvertId);

            Assert.AreEqual(1, grid.Rows.Count);
            Assert.AreEqual(
                VisualProfile.DefaultInvertId,
                grid.Rows[0].Cells["VisualProfile"].Value);
            Assert.AreEqual(identity.ExecutablePath, grid.Rows[0].Tag);

            grid.CurrentCell = grid.Rows[0].Cells["Application"];
            Application.DoEvents();
            Assert.IsFalse(grid.IsCurrentCellInEditMode);
            Assert.AreEqual(1, grid.Rows.Count);
            form.Close();
        }
        finally
        {
            DeleteTemporaryDirectory(directory);
        }
    }

    private static void RunExternalChangeScenario()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var coordinator = CreateCoordinator(directory);
            Assert.IsTrue(coordinator.Commit(settings =>
            {
                ApplicationProfileManagementService.AddOrEnable(
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
                FindControl<ApplicationProfilesGrid>(form));
            Assert.AreEqual(1, grid.Rows.Count);

            Assert.IsTrue(coordinator.Commit(settings =>
            {
                ApplicationProfileManagementService.AddOrEnable(
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
        finally
        {
            DeleteTemporaryDirectory(directory);
        }
    }

    private static void RunOverlayScopeSelectionScenario()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var coordinator = CreateCoordinator(directory);
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
                ApplicationProfileManagementService.AddOrEnable(settings, reader);
                ApplicationProfileManagementService.AddOrEnable(settings, writer);
            }).Succeeded);

            using var form = new ConfigurationForm(coordinator, () => null);
            form.Show();
            Application.DoEvents();

            var grid = FindControl<DataGridView>(
                FindControl<ApplicationProfilesGrid>(form));
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

            WaitFor(() => coordinator.Current.Applications
                .Single(profile => profile.Matches(reader))
                .OverlayScope == OverlayScope.Screen);

            Assert.AreEqual(
                OverlayScope.Screen,
                coordinator.Current.Applications
                    .Single(profile => profile.Matches(reader))
                    .OverlayScope);
            Assert.AreEqual(
                OverlayScope.ClientArea,
                coordinator.Current.Applications
                    .Single(profile => profile.Matches(writer))
                    .OverlayScope);
            Assert.AreEqual("screen", scopeCell.Value);
            form.Close();
        }
        finally
        {
            DeleteTemporaryDirectory(directory);
        }
    }

    private static SettingsCoordinator CreateCoordinator(string directory)
    {
        return new SettingsCoordinator(
            new SettingsStore(Path.Combine(directory, "settings.json")));
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

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "SightAdapt.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteTemporaryDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
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
