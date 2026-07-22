using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class ConfigurationGridCommitRegressionTests
{
    [TestMethod]
    public void ProfileSelectionCommitsWithoutRebuildingActiveGrid()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                RunScenario();
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

    private static void RunScenario()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "SightAdapt.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);

        try
        {
            var coordinator = new SettingsCoordinator(
                new SettingsStore(Path.Combine(directory, "settings.json")));
            var identity = new ApplicationIdentity(
                "Reader",
                "reader.exe",
                @"C:\Apps\reader.exe");
            var seed = coordinator.Commit(settings =>
            {
                ApplicationProfileManagementService.AddOrEnable(settings, identity);
            });
            Assert.IsTrue(seed.Succeeded);

            using var form = new ConfigurationForm(coordinator, () => null);
            form.Show();
            Application.DoEvents();

            var grid = GetPrivateField<DataGridView>(form, "_profilesGrid");
            Assert.AreEqual(1, grid.Rows.Count);
            var row = grid.Rows[0];
            var profileCell = row.Cells["VisualProfile"];
            grid.CurrentCell = profileCell;
            grid.Focus();
            Assert.IsTrue(grid.BeginEdit(true));
            Assert.IsInstanceOfType<ModernVisualProfileEditingControl>(
                grid.EditingControl);

            var editor =
                (ModernVisualProfileEditingControl)grid.EditingControl;
            var option = ((DataGridViewComboBoxCell)profileCell)
                .Items
                .Cast<object>()
                .OfType<VisualProfileOption>()
                .Single(candidate =>
                    candidate.Id == VisualProfile.DefaultInvertId);

            InvokeSelectOption(editor, option);
            WaitFor(() =>
                coordinator.Current.Applications.Single().VisualProfileId ==
                VisualProfile.DefaultInvertId);

            Assert.AreEqual(1, grid.Rows.Count);
            Assert.AreEqual(
                VisualProfile.DefaultInvertId,
                grid.Rows[0].Cells["VisualProfile"].Value);
            Assert.IsInstanceOfType<ApplicationProfile>(grid.Rows[0].Tag);
            var committedRowProfile =
                (ApplicationProfile)grid.Rows[0].Tag!;
            Assert.AreEqual(
                VisualProfile.DefaultInvertId,
                committedRowProfile.VisualProfileId);

            Assert.IsTrue(grid.EndEdit());
            grid.CurrentCell = grid.Rows[0].Cells["Application"];
            Application.DoEvents();
            Assert.AreEqual(1, grid.Rows.Count);
            form.Close();
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
        }
    }

    private static T GetPrivateField<T>(object instance, string name)
        where T : class
    {
        return (T)(instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) ??
            throw new MissingFieldException(instance.GetType().FullName, name));
    }

    private static void InvokeSelectOption(
        ModernVisualProfileEditingControl editor,
        VisualProfileOption option)
    {
        try
        {
            editor.GetType().GetMethod(
                "SelectOption",
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(
                    editor,
                    [option, true]);
        }
        catch (TargetInvocationException exception)
            when (exception.InnerException is not null)
        {
            throw exception.InnerException;
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
