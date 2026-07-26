namespace SightAdapt;

internal interface IRuntimeEnvironment
{
    nint ResolveTargetWindow();

    bool IsSupportedTarget(nint targetWindow);

    ApplicationIdentity? ResolveIdentity(nint targetWindow);

    void ShowNotification(string message);

    void SynchronizeAutomaticMode(bool enabled);
}

internal sealed class DelegateRuntimeEnvironment : IRuntimeEnvironment
{
    private readonly Func<nint> _resolveTargetWindow;
    private readonly Func<nint, bool> _isSupportedTarget;
    private readonly Func<nint, ApplicationIdentity?> _resolveIdentity;
    private readonly Action<string> _showNotification;
    private readonly Action<bool> _synchronizeAutomaticMode;

    public DelegateRuntimeEnvironment(
        Func<nint> resolveTargetWindow,
        Func<nint, bool> isSupportedTarget,
        Func<nint, ApplicationIdentity?> resolveIdentity,
        Action<string> showNotification,
        Action<bool> synchronizeAutomaticMode)
    {
        _resolveTargetWindow = resolveTargetWindow ??
            throw new ArgumentNullException(nameof(resolveTargetWindow));
        _isSupportedTarget = isSupportedTarget ??
            throw new ArgumentNullException(nameof(isSupportedTarget));
        _resolveIdentity = resolveIdentity ??
            throw new ArgumentNullException(nameof(resolveIdentity));
        _showNotification = showNotification ??
            throw new ArgumentNullException(nameof(showNotification));
        _synchronizeAutomaticMode = synchronizeAutomaticMode ??
            throw new ArgumentNullException(nameof(synchronizeAutomaticMode));
    }

    public nint ResolveTargetWindow() =>
        _resolveTargetWindow();

    public bool IsSupportedTarget(nint targetWindow) =>
        _isSupportedTarget(targetWindow);

    public ApplicationIdentity? ResolveIdentity(nint targetWindow) =>
        _resolveIdentity(targetWindow);

    public void ShowNotification(string message) =>
        _showNotification(message);

    public void SynchronizeAutomaticMode(bool enabled) =>
        _synchronizeAutomaticMode(enabled);
}

internal static class RuntimeMessages
{
    public const string NoSupportedWindow =
        "No supported application window is currently available.";

    public const string IdentityUnavailable =
        "The active application's executable path could not be read. " +
        "Use the configuration panel to select its .exe file.";

    public const string SettingsChangeFailed =
        "Settings could not be changed.";

    public static string AssignmentToggled(
        ApplicationAssignmentToggleNotification result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsEnabled
            ? result.WasCreated
                ? $"{VisualProfilePolicy.NewAssignmentProfileName} " +
                  $"profile added and enabled: {result.DisplayName}."
                : $"Automatic profile enabled: {result.DisplayName}."
            : $"Automatic profile disabled: {result.DisplayName}.";
    }

    public static string OverlayCreationFailed(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"Could not create the overlay: {exception.Message}";
    }
}
