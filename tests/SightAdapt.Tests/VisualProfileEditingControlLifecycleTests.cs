using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileEditingControlLifecycleTests
{
    [TestMethod]
    public void DropDownSelectionEndsCellEditAndReleasesTheInterface()
    {
        RunOnSta(RunDropDownSelectionScenario);
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
            "The profile-selector lifecycle test did not finish in time.");

        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }

    private static void RunDropDownSelectionScenario()
    {
        using var form = new Form
        {
            ShowInTaskbar = false,
            Size = new Size(420, 240),
        };
        using var grid = new DataGridView
        {
            AllowUserToAddRows = false,
            Dock = DockStyle.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter,
        };
        var column = new StableVisualProfileComboBoxColumn
        {
            Name = "VisualProfile",
        };
        column.SetProfiles(
        [
            VisualProfile.CreateDefaultInvert(),
            VisualProfile.CreateDefaultSoftInvert(),
        ]);
        grid.Columns.Add(column);
        grid.Rows.Add(VisualProfile.DefaultInvertId);
        form.Controls.Add(grid);
        form.Show();
        Application.DoEvents();

        grid.CurrentCell = grid.Rows[0].Cells[0];
        grid.Focus();
        Assert.IsTrue(grid.BeginEdit(true));
        Assert.IsInstanceOfType<ModernVisualProfileEditingControl>(
            grid.EditingControl);
        var editingControl =
            (ModernVisualProfileEditingControl)grid.EditingControl;

        InvokePrivate(editingControl, "ShowDropDown");
        Application.DoEvents();
        var list = GetPrivateField<ListBox>(editingControl, "_list");
        list.SelectedItem = list.Items
            .Cast<VisualProfileOption>()
            .Single(option => option.Id == VisualProfile.DefaultSoftInvertId);

        var cellEndEditCount = 0;
        grid.CellEndEdit += (_, _) => cellEndEditCount++;
        InvokePrivate(editingControl, "CommitListSelection");

        WaitFor(() =>
            cellEndEditCount == 1 &&
            !grid.IsCurrentCellInEditMode &&
            grid.EditingControl is null);

        Assert.AreEqual(1, cellEndEditCount);
        Assert.IsFalse(grid.IsCurrentCellInEditMode);
        Assert.IsNull(grid.EditingControl);
        Assert.AreEqual(
            VisualProfile.DefaultSoftInvertId,
            grid.Rows[0].Cells[0].Value);
        form.Close();
    }

    private static T GetPrivateField<T>(object instance, string name)
        where T : class
    {
        return (T)(instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(instance) ??
            throw new MissingFieldException(instance.GetType().FullName, name));
    }

    private static void InvokePrivate(object instance, string name)
    {
        try
        {
            instance.GetType().GetMethod(
                name,
                BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(instance, null);
        }
        catch (TargetInvocationException exception) when (exception.InnerException is not null)
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
