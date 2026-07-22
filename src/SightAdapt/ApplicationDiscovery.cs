using System.ComponentModel;
using System.Diagnostics;

namespace SightAdapt;

internal static class ApplicationDiscovery
{
    private static readonly ApplicationIdentityCache IdentityCache = new();

    public static bool TryGetIdentity(
        nint window,
        out ApplicationIdentity identity)
    {
        identity = null!;

        NativeMethods.GetWindowThreadProcessId(window, out var processId);
        if (processId == 0)
        {
            return false;
        }

        if (IdentityCache.TryGet(processId, out identity))
        {
            return true;
        }

        if (!NativeMethods.TryGetProcessPath(
                window,
                out var executablePath))
        {
            IdentityCache.Remove(processId);
            return false;
        }

        try
        {
            identity = FromExecutablePath(executablePath);
            IdentityCache.Set(processId, identity);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException or
            IOException or
            UnauthorizedAccessException)
        {
            IdentityCache.Remove(processId);
            Debug.WriteLine(
                $"SightAdapt could not resolve application identity: " +
                $"{exception}");
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
            Debug.WriteLine(
                $"SightAdapt could not read executable metadata: " +
                $"{exception}");
        }

        return Path.GetFileNameWithoutExtension(
            executableName);
    }
}
