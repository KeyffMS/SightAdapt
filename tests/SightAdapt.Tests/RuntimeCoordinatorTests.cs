using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class RuntimeCoordinatorTests
{
    [TestMethod]
    public void ManualToggleActivatesAndSuppressesConfiguredTarget()
    {
        using var context = new RuntimeTestContext();
        context.AddEnabledAssignment();

        context.Coordinator.ToggleForActiveWindow();

        Assert.AreEqual(1, context.Overlay.ActivationCount);
        Assert.AreEqual(context.Target, context.Overlay.TargetWindow);
        Assert.AreEqual(
            ApplicationRunState.ManualActive,
            context.State.Current.Kind);

        context.Coordinator.ToggleForActiveWindow();

        Assert.IsFalse(context.Overlay.IsActive);
        Assert.AreEqual(
            ApplicationRunState.Inactive,
            context.State.Current.Kind);
        Assert.IsTrue(
            context.State.IsAutomaticSuppressedFor(
                context.Target));
    }

    [TestMethod]
    public void ForegroundChangeAutomaticallyActivatesConfiguredTarget()
    {
        using var context = new RuntimeTestContext();
        context.AddEnabledAssignment();

        context.Coordinator.HandleForegroundWindowChanged(
            context.Target);

        Assert.AreEqual(1, context.Overlay.ActivationCount);
        Assert.AreEqual(
            ApplicationRunState.AutomaticActive,
            context.State.Current.Kind);
        Assert.AreEqual(
            VisualProfile.DefaultSoftInvertId,
            context.State.Current.VisualProfileId);
    }

    [TestMethod]
    public void ProfileToggleWithSettingsEventActivatesOverlayOnce()
    {
        using var context = new RuntimeTestContext();
        context.AddDisabledAssignment();
        context.WireSettingsChanged();

        context.Coordinator.ToggleActiveApplicationProfile();

        Assert.AreEqual(1, context.Overlay.ActivationCount);
        Assert.IsTrue(context.Overlay.IsActive);
        Assert.IsTrue(
            context.Settings.Current.AutomaticMode);
        Assert.AreEqual(
            ApplicationRunState.AutomaticActive,
            context.State.Current.Kind);
    }

    [TestMethod]
    public void EnablingAutomaticModeWithSettingsEventActivatesOverlayOnce()
    {
        using var context = new RuntimeTestContext();
        context.AddEnabledAssignment();
        context.DisableAutomaticMode();
        context.WireSettingsChanged();

        context.Coordinator.SetAutomaticMode(enabled: true);

        Assert.AreEqual(1, context.Overlay.ActivationCount);
        Assert.IsTrue(context.Overlay.IsActive);
        Assert.IsTrue(
            context.Settings.Current.AutomaticMode);
        Assert.AreEqual(
            ApplicationRunState.AutomaticActive,
            context.State.Current.Kind);
    }

    [TestMethod]
    public void EmergencyDisablesOverlayBeforePersistingAutomaticMode()
    {
        using var context = new RuntimeTestContext();
        var order = new List<string>();
        context.Settings.Changed += (_, _) =>
            order.Add("settings-published");
        context.Overlay.BeforeDisable = () =>
        {
            Assert.IsTrue(
                context.Settings.Current.AutomaticMode);
            order.Add("overlay-disabled");
        };
        context.Overlay.Activate(
            context.Target,
            VisualProfile.CreateDefaultSoftInvert(),
            OverlayScope.ClientArea);

        context.Coordinator.EmergencyDisable();

        CollectionAssert.AreEqual(
            new[]
            {
                "overlay-disabled",
                "settings-published",
            },
            order);
        Assert.IsFalse(
            context.Settings.Current.AutomaticMode);
        Assert.AreEqual(
            ApplicationRunState.Emergency,
            context.State.Current.Kind);
        Assert.IsFalse(context.Overlay.IsActive);
    }

    private sealed class RuntimeTestContext : IDisposable
    {
        private readonly string _directory;
        private readonly ApplicationIdentity _identity = new(
            "Reader",
            "reader.exe",
            @"C:\Apps\reader.exe");

        public RuntimeTestContext()
        {
            _directory = Path.Combine(
                Path.GetTempPath(),
                "SightAdapt.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_directory);

            Settings = new SettingsCoordinator(
                new SettingsStore(Path.Combine(
                    _directory,
                    "settings.json")));
            State = new ApplicationStateController();
            Overlay = new FakeRuntimeOverlay();
            Coordinator = new RuntimeCoordinator(
                Settings,
                State,
                Overlay,
                () => Target,
                target => target != nint.Zero,
                target => target == Target
                    ? _identity
                    : null,
                Notifications.Add,
                SynchronizedAutomaticModes.Add);
        }

        public nint Target { get; } = (nint)100;

        public SettingsCoordinator Settings { get; }

        public ApplicationStateController State { get; }

        public FakeRuntimeOverlay Overlay { get; }

        public RuntimeCoordinator Coordinator { get; }

        public List<string> Notifications { get; } = [];

        public List<bool> SynchronizedAutomaticModes { get; } = [];

        public void AddEnabledAssignment()
        {
            var result = Settings.Commit(settings =>
                ApplicationProfileManagementService.AddOrEnable(
                    settings,
                    _identity));
            Assert.IsTrue(result.Succeeded);
        }

        public void AddDisabledAssignment()
        {
            var result = Settings.Commit(settings =>
            {
                var assignment =
                    ApplicationProfileManagementService.AddOrEnable(
                        settings,
                        _identity).Profile;
                ApplicationProfileManagementService.SetEnabled(
                    settings,
                    assignment,
                    enabled: false);
                AutomaticModeManagementService.Disable(settings);
            });
            Assert.IsTrue(result.Succeeded);
        }

        public void DisableAutomaticMode()
        {
            var result = Settings.Commit(settings =>
                AutomaticModeManagementService.Disable(settings));
            Assert.IsTrue(result.Succeeded);
        }

        public void WireSettingsChanged()
        {
            Settings.Changed += (_, _) =>
                Coordinator.HandleSettingsChanged();
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
    }

    private sealed class FakeRuntimeOverlay : IRuntimeOverlay
    {
        public bool IsActive { get; private set; }

        public nint TargetWindow { get; private set; }

        public int ActivationCount { get; private set; }

        public Action? BeforeDisable { get; set; }

        public void Activate(
            nint targetWindow,
            VisualProfile visualProfile,
            OverlayScope overlayScope)
        {
            ArgumentNullException.ThrowIfNull(visualProfile);
            Assert.IsTrue(
                OverlayScopePolicy.IsSupported(overlayScope));
            TargetWindow = targetWindow;
            IsActive = true;
            ActivationCount++;
        }

        public void Disable()
        {
            BeforeDisable?.Invoke();
            IsActive = false;
            TargetWindow = nint.Zero;
        }
    }
}
