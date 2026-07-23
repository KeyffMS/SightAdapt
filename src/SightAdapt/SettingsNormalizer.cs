namespace SightAdapt;

internal static class SettingsNormalizer
{
    public static bool Normalize(
        SightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var schemaChanged =
            settings.SchemaVersion !=
            SightAdaptSettings.CurrentSchemaVersion;
        settings.SchemaVersion =
            SightAdaptSettings.CurrentSchemaVersion;
        settings.EnsureCollections();

        var context =
            new SettingsNormalizationContext(settings);
        CanonicalizeBuiltInProfiles(context);
        NormalizeCustomProfiles(context);
        NormalizeApplicationOverlayScopes(context);
        NormalizeApplications(context);
        RepairProfileReferences(context);
        context.Commit(settings);

        return schemaChanged || context.Changed;
    }

    private static void CanonicalizeBuiltInProfiles(
        SettingsNormalizationContext context)
    {
        var exactInvert = TakeProfile(
            context.RemainingProfiles,
            VisualProfile.DefaultInvertId);

        if (exactInvert is null)
        {
            exactInvert =
                VisualProfile.CreateDefaultInvert();
            context.MarkChanged();
        }

        if (VisualProfileDefaults
            .CanonicalizeExactInvert(exactInvert))
        {
            context.MarkChanged();
        }

        context.AddProfile(exactInvert);

        var softInvert = TakeProfile(
            context.RemainingProfiles,
            VisualProfile.DefaultSoftInvertId);

        if (softInvert is null)
        {
            softInvert =
                VisualProfile.CreateDefaultSoftInvert();
            context.MarkChanged();
        }

        if (VisualProfileDefaults
            .CanonicalizeSoftInvert(softInvert))
        {
            context.MarkChanged();
        }

        context.AddProfile(softInvert);
    }

    private static void NormalizeCustomProfiles(
        SettingsNormalizationContext context)
    {
        foreach (var profile in
                 context.RemainingProfiles)
        {
            NormalizeCustomProfileIdentity(
                context,
                profile);
            NormalizeCustomProfileName(
                context,
                profile);

            if (VisualProfileDefaults
                .NormalizeTuningForTransform(profile))
            {
                context.MarkChanged();
            }

            context.AddProfile(profile);
        }
    }

    private static void NormalizeCustomProfileIdentity(
        SettingsNormalizationContext context,
        VisualProfile profile)
    {
        var normalizedId =
            (profile.Id ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(
                normalizedId) ||
            VisualProfilePolicy.IsBuiltInId(
                normalizedId) ||
            context.ProfileIds.Contains(
                normalizedId))
        {
            normalizedId =
                VisualProfilePolicy
                    .CreateUserProfileId(
                        context.ProfileIds);
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

        if (!VisualProfilePolicy
            .IsSupportedTransformId(
                normalizedTransformId))
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
            profile.TransformId =
                normalizedTransformId;
            context.MarkChanged();
        }
    }

    private static void NormalizeCustomProfileName(
        SettingsNormalizationContext context,
        VisualProfile profile)
    {
        var normalizedName =
            VisualProfilePolicy
                .NormalizeNameOrFallback(
                    profile.Name,
                    VisualProfilePolicy
                        .CustomProfileBaseName);

        if (context.Profiles.Any(
                candidate => string.Equals(
                    candidate.Name,
                    normalizedName,
                    StringComparison.OrdinalIgnoreCase)))
        {
            normalizedName =
                VisualProfilePolicy
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

    private static void NormalizeApplicationOverlayScopes(
        SettingsNormalizationContext context)
    {
        foreach (var application in
                 context.SourceApplications)
        {
            var persistedId =
                application.OverlayScopeId ??
                string.Empty;
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
                continue;
            }

            application.OverlayScopeId = canonicalId;
            context.MarkChanged();
        }
    }

    private static void NormalizeApplications(
        SettingsNormalizationContext context)
    {
        foreach (var application in
                 context.SourceApplications)
        {
            if (NormalizeApplicationStrings(
                application))
            {
                context.MarkChanged();
            }

            MigrateLegacyEffect(
                context,
                application);

            if (string.IsNullOrWhiteSpace(
                    application.ExecutablePath))
            {
                context.MarkChanged();
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    application.ExecutableName))
            {
                application.ExecutableName =
                    Path.GetFileName(
                        application.ExecutablePath) ??
                    string.Empty;
                context.MarkChanged();
            }

            if (string.IsNullOrWhiteSpace(
                    application.ExecutableName) ||
                !context.ExecutablePaths.Add(
                    application.ExecutablePath))
            {
                context.MarkChanged();
                continue;
            }

            if (string.IsNullOrWhiteSpace(
                    application.DisplayName))
            {
                application.DisplayName =
                    Path.GetFileNameWithoutExtension(
                        application.ExecutableName) ??
                    string.Empty;
                context.MarkChanged();
            }

            context.Applications.Add(application);
        }
    }

    private static void RepairProfileReferences(
        SettingsNormalizationContext context)
    {
        foreach (var application in
                 context.Applications)
        {
            if (!string.IsNullOrWhiteSpace(
                    application.VisualProfileId) &&
                context.ProfileIds.Contains(
                    application.VisualProfileId))
            {
                continue;
            }

            application.VisualProfileId =
                VisualProfilePolicy
                    .MissingReferenceFallbackProfileId;
            context.MarkChanged();
        }
    }

    private static void MigrateLegacyEffect(
        SettingsNormalizationContext context,
        ApplicationProfile application)
    {
        if (!string.IsNullOrWhiteSpace(
                application.LegacyEffect))
        {
            application.VisualProfileId =
                VisualProfile.DefaultInvertId;
            application.LegacyEffect = null;
            context.MarkChanged();
        }
        else if (application.LegacyEffect is not null)
        {
            application.LegacyEffect = null;
            context.MarkChanged();
        }
    }

    private static VisualProfile? TakeProfile(
        List<VisualProfile> profiles,
        string profileId)
    {
        var profile = profiles.FirstOrDefault(
            candidate => string.Equals(
                candidate.Id,
                profileId,
                StringComparison.OrdinalIgnoreCase));

        if (profile is not null)
        {
            profiles.Remove(profile);
        }

        return profile;
    }

    private static bool NormalizeApplicationStrings(
        ApplicationProfile application)
    {
        var displayName =
            (application.DisplayName ??
             string.Empty).Trim();
        var executableName =
            (application.ExecutableName ??
             string.Empty).Trim();
        var executablePath =
            (application.ExecutablePath ??
             string.Empty).Trim();
        var visualProfileId =
            (application.VisualProfileId ??
             string.Empty).Trim();

        var changed = !string.Equals(
                application.DisplayName,
                displayName,
                StringComparison.Ordinal) ||
            !string.Equals(
                application.ExecutableName,
                executableName,
                StringComparison.Ordinal) ||
            !string.Equals(
                application.ExecutablePath,
                executablePath,
                StringComparison.Ordinal) ||
            !string.Equals(
                application.VisualProfileId,
                visualProfileId,
                StringComparison.Ordinal);

        application.DisplayName = displayName;
        application.ExecutableName = executableName;
        application.ExecutablePath = executablePath;
        application.VisualProfileId =
            visualProfileId;
        return changed;
    }

    private sealed class SettingsNormalizationContext
    {
        private readonly List<VisualProfile>
            _originalProfiles;
        private readonly List<ApplicationProfile>
            _originalApplications;

        public SettingsNormalizationContext(
            SightAdaptSettings settings)
        {
            _originalProfiles =
                settings.VisualProfiles;
            _originalApplications =
                settings.Applications;
            RemainingProfiles =
                settings.VisualProfiles
                    .OfType<VisualProfile>()
                    .ToList();
            SourceApplications =
                settings.Applications
                    .OfType<ApplicationProfile>()
                    .ToList();

            Changed =
                RemainingProfiles.Count !=
                    settings.VisualProfiles.Count ||
                SourceApplications.Count !=
                    settings.Applications.Count;
        }

        public bool Changed { get; private set; }

        public List<VisualProfile>
            RemainingProfiles { get; }

        public List<ApplicationProfile>
            SourceApplications { get; }

        public List<VisualProfile> Profiles { get; } = [];

        public List<ApplicationProfile>
            Applications { get; } = [];

        public HashSet<string> ProfileIds { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public HashSet<string> ExecutablePaths { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public void AddProfile(
            VisualProfile profile)
        {
            Profiles.Add(profile);
            ProfileIds.Add(profile.Id);
        }

        public void MarkChanged()
        {
            Changed = true;
        }

        public void Commit(
            SightAdaptSettings settings)
        {
            if (!_originalProfiles
                    .SequenceEqual(Profiles) ||
                !_originalApplications
                    .SequenceEqual(Applications))
            {
                Changed = true;
            }

            settings.VisualProfiles = Profiles;
            settings.Applications = Applications;
        }
    }
}
