namespace SightAdapt;

internal sealed class SettingsUseCaseContext
{
    private readonly SettingsCoordinator _coordinator;

    public SettingsUseCaseContext(
        SettingsCoordinator coordinator)
    {
        _coordinator = coordinator ??
            throw new ArgumentNullException(nameof(coordinator));
    }

    public IReadOnlySightAdaptSettings Snapshot =>
        _coordinator.Current;

    public string SettingsPath =>
        _coordinator.SettingsPath;

    public event EventHandler? Changed
    {
        add => _coordinator.Changed += value;
        remove => _coordinator.Changed -= value;
    }

    public SettingsCommitResult Commit(
        Action<SightAdaptSettings> mutation) =>
        _coordinator.Commit(mutation);

    public SettingsCommitResult<T> Commit<T>(
        Func<SightAdaptSettings, T> mutation) =>
        _coordinator.Commit(mutation);
}
