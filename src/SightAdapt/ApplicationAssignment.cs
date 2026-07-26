namespace SightAdapt;

internal sealed class ApplicationAssignment
{
    public string DisplayName { get; set; } = string.Empty;

    public string ExecutableName { get; set; } = string.Empty;

    public string ExecutablePath { get; set; } = string.Empty;

    public bool Enabled { get; set; } = true;

    public string VisualProfileId { get; set; } =
        VisualProfilePolicy.NewAssignmentProfileId;

    public string? MenuVisualProfileId { get; set; }

    private string _overlayScopeId =
        OverlayScopePolicy.ToId(OverlayScopePolicy.Default);

    public string OverlayScopeId
    {
        get => _overlayScopeId;
        set => _overlayScopeId = value ?? string.Empty;
    }

    public OverlayScope OverlayScope =>
        OverlayScopePolicy.ParseRequired(OverlayScopeId);

    public ApplicationAssignment CreateWorkingCopy()
    {
        return new ApplicationAssignment
        {
            DisplayName = DisplayName,
            ExecutableName = ExecutableName,
            ExecutablePath = ExecutablePath,
            Enabled = Enabled,
            VisualProfileId = VisualProfileId,
            MenuVisualProfileId = MenuVisualProfileId,
            OverlayScopeId = OverlayScopeId,
        };
    }

    public bool Matches(ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

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
