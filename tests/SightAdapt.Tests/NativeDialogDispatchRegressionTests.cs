using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class NativeDialogDispatchRegressionTests
{
    [TestMethod]
    public void DisabledObserverRecoversWhenEnabledChangedIsNotRaised()
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
            "The native-dialog dispatch regression test did not finish in time.");

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
            using var observer = new SilentEnabledChangedProbe
            {
                Enabled = false,
            };
            _ = observer.Handle;
            coordinator.Changed += observer.HandleChanged;

            var result = coordinator.Commit(settings =>
                AutomaticModeManagementService.Disable(settings));

            Assert.IsTrue(result.Succeeded);
            Application.DoEvents();
            Assert.AreEqual(
                0,
                observer.CallCount,
                "A disabled owner must not refresh inside a modal message loop.");

            observer.Enabled = true;
            WaitFor(() => observer.CallCount == 1);

            Assert.AreEqual(
                1,
                observer.CallCount,
                "Application.Idle must release an observer when a native dialog re-enables its owner without EnabledChanged.");
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

    private static void WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(2);
        while (!condition() && DateTime.UtcNow < deadline)
        {
            Application.DoEvents();
            Thread.Sleep(1);
        }
    }

    private sealed class SilentEnabledChangedProbe : Control
    {
        public int CallCount { get; private set; }

        public void HandleChanged(object? sender, EventArgs eventArgs)
        {
            CallCount++;
        }

        protected override void OnEnabledChanged(EventArgs eventArgs)
        {
            // Native common dialogs can toggle the owner HWND without raising
            // the managed EnabledChanged event. Suppress it here to reproduce
            // that notification gap deterministically.
        }
    }
}
