from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "SightAdapt.Demo"
TESTS = ROOT / "tests" / "SightAdapt.Tests"
DOCS = ROOT / "docs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8", newline="\n")


def replace_once(path: Path, old: str, new: str) -> None:
    text = read(path)
    if old not in text:
        raise RuntimeError(f"Missing expected text in {path}: {old[:120]!r}")
    write(path, text.replace(old, new, 1))


# Finish the typed scope event and cell restoration mapping.
grid = SRC / "ApplicationProfilesGrid.cs"
replace_once(
    grid,
    '''        else if (columnName == VisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string profileId)
        {
            ValueChanged?.Invoke(
                this,
                new ApplicationProfileGridValueChangedEventArgs(
                    executablePath,
                    ApplicationProfileGridColumn.VisualProfile,
                    profileId));
        }
    }''',
    '''        else if (columnName == VisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string profileId)
        {
            ValueChanged?.Invoke(
                this,
                new ApplicationProfileGridValueChangedEventArgs(
                    executablePath,
                    ApplicationProfileGridColumn.VisualProfile,
                    profileId));
        }
        else if (columnName == OverlayScopeColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string scopeId)
        {
            ValueChanged?.Invoke(
                this,
                new ApplicationProfileGridValueChangedEventArgs(
                    executablePath,
                    ApplicationProfileGridColumn.OverlayScope,
                    scopeId));
        }
    }''')
replace_once(
    grid,
    '''            ApplicationProfileGridColumn.Enabled => EnabledColumnName,
            ApplicationProfileGridColumn.VisualProfile => VisualProfileColumnName,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),''',
    '''            ApplicationProfileGridColumn.Enabled => EnabledColumnName,
            ApplicationProfileGridColumn.VisualProfile => VisualProfileColumnName,
            ApplicationProfileGridColumn.OverlayScope => OverlayScopeColumnName,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),''')

# Commit scope edits through the existing application-assignment authority.
form = SRC / "ConfigurationForm.cs"
replace_once(
    form,
    '''                ApplicationProfileGridColumn.VisualProfile
                    when eventArgs.Value is string visualProfileId =>
                    _settingsCoordinator.Commit(settings =>
                        ApplicationProfileManagementService.AssignVisualProfile(
                            settings,
                            FindAssignment(settings, eventArgs.ExecutablePath),
                            visualProfileId)),
                _ => SettingsCommitResult.Failure(''',
    '''                ApplicationProfileGridColumn.VisualProfile
                    when eventArgs.Value is string visualProfileId =>
                    _settingsCoordinator.Commit(settings =>
                        ApplicationProfileManagementService.AssignVisualProfile(
                            settings,
                            FindAssignment(settings, eventArgs.ExecutablePath),
                            visualProfileId)),
                ApplicationProfileGridColumn.OverlayScope
                    when eventArgs.Value is string overlayScopeId =>
                    _settingsCoordinator.Commit(settings =>
                        ApplicationProfileManagementService.SetOverlayScope(
                            settings,
                            FindAssignment(settings, eventArgs.ExecutablePath),
                            OverlayScopePolicy.ParseRequired(overlayScopeId))),
                _ => SettingsCommitResult.Failure(''')
replace_once(
    form,
    '''                eventArgs.Column == ApplicationProfileGridColumn.Enabled
                    ? displayedProfile.Enabled
                    : displayedProfile.VisualProfileId);''',
    '''                eventArgs.Column switch
                {
                    ApplicationProfileGridColumn.Enabled =>
                        displayedProfile.Enabled,
                    ApplicationProfileGridColumn.VisualProfile =>
                        displayedProfile.VisualProfileId,
                    ApplicationProfileGridColumn.OverlayScope =>
                        displayedProfile.OverlayScopeId,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(eventArgs.Column)),
                });''')

# Add one integration regression to prove the setting is per application.
config_test = TESTS / "ConfigurationGridCommitRegressionTests.cs"
replace_once(
    config_test,
    '''    [TestMethod]
    public void ExternalSettingsChangeRebindsGridFromCurrentSettings()
    {
        RunOnSta(RunExternalChangeScenario);
    }

    private static void RunOnSta''',
    '''    [TestMethod]
    public void ExternalSettingsChangeRebindsGridFromCurrentSettings()
    {
        RunOnSta(RunExternalChangeScenario);
    }

    [TestMethod]
    public void OverlayScopeSelectionCommitsOnlySelectedApplication()
    {
        RunOnSta(RunOverlayScopeSelectionScenario);
    }

    private static void RunOnSta''')
insert = '''    private static SettingsCoordinator CreateCoordinator(string directory)'''
scenario = '''    private static void RunOverlayScopeSelectionScenario()
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

'''
text = read(config_test)
if insert not in text:
    raise RuntimeError("Configuration test insertion point missing")
write(config_test, text.replace(insert, scenario + insert, 1))

write(TESTS / "OverlayScopeTests.cs", '''using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class OverlayScopeTests
{
    [TestMethod]
    public void NewApplicationDefaultsToClientArea()
    {
        var settings = new SightAdaptSettings();
        var result = ApplicationProfileManagementService.AddOrEnable(
            settings,
            new ApplicationIdentity(
                "Reader",
                "reader.exe",
                @"C:\Apps\reader.exe"));

        Assert.IsTrue(result.WasCreated);
        Assert.AreEqual(OverlayScope.ClientArea, result.Profile.OverlayScope);
        Assert.AreEqual("client-area", result.Profile.OverlayScopeId);
    }

    [TestMethod]
    public void ApplicationScopeMutationUsesAssignmentAuthority()
    {
        var settings = new SightAdaptSettings();
        var profile = ApplicationProfileManagementService.AddOrEnable(
            settings,
            new ApplicationIdentity(
                "Reader",
                "reader.exe",
                @"C:\Apps\reader.exe"))
            .Profile;

        ApplicationProfileManagementService.SetOverlayScope(
            settings,
            profile,
            OverlayScope.AllScreens);

        Assert.AreEqual(OverlayScope.AllScreens, profile.OverlayScope);
        Assert.AreEqual("all-screens", profile.OverlayScopeId);
    }

    [TestMethod]
    public void ScopeIdentifiersRoundTrip()
    {
        foreach (var scope in OverlayScopePolicy.All)
        {
            var id = OverlayScopePolicy.ToId(scope);
            Assert.IsTrue(OverlayScopePolicy.TryParseId(id, out var parsed));
            Assert.AreEqual(scope, parsed);
        }
    }

    [TestMethod]
    public void InvalidPersistedScopeRecoversToDefault()
    {
        var profile = new ApplicationProfile
        {
            OverlayScopeId = "unknown-scope",
        };

        Assert.AreEqual(OverlayScope.ClientArea, profile.OverlayScope);
        Assert.AreEqual("client-area", profile.OverlayScopeId);
    }

    [TestMethod]
    public void WorkingCopyPreservesPerApplicationScope()
    {
        var original = new ApplicationProfile
        {
            OverlayScopeId = "screen",
        };

        var copy = original.CreateWorkingCopy();

        Assert.AreEqual(OverlayScope.Screen, copy.OverlayScope);
        Assert.AreEqual("screen", copy.OverlayScopeId);
    }
}
''')

architecture = TESTS / "ArchitectureComplianceTests.cs"
text = read(architecture)
marker = '''    private static void AssertPatternRestrictedTo('''
if "OverlayScopeHasOneModelAndOneGeometryAuthority" not in text:
    test = '''    [TestMethod]
    public void OverlayScopeHasOneModelAndOneGeometryAuthority()
    {
        var model = ReadSource("ApplicationProfile.cs");
        var grid = ReadSource("ApplicationProfilesGrid.cs");
        var resolver = ReadSource("OverlayBoundsResolver.cs");
        var form = ReadSource("ConfigurationForm.cs");

        StringAssert.Contains(model, "OverlayScopeId");
        StringAssert.Contains(model, "public OverlayScope OverlayScope");
        StringAssert.Contains(grid, "OverlayScopeColumnName");
        StringAssert.Contains(form, "SetOverlayScope");
        StringAssert.Contains(resolver, "OverlayScope.ClientArea");
        StringAssert.Contains(resolver, "OverlayScope.AllScreens");
        Assert.IsFalse(grid.Contains("SettingsCoordinator", StringComparison.Ordinal));
        Assert.IsFalse(resolver.Contains("SettingsCoordinator", StringComparison.Ordinal));
    }

'''
    if marker not in text:
        raise RuntimeError("Architecture test insertion point missing")
    write(architecture, text.replace(marker, test + marker, 1))

write(DOCS / "OVERLAY_SCOPE_0.4B.1.md", '''# Overlay scope per application — 0.4B.1

## Persisted model

Every `ApplicationProfile` stores one normalized `overlayScope` identifier. Existing and invalid values recover to `client-area` during deserialization. Settings schema version 4 records the new model.

## Scope choices

- `client-area` — target client content without frame/title bar; default;
- `window` — full visible target window;
- `screen` — full monitor containing the target;
- `all-screens` — full Windows virtual desktop.

## Ownership

- `ApplicationProfile` — persisted source of truth;
- `ApplicationProfileManagementService` — mutation authority;
- `ApplicationProfilesGrid` — per-row selector and typed edit event;
- `ConfigurationForm` — settings transaction orchestration;
- `OverlayBoundsResolver` — geometry authority;
- `OverlayController` — active overlay resource and scope.

## Runtime

Manual and automatic activation use the assigned application's scope. A settings change updates the active overlay through the existing settings-change path. The overlay remains input-transparent and remains tied to the target application's foreground lifecycle.

## Build identity

The issue-8 build displays `Alpha 0.4B.1 · Overlay scope per app` and reports `0.4.0-alpha.5+<commit>`.
''')
