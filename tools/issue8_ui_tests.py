from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "SightAdapt.Demo"
TESTS = ROOT / "tests" / "SightAdapt.Tests"
DOCS = ROOT / "docs"


def read(path: Path) -> str:
    return path.read_text(encoding="utf-8")


def write(path: Path, content: str) -> None:
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_once(path: Path, old: str, new: str) -> None:
    text = read(path)
    if old not in text:
        raise RuntimeError(f"Missing expected text in {path}: {old[:120]!r}")
    write(path, text.replace(old, new, 1))


# Generalize the existing modern selector once; both profile and scope columns use it.
renames = {
    "VisualProfileOption": "ModernSelectorOption",
    "StableVisualProfileComboBoxColumn": "StableModernSelectorComboBoxColumn",
    "ModernVisualProfileComboBoxCell": "ModernSelectorComboBoxCell",
    "ModernVisualProfileEditingControl": "ModernSelectorEditingControl",
}
for path in list(SRC.glob("*.cs")) + list(TESTS.glob("*.cs")):
    text = read(path)
    for old, new in renames.items():
        text = text.replace(old, new)
    text = text.replace(
        'ReadSource("VisualProfileComboBoxColumn.cs")',
        'ReadSource("ModernSelectorComboBoxColumn.cs")')
    write(path, text)

selector_old = SRC / "VisualProfileComboBoxColumn.cs"
selector_new = SRC / "ModernSelectorComboBoxColumn.cs"
if selector_new.exists():
    selector_new.unlink()
selector_old.rename(selector_new)

text = read(selector_new)
pattern = r"    public void SetProfiles\(\n        IEnumerable<VisualProfile> profiles\)\n    \{.*?\n    \}\n\n    public override object Clone\(\)"
replacement = '''    public void SetProfiles(
        IEnumerable<VisualProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(profiles);

        SetOptions(profiles
            .Where(profile => profile is not null)
            .Select(profile =>
                new ModernSelectorOption(
                    profile.Id,
                    profile.Name)));
    }

    public void SetOptions(
        IEnumerable<ModernSelectorOption> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var nextOptions = options.ToArray();
        if (_options.SequenceEqual(nextOptions))
        {
            return;
        }

        _options = nextOptions;
        Items.Clear();
        Items.AddRange(_options.Cast<object>().ToArray());
    }

    public override object Clone()'''
text, count = re.subn(pattern, replacement, text, count=1, flags=re.S)
if count != 1:
    raise RuntimeError(f"Expected one selector SetProfiles method, found {count}")
text = text.replace(
    '''        editingControl.Configure(
            options,
            Value?.ToString(),
            dataGridViewCellStyle);''',
    '''        editingControl.Configure(
            options,
            Value?.ToString(),
            dataGridViewCellStyle,
            OwningColumn?.HeaderText ?? "Selection");''',
    1)
text = text.replace(
    '''    public void Configure(
        IEnumerable<ModernSelectorOption> options,
        string? selectedId,
        DataGridViewCellStyle style)''',
    '''    public void Configure(
        IEnumerable<ModernSelectorOption> options,
        string? selectedId,
        DataGridViewCellStyle style,
        string accessibleName)''',
    1)
text = text.replace(
    '        AccessibleName = "Visual profile";',
    '        AccessibleName = accessibleName;',
    1)
text = text.replace("Selected profile:", "Selected option:")
write(selector_new, text)

# Add the per-application overlay-scope column to the grid presentation authority.
grid_path = SRC / "ApplicationProfilesGrid.cs"
replace_once(
    grid_path,
    '''internal enum ApplicationProfileGridColumn
{
    Enabled,
    VisualProfile,
}''',
    '''internal enum ApplicationProfileGridColumn
{
    Enabled,
    VisualProfile,
    OverlayScope,
}''')
replace_once(
    grid_path,
    '''    private const string VisualProfileColumnName = "VisualProfile";
    private const string ExecutableColumnName = "Executable";''',
    '''    private const string VisualProfileColumnName = "VisualProfile";
    private const string OverlayScopeColumnName = "OverlayScope";
    private const string ExecutableColumnName = "Executable";''')
replace_once(
    grid_path,
    '''            SetVisualProfiles(visualProfiles);
            _grid.Rows.Clear();''',
    '''            SetVisualProfiles(visualProfiles);
            SetOverlayScopes();
            _grid.Rows.Clear();''')
replace_once(
    grid_path,
    '''            row.Cells[VisualProfileColumnName].Value = application.VisualProfileId;
            row.Cells[ExecutableColumnName].Value = application.ExecutableName;''',
    '''            row.Cells[VisualProfileColumnName].Value = application.VisualProfileId;
            row.Cells[OverlayScopeColumnName].Value =
                OverlayScopePolicy.ToId(application.OverlayScope);
            row.Cells[ExecutableColumnName].Value = application.ExecutableName;''')
replace_once(
    grid_path,
    '''        grid.Columns.Add(new StableModernSelectorComboBoxColumn
        {
            Name = VisualProfileColumnName,
            HeaderText = "VISUAL PROFILE",
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            FlatStyle = FlatStyle.Flat,
            Width = 185,
            MinimumWidth = 160,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(CreateTextColumn(''',
    '''        grid.Columns.Add(new StableModernSelectorComboBoxColumn
        {
            Name = VisualProfileColumnName,
            HeaderText = "VISUAL PROFILE",
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            FlatStyle = FlatStyle.Flat,
            Width = 185,
            MinimumWidth = 160,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new StableModernSelectorComboBoxColumn
        {
            Name = OverlayScopeColumnName,
            HeaderText = "OVERLAY SCOPE",
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            FlatStyle = FlatStyle.Flat,
            Width = 170,
            MinimumWidth = 150,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(CreateTextColumn(''')
replace_once(
    grid_path,
    '''            application.DisplayName,
            application.VisualProfileId,
            application.ExecutableName,''',
    '''            application.DisplayName,
            application.VisualProfileId,
            OverlayScopePolicy.ToId(application.OverlayScope),
            application.ExecutableName,''')
replace_once(
    grid_path,
    '''    private void SetVisualProfiles(IReadOnlyList<VisualProfile> profiles)
    {
        if (_grid.Columns[VisualProfileColumnName] is
            StableModernSelectorComboBoxColumn column)
        {
            column.SetProfiles(profiles);
        }
    }

    private DataGridViewRow? FindRow''',
    '''    private void SetVisualProfiles(IReadOnlyList<VisualProfile> profiles)
    {
        if (_grid.Columns[VisualProfileColumnName] is
            StableModernSelectorComboBoxColumn column)
        {
            column.SetProfiles(profiles);
        }
    }

    private void SetOverlayScopes()
    {
        if (_grid.Columns[OverlayScopeColumnName] is not
            StableModernSelectorComboBoxColumn column)
        {
            return;
        }

        column.SetOptions(OverlayScopePolicy.All.Select(scope =>
            new ModernSelectorOption(
                OverlayScopePolicy.ToId(scope),
                OverlayScopePolicy.GetDisplayName(scope))));
    }

    private DataGridViewRow? FindRow''')
replace_once(
    grid_path,
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
    grid_path,
    '''            ApplicationProfileGridColumn.Enabled => EnabledColumnName,
            ApplicationProfileGridColumn.VisualProfile => VisualProfileColumnName,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),''',
    '''            ApplicationProfileGridColumn.Enabled => EnabledColumnName,
            ApplicationProfileGridColumn.VisualProfile => VisualProfileColumnName,
            ApplicationProfileGridColumn.OverlayScope => OverlayScopeColumnName,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),''')

# ConfigurationForm remains the transaction/use-case authority.
form_path = SRC / "ConfigurationForm.cs"
replace_once(
    form_path,
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
    form_path,
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

# Regression and domain coverage.
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
insert_before = '''    private static SettingsCoordinator CreateCoordinator(string directory)'''
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
if insert_before not in text:
    raise RuntimeError("Configuration test insertion point missing")
write(config_test, text.replace(insert_before, scenario + insert_before, 1))

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

# Enforce authority and version boundaries.
architecture = TESTS / "ArchitectureComplianceTests.cs"
text = read(architecture)
marker = '''    private static void AssertPatternRestrictedTo('''
new_test = '''    [TestMethod]
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
write(architecture, text.replace(marker, new_test + marker, 1))

# Document the accepted requirement and ownership before implementation review.
write(DOCS / "OVERLAY_SCOPE_0.4B.1.md", '''# Overlay scope per application — 0.4B.1

## Requirement

Each configured application owns one persisted overlay scope:

1. `client-area` — client content only, excluding title bar and frame; default;
2. `window` — full visible window including frame and title bar;
3. `screen` — full monitor containing the target window;
4. `all-screens` — complete Windows virtual desktop.

## Authority and source of truth

- `ApplicationProfile.OverlayScopeId` is the persisted source of truth.
- `ApplicationProfileManagementService.SetOverlayScope` is the mutation authority.
- `ApplicationProfilesGrid` owns the selector presentation and typed edit event.
- `ConfigurationForm` translates edits into committed domain operations.
- `OverlayBoundsResolver` is the only geometry authority.
- `OverlayController` owns the active overlay resource and selected scope.

## Runtime behavior

The scope follows the application assignment in both automatic and manual activation.
Changing the scope while the application's overlay is active updates the current overlay without creating a second settings authority.
The overlay remains input-transparent and is hidden when the target application is minimized, invisible, or no longer foreground.

## Migration

Settings schema version 4 adds `overlayScope` to each application assignment.
Existing assignments and invalid values deterministically migrate to `client-area`.

## Build identity

This increment is displayed as `Alpha 0.4B.1 · Overlay scope per app` and uses product version `0.4.0-alpha.5+<commit>`.
''')
