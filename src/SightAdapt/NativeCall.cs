using System.ComponentModel;
using System.Diagnostics;
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
        return TryTransient(
            succeeded,
            operation,
            Marshal.GetLastWin32Error,
            ReportFailure);
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
        BestEffort(
            succeeded,
            operation,
            Marshal.GetLastWin32Error,
            ReportFailure);
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
        return new Win32Exception(
            errorCode,
            FormatFailure(operation, errorCode));
    }

    private static void ValidateArguments(
        string operation,
        Func<int> getLastError)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        ArgumentNullException.ThrowIfNull(getLastError);
    }

    private static void ReportFailure(
        string message)
    {
        Debug.WriteLine($"SightAdapt native call: {message}");
    }
}
