namespace SightAdapt;

internal sealed class VisualProfileUseCases
{
    private readonly SettingsUseCaseContext _settings;

    public VisualProfileUseCases(
        SettingsCoordinator settingsCoordinator)
        : this(new SettingsUseCaseContext(
            settingsCoordinator ??
            throw new ArgumentNullException(
                nameof(settingsCoordinator))))
    {
    }

    internal VisualProfileUseCases(
        SettingsUseCaseContext settings)
    {
        _settings = settings ??
            throw new ArgumentNullException(nameof(settings));
    }

    public IReadOnlySightAdaptSettings Snapshot =>
        _settings.Snapshot;

    public event EventHandler<SettingsChangedEventArgs>? Changed
    {
        add => _settings.Changed += value;
        remove => _settings.Changed -= value;
    }

    public bool IsLocalChange(
        SettingsChangedEventArgs eventArgs) =>
        _settings.IsLocalChange(eventArgs);

    public string CreateAvailableName(string baseName)
    {
        var snapshot = Snapshot;
        return VisualProfileManagementService.CreateAvailableName(
            snapshot,
            baseName);
    }

    public SettingsCommitResult<string> Create(string name)
    {
        return _settings.Commit(settings =>
            VisualProfileManagementService.Create(
                settings,
                name).Id);
    }

    public SettingsCommitResult<string> Duplicate(
        string sourceProfileId,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceProfileId);

        return _settings.Commit(settings =>
            VisualProfileManagementService.Duplicate(
                settings,
                ProfileResolver.RequireVisualProfile(
                    settings,
                    sourceProfileId),
                name).Id);
    }

    public SettingsCommitResult<string> Rename(
        string profileId,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);

        return _settings.Commit(settings =>
        {
            VisualProfileManagementService.Rename(
                settings,
                ProfileResolver.RequireVisualProfile(
                    settings,
                    profileId),
                name);
            return profileId;
        });
    }

    public SettingsCommitResult<string> UpdateTuning(
        string profileId,
        VisualProfile values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentNullException.ThrowIfNull(values);

        return _settings.Commit(settings =>
        {
            VisualProfileManagementService.UpdateTuning(
                settings,
                ProfileResolver.RequireVisualProfile(
                    settings,
                    profileId),
                values);
            return profileId;
        });
    }

    public SettingsCommitResult<string> Delete(
        string profileId,
        string fallbackProfileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        ArgumentException.ThrowIfNullOrWhiteSpace(
            fallbackProfileId);

        return _settings.Commit(settings =>
        {
            VisualProfileManagementService.Delete(
                settings,
                ProfileResolver.RequireVisualProfile(
                    settings,
                    profileId),
                fallbackProfileId);
            return fallbackProfileId;
        });
    }
}
