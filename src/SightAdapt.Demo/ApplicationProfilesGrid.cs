using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace SightAdapt.Demo;

internal enum ApplicationProfileGridColumn
{
    Enabled,
    VisualProfile,
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

internal sealed class ApplicationProfilesGrid : UserControl
{
    private const string EnabledColumnName = "Enabled";
    private const string ApplicationColumnName = "Application";
    private const string VisualProfileColumnName = "VisualProfile";
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

    public event EventHandler<ApplicationProfileGridValueChangedEventArgs>? ValueChanged;

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
            row.Cells[ExecutableColumnName].Value = application.ExecutableName;
            row.Cells[PathColumnName].Value = application.ExecutablePath;
            row.Tag = application.ExecutablePath;
        }
        finally
        {
            _binding = false;
        }
    }

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
        grid.Columns.Add(new StableVisualProfileComboBoxColumn
        {
            Name = VisualProfileColumnName,
            HeaderText = "VISUAL PROFILE",
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            FlatStyle = FlatStyle.Flat,
            Width = 185,
            MinimumWidth = 160,
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
            StableVisualProfileComboBoxColumn column)
        {
            column.SetProfiles(profiles);
        }
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
    }

    private static void GridDataError(
        object? sender,
        DataGridViewDataErrorEventArgs eventArgs)
    {
        if (eventArgs.Exception is ArgumentException or InvalidOperationException)
        {
            Debug.WriteLine(
                $"SightAdapt ignored an expected grid binding race: {eventArgs.Exception}");
            eventArgs.ThrowException = false;
        }
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

    private static string GetColumnName(ApplicationProfileGridColumn column)
    {
        return column switch
        {
            ApplicationProfileGridColumn.Enabled => EnabledColumnName,
            ApplicationProfileGridColumn.VisualProfile => VisualProfileColumnName,
            _ => throw new ArgumentOutOfRangeException(nameof(column)),
        };
    }
}
