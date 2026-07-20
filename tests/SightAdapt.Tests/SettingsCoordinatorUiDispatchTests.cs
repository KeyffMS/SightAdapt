using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class SettingsCoordinatorUiDispatchTests
{
    [TestMethod]
    public void WinFormsObserverRunsAfterCommitStackCompletes()
    {
        Exception? failure = null;
        var completed = false;

        var thread = new Thread(() =>
        {
            try
            {
                RunDeferredObserverScenario();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                completed = true;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(
            thread.Join(TimeSpan.FromSeconds(10)),
            "The STA dispatch test did not finish in time.");
        Assert.IsTrue(completed);

        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }

    private static void RunDeferredObserverScenario()
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

            var deadline = DateTime.UtcNow.AddSeconds(2);
            while (observer.CallCount == 0 && DateTime.UtcNow < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(1);
            }

            Assert.AreEqual(1, observer.CallCount);
            Assert.IsFalse(coordinator.Current.AutomaticMode);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, true);
            }
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
}
