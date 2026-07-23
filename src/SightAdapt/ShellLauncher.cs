using System.ComponentModel;
using System.Diagnostics;

namespace SightAdapt;

internal static class ShellLauncher
{
    public static bool TryOpenUrl(
        IWin32Window owner,
        string url)
    {
        return TryOpenUrl(
            owner,
            url,
            startInfo =>
            {
                Process.Start(startInfo);
            },
            ShowError);
    }

    internal static bool TryOpenUrl(
        IWin32Window owner,
        string url,
        Action<ProcessStartInfo> start,
        Action<IWin32Window, string> showError)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(showError);

        if (!TryCreateStartInfo(url, out var startInfo))
        {
            showError(
                owner,
                "The link is not a supported web address.");
            return false;
        }

        try
        {
            start(startInfo);
            return true;
        }
        catch (Exception exception) when (
            exception is Win32Exception or
            InvalidOperationException)
        {
            Debug.WriteLine(
                $"SightAdapt could not open '{url}': {exception}");
            showError(
                owner,
                $"The link could not be opened.\n\n{exception.Message}");
            return false;
        }
    }

    internal static bool TryCreateStartInfo(
        string? url,
        out ProcessStartInfo startInfo)
    {
        startInfo = null!;
        if (!Uri.TryCreate(
                url?.Trim(),
                UriKind.Absolute,
                out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        startInfo = new ProcessStartInfo(uri.AbsoluteUri)
        {
            UseShellExecute = true,
        };
        return true;
    }

    private static void ShowError(
        IWin32Window owner,
        string message)
    {
        MessageBox.Show(
            owner,
            message,
            ProductInfo.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}