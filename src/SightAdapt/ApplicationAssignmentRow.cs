namespace SightAdapt;

internal sealed record ApplicationAssignmentRow(
    string ExecutablePath,
    bool Enabled,
    string DisplayName,
    string VisualProfileId,
    string MenuVisualProfileSelectorId,
    string OverlayScopeId,
    string ExecutableName);

internal static class ApplicationAssignmentRowMapper
{
    public static ApplicationAssignmentRow Map(
        ApplicationAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new ApplicationAssignmentRow(
            assignment.ExecutablePath,
            assignment.Enabled,
            assignment.DisplayName,
            assignment.VisualProfileId,
            ApplicationMenuProfilePolicy.ToSelectorId(
                assignment.MenuVisualProfileId),
            OverlayScopePolicy.ToId(assignment.OverlayScope),
            assignment.ExecutableName);
    }

    public static IReadOnlyList<ApplicationAssignmentRow> MapAll(
        IEnumerable<ApplicationAssignment> assignments)
    {
        ArgumentNullException.ThrowIfNull(assignments);
        return assignments.Select(Map).ToArray();
    }
}

internal abstract record ApplicationAssignmentChange(
    string ExecutablePath)
{
    public sealed record Enabled(
        string ExecutablePath,
        bool Value) : ApplicationAssignmentChange(ExecutablePath);

    public sealed record VisualProfile(
        string ExecutablePath,
        string ProfileId) : ApplicationAssignmentChange(ExecutablePath);

    public sealed record MenuVisualProfile(
        string ExecutablePath,
        string? ProfileId) : ApplicationAssignmentChange(ExecutablePath);

    public sealed record OverlayScope(
        string ExecutablePath,
        SightAdapt.OverlayScope Value) : ApplicationAssignmentChange(ExecutablePath);
}
