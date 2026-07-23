namespace SightAdapt;

internal sealed class SettingsValidationException : Exception
{
    public SettingsValidationException(string message)
        : base(message)
    {
    }
}
