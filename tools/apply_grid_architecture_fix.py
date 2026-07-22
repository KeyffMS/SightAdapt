from pathlib import Path
import re
import subprocess

root = Path.cwd()
source = root / "src" / "SightAdapt.Demo"
tests = root / "tests" / "SightAdapt.Tests"

coordinator = '''namespace SightAdapt.Demo;

internal sealed record SettingsCommitResult(
    bool Succeeded,
    string? ErrorMessage)
{
    public static SettingsCommitResult Success() => new(true, null);

    public static SettingsCommitResult Failure(string message) => new(false, message);
}

internal sealed record SettingsCommitResult<T>(
    bool Succeeded,
    T Value,
    string? ErrorMessage)
{
    public static SettingsCommitResult<T> Success(T value) => new(true, value, null);

    public static SettingsCommitResult<T> Failure(string message) =>
        new(false, default!, message);
}

internal sealed class SettingsCoordinator
{
    private readonly SettingsStore _store;

    public SettingsCoordinator(SettingsStore? store = null)
    {
        _store = store ?? new SettingsStore();
        Current = _store.Load();
    }

    public SightAdaptSettings Current { get; }

    public string SettingsPath => _store.SettingsPath;

    public string? LastLoadWarning => _store.LastLoadWarning;

    public bool SettingsWereMigrated => _store.SettingsWereMigrated;

    public event EventHandler? Changed;

    public SettingsCommitResult Commit(Action<SightAdaptSettings> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        var result = Commit<object?>(settings =>
        {
            mutation(settings);
            return null;
        });

        return result.Succeeded
            ? SettingsCommitResult.Success()
            : SettingsCommitResult.Failure(
                result.ErrorMessage ?? "Settings could not be changed.");
    }

    public SettingsCommitResult<T> Commit<T>(
        Func<SightAdaptSettings, T> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        var candidate = Current.CreateWorkingCopy();
        T value;

        try
        {
            value = mutation(candidate);
            _store.Save(candidate);
        }
        catch (Exception exception) when (IsExpectedError(exception))
        {
            return SettingsCommitResult<T>.Failure(FormatError(exception));
        }

        Current.ReplaceWith(candidate);
        Changed?.Invoke(this, EventArgs.Empty);
        return SettingsCommitResult<T>.Success(value);
    }

    public SettingsCommitResult PersistCurrent()
    {
        var candidate = Current.CreateWorkingCopy();

        try
        {
            _store.Save(candidate);
        }
        catch (Exception exception) when (IsExpectedError(exception))
        {
            return SettingsCommitResult.Failure(FormatError(exception));
        }

        Current.ReplaceWith(candidate);
        return SettingsCommitResult.Success();
    }

    private static bool IsExpectedError(Exception exception)
    {
        return exception is ArgumentException or
            InvalidOperationException or
            IOException or
            UnauthorizedAccessException;
    }

    private static string FormatError(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException
            ? $"Settings could not be saved: {exception.Message}"
            : exception.Message;
    }
}
'''
(source / "SettingsCoordinator.cs").write_text(coordinator, encoding="utf-8")

selector_path = source / "VisualProfileComboBoxColumn.cs"
selector = selector_path.read_text(encoding="utf-8")
selector = selector.replace("using System.Diagnostics;\n", "")
selector = selector.replace("    private bool _editCompletionQueued;\n", "")
selector = selector.replace(
    '''        _dropDown.Closed += (_, _) =>
        {
            Invalidate();
            QueueGridEditCompletion();
        };''',
    '''        _dropDown.Closed += (_, _) => Invalidate();''')
selector = selector.replace(
    '''        _options = options.ToArray();
        Font = style.Font ?? AppTheme.CreateUiFont(9.5f);''',
    '''        _options = options.ToArray();
        EditingControlValueChanged = false;
        Font = style.Font ?? AppTheme.CreateUiFont(9.5f);''')
selector = selector.replace(
    '''        SelectOption(_options[nextIndex], notifyGrid: true);
        QueueGridEditCompletion();''',
    '''        SelectOption(_options[nextIndex], notifyGrid: true);''')
selector, count = re.subn(
    r"\n    private void QueueGridEditCompletion\(\)\n    \{.*?\n    \}\n\n    private void SelectByValue",
    "\n    private void SelectByValue",
    selector,
    flags=re.S)
if count != 1:
    raise RuntimeError(f"Expected one QueueGridEditCompletion method, found {count}")
selector = selector.replace(
    '''        EditingControlValueChanged = true;
        if (EditingControlDataGridView?.CurrentCell is { } cell)
        {
            cell.Value = _selected?.Id;
            EditingControlDataGridView.NotifyCurrentCellDirty(true);
        }''',
    '''        EditingControlValueChanged = true;
        EditingControlDataGridView?.NotifyCurrentCellDirty(true);''')
if "cell.Value = _selected?.Id" in selector:
    raise RuntimeError("Direct cell mutation remains in the editing control")
if "QueueGridEditCompletion" in selector:
    raise RuntimeError("Selector completion workaround remains")
selector_path.write_text(selector, encoding="utf-8")

form_path = source / "ConfigurationForm.cs"
form = form_path.read_text(encoding="utf-8")
if "private bool _committingGridValue;" not in form:
    form = form.replace(
        "    private bool _refreshing;\n",
        "    private bool _refreshing;\n    private bool _committingGridValue;\n")

replacement = '''    private void ProfilesGridCellValueChanged(object? sender, DataGridViewCellEventArgs eventArgs)
    {
        if (_refreshing || eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0)
        {
            return;
        }

        var row = _profilesGrid.Rows[eventArgs.RowIndex];
        if (row.Tag is not ApplicationProfile displayedProfile)
        {
            return;
        }

        var executablePath = displayedProfile.ExecutablePath;
        var columnName = _profilesGrid.Columns[eventArgs.ColumnIndex].Name;
        SettingsCommitResult result;

        _committingGridValue = true;
        try
        {
            if (columnName == EnabledColumnName)
            {
                var enabled = row.Cells[eventArgs.ColumnIndex].Value is true;
                result = _settingsCoordinator.Commit(settings =>
                    ApplicationProfileManagementService.SetEnabled(
                        settings,
                        FindAssignment(settings, executablePath),
                        enabled));
            }
            else if (columnName == VisualProfileColumnName &&
                     row.Cells[eventArgs.ColumnIndex].Value is string visualProfileId)
            {
                result = _settingsCoordinator.Commit(settings =>
                    ApplicationProfileManagementService.AssignVisualProfile(
                        settings,
                        FindAssignment(settings, executablePath),
                        visualProfileId));
            }
            else
            {
                return;
            }
        }
        finally
        {
            _committingGridValue = false;
        }

        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            _refreshing = true;
            try
            {
                row.Cells[eventArgs.ColumnIndex].Value =
                    columnName == EnabledColumnName
                        ? displayedProfile.Enabled
                        : displayedProfile.VisualProfileId;
            }
            finally
            {
                _refreshing = false;
            }
            return;
        }

        row.Tag = FindAssignment(Settings, executablePath);
        UpdateSelectedProfileActions();
    }

    private void ProfilesGridDataError'''

form, count = re.subn(
    r"    private void ProfilesGridCellValueChanged\(object\? sender, DataGridViewCellEventArgs eventArgs\)\n    \{.*?\n    \}\n\n    private void ProfilesGridDataError",
    replacement,
    form,
    flags=re.S)
if count != 1:
    raise RuntimeError(f"Expected one ProfilesGridCellValueChanged method, found {count}")

form = form.replace(
    '''    private void SettingsChanged(object? sender, EventArgs eventArgs)
    {
        RefreshProfiles();
    }''',
    '''    private void SettingsChanged(object? sender, EventArgs eventArgs)
    {
        if (_committingGridValue)
        {
            return;
        }

        RefreshProfiles();
    }''')
if form.count("_committingGridValue") != 4:
    raise RuntimeError("Unexpected grid-commit guard shape")
form_path.write_text(form, encoding="utf-8")

for path in [
    source / "VisualProfileEditingLifecycleGuard.cs",
    tests / "SettingsCoordinatorUiDispatchTests.cs",
    tests / "NativeDialogDispatchRegressionTests.cs",
    tests / "VisualProfileEditingControlLifecycleTests.cs",
]:
    if path.exists():
        path.unlink()

architecture_path = tests / "ArchitectureComplianceTests.cs"
architecture = architecture_path.read_text(encoding="utf-8")
marker = "    private static void AssertPatternRestrictedTo(\n"
additions = '''    [TestMethod]
    public void SettingsCoordinatorDoesNotOwnWinFormsLifecycle()
    {
        var source = ReadSource("SettingsCoordinator.cs");
        StringAssert.Contains(source, "Changed?.Invoke(this, EventArgs.Empty);");
        Assert.IsFalse(source.Contains("DataGridView", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("Control control", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("System.Windows.Forms.Timer", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ProfileSelectorUsesEditingControlContract()
    {
        var source = ReadSource("VisualProfileComboBoxColumn.cs");
        StringAssert.Contains(source, "EditingControlValueChanged = true;");
        StringAssert.Contains(source, "NotifyCurrentCellDirty(true);");
        Assert.IsFalse(source.Contains("cell.Value = _selected?.Id", StringComparison.Ordinal));
        Assert.IsFalse(source.Contains("QueueGridEditCompletion", StringComparison.Ordinal));
    }

    [TestMethod]
    public void ConfigurationFormOwnsItsGridRefreshBoundary()
    {
        var source = ReadSource("ConfigurationForm.cs");
        StringAssert.Contains(source, "private bool _committingGridValue;");
        StringAssert.Contains(source, "if (_committingGridValue)");
        StringAssert.Contains(source, "row.Tag = FindAssignment(Settings, executablePath);");
    }

'''
if additions not in architecture:
    if marker not in architecture:
        raise RuntimeError("Architecture test insertion marker not found")
    architecture = architecture.replace(marker, additions + marker)
architecture_path.write_text(architecture, encoding="utf-8")

# Remove the temporary delivery mechanism from the resulting source commit.
for path in [
    root / ".github" / "workflows" / "apply-grid-architecture-fix.yml",
    root / "tools" / "apply_grid_architecture_fix.py",
]:
    if path.exists():
        path.unlink()

original_build = subprocess.check_output(
    ["git", "show", "HEAD^:.github/workflows/build.yml"],
    text=True)
(root / ".github" / "workflows" / "build.yml").write_text(
    original_build,
    encoding="utf-8")
