using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace SightAdapt;

internal sealed record SettingsCommitResult(
    bool Succeeded,
    string? ErrorMessage)
{
    public static SettingsCommitResult Success() => new(true, null);

    public static SettingsCommitResult Failure(string message) =>
        new(false, message);
}

internal sealed class SettingsCommitResult<T>
{
    private readonly T _value;

    private SettingsCommitResult(
        bool succeeded,
        T value,
        string? errorMessage)
    {
        Succeeded = succeeded;
        _value = value;
        ErrorMessage = errorMessage;
    }

    public bool Succeeded { get; }

    public string? ErrorMessage { get; }

    public T Value => Succeeded
        ? _value
        : throw new InvalidOperationException(
            "A failed settings commit does not have a value.");

    public bool TryGetValue(
        [MaybeNullWhen(false)] out T value)
    {
        value = _value;
        return Succeeded;
    }

    public static SettingsCommitResult<T> Success(T value) =>
        new(true, value, null);

    public static SettingsCommitResult<T> Failure(string message) =>
        new(false, default!, message);
}

internal sealed class SettingsCoordinator
{
    private readonly SettingsStore _store;
    private readonly SightAdaptSettings _current;
    private readonly Action<Exception> _reportUnexpectedError;

    public SettingsCoordinator(
        SettingsStore? store = null,
        Action<Exception>? reportUnexpectedError = null)
    {
        _store = store ?? new SettingsStore();
        _reportUnexpectedError =
            reportUnexpectedError ?? ReportUnexpectedError;
        _current = _store.Load();
    }

    public IReadOnlySightAdaptSettings Current =>
        _current.CreateWorkingCopy();

    public string SettingsPath => _store.SettingsPath;

    public string? LastLoadWarning => _store.LastLoadWarning;

    public bool SettingsWereMigrated => _store.SettingsWereMigrated;

    public event EventHandler? Changed;

    public SettingsCommitResult Commit(
        Action<SightAdaptSettings> mutation)
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
                result.ErrorMessage ??
                "Settings could not be changed.");
    }

    public SettingsCommitResult<T> Commit<T>(
        Func<SightAdaptSettings, T> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        var candidate = _current.CreateWorkingCopy();
        T value;

        try
        {
            value = mutation(candidate);
            _store.Save(candidate);
        }
        catch (Exception exception)
            when (IsExpectedError(exception))
        {
            return SettingsCommitResult<T>.Failure(
                FormatError(exception));
        }
        catch (Exception exception)
        {
            _reportUnexpectedError(exception);
            throw;
        }

        _current.ReplaceWith(candidate);
        Changed?.Invoke(this, EventArgs.Empty);
        return SettingsCommitResult<T>.Success(value);
    }

    public SettingsCommitResult PersistCurrent()
    {
        var candidate = _current.CreateWorkingCopy();

        try
        {
            _store.Save(candidate);
        }
        catch (Exception exception)
            when (IsExpectedError(exception))
        {
            return SettingsCommitResult.Failure(
                FormatError(exception));
        }
        catch (Exception exception)
        {
            _reportUnexpectedError(exception);
            throw;
        }

        _current.ReplaceWith(candidate);
        return SettingsCommitResult.Success();
    }

    private static bool IsExpectedError(
        Exception exception)
    {
        return exception is SettingsValidationException or
            IOException or
            UnauthorizedAccessException;
    }

    private static string FormatError(
        Exception exception)
    {
        return exception is IOException or
            UnauthorizedAccessException
                ? $"Settings could not be saved: {exception.Message}"
                : exception.Message;
    }

    private static void ReportUnexpectedError(
        Exception exception)
    {
        Debug.WriteLine(
            $"Unexpected SightAdapt settings transaction failure: " +
            $"{exception}");
    }
}
