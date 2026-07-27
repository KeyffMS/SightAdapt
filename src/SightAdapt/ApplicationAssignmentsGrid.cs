using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed class ApplicationAssignmentsGrid : UserControl
{
    private const string EnabledColumnName = "Enabled";
    private const string ApplicationColumnName = "Application";
    internal const string VisualProfileColumnName = "VisualProfile";
    internal const string MenuVisualProfileColumnName =
        "MenuVisualProfile";
    internal const string OverlayScopeColumnName = "OverlayScope";

    private const DataGridViewDataErrorContexts
        RecoverableSelectorContexts =
            DataGridViewDataErrorContexts.Formatting |
            DataGridViewDataErrorContexts.Display |
            DataGridViewDataErrorContexts.PreferredSize |
            DataGridViewDataErrorContexts.InitialValueRestoration;
    private const string ExecutableColumnName = "Executable";
    private const string PathColumnName = "Path";

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
            SetVisualProfiles(visualProfiles);
            SetMenuVisualProfiles(visualProfiles);
            SetOverlayScopes();
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
        SelectedApplicationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateAssignment(ApplicationAssignmentRow assignment)
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
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
        AppTheme.StyleGrid(grid);

        var enabled = new DataGridViewCheckBoxColumn
        {
            Name = EnabledColumnName,
            HeaderText = "ACTIVE",
            Width = 92,
            MinimumWidth = 92,
            Resizable = DataGridViewTriState.False,
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        enabled.HeaderCell.Style.Alignment =
            DataGridViewContentAlignment.MiddleCenter;
        enabled.DefaultCellStyle.Alignment =
            DataGridViewContentAlignment.MiddleCenter;
        enabled.DefaultCellStyle.Padding = Padding.Empty;

        grid.Columns.Add(enabled);
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn(
            ApplicationColumnName,
            "APPLICATION",
            205));
        grid.Columns.Add(new StableModernSelectorComboBoxColumn
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
            Name = MenuVisualProfileColumnName,
            HeaderText = "MENU PROFILE",
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
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn(
            ExecutableColumnName,
            "EXECUTABLE",
            155));
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn(
            PathColumnName,
            "FULL PATH",
            220,
            fill: true));

        grid.CellPainting += GridCellPainting;
        grid.CellValueChanged += GridCellValueChanged;
        grid.CurrentCellDirtyStateChanged += GridCurrentCellDirtyStateChanged;
        grid.SelectionChanged += (_, _) =>
        {
            if (!_binding)
            {
                SelectedApplicationChanged?.Invoke(this, EventArgs.Empty);
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
        _grid.CurrentCell = row.Cells[ApplicationColumnName];
    }

    private static void WriteRow(
        DataGridViewRow row,
        ApplicationAssignmentRow assignment)
    {
        row.Cells[EnabledColumnName].Value = assignment.Enabled;
        row.Cells[ApplicationColumnName].Value = assignment.DisplayName;
        row.Cells[VisualProfileColumnName].Value = assignment.VisualProfileId;
        row.Cells[MenuVisualProfileColumnName].Value =
            assignment.MenuVisualProfileSelectorId;
        row.Cells[OverlayScopeColumnName].Value = assignment.OverlayScopeId;
        row.Cells[ExecutableColumnName].Value = assignment.ExecutableName;
        row.Cells[PathColumnName].Value = assignment.ExecutablePath;
        row.Tag = assignment.ExecutablePath;
    }

    private void SetVisualProfiles(IReadOnlyList<VisualProfile> profiles)
    {
        if (_grid.Columns[VisualProfileColumnName] is
            StableModernSelectorComboBoxColumn column)
        {
            column.SetProfiles(profiles);
        }
    }

    private void SetMenuVisualProfiles(
        IReadOnlyList<VisualProfile> profiles)
    {
        if (_grid.Columns[MenuVisualProfileColumnName] is not
            StableModernSelectorComboBoxColumn column)
        {
            return;
        }

        column.SetOptions(
            new[]
            {
                new ModernSelectorOption(
                    ApplicationMenuProfilePolicy.InheritSelectorId,
                    ApplicationMenuProfilePolicy.InheritDisplayName),
            }.Concat(profiles.Select(profile =>
                new ModernSelectorOption(
                    profile.Id,
                    profile.Name))));
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

    private DataGridViewRow? FindRow(string executablePath)
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
            _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

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
            AssignmentChanged?.Invoke(
                new ApplicationAssignmentChange.Enabled(
                    executablePath,
                    enabled));
        }
        else if (columnName == VisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string profileId)
        {
            AssignmentChanged?.Invoke(
                new ApplicationAssignmentChange.VisualProfile(
                    executablePath,
                    profileId));
        }
        else if (columnName == MenuVisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string menuProfileId)
        {
            AssignmentChanged?.Invoke(
                new ApplicationAssignmentChange.MenuVisualProfile(
                    executablePath,
                    ApplicationMenuProfilePolicy.FromSelectorId(
                        menuProfileId)));
        }
        else if (columnName == OverlayScopeColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string scopeId)
        {
            AssignmentChanged?.Invoke(
                new ApplicationAssignmentChange.OverlayScope(
                    executablePath,
                    OverlayScopePolicy.ParseRequired(scopeId)));
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
        var recovered = IsExpectedSelectorDataError(
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
            CreateDataErrorDiagnostic(
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

    internal static bool IsExpectedSelectorDataError(
        Exception? exception,
        DataGridViewDataErrorContexts context,
        string? columnName)
    {
        if (exception is not ArgumentException ||
            !IsSelectorColumn(columnName))
        {
            return false;
        }

        var recoverableContext =
            context & RecoverableSelectorContexts;
        var unexpectedContext =
            context & ~RecoverableSelectorContexts;
        return recoverableContext != 0 &&
            unexpectedContext == 0;
    }

    internal static string CreateDataErrorDiagnostic(
        Exception? exception,
        DataGridViewDataErrorContexts context,
        int rowIndex,
        int columnIndex,
        string? columnName,
        string? executablePath,
        bool recovered)
    {
        return
            $"SightAdapt grid data error; recovered={recovered}; " +
            $"row={rowIndex}; column={columnIndex}; " +
            $"columnName={columnName ?? "<unknown>"}; " +
            $"executablePath={executablePath ?? "<unknown>"}; " +
            $"context={context}; " +
            $"exception={exception?.ToString() ?? "<none>"}";
    }

    private static bool IsSelectorColumn(
        string? columnName)
    {
        return string.Equals(
                columnName,
                VisualProfileColumnName,
                StringComparison.Ordinal) ||
            string.Equals(
                columnName,
                MenuVisualProfileColumnName,
                StringComparison.Ordinal) ||
            string.Equals(
                columnName,
                OverlayScopeColumnName,
                StringComparison.Ordinal);
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
                EnabledColumnName,
                StringComparison.Ordinal))
        {
            return;
        }

        var graphics = eventArgs.Graphics;
        if (graphics is null)
        {
            return;
        }

        eventArgs.PaintBackground(
            eventArgs.CellBounds,
            (eventArgs.State & DataGridViewElementStates.Selected) != 0);

        var enabled = eventArgs.FormattedValue is true;
        const int diameter = 15;
        var bounds = new Rectangle(
            eventArgs.CellBounds.Left +
                (eventArgs.CellBounds.Width - diameter) / 2,
            eventArgs.CellBounds.Top +
                (eventArgs.CellBounds.Height - diameter) / 2,
            diameter,
            diameter);

        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(
            enabled ? AppTheme.Success : AppTheme.Surface);
        using var border = new Pen(
            enabled ? AppTheme.Success : AppTheme.TextMuted,
            enabled ? 1.5f : 1.2f);
        graphics.FillEllipse(fill, bounds);
        graphics.DrawEllipse(border, bounds);

        if ((eventArgs.State & DataGridViewElementStates.Selected) != 0 &&
            grid.CurrentCellAddress.X == eventArgs.ColumnIndex &&
            grid.CurrentCellAddress.Y == eventArgs.RowIndex)
        {
            var focusBounds = Rectangle.Inflate(bounds, 5, 5);
            ControlPaint.DrawFocusRectangle(
                graphics,
                focusBounds,
                AppTheme.TextPrimary,
                AppTheme.Selection);
        }

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
