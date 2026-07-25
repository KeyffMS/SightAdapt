using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class RuntimeMenuProfileTests
{
    [TestMethod]
    public void ExplicitMenuProfileFlowsToRuntimeOverlay()
    {
        using var context = new RuntimeTestContext();
        var expected = context.AddAssignment(
            explicitMenuProfile: true);

        context.Coordinator
            .HandleForegroundWindowChanged(
                context.Target);

        Assert.AreEqual(
            expected.PrimaryProfileId,
            context.Overlay.PrimaryVisualProfileId);
        Assert.AreEqual(
            expected.MenuProfileId,
            context.Overlay.MenuVisualProfileId);
    }

    [TestMethod]
    public void UnsetMenuProfileFlowsAsPrimaryProfile()
    {
        using var context = new RuntimeTestContext();
        var expected = context.AddAssignment(
            explicitMenuProfile: false);

        context.Coordinator
            .HandleForegroundWindowChanged(
                context.Target);

        Assert.AreEqual(
            expected.PrimaryProfileId,
            context.Overlay.PrimaryVisualProfileId);
        Assert.AreEqual(
            expected.PrimaryProfileId,
            context.Overlay.MenuVisualProfileId);
    }

    private sealed class RuntimeTestContext : IDisposable
    {
        private readonly string _directory;
        private readonly ApplicationIdentity _identity = new(
            "Reader",
            "reader.exe",
            "C:\\Apps\\reader.exe");

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
            Overlay = new FakeRuntimeOverlay();
            Coordinator = new RuntimeCoordinator(
                Settings,
                new ApplicationStateController(),
                Overlay,
                () => Target,
                target => target == Target,
                target => target == Target
                    ? _identity
                    : null,
                _ => { },
                _ => { });
        }

        public nint Target { get; } = (nint)100;

        public SettingsCoordinator Settings { get; }

        public FakeRuntimeOverlay Overlay { get; }

        public RuntimeCoordinator Coordinator { get; }

        public (
            string PrimaryProfileId,
            string? MenuProfileId) AddAssignment(
                bool explicitMenuProfile)
        {
            var result = Settings.Commit(settings =>
            {
                var assignment =
                    ApplicationProfileManagementService
                        .AddOrEnable(
                            settings,
                            _identity)
                        .Profile;
                var primary =
                    VisualProfileManagementService.Create(
                        settings,
                        "Reader primary");
                ApplicationProfileManagementService
                    .AssignVisualProfile(
                        settings,
                        assignment,
                        primary.Id);

                VisualProfile? menu = null;
                if (explicitMenuProfile)
                {
                    menu = VisualProfileManagementService
                        .Create(
                            settings,
                            "Reader menus");
                    ApplicationProfileManagementService
                        .AssignMenuVisualProfile(
                            settings,
                            assignment,
                            menu.Id);
                }

                return (
                    primary.Id,
                    menu?.Id);
            });

            Assert.IsTrue(result.Succeeded);
            return result.Value;
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, true);
            }
        }
    }

    private sealed class FakeRuntimeOverlay :
        IRuntimeOverlay
    {
        public bool IsActive { get; private set; }

        public nint TargetWindow { get; private set; }

        public string? PrimaryVisualProfileId
        {
            get;
            private set;
        }

        public string? MenuVisualProfileId
        {
            get;
            private set;
        }

        public void Activate(
            nint targetWindow,
            VisualProfile visualProfile,
            OverlayScope overlayScope)
        {
            Activate(
                targetWindow,
                visualProfile,
                visualProfile,
                overlayScope);
        }

        public void Activate(
            nint targetWindow,
            VisualProfile visualProfile,
            VisualProfile menuVisualProfile,
            OverlayScope overlayScope)
        {
            ArgumentNullException.ThrowIfNull(
                visualProfile);
            ArgumentNullException.ThrowIfNull(
                menuVisualProfile);
            Assert.IsTrue(
                OverlayScopePolicy.IsSupported(
                    overlayScope));

            TargetWindow = targetWindow;
            PrimaryVisualProfileId =
                visualProfile.Id;
            MenuVisualProfileId =
                menuVisualProfile.Id;
            IsActive = true;
        }

        public void Disable()
        {
            IsActive = false;
            TargetWindow = nint.Zero;
        }
    }
}
