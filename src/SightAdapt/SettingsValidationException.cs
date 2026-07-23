namespace SightAdapt;

internal sealed class SettingsValidationException :
    InvalidOperationException
{
    public SettingsValidationException(string message)
        : base(message)
    {
    }
}
