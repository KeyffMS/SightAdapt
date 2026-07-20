using System.Diagnostics;

namespace SightAdapt.Demo;

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
        PublishChanged();
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

    private void PublishChanged()
    {
        var handlers = Changed?
            .GetInvocationList()
            .OfType<EventHandler>()
            .ToArray();

        if (handlers is null)
        {
            return;
        }

        foreach (var handler in handlers)
        {
            if (handler.Target is Control control &&
                control.IsHandleCreated &&
                !control.IsDisposed)
            {
                DeferControlObserver(control, handler);
                continue;
            }

            handler(this, EventArgs.Empty);
        }
    }

    private void DeferControlObserver(
        Control control,
        EventHandler handler)
    {
        try
        {
            control.BeginInvoke((Action)(() =>
                InvokeControlObserverWhenReady(control, handler)));
        }
        catch (InvalidOperationException exception) when (
            control.IsDisposed ||
            !control.IsHandleCreated)
        {
            Debug.WriteLine(
                $"SightAdapt skipped a disposed settings observer: {exception}");
        }
    }

    private void InvokeControlObserverWhenReady(
        Control control,
        EventHandler handler)
    {
        if (control.IsDisposed || control.Disposing)
        {
            return;
        }

        var editingGrid = FindEditingGrid(control);
        if (editingGrid is not null)
        {
            DeferUntilGridEditEnds(control, editingGrid, handler);
            return;
        }

        handler(this, EventArgs.Empty);
    }

    private void DeferUntilGridEditEnds(
        Control control,
        DataGridView grid,
        EventHandler handler)
    {
        DataGridViewCellEventHandler cellEndEdit = null!;
        cellEndEdit = (_, _) =>
        {
            grid.CellEndEdit -= cellEndEdit;
            if (!control.IsDisposed && !control.Disposing)
            {
                DeferControlObserver(control, handler);
            }
        };
        grid.CellEndEdit += cellEndEdit;
    }

    private static DataGridView? FindEditingGrid(Control control)
    {
        if (control is DataGridView grid && IsEditing(grid))
        {
            return grid;
        }

        foreach (Control child in control.Controls)
        {
            var editingGrid = FindEditingGrid(child);
            if (editingGrid is not null)
            {
                return editingGrid;
            }
        }

        return null;
    }

    private static bool IsEditing(DataGridView grid)
    {
        return !grid.IsDisposed &&
               (grid.IsCurrentCellInEditMode ||
                grid.IsCurrentCellDirty ||
                grid.IsCurrentRowDirty ||
                grid.EditingControl is not null);
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
