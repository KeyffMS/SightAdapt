namespace SightAdapt.Demo;

internal sealed class OverlayController : IDisposable
{
    private readonly VisualTransformCatalog _transformCatalog;
    private MagnifierOverlay? _overlay;
    private bool _disposed;

    public OverlayController(VisualTransformCatalog transformCatalog)
    {
        _transformCatalog = transformCatalog
            ?? throw new ArgumentNullException(nameof(transformCatalog));
    }

    public bool IsActive => _overlay is not null && !_overlay.IsDisposed;

    public nint TargetWindow => IsActive ? _overlay!.TargetHandle : nint.Zero;

    public string? VisualProfileId { get; private set; }

    public event EventHandler? OverlayClosed;

    public void Activate(nint targetWindow, VisualProfile visualProfile)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(visualProfile);

        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException("A target window is required.", nameof(targetWindow));
        }

        var transform = _transformCatalog.GetRequired(visualProfile.TransformId);
        var colorEffect = transform.CreateColorEffect(visualProfile);

        if (IsActive && TargetWindow == targetWindow)
        {
            _overlay!.ApplyColorEffect(colorEffect, transform.Id);
            VisualProfileId = visualProfile.Id;
            return;
        }

        Disable();

        var overlay = new MagnifierOverlay(targetWindow, colorEffect, transform.Id);
        overlay.FormClosed += HandleOverlayClosed;

        try
        {
            overlay.Show();
            _overlay = overlay;
            VisualProfileId = visualProfile.Id;
        }
        catch
        {
            overlay.FormClosed -= HandleOverlayClosed;
            overlay.Dispose();
            throw;
        }
    }

    public void Disable()
    {
        if (_overlay is null)
        {
            VisualProfileId = null;
            return;
        }

        var overlay = _overlay;
        _overlay = null;
        VisualProfileId = null;

        overlay.FormClosed -= HandleOverlayClosed;
        overlay.Close();
        overlay.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Disable();
        _disposed = true;
    }

    private void HandleOverlayClosed(object? sender, FormClosedEventArgs eventArgs)
    {
        if (!ReferenceEquals(sender, _overlay))
        {
            return;
        }

        var overlay = _overlay!;
        _overlay = null;
        VisualProfileId = null;

        overlay.FormClosed -= HandleOverlayClosed;
        overlay.Dispose();
        OverlayClosed?.Invoke(this, EventArgs.Empty);
    }
}
