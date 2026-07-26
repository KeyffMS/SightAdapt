using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SightAdapt;

internal static class NativeCall
{
    public static nint RequireHandle(
        nint handle,
        string operation)
    {
        return RequireHandle(
            handle,
            operation,
            Marshal.GetLastWin32Error);
    }

    internal static nint RequireHandle(
        nint handle,
        string operation,
        Func<int> getLastError)
    {
        ValidateArguments(operation, getLastError);
        return handle != nint.Zero
            ? handle
            : throw CreateException(
                operation,
                getLastError());
    }

    public static void RequireSuccess(
        bool succeeded,
        string operation)
    {
        RequireSuccess(
            succeeded,
            operation,
            Marshal.GetLastWin32Error);
    }

    internal static void RequireSuccess(
        bool succeeded,
        string operation,
        Func<int> getLastError)
    {
        ValidateArguments(operation, getLastError);
        if (!succeeded)
        {
            throw CreateException(
                operation,
                getLastError());
        }
    }

    public static bool TryTransient(
        bool succeeded,
        string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        if (succeeded)
        {
            return true;
        }

        var errorCode = Marshal.GetLastWin32Error();
        Diagnostics.Report(
            nameof(NativeCall),
            operation,
            DiagnosticSeverity.Warning,
            DiagnosticFailurePolicy.Transient,
            FormatFailure(operation, errorCode),
            nativeErrorCode: errorCode);
        return false;
    }

    internal static bool TryTransient(
        bool succeeded,
        string operation,
        Func<int> getLastError,
        Action<string> reportFailure)
    {
        ValidateArguments(operation, getLastError);
        ArgumentNullException.ThrowIfNull(reportFailure);

        if (succeeded)
        {
            return true;
        }

        reportFailure(FormatFailure(
            operation,
            getLastError()));
        return false;
    }

    public static void BestEffort(
        bool succeeded,
        string operation)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        if (succeeded)
        {
            return;
        }

        var errorCode = Marshal.GetLastWin32Error();
        Diagnostics.Report(
            nameof(NativeCall),
            operation,
            DiagnosticSeverity.Warning,
            DiagnosticFailurePolicy.BestEffort,
            FormatFailure(operation, errorCode),
            nativeErrorCode: errorCode);
    }

    internal static void BestEffort(
        bool succeeded,
        string operation,
        Func<int> getLastError,
        Action<string> reportFailure)
    {
        ValidateArguments(operation, getLastError);
        ArgumentNullException.ThrowIfNull(reportFailure);

        if (!succeeded)
        {
            reportFailure(FormatFailure(
                operation,
                getLastError()));
        }
    }

    internal static string FormatFailure(
        string operation,
        int errorCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        var description =
            new Win32Exception(errorCode).Message;
        return $"{operation} failed with Win32 error " +
            $"{errorCode}: {description}";
    }

    private static Win32Exception CreateException(
        string operation,
        int errorCode)
    {
        var message = FormatFailure(operation, errorCode);
        Diagnostics.Report(
            nameof(NativeCall),
            operation,
            DiagnosticSeverity.Error,
            DiagnosticFailurePolicy.Critical,
            message,
            nativeErrorCode: errorCode);
        return new Win32Exception(errorCode, message);
    }

    private static void ValidateArguments(
        string operation,
        Func<int> getLastError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(getLastError);
    }

}
