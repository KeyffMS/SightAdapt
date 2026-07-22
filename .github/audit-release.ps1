Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

function Write-ExistingFile(
    [string]$Path,
    [string]$ExpectedMarker,
    [string]$Content) {
    if (-not (Test-Path $Path)) {
        throw "File '$Path' does not exist."
    }

    $current = Normalize-Newlines (Get-Content -Raw $Path)
    if (-not $current.Contains((Normalize-Newlines $ExpectedMarker))) {
        throw "Expected marker was not found in '$Path'."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

function Write-NewFile([string]$Path, [string]$Content) {
    if (Test-Path $Path) {
        throw "File '$Path' already exists."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

Replace-Exact 'src/SightAdapt/OverlayController.cs' `
    'internal sealed class OverlayController : IDisposable' `
    'internal sealed class OverlayController : IRuntimeOverlay, IDisposable'

Write-NewFile 'src/SightAdapt/RuntimeCoordinator.cs' @'
namespace SightAdapt;

internal sealed record ApplicationProfileToggleNotification(
    string DisplayName,
    bool WasCreated,
    bool IsEnabled);

internal enum RuntimeActivationMode
{
    Manual,
    Automatic,
}

internal interface IRuntimeOverlay
{
    bool IsActive { get; }

    nint TargetWindow { get; }

    void Activate(
        nint targetWindow,
        VisualProfile visualProfile,
        OverlayScope overlayScope);

    void Disable();
}

internal sealed class RuntimeCoordinator
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly ApplicationStateController _stateController;
    private readonly IRuntimeOverlay _overlay;
    private readonly Func<nint> _resolveTargetWindow;
    private readonly Func<nint, bool> _isSupportedTarget;
    private readonly Func<nint, ApplicationIdentity?> _resolveIdentity;
    private readonly Action<string> _showNotification;
    private readonly Action<bool> _synchronizeAutomaticMode;

    public RuntimeCoordinator(
        SettingsCoordinator settingsCoordinator,
        ApplicationStateController stateController,
        IRuntimeOverlay overlay,
        Func<nint> resolveTargetWindow,
        Func<nint, bool> isSupportedTarget,
        Func<nint, ApplicationIdentity?> resolveIdentity,
        Action<string> showNotification,
        Action<bool> synchronizeAutomaticMode)
    {
        _settingsCoordinator = settingsCoordinator ??
            throw new ArgumentNullException(nameof(settingsCoordinator));
        _stateController = stateController ??
            throw new ArgumentNullException(nameof(stateController));
        _overlay = overlay ??
            throw new ArgumentNullException(nameof(overlay));
        _resolveTargetWindow = resolveTargetWindow ??
            throw new ArgumentNullException(nameof(resolveTargetWindow));
        _isSupportedTarget = isSupportedTarget ??
            throw new ArgumentNullException(nameof(isSupportedTarget));
        _resolveIdentity = resolveIdentity ??
            throw new ArgumentNullException(nameof(resolveIdentity));
        _showNotification = showNotification ??
            throw new ArgumentNullException(nameof(showNotification));
        _synchronizeAutomaticMode = synchronizeAutomaticMode ??
            throw new ArgumentNullException(nameof(synchronizeAutomaticMode));
    }

    private SightAdaptSettings Settings =>
        _settingsCoordinator.Current;

    public void ToggleForActiveWindow()
    {
        var target = _resolveTargetWindow();
        if (target == nint.Zero)
        {
            _showNotification(
                "No supported application window is currently available.");
            return;
        }

        if (_overlay.IsActive &&
            _overlay.TargetWindow == target)
        {
            DisableOverlay();

            if (Settings.AutomaticMode &&
                IsConfiguredApplication(target))
            {
                _stateController.SuppressAutomaticFor(target);
            }

            return;
        }

        var identity = _resolveIdentity(target);
        var assignment = identity is null
            ? null
            : ProfileResolver.FindAssignment(
                Settings,
                identity);

        _stateController.ClearAutomaticSuppression();
        ActivateOverlay(
            target,
            RuntimeActivationMode.Manual,
            assignment);
    }

    public void ToggleActiveApplicationProfile()
    {
        var target = _resolveTargetWindow();
        var identity = target == nint.Zero
            ? null
            : _resolveIdentity(target);
        if (identity is null)
        {
            _showNotification(
                "The active application's executable path could not be read. " +
                "Use the configuration panel to select its .exe file.");
            return;
        }

        var commit = _settingsCoordinator.Commit(settings =>
        {
            var result =
                ApplicationProfileManagementService.Toggle(
                    settings,
                    identity);

            if (result.IsEnabled)
            {
                AutomaticModeManagementService.Enable(settings);
            }

            return new ApplicationProfileToggleNotification(
                identity.DisplayName,
                result.WasCreated,
                result.IsEnabled);
        });

        if (!commit.Succeeded || commit.Value is null)
        {
            ShowCommitError(commit.ErrorMessage);
            return;
        }

        var result = commit.Value;
        if (result.IsEnabled)
        {
            ResumeAutomaticOperation();
        }

        _showNotification(result.IsEnabled
            ? result.WasCreated
                ? $"Soft invert profile added and enabled: " +
                  $"{result.DisplayName}."
                : $"Automatic profile enabled: {result.DisplayName}."
            : $"Automatic profile disabled: {result.DisplayName}.");
    }

    public void SetAutomaticMode(bool enabled)
    {
        var commit = _settingsCoordinator.Commit(settings =>
            AutomaticModeManagementService.Set(settings, enabled));

        if (!commit.Succeeded)
        {
            _synchronizeAutomaticMode(Settings.AutomaticMode);
            ShowCommitError(commit.ErrorMessage);
            return;
        }

        if (enabled)
        {
            ResumeAutomaticOperation();
        }
    }

    public void HandleForegroundWindowChanged(nint candidate)
    {
        _stateController.ObserveForeground(candidate);

        if (_stateController.Current.Kind ==
                ApplicationRunState.ManualActive &&
            _stateController.Current.TargetWindow != candidate)
        {
            DisableOverlay();
        }

        EvaluateAutomaticForWindow(candidate);
    }

    public void HandleSettingsChanged()
    {
        if (_stateController.Current.Kind ==
            ApplicationRunState.ManualActive)
        {
            RefreshManualOverlayFromSettings();
            return;
        }

        if (!Settings.AutomaticMode)
        {
            _stateController.ClearAutomaticSuppression();

            if (_stateController.Current.Kind ==
                ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
            }

            return;
        }

        if (!_stateController.AllowsAutomaticActivation)
        {
            return;
        }

        var target = _resolveTargetWindow();
        if (target != nint.Zero)
        {
            EvaluateAutomaticForWindow(target);
        }
    }

    public void HandleOverlayClosed()
    {
        if (_stateController.Current.HasActiveOverlay)
        {
            _stateController.SetInactive();
        }
    }

    public void EmergencyDisable()
    {
        _overlay.Disable();
        _stateController.SetEmergency(
            "All overlays were disabled.");

        var commit = _settingsCoordinator.Commit(settings =>
            AutomaticModeManagementService.Disable(settings));

        if (commit.Succeeded)
        {
            _showNotification(
                "All overlays were disabled. Automatic mode is off.");
        }
        else
        {
            _synchronizeAutomaticMode(Settings.AutomaticMode);
            _showNotification(
                "All overlays were disabled for this session, but " +
                (commit.ErrorMessage ??
                 "automatic mode could not be saved."));
        }
    }

    public void DisableForExit()
    {
        _overlay.Disable();
        _stateController.SetInactive();
    }

    private void ResumeAutomaticOperation()
    {
        if (_stateController.Current.Kind is
            ApplicationRunState.Emergency or
            ApplicationRunState.Fault)
        {
            _stateController.SetInactive();
        }

        _stateController.ClearAutomaticSuppression();

        var target = _resolveTargetWindow();
        if (target != nint.Zero)
        {
            EvaluateAutomaticForWindow(target);
        }
    }

    private void ActivateOverlay(
        nint target,
        RuntimeActivationMode activationMode,
        ApplicationProfile? assignment = null)
    {
        try
        {
            var visualProfile =
                ProfileResolver.ResolveVisualProfile(
                    Settings,
                    assignment);
            _overlay.Activate(
                target,
                visualProfile,
                assignment?.OverlayScope ??
                    OverlayScopePolicy.Default);

            switch (activationMode)
            {
                case RuntimeActivationMode.Manual:
                    _stateController.SetManualActive(
                        target,
                        visualProfile.Id);
                    break;
                case RuntimeActivationMode.Automatic:
                    _stateController.SetAutomaticActive(
                        target,
                        visualProfile.Id);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(activationMode));
            }
        }
        catch (Exception exception)
        {
            _overlay.Disable();

            var message =
                $"Could not create the overlay: {exception.Message}";
            _stateController.SetFault(
                message,
                activationMode == RuntimeActivationMode.Automatic
                    ? target
                    : nint.Zero);
            _showNotification(message);
        }
    }

    private void DisableOverlay()
    {
        _overlay.Disable();
        _stateController.SetInactive();
    }

    private void EvaluateAutomaticForWindow(nint target)
    {
        var currentState = _stateController.Current.Kind;
        if (!Settings.AutomaticMode ||
            !_stateController.AllowsAutomaticActivation ||
            currentState == ApplicationRunState.ManualActive ||
            !_isSupportedTarget(target))
        {
            return;
        }

        if (_stateController.IsAutomaticSuppressedFor(target))
        {
            if (currentState ==
                ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
                _stateController.SuppressAutomaticFor(target);
            }

            return;
        }

        var identity = _resolveIdentity(target);
        if (identity is null)
        {
            if (currentState ==
                ApplicationRunState.AutomaticActive)
            {
                DisableOverlay();
            }

            return;
        }

        var assignment =
            ProfileResolver.FindEnabledAssignment(
                Settings,
                identity);

        if (assignment is not null)
        {
            ActivateOverlay(
                target,
                RuntimeActivationMode.Automatic,
                assignment);
        }
        else if (currentState ==
                 ApplicationRunState.AutomaticActive)
        {
            DisableOverlay();
        }
    }

    private bool IsConfiguredApplication(nint target)
    {
        var identity = _resolveIdentity(target);
        return identity is not null &&
            ProfileResolver.FindEnabledAssignment(
                Settings,
                identity) is not null;
    }

    private void RefreshManualOverlayFromSettings()
    {
        var target = _stateController.Current.TargetWindow;
        if (target == nint.Zero ||
            !_isSupportedTarget(target))
        {
            DisableOverlay();
            return;
        }

        var identity = _resolveIdentity(target);
        var assignment = identity is null
            ? null
            : ProfileResolver.FindAssignment(
                Settings,
                identity);

        ActivateOverlay(
            target,
            RuntimeActivationMode.Manual,
            assignment);
    }

    private void ShowCommitError(string? message)
    {
        _showNotification(
            string.IsNullOrWhiteSpace(message)
                ? "Settings could not be changed."
                : message);
    }
}
'@

Write-ExistingFile 'src/SightAdapt/SightAdaptContext.cs' 'private void EvaluateAutomaticForWindow(nint target)' @'
namespace SightAdapt;

internal sealed class SightAdaptContext : ApplicationContext
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly ApplicationStateController _stateController;
    private readonly OverlayController _overlayController;
    private readonly ForegroundWindowTracker _foregroundTracker;
    private readonly TrayPresenter _tray;
    private readonly RuntimeCoordinator _runtimeCoordinator;
    private readonly HotkeyWindow _hotkeys;
    private readonly System.Windows.Forms.Timer _faultStateTimer;

    private ConfigurationForm? _configurationForm;
    private bool _disposed;

    public SightAdaptContext()
    {
        _settingsCoordinator = new SettingsCoordinator();
        _stateController = new ApplicationStateController();
        _overlayController = new OverlayController(
            VisualTransformCatalog.Default);
        _foregroundTracker = new ForegroundWindowTracker();
        _faultStateTimer = new System.Windows.Forms.Timer
        {
            Interval = 5000,
        };

        _tray = new TrayPresenter(
            _settingsCoordinator.Current.AutomaticMode,
            ToggleForActiveWindow,
            ToggleActiveApplicationProfile,
            SetAutomaticMode,
            ShowConfiguration,
            EmergencyDisable,
            ExitThread);

        _runtimeCoordinator = new RuntimeCoordinator(
            _settingsCoordinator,
            _stateController,
            _overlayController,
            _foregroundTracker.ResolveTargetWindow,
            ForegroundWindowTracker.IsSupportedTarget,
            ResolveApplicationIdentity,
            _tray.ShowNotification,
            _tray.SetAutomaticMode);
        _hotkeys = new HotkeyWindow(HandleHotkey);

        _settingsCoordinator.Changed += SettingsChanged;
        _stateController.Changed += ApplicationStateChanged;
        _overlayController.OverlayClosed += OverlayControllerClosed;
        _foregroundTracker.Changed += ForegroundWindowChanged;
        _faultStateTimer.Tick += FaultStateTimerTick;

        ApplyApplicationState(_stateController.Current);
        _foregroundTracker.Start();
        _tray.ShowStartup(
            _hotkeys.LocalToggleShortcut,
            _hotkeys.ProfileToggleShortcut);

        if (_settingsCoordinator.SettingsWereMigrated)
        {
            TrySaveMigratedSettings();
        }

        if (!string.IsNullOrWhiteSpace(
                _settingsCoordinator.LastLoadWarning))
        {
            _tray.ShowNotification(
                _settingsCoordinator.LastLoadWarning);
        }
    }

    private SightAdaptSettings Settings =>
        _settingsCoordinator.Current;

    protected override void ExitThreadCore()
    {
        _configurationForm?.Close();
        _runtimeCoordinator.DisableForExit();
        base.ExitThreadCore();
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _settingsCoordinator.Changed -= SettingsChanged;
            _stateController.Changed -= ApplicationStateChanged;
            _overlayController.OverlayClosed -= OverlayControllerClosed;
            _foregroundTracker.Changed -= ForegroundWindowChanged;
            _faultStateTimer.Tick -= FaultStateTimerTick;

            _configurationForm?.Dispose();
            _foregroundTracker.Dispose();
            _faultStateTimer.Stop();
            _faultStateTimer.Dispose();
            _hotkeys.Dispose();
            _overlayController.Dispose();
            _tray.Dispose();
            _disposed = true;
        }

        base.Dispose(disposing);
    }

    private void HandleHotkey(int id)
    {
        if (id == HotkeyWindow.LocalToggleId)
        {
            ToggleForActiveWindow();
        }
        else if (id == HotkeyWindow.ProfileToggleId)
        {
            ToggleActiveApplicationProfile();
        }
    }

    private void ToggleForActiveWindow()
    {
        _runtimeCoordinator.ToggleForActiveWindow();
    }

    private void ToggleActiveApplicationProfile()
    {
        _runtimeCoordinator.ToggleActiveApplicationProfile();
    }

    private void SetAutomaticMode(bool enabled)
    {
        _runtimeCoordinator.SetAutomaticMode(enabled);
    }

    private void EmergencyDisable()
    {
        _runtimeCoordinator.EmergencyDisable();
    }

    private void OverlayControllerClosed(
        object? sender,
        EventArgs eventArgs)
    {
        _runtimeCoordinator.HandleOverlayClosed();
    }

    private void ForegroundWindowChanged(
        object? sender,
        ForegroundWindowChangedEventArgs eventArgs)
    {
        _runtimeCoordinator.HandleForegroundWindowChanged(
            eventArgs.Window);
    }

    private void SettingsChanged(
        object? sender,
        EventArgs eventArgs)
    {
        _tray.SetAutomaticMode(Settings.AutomaticMode);
        ApplyApplicationState(_stateController.Current);
        _runtimeCoordinator.HandleSettingsChanged();
    }

    private void ShowConfiguration()
    {
        if (_configurationForm is not null &&
            !_configurationForm.IsDisposed)
        {
            _configurationForm.Show();
            _configurationForm.Activate();
            return;
        }

        var form = new ConfigurationForm(
            _settingsCoordinator,
            _foregroundTracker.GetCurrentApplicationIdentity)
        {
            ShowIcon = true,
            Icon = _tray.GetIcon(
                _stateController.Current.Kind),
        };

        form.FormClosed += (_, _) =>
            _configurationForm = null;
        _configurationForm = form;
        form.Show();
    }

    private void ApplicationStateChanged(
        object? sender,
        ApplicationStateChangedEventArgs eventArgs)
    {
        if (eventArgs.Current.Kind ==
            ApplicationRunState.Fault)
        {
            _faultStateTimer.Stop();
            _faultStateTimer.Start();
        }
        else
        {
            _faultStateTimer.Stop();
        }

        ApplyApplicationState(eventArgs.Current);
    }

    private void ApplyApplicationState(
        ApplicationState state)
    {
        var title = state.TargetWindow == nint.Zero
            ? null
            : NativeMethods.GetWindowTitle(
                state.TargetWindow);
        var profileName = ResolveProfileName(
            state.VisualProfileId);

        _tray.ApplyState(
            state,
            Settings.AutomaticMode,
            title,
            profileName);

        if (_configurationForm is not null &&
            !_configurationForm.IsDisposed)
        {
            _configurationForm.Icon =
                _tray.GetIcon(state.Kind);
        }
    }

    private string ResolveProfileName(
        string? profileId)
    {
        return Settings.VisualProfiles
            .FirstOrDefault(profile => string.Equals(
                profile.Id,
                profileId,
                StringComparison.OrdinalIgnoreCase))
            ?.Name ?? "Visual correction";
    }

    private void FaultStateTimerTick(
        object? sender,
        EventArgs eventArgs)
    {
        _faultStateTimer.Stop();

        if (_stateController.Current.Kind ==
            ApplicationRunState.Fault)
        {
            _stateController.SetInactive();
        }
    }

    private void TrySaveMigratedSettings()
    {
        var result =
            _settingsCoordinator.PersistCurrent();

        _tray.ShowNotification(result.Succeeded
            ? "Settings were upgraded to the current " +
              "color-profile format."
            : result.ErrorMessage ??
              "Migrated settings could not be saved.");
    }

    private static ApplicationIdentity? ResolveApplicationIdentity(
        nint window)
    {
        return ApplicationDiscovery.TryGetIdentity(
                window,
                out var identity)
            ? identity
            : null;
    }
}
'@

Write-NewFile 'tests/SightAdapt.Tests/RuntimeCoordinatorTests.cs' @'
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
'@

Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' @'
                     "VisualProfileManagerForm.cs",
                     "SightAdaptContext.cs",
'@ @'
                     "VisualProfileManagerForm.cs",
                     "SightAdaptContext.cs",
                     "RuntimeCoordinator.cs",
'@

Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' @'
    public void EmergencyStopsOverlayBeforePersistence()
    {
        var source = ReadSource("SightAdaptContext.cs");
        var methodIndex = source.IndexOf(
            "private void EmergencyDisable()",
            StringComparison.Ordinal);
        var disableIndex = source.IndexOf(
            "_overlayController.Disable();",
            methodIndex,
            StringComparison.Ordinal);
        var commitIndex = source.IndexOf(
            "_settingsCoordinator.Commit",
            methodIndex,
            StringComparison.Ordinal);
'@ @'
    public void EmergencyStopsOverlayBeforePersistence()
    {
        var source = ReadSource("RuntimeCoordinator.cs");
        var methodIndex = source.IndexOf(
            "public void EmergencyDisable()",
            StringComparison.Ordinal);
        var disableIndex = source.IndexOf(
            "_overlay.Disable();",
            methodIndex,
            StringComparison.Ordinal);
        var commitIndex = source.IndexOf(
            "_settingsCoordinator.Commit",
            methodIndex,
            StringComparison.Ordinal);
'@

Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' @'
        var context = ReadSource("SightAdaptContext.cs");
        StringAssert.Contains(context, "ForegroundWindowTracker");
        StringAssert.Contains(context, "TrayPresenter");
        Assert.IsFalse(context.Contains("NotifyIcon", StringComparison.Ordinal));
        Assert.IsTrue(SourceExists("ForegroundWindowTracker.cs"));
        Assert.IsTrue(SourceExists("TrayPresenter.cs"));
'@ @'
        var context = ReadSource("SightAdaptContext.cs");
        var runtime = ReadSource("RuntimeCoordinator.cs");
        StringAssert.Contains(context, "ForegroundWindowTracker");
        StringAssert.Contains(context, "TrayPresenter");
        StringAssert.Contains(context, "RuntimeCoordinator");
        Assert.IsFalse(context.Contains("NotifyIcon", StringComparison.Ordinal));
        Assert.IsFalse(context.Contains("EvaluateAutomaticForWindow", StringComparison.Ordinal));
        StringAssert.Contains(runtime, "EvaluateAutomaticForWindow");
        StringAssert.Contains(runtime, "RuntimeActivationMode");
        Assert.IsTrue(SourceExists("ForegroundWindowTracker.cs"));
        Assert.IsTrue(SourceExists("TrayPresenter.cs"));
        Assert.IsTrue(SourceExists("RuntimeCoordinator.cs"));
'@

Replace-Exact 'docs/ARCHITECTURE.md' @'
SightAdaptContext
(use-case orchestration)
      ↓
OverlayController
'@ @'
SightAdaptContext
(lifecycle and composition)
      ↓
RuntimeCoordinator
(use-case orchestration)
      ↓
OverlayController
'@

Replace-Exact 'docs/ARCHITECTURE.md' `
    '| Settings transaction | `SettingsCoordinator` |' `
    '| Settings transaction | `SettingsCoordinator` |`n| Runtime use-case orchestration | `RuntimeCoordinator` |'
