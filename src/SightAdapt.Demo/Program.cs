using System.Runtime.Versioning;

namespace SightAdapt.Demo;

internal static class Program
{
    [STAThread]
    [SupportedOSPlatform("windows10.0.19041")]
    private static void Main()
    {
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        if (!NativeMethods.MagInitialize())
        {
            MessageBox.Show(
                "Windows Magnification API could not be initialized. " +
                "SightAdapt Demo requires a WDDM-capable graphics driver.",
                "SightAdapt Demo 0.2",
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
                $"SightAdapt Demo stopped because of an unexpected error.\n\n{exception.Message}",
                "SightAdapt Demo 0.2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        finally
        {
            NativeMethods.MagUninitialize();
        }
    }
}
