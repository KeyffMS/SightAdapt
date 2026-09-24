namespace SightAdapt;

internal sealed class SettingsUseCaseContext
{
    private readonly SettingsCoordinator _coordinator;
    private readonly object _changeOrigin = new();

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

    public event EventHandler<SettingsChangedEventArgs>? Changed
    {
        add => _coordinator.Changed += value;
        remove => _coordinator.Changed -= value;
    }

    public bool IsLocalChange(
        SettingsChangedEventArgs eventArgs)
    {
        ArgumentNullException.ThrowIfNull(eventArgs);
        return ReferenceEquals(
            eventArgs.Origin,
            _changeOrigin);
    }

    public SettingsCommitResult Commit(
        Action<SightAdaptSettings> mutation) =>
        _coordinator.Commit(
            mutation,
            _changeOrigin);

    public SettingsCommitResult<T> Commit<T>(
        Func<SightAdaptSettings, T> mutation) =>
        _coordinator.Commit(
            mutation,
            _changeOrigin);
}
