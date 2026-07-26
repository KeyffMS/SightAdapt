namespace SightAdapt;

internal sealed class VisualProfileUseCases
{
    private readonly SettingsCoordinator _settingsCoordinator;

    public VisualProfileUseCases(
        SettingsCoordinator settingsCoordinator)
    {
        _settingsCoordinator = settingsCoordinator ??
            throw new ArgumentNullException(
                nameof(settingsCoordinator));
    }

    public SightAdaptSettings Snapshot =>
        _settingsCoordinator.Current;

    public event EventHandler? Changed
    {
        add => _settingsCoordinator.Changed += value;
        remove => _settingsCoordinator.Changed -= value;
    }

    public string CreateAvailableName(string baseName)
    {
        var snapshot = Snapshot;
        return VisualProfileManagementService.CreateAvailableName(
            snapshot,
            baseName);
    }

    public SettingsCommitResult<string> Create(string name)
    {
        return _settingsCoordinator.Commit(settings =>
            VisualProfileManagementService.Create(
                settings,
                name).Id);
    }

    public SettingsCommitResult<string> Duplicate(
        string sourceProfileId,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceProfileId);

        return _settingsCoordinator.Commit(settings =>
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

        return _settingsCoordinator.Commit(settings =>
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

        return _settingsCoordinator.Commit(settings =>
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

        return _settingsCoordinator.Commit(settings =>
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
