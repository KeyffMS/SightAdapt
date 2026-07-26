using System.Drawing;

namespace SightAdapt;

internal enum MagnifierOverlayTargetKind
{
    ForegroundWindow,
    TransientPopup,
}

internal sealed class MagnifierOverlay : Form
{
    internal static int ForegroundTransitionGraceMilliseconds =>
        RuntimeTimingPolicy.Default
            .ForegroundTransitionGraceMilliseconds;

    private readonly System.Windows.Forms.Timer _updateTimer;
    private readonly MagnifierOverlayTargetKind _targetKind;
    private readonly RuntimeTimingPolicy _timingPolicy;
    private readonly INativeWindowApi _windowApi;
    private readonly INativeMagnificationApi _magnificationApi;
    private readonly IOverlayTargetAvailability _targetAvailability;
    private readonly MagnifierFrameRenderer _frameRenderer;
    private MagColorEffect _colorEffect;
    private string _transformId;
    private OverlayScope _overlayScope;
    private nint[] _excludedWindows = [];
    private nint _magnifierWindow;
    private long _transitionStartedAt = -1;
    private bool _hasRenderedFrame;
    private bool _initialized;

    public MagnifierOverlay(
        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId,
        OverlayScope overlayScope)
        : this(
            targetHandle,
            colorEffect,
            transformId,
            overlayScope,
            MagnifierOverlayTargetKind.ForegroundWindow)
    {
    }

    internal MagnifierOverlay(
        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId,
        OverlayScope overlayScope,
        MagnifierOverlayTargetKind targetKind)
        : this(
            targetHandle,
            colorEffect,
            transformId,
            overlayScope,
            targetKind,
            RuntimeTimingPolicy.Default,
            NativeWindowApi.Default,
            NativeMagnificationApi.Default,
            CreateAvailability(targetKind),
            new MagnifierFrameRenderer())
    {
    }

    internal MagnifierOverlay(
        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId,
        OverlayScope overlayScope,
        MagnifierOverlayTargetKind targetKind,
        RuntimeTimingPolicy timingPolicy,
        INativeWindowApi windowApi,
        INativeMagnificationApi magnificationApi,
        IOverlayTargetAvailability targetAvailability,
        MagnifierFrameRenderer frameRenderer)
    {
        TargetHandle = ValidateTarget(targetHandle);
        _colorEffect = colorEffect;
        _transformId = NormalizeTransformId(transformId);
        _overlayScope = ValidateOverlayScope(overlayScope);
        _targetKind = ValidateTargetKind(targetKind);
        _timingPolicy = timingPolicy ??
            throw new ArgumentNullException(nameof(timingPolicy));
        _windowApi = windowApi ??
            throw new ArgumentNullException(nameof(windowApi));
        _magnificationApi = magnificationApi ??
            throw new ArgumentNullException(nameof(magnificationApi));
        _targetAvailability = targetAvailability ??
            throw new ArgumentNullException(
                nameof(targetAvailability));
        _frameRenderer = frameRenderer ??
            throw new ArgumentNullException(nameof(frameRenderer));

        AutoScaleMode = AutoScaleMode.None;
        BackColor = Color.Black;
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;

        _updateTimer = new System.Windows.Forms.Timer
        {
            Interval = _timingPolicy.OverlayRefreshMilliseconds,
        };
        _updateTimer.Tick += TimerTick;
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
                NativeConstants.WsExLayered |
                NativeConstants.WsExTransparent |
                NativeConstants.WsExToolWindow |
                NativeConstants.WsExNoActivate;
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

    public void SetExcludedWindows(
        IEnumerable<nint> windows)
    {
        ArgumentNullException.ThrowIfNull(windows);

        _excludedWindows = windows
            .Where(window => window != nint.Zero)
            .Append(Handle)
            .Distinct()
            .ToArray();

        if (_initialized)
        {
            ApplyExcludedWindowsToMagnifier();
        }
    }

    protected override void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);

        NativeCall.RequireSuccess(
            _windowApi.SetLayeredOpacity(
                Handle,
                byte.MaxValue),
            "Set layered overlay opacity");

        _magnifierWindow = NativeCall.RequireHandle(
            _windowApi.CreateWindow(
                0,
                NativeConstants.WcMagnifier,
                "SightAdapt Magnifier",
                NativeConstants.WsChild |
                    NativeConstants.WsVisible,
                0,
                0,
                Math.Max(ClientSize.Width, 1),
                Math.Max(ClientSize.Height, 1),
                Handle),
            "Create Windows magnifier control");

        var transform = MagTransform.Identity;
        NativeCall.RequireSuccess(
            _magnificationApi.SetWindowTransform(
                _magnifierWindow,
                ref transform),
            "Initialize magnifier transform");

        ApplyColorEffectToMagnifier();
        ApplyExcludedWindowsToMagnifier();

        _initialized = true;
        _updateTimer.Start();
        UpdateOverlay();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _updateTimer.Stop();
            _updateTimer.Tick -= TimerTick;
            _updateTimer.Dispose();
        }

        if (_magnifierWindow != nint.Zero)
        {
            NativeCall.BestEffort(
                _windowApi.DestroyWindow(
                    _magnifierWindow),
                "Destroy magnifier control");
            _magnifierWindow = nint.Zero;
        }

        base.Dispose(disposing);
    }

    protected override void WndProc(ref Message message)
    {
        if (message.Msg == NativeConstants.WmNcHitTest)
        {
            message.Result =
                (nint)NativeConstants.HtTransparent;
            return;
        }

        base.WndProc(ref message);
    }

    private void TimerTick(
        object? sender,
        EventArgs eventArgs)
    {
        UpdateOverlay();
    }

    private void ApplyColorEffectToMagnifier()
    {
        var colorEffect = _colorEffect;
        NativeCall.RequireSuccess(
            _magnificationApi.SetColorEffect(
                _magnifierWindow,
                ref colorEffect),
            $"Apply '{_transformId}' visual transform");
    }

    private void ApplyExcludedWindowsToMagnifier()
    {
        var excludedWindows =
            _excludedWindows.Length > 0
                ? _excludedWindows
                : new[] { Handle };

        NativeCall.RequireSuccess(
            _magnificationApi.SetWindowFilterList(
                _magnifierWindow,
                NativeConstants.MwFilterModeExclude,
                excludedWindows.Length,
                excludedWindows),
            "Exclude SightAdapt overlays from magnifier source");
    }

    private void UpdateOverlay()
    {
        if (!_initialized)
        {
            return;
        }

        var availability =
            _targetAvailability.Evaluate(TargetHandle);
        if (!availability.IsAvailable)
        {
            if (_targetKind ==
                    MagnifierOverlayTargetKind.ForegroundWindow &&
                _hasRenderedFrame &&
                IsWithinTransitionGrace())
            {
                return;
            }

            if (!availability.Exists)
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
        var rendered = _frameRenderer.TryRender(
            new MagnifierFrameRequest(
                Handle,
                _magnifierWindow,
                TargetHandle,
                _overlayScope,
                PreservePopupZOrder:
                    _targetKind ==
                        MagnifierOverlayTargetKind.TransientPopup &&
                    _hasRenderedFrame));
        if (!rendered)
        {
            HideOverlay();
            return;
        }

        _hasRenderedFrame = true;
    }

    private void HideOverlay()
    {
        // ShowWindow returns the previous visibility state, not success.
        _ = _windowApi.ShowWindow(
            Handle,
            NativeConstants.SwHide);
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
            _timingPolicy
                .ForegroundTransitionGraceMilliseconds;
    }

    private void ResetTransitionGrace()
    {
        _transitionStartedAt = -1;
    }

    private static IOverlayTargetAvailability
        CreateAvailability(
            MagnifierOverlayTargetKind targetKind)
    {
        return targetKind switch
        {
            MagnifierOverlayTargetKind.ForegroundWindow =>
                new ForegroundOverlayTargetAvailability(),
            MagnifierOverlayTargetKind.TransientPopup =>
                new PopupOverlayTargetAvailability(),
            _ => throw new ArgumentOutOfRangeException(
                nameof(targetKind)),
        };
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

    private static MagnifierOverlayTargetKind
        ValidateTargetKind(
            MagnifierOverlayTargetKind targetKind)
    {
        return targetKind is
               MagnifierOverlayTargetKind.ForegroundWindow or
               MagnifierOverlayTargetKind.TransientPopup
            ? targetKind
            : throw new ArgumentOutOfRangeException(
                nameof(targetKind),
                targetKind,
                "The overlay target kind is not supported.");
    }
}
