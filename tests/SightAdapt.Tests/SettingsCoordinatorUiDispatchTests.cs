using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class SettingsCoordinatorUiDispatchTests
{
    [TestMethod]
    public void WinFormsObserverRunsAfterCommitStackCompletes()
    {
        RunOnSta(RunDeferredObserverScenario);
    }

    [TestMethod]
    public void WinFormsObserverWaitsForActiveGridEditToEnd()
    {
        RunOnSta(RunGridEditScenario);
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
            "The STA dispatch test did not finish in time.");

        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }

    private static void RunDeferredObserverScenario()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var coordinator = CreateCoordinator(directory);
            using var observer = new SettingsChangedProbe();
            _ = observer.Handle;
            coordinator.Changed += observer.HandleChanged;

            var result = coordinator.Commit(settings =>
                AutomaticModeManagementService.Disable(settings));

            Assert.IsTrue(result.Succeeded);
            Assert.AreEqual(
                0,
                observer.CallCount,
                "A Control observer must not run inside the active commit stack.");

            WaitFor(() => observer.CallCount == 1);

            Assert.AreEqual(1, observer.CallCount);
            Assert.IsFalse(coordinator.Current.AutomaticMode);
        }
        finally
        {
            DeleteTemporaryDirectory(directory);
        }
    }

    private static void RunGridEditScenario()
    {
        var directory = CreateTemporaryDirectory();

        try
        {
            var coordinator = CreateCoordinator(directory);
            using var observer = new GridSettingsChangedProbe();
            observer.Show();
            Application.DoEvents();
            observer.StartEditing();
            coordinator.Changed += observer.HandleChanged;

            var result = coordinator.Commit(settings =>
                AutomaticModeManagementService.Disable(settings));

            Assert.IsTrue(result.Succeeded);
            Application.DoEvents();
            Assert.AreEqual(
                0,
                observer.CallCount,
                "A settings observer must not rebuild a DataGridView while its cell is still being edited.");
            Assert.AreEqual(1, observer.Grid.Rows.Count);
            Assert.IsTrue(observer.Grid.IsCurrentCellInEditMode);

            Assert.IsTrue(observer.Grid.EndEdit());
            WaitFor(() => observer.CallCount == 1);

            Assert.AreEqual(1, observer.CallCount);
            Assert.AreEqual(0, observer.Grid.Rows.Count);
            Assert.IsFalse(coordinator.Current.AutomaticMode);
            observer.Close();
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

    private sealed class SettingsChangedProbe : Control
    {
        public int CallCount { get; private set; }

        public void HandleChanged(object? sender, EventArgs eventArgs)
        {
            CallCount++;
        }
    }

    private sealed class GridSettingsChangedProbe : Form
    {
        public GridSettingsChangedProbe()
        {
            ShowInTaskbar = false;
            Size = new Size(320, 180);
            Grid = new DataGridView
            {
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                Dock = DockStyle.Fill,
            };
            Grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Value",
                HeaderText = "Value",
            });
            Grid.Rows.Add("before");
            Controls.Add(Grid);
        }

        public DataGridView Grid { get; }

        public int CallCount { get; private set; }

        public void StartEditing()
        {
            Grid.CurrentCell = Grid.Rows[0].Cells[0];
            Grid.Focus();
            Assert.IsTrue(Grid.BeginEdit(true));
            Assert.IsTrue(Grid.IsCurrentCellInEditMode);
        }

        public void HandleChanged(object? sender, EventArgs eventArgs)
        {
            CallCount++;
            Grid.Rows.Clear();
        }
    }
}
