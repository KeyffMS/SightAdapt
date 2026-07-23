using System.Drawing;

namespace SightAdapt;

internal sealed class MagnifierOverlay : Form
{
    internal const int ForegroundTransitionGraceMilliseconds = 125;

    private readonly System.Windows.Forms.Timer _updateTimer;
    private MagColorEffect _colorEffect;
    private string _transformId;
    private OverlayScope _overlayScope;
    private nint _magnifierWindow;
    private long _transitionStartedAt = -1;
    private bool _hasRenderedFrame;
    private bool _initialized;

    public MagnifierOverlay(
        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId,
        OverlayScope overlayScope)
    {
        TargetHandle = ValidateTarget(targetHandle);
        _colorEffect = colorEffect;
        _transformId = NormalizeTransformId(transformId);
        _overlayScope = ValidateOverlayScope(overlayScope);

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        _updateTimer = new System.Windows.Forms.Timer
        {
            Interval = 33,
        };
        _updateTimer.Tick += (_, _) => UpdateOverlay();
    }

    public nint TargetHandle { get; private set; }

    public OverlayScope OverlayScope => _overlayScope;

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            var parameters = base.CreateParams;
            parameters.ExStyle |=
                NativeMethods.WsExLayered |
                NativeMethods.WsExTransparent |
                NativeMethods.WsExToolWindow |
                NativeMethods.WsExNoActivate;
            return parameters;
        }
    }

    public void Retarget(
        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId,
        OverlayScope overlayScope)
    {
        TargetHandle = ValidateTarget(targetHandle);
        _colorEffect = colorEffect;
        _transformId = NormalizeTransformId(transformId);
        _overlayScope = ValidateOverlayScope(overlayScope);
        ResetTransitionGrace();

        if (!_initialized)
        {
            return;
        }

        ApplyColorEffectToMagnifier();
        UpdateOverlay();
    }

    protected override void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);

        NativeCall.RequireSuccess(
            NativeMethods.SetLayeredWindowAttributes(
                Handle,
                0,
                255,
                NativeMethods.LwaAlpha),
            "Set layered overlay opacity");

        _magnifierWindow = NativeCall.RequireHandle(
            NativeMethods.CreateWindowEx(
                0,
                NativeMethods.WcMagnifier,
                "SightAdapt Magnifier",
                NativeMethods.WsChild |
                    NativeMethods.WsVisible,
                0,
                0,
                Math.Max(ClientSize.Width, 1),
                Math.Max(ClientSize.Height, 1),
                Handle,
                nint.Zero,
                nint.Zero,
                nint.Zero),
            "Create Windows magnifier control");

        var transform = MagTransform.Identity;
        NativeCall.RequireSuccess(
            NativeMethods.MagSetWindowTransform(
                _magnifierWindow,
                ref transform),
            "Initialize magnifier transform");

        ApplyColorEffectToMagnifier();

        var excludedWindows = new[] { Handle };
        NativeCall.RequireSuccess(
            NativeMethods.MagSetWindowFilterList(
                _magnifierWindow,
                NativeMethods.MwFilterModeExclude,
                excludedWindows.Length,
                excludedWindows),
            "Exclude overlay window from magnifier source");

        _initialized = true;
        _updateTimer.Start();
        UpdateOverlay();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateTimer.Stop();
            _updateTimer.Dispose();
        }

        if (_magnifierWindow != nint.Zero)
        {
            NativeCall.BestEffort(
                NativeMethods.DestroyWindow(
                    _magnifierWindow),
                "Destroy magnifier control");
            _magnifierWindow = nint.Zero;
        }

        base.Dispose(disposing);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeMethods.WmNcHitTest)
        {
            message.Result = (nint)NativeMethods.HtTransparent;
            return;
        }

        base.WndProc(ref message);
    }

    private void ApplyColorEffectToMagnifier()
    {
        var colorEffect = _colorEffect;
        NativeCall.RequireSuccess(
            NativeMethods.MagSetColorEffect(
                _magnifierWindow,
                ref colorEffect),
            $"Apply '{_transformId}' visual transform");
    }

    private void UpdateOverlay()
    {
        if (!_initialized)
        {
            return;
        }

        var targetExists =
            NativeMethods.IsWindow(TargetHandle);
        var targetAvailable = targetExists &&
            NativeMethods.IsWindowVisible(TargetHandle) &&
            !NativeMethods.IsIconic(TargetHandle) &&
            IsTargetForeground();

        if (!targetAvailable)
        {
            if (_hasRenderedFrame &&
                IsWithinTransitionGrace())
            {
                return;
            }

            if (!targetExists)
            {
                Close();
            }
            else
            {
                HideOverlay();
            }

            return;
        }

        ResetTransitionGrace();

        if (!OverlayBoundsResolver.TryResolve(
                TargetHandle,
                _overlayScope,
                out var geometry))
        {
            HideOverlay();
            return;
        }

        var destination = geometry.Destination;
        if (!NativeCall.TryTransient(
                NativeMethods.SetWindowPos(
                    Handle,
                    NativeMethods.HwndTopMost,
                    destination.Left,
                    destination.Top,
                    destination.Width,
                    destination.Height,
                    NativeMethods.SwpNoActivate |
                        NativeMethods.SwpShowWindow),
                "Position overlay window"))
        {
            HideOverlay();
            return;
        }

        if (!NativeCall.TryTransient(
                NativeMethods.SetWindowPos(
                    _magnifierWindow,
                    nint.Zero,
                    0,
                    0,
                    destination.Width,
                    destination.Height,
                    NativeMethods.SwpNoActivate |
                        NativeMethods.SwpNoZOrder),
                "Resize magnifier control"))
        {
            HideOverlay();
            return;
        }

        var source = geometry.Source;
        if (!NativeCall.TryTransient(
                NativeMethods.MagSetWindowSource(
                    _magnifierWindow,
                    source),
                "Set magnifier source rectangle"))
        {
            HideOverlay();
            return;
        }

        _hasRenderedFrame = true;

        // A repaint request is intentionally best effort. InvalidateRect's
        // return value does not provide a useful extended error contract.
        _ = NativeMethods.InvalidateRect(
            _magnifierWindow,
            nint.Zero,
            true);
    }

    private void HideOverlay()
    {
        // ShowWindow returns the previous visibility state, not success.
        _ = NativeMethods.ShowWindow(
            Handle,
            NativeMethods.SwHide);
    }

    private bool IsWithinTransitionGrace()
    {
        var now = Environment.TickCount64;
        if (_transitionStartedAt < 0)
        {
            _transitionStartedAt = now;
            return true;
        }

        return now - _transitionStartedAt <
            ForegroundTransitionGraceMilliseconds;
    }

    private void ResetTransitionGrace()
    {
        _transitionStartedAt = -1;
    }

    private bool IsTargetForeground()
    {
        var foreground =
            NativeMethods.GetForegroundWindow();
        foreground = NativeMethods.GetAncestor(
            foreground,
            NativeMethods.GaRoot);
        return foreground == TargetHandle;
    }

    private static nint ValidateTarget(nint targetHandle)
    {
        return targetHandle != nint.Zero
            ? targetHandle
            : throw new ArgumentException(
                "A target window is required.",
                nameof(targetHandle));
    }

    private static string NormalizeTransformId(
        string transformId)
    {
        return !string.IsNullOrWhiteSpace(transformId)
            ? transformId.Trim()
            : throw new ArgumentException(
                "A transform identifier is required.",
                nameof(transformId));
    }

    private static OverlayScope ValidateOverlayScope(
        OverlayScope overlayScope)
    {
        return OverlayScopePolicy.IsSupported(overlayScope)
            ? overlayScope
            : throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");
    }
}
