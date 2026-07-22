namespace SightAdapt;

internal static class AutomaticModeManagementService
{
    public static bool Set(SightAdaptSettings settings, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.AutomaticMode == enabled)
        {
            return false;
        }

        settings.AutomaticMode = enabled;
        return true;
    }

    public static bool Enable(SightAdaptSettings settings)
    {
        return Set(settings, true);
    }

    public static bool Disable(SightAdaptSettings settings)
    {
        return Set(settings, false);
    }
}
