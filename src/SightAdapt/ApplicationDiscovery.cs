using System.Diagnostics;
using System.ComponentModel;

namespace SightAdapt;

internal static class ApplicationDiscovery
{
    private static readonly ApplicationIdentityCache IdentityCache = new();

    public static bool TryGetIdentity(
        nint window,
        out ApplicationIdentity identity)
    {
        identity = null!;

        if (!NativeProcessApi.Default.TryGetProcessIdentityKey(
                window,
                out var processKey))
        {
            return false;
        }

        if (IdentityCache.TryGet(processKey, out identity))
        {
            return true;
        }

        if (!NativeProcessApi.Default.TryGetProcessPath(
                processKey,
                out var executablePath))
        {
            IdentityCache.Remove(processKey);
            return false;
        }

        try
        {
            identity = FromExecutablePath(executablePath);
            IdentityCache.Set(processKey, identity);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException)
        {
            IdentityCache.Remove(processKey);
            Diagnostics.Report(
                nameof(ApplicationDiscovery),
                "Resolve application identity",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.Recovered,
                "Application identity could not be resolved.",
                exception);
            return false;
        }
    }

    public static ApplicationIdentity FromExecutablePath(
        string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException(
                "An executable path is required.",
                nameof(executablePath));
        }

        var fullPath =
            Path.GetFullPath(executablePath);

        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "The selected executable does not exist.",
                fullPath);
        }

        var executableName =
            Path.GetFileName(fullPath);

        if (string.IsNullOrWhiteSpace(
                executableName))
        {
            throw new ArgumentException(
                "The executable name could not be resolved.",
                nameof(executablePath));
        }

        return new ApplicationIdentity(
            GetDisplayName(
                fullPath,
                executableName),
            executableName,
            fullPath);
    }

    private static string GetDisplayName(
        string executablePath,
        string executableName)
    {
        try
        {
            var description =
                FileVersionInfo
                    .GetVersionInfo(executablePath)
                    .FileDescription;

            if (!string.IsNullOrWhiteSpace(
                    description))
            {
                return description.Trim();
            }
        }
        catch (Exception exception) when (
            exception is IOException or
            UnauthorizedAccessException or
            Win32Exception)
        {
            Diagnostics.Report(
                nameof(ApplicationDiscovery),
                "Read executable metadata",
                DiagnosticSeverity.Warning,
                DiagnosticFailurePolicy.Recovered,
                "Executable metadata could not be read; the file name will be used.",
                exception);
        }

        return Path.GetFileNameWithoutExtension(
            executableName);
    }
}
