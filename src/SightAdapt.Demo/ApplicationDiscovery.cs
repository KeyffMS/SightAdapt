using System.Diagnostics;

namespace SightAdapt.Demo;

internal static class ApplicationDiscovery
{
    public static bool TryGetIdentity(nint window, out ApplicationIdentity identity)
    {
        identity = null!;

        if (!NativeMethods.TryGetProcessPath(window, out var executablePath))
        {
            return false;
        }

        try
        {
            identity = FromExecutablePath(executablePath);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static ApplicationIdentity FromExecutablePath(string executablePath)
    {
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            throw new ArgumentException("An executable path is required.", nameof(executablePath));
        }

        var fullPath = Path.GetFullPath(executablePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException("The selected executable does not exist.", fullPath);
        }

        var executableName = Path.GetFileName(fullPath);
        if (string.IsNullOrWhiteSpace(executableName))
        {
            throw new ArgumentException("The executable name could not be resolved.", nameof(executablePath));
        }

        return new ApplicationIdentity(
            GetDisplayName(fullPath, executableName),
            executableName,
            fullPath);
    }

    private static string GetDisplayName(string executablePath, string executableName)
    {
        try
        {
            var description = FileVersionInfo.GetVersionInfo(executablePath).FileDescription;
            if (!string.IsNullOrWhiteSpace(description))
            {
                return description.Trim();
            }
        }
        catch (FileNotFoundException)
        {
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }

        return Path.GetFileNameWithoutExtension(executableName);
    }
}
