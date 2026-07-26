namespace SightAdapt;

internal sealed class OverlayActivationRequest
{
    public OverlayActivationRequest(
        nint targetWindow,
        VisualProfile visualProfile,
        VisualProfile menuVisualProfile,
        OverlayScope overlayScope)
    {
        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException(
                "A target window is required.",
                nameof(targetWindow));
        }

        if (!OverlayScopePolicy.IsSupported(overlayScope))
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");
        }

        TargetWindow = targetWindow;
        VisualProfile = visualProfile ??
            throw new ArgumentNullException(nameof(visualProfile));
        MenuVisualProfile = menuVisualProfile ??
            throw new ArgumentNullException(nameof(menuVisualProfile));
        OverlayScope = overlayScope;
    }

    public nint TargetWindow { get; }

    public VisualProfile VisualProfile { get; }

    public VisualProfile MenuVisualProfile { get; }

    public OverlayScope OverlayScope { get; }
}

internal interface IRuntimeOverlay
{
    bool IsActive { get; }

    nint TargetWindow { get; }

    void Activate(OverlayActivationRequest request);

    void Disable();
}
