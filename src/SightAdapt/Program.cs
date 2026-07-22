using System.Runtime.Versioning;

namespace SightAdapt;

internal static class Program
{
    private const string SingleInstanceMutexName = @"Local\SightAdapt.SingleInstance";

    [STAThread]
    [SupportedOSPlatform("windows10.0.19041")]
    private static void Main()
    {
        using var singleInstanceMutex = new Mutex(
            initiallyOwned: true,
            name: SingleInstanceMutexName,
            createdNew: out var isFirstInstance);

        if (!isFirstInstance)
        {
            MessageBox.Show(
                "SightAdapt is already running in the notification area.",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        ToolStripManager.Renderer = new DarkMenuRenderer();

        if (!NativeMethods.MagInitialize())
        {
            MessageBox.Show(
                "Windows Magnification API could not be initialized. " +
                "SightAdapt requires a WDDM-capable graphics driver.",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        try
        {
            using var context = new SightAdaptContext();
            Application.Run(context);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"SightAdapt stopped because of an unexpected error.\n\n{exception.Message}",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            NativeMethods.MagUninitialize();
        }
    }
}
