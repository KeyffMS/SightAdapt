namespace SightAdapt;

internal sealed class ConfigurationUseCases
{
    private readonly SettingsUseCaseContext _settings;

    public ConfigurationUseCases(
        SettingsCoordinator settingsCoordinator)
        : this(new SettingsUseCaseContext(
            settingsCoordinator ??
            throw new ArgumentNullException(
                nameof(settingsCoordinator))))
    {
    }

    internal ConfigurationUseCases(
        SettingsUseCaseContext settings)
    {
        _settings = settings ??
            throw new ArgumentNullException(nameof(settings));
    }

    public IReadOnlySightAdaptSettings Snapshot =>
        _settings.Snapshot;

    public string SettingsPath =>
        _settings.SettingsPath;

    public event EventHandler? Changed
    {
        add => _settings.Changed += value;
        remove => _settings.Changed -= value;
    }

    public SettingsCommitResult SetAutomaticMode(bool enabled)
    {
        return _settings.Commit(settings =>
        {
            AutomaticModeManagementService.Set(
                settings,
                enabled);
        });
    }

    public SettingsCommitResult Apply(
        ApplicationAssignmentChange change)
    {
        ArgumentNullException.ThrowIfNull(change);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            change.ExecutablePath);

        return _settings.Commit(settings =>
        {
            var assignment =
                ProfileResolver.RequireAssignmentByExecutablePath(
                    settings,
                    change.ExecutablePath);
            switch (change)
            {
                case ApplicationAssignmentChange.Enabled enabled:
                    ApplicationAssignmentService.SetEnabled(
                        settings,
                        assignment,
                        enabled.Value);
                    break;
                case ApplicationAssignmentChange.VisualProfile visual:
                    ApplicationAssignmentService.AssignVisualProfile(
                        settings,
                        assignment,
                        visual.ProfileId);
                    break;
                case ApplicationAssignmentChange.MenuVisualProfile menu:
                    ApplicationAssignmentService.AssignMenuVisualProfile(
                        settings,
                        assignment,
                        menu.ProfileId);
                    break;
                case ApplicationAssignmentChange.OverlayScope scope:
                    ApplicationAssignmentService.SetOverlayScope(
                        settings,
                        assignment,
                        scope.Value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(change));
            }
        });
    }

    public SettingsCommitResult<bool> AddOrEnable(
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(identity);

        return _settings.Commit(settings =>
        {
            var result =
                ApplicationAssignmentService.AddOrEnable(
                    settings,
                    identity);
            AutomaticModeManagementService.Enable(settings);
            return result.WasCreated;
        });
    }

    public SettingsCommitResult Remove(string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);

        return _settings.Commit(settings =>
            ApplicationAssignmentService.Remove(
                settings,
                ProfileResolver.RequireAssignmentByExecutablePath(
                    settings,
                    executablePath)));
    }

    public SettingsCommitResult UpdateTuning(
        string profileId,
        VisualProfile values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(values);

        return _settings.Commit(settings =>
            VisualProfileManagementService.UpdateTuning(
                settings,
                ProfileResolver.RequireVisualProfile(
                    settings,
                    profileId),
                values));
    }
}
