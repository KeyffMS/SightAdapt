using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace SightAdapt;

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

internal sealed class ApplicationProfilesGrid : UserControl
{
    private const string EnabledColumnName = "Enabled";
    private const string ApplicationColumnName = "Application";
    internal const string VisualProfileColumnName = "VisualProfile";
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

    public ApplicationProfilesGrid()
    {
        BackColor = AppTheme.Surface;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;

        _grid = CreateGrid();
        _emptyStateLabel = CreateEmptyStateLabel();
        Controls.Add(_grid);
        Controls.Add(_emptyStateLabel);
    }

    public event EventHandler<ApplicationProfileEnabledChangedEventArgs>? ApplicationEnabledChanged;

    public event EventHandler<ApplicationProfileVisualProfileChangedEventArgs>? VisualProfileChanged;

    public event EventHandler<ApplicationProfileOverlayScopeChangedEventArgs>? OverlayScopeChanged;

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
        IReadOnlyList<ApplicationProfile> applications,
        IReadOnlyList<VisualProfile> visualProfiles)
    {
        ArgumentNullException.ThrowIfNull(applications);
        ArgumentNullException.ThrowIfNull(visualProfiles);

        var selectedPath = SelectedExecutablePath;
        _binding = true;
        try
        {
            SetVisualProfiles(visualProfiles);
            SetOverlayScopes();
            _grid.Rows.Clear();

            foreach (var application in applications)
            {
                AddRow(application, selectedPath);
            }
        }
        finally
        {
            _binding = false;
        }

        UpdateVisibility(applications.Count);
        SelectedApplicationChanged?.Invoke(this, EventArgs.Empty);
    }

    public void UpdateApplication(ApplicationProfile application)
    {
        ArgumentNullException.ThrowIfNull(application);

        var row = FindRow(application.ExecutablePath);
        if (row is null)
        {
            return;
        }

        _binding = true;
        try
        {
            row.Cells[EnabledColumnName].Value = application.Enabled;
            row.Cells[ApplicationColumnName].Value = application.DisplayName;
            row.Cells[VisualProfileColumnName].Value = application.VisualProfileId;
            row.Cells[OverlayScopeColumnName].Value =
                OverlayScopePolicy.ToId(application.OverlayScope);
            row.Cells[ExecutableColumnName].Value = application.ExecutableName;
            row.Cells[PathColumnName].Value = application.ExecutablePath;
            row.Tag = application.ExecutablePath;
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
        grid.Columns.Add(CreateTextColumn(
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
            Name = OverlayScopeColumnName,
            HeaderText = "OVERLAY SCOPE",
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            FlatStyle = FlatStyle.Flat,
            Width = 170,
            MinimumWidth = 150,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(CreateTextColumn(
            ExecutableColumnName,
            "EXECUTABLE",
            155));
        grid.Columns.Add(CreateTextColumn(
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

    private void AddRow(ApplicationProfile application, string? selectedPath)
    {
        var index = _grid.Rows.Add(
            application.Enabled,
            application.DisplayName,
            application.VisualProfileId,
            OverlayScopePolicy.ToId(application.OverlayScope),
            application.ExecutableName,
            application.ExecutablePath);
        var row = _grid.Rows[index];
        row.Tag = application.ExecutablePath;

        if (!string.Equals(
                selectedPath,
                application.ExecutablePath,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        row.Selected = true;
        _grid.CurrentCell = row.Cells[ApplicationColumnName];
    }

    private void SetVisualProfiles(IReadOnlyList<VisualProfile> profiles)
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

        Debug.WriteLine(CreateDataErrorDiagnostic(
            eventArgs.Exception,
            eventArgs.Context,
            eventArgs.RowIndex,
            eventArgs.ColumnIndex,
            columnName,
            executablePath,
            recovered));
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

    private static DataGridViewTextBoxColumn CreateTextColumn(
        string name,
        string header,
        int width,
        bool fill = false)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            AutoSizeMode = fill
                ? DataGridViewAutoSizeColumnMode.Fill
                : DataGridViewAutoSizeColumnMode.None,
            MinimumWidth = width,
            ReadOnly = true,
            Width = width,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
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
            Text = "No application profiles yet.\n\n" +
                   "Add the currently active application or select an executable file.",
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
        };
    }


}
