namespace SightAdapt;

internal sealed class BuiltInVisualProfileNormalizationPass :
    ISettingsNormalizationPass
{
    public void Apply(SettingsNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var definition in
                 VisualProfileCatalog.Default.Definitions)
        {
            var profile = context.TakeProfile(
                definition.ProfileId);
            if (profile is null)
            {
                profile = definition.CreateBuiltInProfile();
                context.MarkChanged();
            }
            else if (definition.CanonicalizeBuiltInProfile(
                         profile))
            {
                context.MarkChanged();
            }

            context.AddProfile(profile);
        }
    }
}

internal sealed class UserVisualProfileNormalizationPass :
    ISettingsNormalizationPass
{
    public void Apply(SettingsNormalizationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        foreach (var profile in context.RemainingProfiles)
        {
            NormalizeIdentity(context, profile);
            NormalizeName(context, profile);

            if (VisualProfileCatalog.Default
                .NormalizeTuningForTransform(profile))
            {
                context.MarkChanged();
            }

            context.AddProfile(profile);
        }
    }

    private static void NormalizeIdentity(
        SettingsNormalizationContext context,
        VisualProfile profile)
    {
        var normalizedId =
            (profile.Id ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedId) ||
            VisualProfileCatalog.Default.IsBuiltInId(
                normalizedId) ||
            ApplicationMenuProfilePolicy.IsReservedProfileId(
                normalizedId) ||
            context.ProfileIds.Contains(normalizedId))
        {
            normalizedId = VisualProfilePolicy
                .CreateUserProfileId(context.ProfileIds);
            context.MarkChanged();
        }

        if (!string.Equals(
                profile.Id,
                normalizedId,
                StringComparison.Ordinal))
        {
            profile.Id = normalizedId;
            context.MarkChanged();
        }

        var normalizedTransformId =
            (profile.TransformId ?? string.Empty)
                .Trim()
                .ToLowerInvariant();
        if (!VisualProfileCatalog.Default
            .IsSupportedTransform(normalizedTransformId))
        {
            normalizedTransformId =
                SoftInvertVisualTransform.TransformId;
            context.MarkChanged();
        }

        if (!string.Equals(
                profile.TransformId,
                normalizedTransformId,
                StringComparison.Ordinal))
        {
            profile.TransformId = normalizedTransformId;
            context.MarkChanged();
        }
    }

    private static void NormalizeName(
        SettingsNormalizationContext context,
        VisualProfile profile)
    {
        var normalizedName = VisualProfilePolicy
            .NormalizeNameOrFallback(
                profile.Name,
                VisualProfilePolicy.CustomProfileBaseName);
        if (context.Profiles.Any(
                candidate => string.Equals(
                    candidate.Name,
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase)))
        {
            normalizedName = VisualProfilePolicy
                .CreateUniqueName(
                    context.Profiles,
                    normalizedName);
            context.MarkChanged();
        }

        if (!string.Equals(
                profile.Name,
                normalizedName,
                StringComparison.Ordinal))
        {
            profile.Name = normalizedName;
            context.MarkChanged();
        }
    }
}
