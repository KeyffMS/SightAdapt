namespace SightAdapt;

internal sealed class ApplicationAssignmentNormalizationPass :
    ISettingsNormalizationPass
{
    public void Apply(SettingsNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var assignment in context.SourceAssignments)
        {
            NormalizeStrings(context, assignment);
            NormalizeOverlayScope(context, assignment);

            if (string.IsNullOrWhiteSpace(
                    assignment.ExecutablePath))
            {
                context.MarkChanged();
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    assignment.ExecutableName))
            {
                assignment.ExecutableName =
                    Path.GetFileName(
                        assignment.ExecutablePath) ??
                    string.Empty;
                context.MarkChanged();
            }

            if (string.IsNullOrWhiteSpace(
                    assignment.ExecutableName) ||
                !context.ExecutablePaths.Add(
                    assignment.ExecutablePath))
            {
                context.MarkChanged();
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    assignment.DisplayName))
            {
                assignment.DisplayName =
                    Path.GetFileNameWithoutExtension(
                        assignment.ExecutableName) ??
                    string.Empty;
                context.MarkChanged();
            }

            context.Assignments.Add(assignment);
        }
    }

    private static void NormalizeStrings(
        SettingsNormalizationContext context,
        ApplicationAssignment assignment)
    {
        var displayName =
            (assignment.DisplayName ?? string.Empty).Trim();
        var executableName =
            (assignment.ExecutableName ?? string.Empty).Trim();
        var executablePath =
            (assignment.ExecutablePath ?? string.Empty).Trim();
        var visualProfileId =
            (assignment.VisualProfileId ?? string.Empty).Trim();
        var menuVisualProfileId =
            ApplicationMenuProfilePolicy.FromSelectorId(
                assignment.MenuVisualProfileId);

        if (!string.Equals(
                assignment.DisplayName,
                displayName,
                StringComparison.Ordinal) ||
            !string.Equals(
                assignment.ExecutableName,
                executableName,
                StringComparison.Ordinal) ||
            !string.Equals(
                assignment.ExecutablePath,
                executablePath,
                StringComparison.Ordinal) ||
            !string.Equals(
                assignment.VisualProfileId,
                visualProfileId,
                StringComparison.Ordinal) ||
            !string.Equals(
                assignment.MenuVisualProfileId,
                menuVisualProfileId,
                StringComparison.Ordinal))
        {
            context.MarkChanged();
        }

        assignment.DisplayName = displayName;
        assignment.ExecutableName = executableName;
        assignment.ExecutablePath = executablePath;
        assignment.VisualProfileId = visualProfileId;
        assignment.MenuVisualProfileId = menuVisualProfileId;
    }

    private static void NormalizeOverlayScope(
        SettingsNormalizationContext context,
        ApplicationAssignment assignment)
    {
        var persistedId =
            assignment.OverlayScopeId ?? string.Empty;
        OverlayScopePolicy.TryParseId(
            persistedId,
            out var scope);
        var canonicalId =
            OverlayScopePolicy.ToId(scope);

        if (string.Equals(
                persistedId,
                canonicalId,
                StringComparison.Ordinal))
        {
            return;
        }

        assignment.OverlayScopeId = canonicalId;
        context.MarkChanged();
    }
}

internal sealed class ProfileReferenceNormalizationPass :
    ISettingsNormalizationPass
{
    public void Apply(SettingsNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var assignment in context.Assignments)
        {
            if (string.IsNullOrWhiteSpace(
                    assignment.VisualProfileId) ||
                !context.ProfileIds.Contains(
                    assignment.VisualProfileId))
            {
                assignment.VisualProfileId =
                    VisualProfilePolicy
                        .MissingReferenceFallbackProfileId;
                context.MarkChanged();
            }

            if (assignment.MenuVisualProfileId is not null &&
                !context.ProfileIds.Contains(
                    assignment.MenuVisualProfileId))
            {
                assignment.MenuVisualProfileId = null;
                context.MarkChanged();
            }
        }
    }
}
