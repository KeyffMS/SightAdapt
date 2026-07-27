namespace SightAdapt;

internal sealed class OverlayController :
    IRuntimeOverlay,
    IDisposable
{
    private readonly VisualProfileCatalog _profileCatalog;
    private readonly OverlaySession _session;
    private bool _disposed;

    public OverlayController(
        VisualProfileCatalog profileCatalog)
        : this(
            profileCatalog,
            new OverlaySession(
                new Win32MenuWindowTracker(),
                new MagnifierOverlayWindowFactory()))
    {
    }

    internal OverlayController(
        VisualProfileCatalog profileCatalog,
        IWin32MenuWindowTracker menuWindowTracker)
        : this(
            profileCatalog,
            new OverlaySession(
                menuWindowTracker,
                new MagnifierOverlayWindowFactory()))
    {
    }

    internal OverlayController(
        VisualProfileCatalog profileCatalog,
        OverlaySession session)
    {
        _profileCatalog = profileCatalog ??
            throw new ArgumentNullException(
                nameof(profileCatalog));
        _session = session ??
            throw new ArgumentNullException(nameof(session));
        _session.Closed += SessionClosed;
    }

    public event EventHandler? OverlayClosed;

    public bool IsActive => _session.IsActive;

    public nint TargetWindow => _session.TargetWindow;

    public void Activate(OverlayActivationRequest request)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        _session.Activate(
            request.TargetWindow,
            ResolveEffect(request.VisualProfile),
            ResolveEffect(request.MenuVisualProfile),
            request.OverlayScope);
    }

    public void Disable()
    {
        _session.Disable();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _session.Closed -= SessionClosed;
        _session.Dispose();
        _disposed = true;
    }

    private ResolvedVisualEffect ResolveEffect(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        var transform =
            _profileCatalog.GetRequiredTransform(
                profile.TransformId);
        return new ResolvedVisualEffect(
            transform.Id,
            transform.CreateColorEffect(profile));
    }

    private void SessionClosed(
        object? sender,
        EventArgs eventArgs)
    {
        OverlayClosed?.Invoke(this, EventArgs.Empty);
    }
}
