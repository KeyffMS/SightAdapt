namespace SightAdapt;

internal sealed class ApplicationAssignmentsGrid : UserControl
{
    internal const string VisualProfileColumnName =
        ApplicationAssignmentsGridColumns.VisualProfileColumnName;
    internal const string MenuVisualProfileColumnName =
        ApplicationAssignmentsGridColumns.MenuVisualProfileColumnName;
    internal const string OverlayScopeColumnName =
        ApplicationAssignmentsGridColumns.OverlayScopeColumnName;

    private readonly DataGridView _grid;
    private readonly Label _emptyStateLabel;
    private bool _binding;

    public ApplicationAssignmentsGrid()
    {
        BackColor = AppTheme.Surface;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;

        _grid = CreateGrid();
        _emptyStateLabel = CreateEmptyStateLabel();
        Controls.Add(_grid);
        Controls.Add(_emptyStateLabel);
    }

    public event Action<ApplicationAssignmentChange>? AssignmentChanged;

    public event EventHandler? SelectedApplicationChanged;

    public string? SelectedExecutablePath
    {
        get
        {
            var row = _grid.SelectedRows.Count > 0
                ? _grid.SelectedRows[0]
                : _grid.CurrentRow;
            return row?.Tag as string;
        }
    }

    public void Bind(
        IReadOnlyList<ApplicationAssignmentRow> assignments,
        IReadOnlyList<VisualProfile> visualProfiles)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        ArgumentNullException.ThrowIfNull(visualProfiles);

        var selectedPath = SelectedExecutablePath;
        _binding = true;
        try
        {
            ApplicationAssignmentsGridColumns
                .SetSelectorOptions(
                    _grid,
                    visualProfiles);
            _grid.Rows.Clear();

            foreach (var assignment in assignments)
            {
                AddRow(assignment, selectedPath);
            }
        }
        finally
        {
            _binding = false;
        }

        UpdateVisibility(assignments.Count);
        SelectedApplicationChanged?.Invoke(
            this,
            EventArgs.Empty);
    }

    public void UpdateAssignment(
        ApplicationAssignmentRow assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        var row = FindRow(assignment.ExecutablePath);
        if (row is null)
        {
            return;
        }

        _binding = true;
        try
        {
            WriteRow(row, assignment);
        }
        finally
        {
            _binding = false;
        }
    }

    private DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            Dock = DockStyle.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter,
            MultiSelect = false,
            ReadOnly = false,
            SelectionMode =
                DataGridViewSelectionMode.FullRowSelect,
        };
        AppTheme.StyleGrid(grid);
        ApplicationAssignmentsGridColumns.AddTo(grid);

        grid.CellPainting += GridCellPainting;
        grid.CellValueChanged += GridCellValueChanged;
        grid.CurrentCellDirtyStateChanged +=
            GridCurrentCellDirtyStateChanged;
        grid.SelectionChanged += (_, _) =>
        {
            if (!_binding)
            {
                SelectedApplicationChanged?.Invoke(
                    this,
                    EventArgs.Empty);
            }
        };
        grid.DataError += GridDataError;
        return grid;
    }

    private void AddRow(
        ApplicationAssignmentRow assignment,
        string? selectedPath)
    {
        var index = _grid.Rows.Add();
        var row = _grid.Rows[index];
        WriteRow(row, assignment);

        if (!string.Equals(
                selectedPath,
                assignment.ExecutablePath,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        row.Selected = true;
        _grid.CurrentCell =
            row.Cells[
                ApplicationAssignmentsGridColumns
                    .ApplicationColumnName];
    }

    private static void WriteRow(
        DataGridViewRow row,
        ApplicationAssignmentRow assignment)
    {
        row.Cells[
            ApplicationAssignmentsGridColumns
                .EnabledColumnName].Value =
            assignment.Enabled;
        row.Cells[
            ApplicationAssignmentsGridColumns
                .ApplicationColumnName].Value =
            assignment.DisplayName;
        row.Cells[
            VisualProfileColumnName].Value =
            assignment.VisualProfileId;
        row.Cells[
            MenuVisualProfileColumnName].Value =
            assignment.MenuVisualProfileSelectorId;
        row.Cells[
            OverlayScopeColumnName].Value =
            assignment.OverlayScopeId;
        row.Cells[
            ApplicationAssignmentsGridColumns
                .ExecutableColumnName].Value =
            assignment.ExecutableName;
        row.Cells[
            ApplicationAssignmentsGridColumns
                .PathColumnName].Value =
            assignment.ExecutablePath;
        row.Tag = assignment.ExecutablePath;
    }

    private DataGridViewRow? FindRow(
        string executablePath)
    {
        return _grid.Rows
            .Cast<DataGridViewRow>()
            .FirstOrDefault(row =>
                row.Tag is string rowPath &&
                string.Equals(
                    rowPath,
                    executablePath,
                    StringComparison.OrdinalIgnoreCase));
    }

    private void UpdateVisibility(int count)
    {
        _emptyStateLabel.Visible = count == 0;
        _grid.Visible = count > 0;
    }

    private void GridCurrentCellDirtyStateChanged(
        object? sender,
        EventArgs eventArgs)
    {
        if (_grid.IsCurrentCellDirty)
        {
            _grid.CommitEdit(
                DataGridViewDataErrorContexts.Commit);
        }
    }

    private void GridCellValueChanged(
        object? sender,
        DataGridViewCellEventArgs eventArgs)
    {
        if (_binding ||
            eventArgs.RowIndex < 0 ||
            eventArgs.ColumnIndex < 0)
        {
            return;
        }

        var row = _grid.Rows[eventArgs.RowIndex];
        if (row.Tag is not string executablePath)
        {
            return;
        }

        var columnName =
            _grid.Columns[eventArgs.ColumnIndex].Name;
        var change =
            ApplicationAssignmentCellChangeMapper.Map(
                executablePath,
                columnName,
                row.Cells[eventArgs.ColumnIndex].Value);
        if (change is not null)
        {
            AssignmentChanged?.Invoke(change);
        }
    }

    private static void GridDataError(
        object? sender,
        DataGridViewDataErrorEventArgs eventArgs)
    {
        var grid = sender as DataGridView;
        var columnName = GetColumnName(
            grid,
            eventArgs.ColumnIndex);
        var executablePath = GetExecutablePath(
            grid,
            eventArgs.RowIndex);
        var recovered =
            ApplicationAssignmentSelectorErrorPolicy
                .IsRecoverable(
                    eventArgs.Exception,
                    eventArgs.Context,
                    columnName);

        Diagnostics.Report(
            nameof(ApplicationAssignmentsGrid),
            "Handle selector data error",
            recovered
                ? DiagnosticSeverity.Warning
                : DiagnosticSeverity.Error,
            recovered
                ? DiagnosticFailurePolicy.Recovered
                : DiagnosticFailurePolicy.None,
            ApplicationAssignmentSelectorErrorPolicy
                .CreateDiagnostic(
                    eventArgs.Exception,
                    eventArgs.Context,
                    eventArgs.RowIndex,
                    eventArgs.ColumnIndex,
                    columnName,
                    executablePath,
                    recovered),
            eventArgs.Exception);
        eventArgs.ThrowException = !recovered;
    }

    private static string? GetColumnName(
        DataGridView? grid,
        int columnIndex)
    {
        return grid is not null &&
            columnIndex >= 0 &&
            columnIndex < grid.Columns.Count
                ? grid.Columns[columnIndex].Name
                : null;
    }

    private static string? GetExecutablePath(
        DataGridView? grid,
        int rowIndex)
    {
        return grid is not null &&
            rowIndex >= 0 &&
            rowIndex < grid.Rows.Count &&
            grid.Rows[rowIndex].Tag is string path
                ? path
                : null;
    }

    private static void GridCellPainting(
        object? sender,
        DataGridViewCellPaintingEventArgs eventArgs)
    {
        if (sender is not DataGridView grid ||
            eventArgs.RowIndex < 0 ||
            eventArgs.ColumnIndex < 0 ||
            !string.Equals(
                grid.Columns[eventArgs.ColumnIndex].Name,
                ApplicationAssignmentsGridColumns
                    .EnabledColumnName,
                StringComparison.Ordinal))
        {
            return;
        }

        var graphics = eventArgs.Graphics;
        if (graphics is null)
        {
            return;
        }

        var selected =
            (eventArgs.State &
             DataGridViewElementStates.Selected) != 0;
        eventArgs.PaintBackground(
            eventArgs.CellBounds,
            selected);

        ApplicationAssignmentEnabledCellRenderer.Paint(
            graphics,
            eventArgs.CellBounds,
            new EnabledCellRenderState(
                eventArgs.FormattedValue is true,
                selected,
                grid.CurrentCellAddress.X ==
                    eventArgs.ColumnIndex &&
                grid.CurrentCellAddress.Y ==
                    eventArgs.RowIndex));

        eventArgs.Handled = true;
    }

    private static Label CreateEmptyStateLabel()
    {
        return new Label
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(10.5f),
            Padding = new Padding(32),
            Text = "No application assignments yet.\n\n" +
                   "Add the currently active application or select an executable file.",
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
        };
    }
}
