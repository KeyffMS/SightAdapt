namespace SightAdapt.Demo;

internal sealed class SightAdaptSettings
{
    public bool AutomaticMode { get; set; } = true;

    public List<ApplicationProfile> Applications { get; set; } = [];
}

internal sealed class ApplicationProfile
{
    public string DisplayName { get; set; } = string.Empty;

    public string ExecutableName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public string Effect { get; set; } = "invert";

    public bool Matches(ApplicationIdentity identity)
    {
        if (!string.IsNullOrWhiteSpace(ExecutablePath) &&
            !string.IsNullOrWhiteSpace(identity.ExecutablePath))
        {
            return string.Equals(
                ExecutablePath,
                identity.ExecutablePath,
                StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            ExecutableName,
            identity.ExecutableName,
            StringComparison.OrdinalIgnoreCase);
    }
}

internal sealed record ApplicationIdentity(
    string DisplayName,
    string ExecutableName,
    string ExecutablePath);
