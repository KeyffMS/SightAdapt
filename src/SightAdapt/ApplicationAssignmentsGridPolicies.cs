using System.Drawing.Drawing2D;

namespace SightAdapt;

internal static class ApplicationAssignmentsGridColumns
{
    public const string EnabledColumnName = "Enabled";
    public const string ApplicationColumnName = "Application";
    public const string VisualProfileColumnName = "VisualProfile";
    public const string MenuVisualProfileColumnName =
        "MenuVisualProfile";
    public const string OverlayScopeColumnName = "OverlayScope";
    public const string ExecutableColumnName = "Executable";
    public const string PathColumnName = "Path";

    public static void AddTo(DataGridView grid)
    {
        ArgumentNullException.ThrowIfNull(grid);

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
        grid.Columns.Add(CreateSelector(
            VisualProfileColumnName,
            "VISUAL PROFILE",
            185,
            160));
        grid.Columns.Add(CreateSelector(
            MenuVisualProfileColumnName,
            "MENU PROFILE",
            185,
            160));
        grid.Columns.Add(CreateSelector(
            OverlayScopeColumnName,
            "OVERLAY SCOPE",
            170,
            150));
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn(
            ExecutableColumnName,
            "EXECUTABLE",
            155));
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn(
            PathColumnName,
            "FULL PATH",
            220,
            fill: true));
    }

    public static void SetSelectorOptions(
        DataGridView grid,
        IReadOnlyList<VisualProfile> profiles)
    {
        ArgumentNullException.ThrowIfNull(grid);
        ArgumentNullException.ThrowIfNull(profiles);

        Selector(grid, VisualProfileColumnName)
            ?.SetProfiles(profiles);

        Selector(grid, MenuVisualProfileColumnName)
            ?.SetOptions(
                new[]
                {
                    new ModernSelectorOption(
                        ApplicationMenuProfilePolicy
                            .InheritSelectorId,
                        ApplicationMenuProfilePolicy
                            .InheritDisplayName),
                }.Concat(profiles.Select(profile =>
                    new ModernSelectorOption(
                        profile.Id,
                        profile.Name))));

        Selector(grid, OverlayScopeColumnName)
            ?.SetOptions(
                OverlayScopePolicy.All.Select(scope =>
                    new ModernSelectorOption(
                        OverlayScopePolicy.ToId(scope),
                        OverlayScopePolicy.GetDisplayName(
                            scope))));
    }

    public static bool IsSelectorColumn(
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

    private static StableModernSelectorComboBoxColumn
        CreateSelector(
            string name,
            string header,
            int width,
            int minimumWidth)
    {
        return new StableModernSelectorComboBoxColumn
        {
            Name = name,
            HeaderText = header,
            DisplayStyle =
                DataGridViewComboBoxDisplayStyle.ComboBox,
            FlatStyle = FlatStyle.Flat,
            Width = width,
            MinimumWidth = minimumWidth,
            SortMode =
                DataGridViewColumnSortMode.NotSortable,
        };
    }

    private static StableModernSelectorComboBoxColumn?
        Selector(
            DataGridView grid,
            string name)
    {
        return grid.Columns[name] as
            StableModernSelectorComboBoxColumn;
    }
}

internal static class ApplicationAssignmentCellChangeMapper
{
    public static ApplicationAssignmentChange? Map(
        string executablePath,
        string columnName,
        object? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            columnName);

        if (string.Equals(
                columnName,
                ApplicationAssignmentsGridColumns.EnabledColumnName,
                StringComparison.Ordinal) &&
            value is bool enabled)
        {
            return new ApplicationAssignmentChange.Enabled(
                executablePath,
                enabled);
        }

        if (string.Equals(
                columnName,
                ApplicationAssignmentsGridColumns.VisualProfileColumnName,
                StringComparison.Ordinal) &&
            value is string profileId)
        {
            return new ApplicationAssignmentChange.VisualProfile(
                executablePath,
                profileId);
        }

        if (string.Equals(
                columnName,
                ApplicationAssignmentsGridColumns.MenuVisualProfileColumnName,
                StringComparison.Ordinal) &&
            value is string menuProfileId)
        {
            return new ApplicationAssignmentChange.MenuVisualProfile(
                executablePath,
                ApplicationMenuProfilePolicy.FromSelectorId(
                    menuProfileId));
        }

        if (string.Equals(
                columnName,
                ApplicationAssignmentsGridColumns.OverlayScopeColumnName,
                StringComparison.Ordinal) &&
            value is string scopeId)
        {
            return new ApplicationAssignmentChange.OverlayScope(
                executablePath,
                OverlayScopePolicy.ParseRequired(scopeId));
        }

        return null;
    }
}

internal static class ApplicationAssignmentSelectorErrorPolicy
{
    private const DataGridViewDataErrorContexts
        RecoverableContexts =
            DataGridViewDataErrorContexts.Formatting |
            DataGridViewDataErrorContexts.Display |
            DataGridViewDataErrorContexts.PreferredSize |
            DataGridViewDataErrorContexts
                .InitialValueRestoration;

    public static bool IsRecoverable(
        Exception? exception,
        DataGridViewDataErrorContexts context,
        string? columnName)
    {
        if (exception is not ArgumentException ||
            !ApplicationAssignmentsGridColumns
                .IsSelectorColumn(columnName))
        {
            return false;
        }

        var recoverable =
            context & RecoverableContexts;
        var unexpected =
            context & ~RecoverableContexts;
        return recoverable != 0 &&
            unexpected == 0;
    }

    public static string CreateDiagnostic(
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
}

internal readonly record struct EnabledCellRenderState(
    bool Enabled,
    bool Selected,
    bool Focused);

internal static class ApplicationAssignmentEnabledCellRenderer
{
    private const int IndicatorDiameter = 15;

    public static Rectangle GetIndicatorBounds(
        Rectangle cellBounds)
    {
        return new Rectangle(
            cellBounds.Left +
                (cellBounds.Width - IndicatorDiameter) / 2,
            cellBounds.Top +
                (cellBounds.Height - IndicatorDiameter) / 2,
            IndicatorDiameter,
            IndicatorDiameter);
    }

    public static void Paint(
        Graphics graphics,
        Rectangle cellBounds,
        EnabledCellRenderState state)
    {
        ArgumentNullException.ThrowIfNull(graphics);

        var bounds = GetIndicatorBounds(cellBounds);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var fill = new SolidBrush(
            state.Enabled
                ? AppTheme.Success
                : AppTheme.Surface);
        using var border = new Pen(
            state.Enabled
                ? AppTheme.Success
                : AppTheme.TextMuted,
            state.Enabled ? 1.5f : 1.2f);
        graphics.FillEllipse(fill, bounds);
        graphics.DrawEllipse(border, bounds);

        if (state.Selected && state.Focused)
        {
            ControlPaint.DrawFocusRectangle(
                graphics,
                Rectangle.Inflate(bounds, 5, 5),
                AppTheme.TextPrimary,
                AppTheme.Selection);
        }
    }
}
