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

Replace-Exact 'src/SightAdapt/ApplicationProfilesGrid.cs' @'
internal enum ApplicationProfileGridColumn
{
    Enabled,
    VisualProfile,
    OverlayScope,
}

internal sealed class ApplicationProfileGridValueChangedEventArgs : EventArgs
{
    public ApplicationProfileGridValueChangedEventArgs(
        string executablePath,
        ApplicationProfileGridColumn column,
        object value)
    {
        ExecutablePath = executablePath ??
            throw new ArgumentNullException(nameof(executablePath));
        Column = column;
        Value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public string ExecutablePath { get; }

    public ApplicationProfileGridColumn Column { get; }

    public object Value { get; }
}
'@ @'
internal sealed class ApplicationProfileEnabledChangedEventArgs(
    string executablePath,
    bool enabled) : EventArgs
{
    public string ExecutablePath { get; } =
        !string.IsNullOrWhiteSpace(executablePath)
            ? executablePath
            : throw new ArgumentException(
                "An executable path is required.",
                nameof(executablePath));

    public bool Enabled { get; } = enabled;
}

internal sealed class ApplicationProfileVisualProfileChangedEventArgs(
    string executablePath,
    string visualProfileId) : EventArgs
{
    public string ExecutablePath { get; } =
        !string.IsNullOrWhiteSpace(executablePath)
            ? executablePath
            : throw new ArgumentException(
                "An executable path is required.",
                nameof(executablePath));

    public string VisualProfileId { get; } =
        !string.IsNullOrWhiteSpace(visualProfileId)
            ? visualProfileId
            : throw new ArgumentException(
                "A visual profile identifier is required.",
                nameof(visualProfileId));
}

internal sealed class ApplicationProfileOverlayScopeChangedEventArgs(
    string executablePath,
    OverlayScope overlayScope) : EventArgs
{
    public string ExecutablePath { get; } =
        !string.IsNullOrWhiteSpace(executablePath)
            ? executablePath
            : throw new ArgumentException(
                "An executable path is required.",
                nameof(executablePath));

    public OverlayScope OverlayScope { get; } =
        OverlayScopePolicy.IsSupported(overlayScope)
            ? overlayScope
            : throw new ArgumentOutOfRangeException(
                nameof(overlayScope));
}
'@

Replace-Exact 'src/SightAdapt/ApplicationProfilesGrid.cs' @'
    public event EventHandler<ApplicationProfileGridValueChangedEventArgs>? ValueChanged;

    public event EventHandler? SelectedApplicationChanged;
'@ @'
    public event EventHandler<ApplicationProfileEnabledChangedEventArgs>? ApplicationEnabledChanged;

    public event EventHandler<ApplicationProfileVisualProfileChangedEventArgs>? VisualProfileChanged;

    public event EventHandler<ApplicationProfileOverlayScopeChangedEventArgs>? OverlayScopeChanged;

    public event EventHandler? SelectedApplicationChanged;
'@

Replace-Exact 'src/SightAdapt/ApplicationProfilesGrid.cs' @'
    public void RestoreValue(
        string executablePath,
        ApplicationProfileGridColumn column,
        object value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(value);

        var row = FindRow(executablePath);
        if (row is null)
        {
            return;
        }

        _binding = true;
        try
        {
            row.Cells[GetColumnName(column)].Value = value;
        }
        finally
        {
            _binding = false;
        }
    }

'@ ''

Replace-Exact 'src/SightAdapt/ApplicationProfilesGrid.cs' @'
    private void GridCellValueChanged(
        object? sender,
        DataGridViewCellEventArgs eventArgs)
    {
        if (_binding || eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0)
        {
            return;
        }

        var row = _grid.Rows[eventArgs.RowIndex];
        if (row.Tag is not string executablePath)
        {
            return;
        }

        var columnName = _grid.Columns[eventArgs.ColumnIndex].Name;
        if (columnName == EnabledColumnName &&
            row.Cells[eventArgs.ColumnIndex].Value is bool enabled)
        {
            ValueChanged?.Invoke(
                this,
                new ApplicationProfileGridValueChangedEventArgs(
                    executablePath,
                    ApplicationProfileGridColumn.Enabled,
                    enabled));
        }
        else if (columnName == VisualProfileColumnName &&
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
    }
'@ @'
    private void GridCellValueChanged(
        object? sender,
        DataGridViewCellEventArgs eventArgs)
    {
        if (_binding || eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0)
        {
            return;
        }

        var row = _grid.Rows[eventArgs.RowIndex];
        if (row.Tag is not string executablePath)
        {
            return;
        }

        var columnName = _grid.Columns[eventArgs.ColumnIndex].Name;
        if (columnName == EnabledColumnName &&
            row.Cells[eventArgs.ColumnIndex].Value is bool enabled)
        {
            ApplicationEnabledChanged?.Invoke(
                this,
                new ApplicationProfileEnabledChangedEventArgs(
                    executablePath,
                    enabled));
        }
        else if (columnName == VisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string profileId)
        {
            VisualProfileChanged?.Invoke(
                this,
                new ApplicationProfileVisualProfileChangedEventArgs(
                    executablePath,
                    profileId));
        }
        else if (columnName == OverlayScopeColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string scopeId)
        {
            OverlayScopeChanged?.Invoke(
                this,
                new ApplicationProfileOverlayScopeChangedEventArgs(
                    executablePath,
                    OverlayScopePolicy.ParseRequired(scopeId)));
        }
    }
'@

Replace-Exact 'src/SightAdapt/ApplicationProfilesGrid.cs' @'

    private static string GetColumnName(ApplicationProfileGridColumn column)
    {
        return column switch
        {
            ApplicationProfileGridColumn.Enabled => EnabledColumnName,
            ApplicationProfileGridColumn.VisualProfile => VisualProfileColumnName,
            ApplicationProfileGridColumn.OverlayScope => OverlayScopeColumnName,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),
        };
    }
'@ "`n"

Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' @'
        _profilesGrid = new ApplicationProfilesGrid();
        _profilesGrid.ValueChanged += ProfilesGridValueChanged;
        _profilesGrid.SelectedApplicationChanged += (_, _) =>
'@ @'
        _profilesGrid = new ApplicationProfilesGrid();
        _profilesGrid.ApplicationEnabledChanged += ProfilesGridEnabledChanged;
        _profilesGrid.VisualProfileChanged += ProfilesGridVisualProfileChanged;
        _profilesGrid.OverlayScopeChanged += ProfilesGridOverlayScopeChanged;
        _profilesGrid.SelectedApplicationChanged += (_, _) =>
'@

Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' @'
    private void ProfilesGridValueChanged(
        object? sender,
        ApplicationProfileGridValueChangedEventArgs eventArgs)
    {
        var displayedProfile = FindAssignment(
            Settings,
            eventArgs.ExecutablePath);
        SettingsCommitResult result;

        _committingGridValue = true;
        try
        {
            result = eventArgs.Column switch
            {
                ApplicationProfileGridColumn.Enabled
                    when eventArgs.Value is bool enabled =>
                    _settingsCoordinator.Commit(settings =>
                        ApplicationProfileManagementService.SetEnabled(
                            settings,
                            FindAssignment(settings, eventArgs.ExecutablePath),
                            enabled)),
                ApplicationProfileGridColumn.VisualProfile
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
                _ => SettingsCommitResult.Failure(
                    "The edited application-profile value is not supported."),
            };
        }
        finally
        {
            _committingGridValue = false;
        }

        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            _profilesGrid.RestoreValue(
                eventArgs.ExecutablePath,
                eventArgs.Column,
                eventArgs.Column switch
                {
                    ApplicationProfileGridColumn.Enabled =>
                        displayedProfile.Enabled,
                    ApplicationProfileGridColumn.VisualProfile =>
                        displayedProfile.VisualProfileId,
                    ApplicationProfileGridColumn.OverlayScope =>
                        displayedProfile.OverlayScopeId,
                    _ => throw new ArgumentOutOfRangeException(
                        nameof(eventArgs.Column)),
                });
            return;
        }

        _profilesGrid.UpdateApplication(FindAssignment(
            Settings,
            eventArgs.ExecutablePath));
        UpdateSelectedProfileActions();
    }
'@ @'
    private void ProfilesGridEnabledChanged(
        object? sender,
        ApplicationProfileEnabledChangedEventArgs eventArgs)
    {
        CommitGridChange(
            eventArgs.ExecutablePath,
            (settings, profile) =>
                ApplicationProfileManagementService.SetEnabled(
                    settings,
                    profile,
                    eventArgs.Enabled));
    }

    private void ProfilesGridVisualProfileChanged(
        object? sender,
        ApplicationProfileVisualProfileChangedEventArgs eventArgs)
    {
        CommitGridChange(
            eventArgs.ExecutablePath,
            (settings, profile) =>
                ApplicationProfileManagementService.AssignVisualProfile(
                    settings,
                    profile,
                    eventArgs.VisualProfileId));
    }

    private void ProfilesGridOverlayScopeChanged(
        object? sender,
        ApplicationProfileOverlayScopeChangedEventArgs eventArgs)
    {
        CommitGridChange(
            eventArgs.ExecutablePath,
            (settings, profile) =>
                ApplicationProfileManagementService.SetOverlayScope(
                    settings,
                    profile,
                    eventArgs.OverlayScope));
    }

    private void CommitGridChange(
        string executablePath,
        Action<SightAdaptSettings, ApplicationProfile> mutation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(mutation);

        var displayedProfile = FindAssignment(
            Settings,
            executablePath);
        SettingsCommitResult result;

        _committingGridValue = true;
        try
        {
            result = _settingsCoordinator.Commit(settings =>
                mutation(
                    settings,
                    FindAssignment(settings, executablePath)));
        }
        finally
        {
            _committingGridValue = false;
        }

        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            _profilesGrid.UpdateApplication(displayedProfile);
            return;
        }

        _profilesGrid.UpdateApplication(FindAssignment(
            Settings,
            executablePath));
        UpdateSelectedProfileActions();
    }
'@

Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' @'
        StringAssert.Contains(form, "_profilesGrid.UpdateApplication");
        StringAssert.Contains(form, "_profilesGrid.RestoreValue");
        Assert.IsFalse(form.Contains("Rows.Clear()", StringComparison.Ordinal));

        StringAssert.Contains(grid, "_grid.Rows.Clear();");
        StringAssert.Contains(grid, "row.Tag = application.ExecutablePath;");
        StringAssert.Contains(grid, "public string? SelectedExecutablePath");
        Assert.IsFalse(grid.Contains("SettingsCoordinator", StringComparison.Ordinal));
'@ @'
        StringAssert.Contains(form, "_profilesGrid.UpdateApplication");
        Assert.IsFalse(form.Contains("_profilesGrid.RestoreValue", StringComparison.Ordinal));
        Assert.IsFalse(form.Contains("Rows.Clear()", StringComparison.Ordinal));

        StringAssert.Contains(grid, "_grid.Rows.Clear();");
        StringAssert.Contains(grid, "row.Tag = application.ExecutablePath;");
        StringAssert.Contains(grid, "public string? SelectedExecutablePath");
        StringAssert.Contains(grid, "ApplicationProfileEnabledChangedEventArgs");
        StringAssert.Contains(grid, "ApplicationProfileVisualProfileChangedEventArgs");
        StringAssert.Contains(grid, "ApplicationProfileOverlayScopeChangedEventArgs");
        Assert.IsFalse(grid.Contains("object Value", StringComparison.Ordinal));
        Assert.IsFalse(grid.Contains("SettingsCoordinator", StringComparison.Ordinal));
'@

Replace-Exact 'docs/ARCHITECTURE.md' `
    'typed value-change events' `
    'separate typed change events'
