using System.ComponentModel;
using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class MagnifierOverlay : Form
{
    private readonly System.Windows.Forms.Timer _updateTimer;
    private MagColorEffect _colorEffect;
    private string _transformId;
    private OverlayScope _overlayScope;
    private nint _magnifierWindow;
    private bool _initialized;

    public MagnifierOverlay(
        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId,
        OverlayScope overlayScope)
    {
        if (targetHandle == nint.Zero)
        {
            throw new ArgumentException("A target window is required.", nameof(targetHandle));
        }

        TargetHandle = targetHandle;
        _colorEffect = colorEffect;
        _transformId = string.IsNullOrWhiteSpace(transformId)
            ? throw new ArgumentException("A transform identifier is required.", nameof(transformId))
            : transformId.Trim();
        _overlayScope = OverlayScopePolicy.IsSupported(overlayScope)
            ? overlayScope
            : throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");

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

    public nint TargetHandle { get; }

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

    public void ApplyColorEffect(MagColorEffect colorEffect, string transformId)
    {
        _colorEffect = colorEffect;
        _transformId = string.IsNullOrWhiteSpace(transformId)
            ? throw new ArgumentException("A transform identifier is required.", nameof(transformId))
            : transformId.Trim();

        if (_initialized)
        {
            ApplyColorEffectToMagnifier();
            NativeMethods.InvalidateRect(_magnifierWindow, nint.Zero, true);
        }
    }

    public void ApplyOverlayScope(OverlayScope overlayScope)
    {
        if (!OverlayScopePolicy.IsSupported(overlayScope))
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");
        }

        _overlayScope = overlayScope;
        if (_initialized)
        {
            UpdateOverlay();
        }
    }

    protected override void OnShown(EventArgs eventArgs)
    {
        base.OnShown(eventArgs);

        NativeMethods.SetLayeredWindowAttributes(
            Handle,
            0,
            255,
            NativeMethods.LwaAlpha);

        _magnifierWindow = NativeMethods.CreateWindowEx(
            0,
            NativeMethods.WcMagnifier,
            "SightAdapt Magnifier",
            NativeMethods.WsChild | NativeMethods.WsVisible,
            0,
            0,
            Math.Max(ClientSize.Width, 1),
            Math.Max(ClientSize.Height, 1),
            Handle,
            nint.Zero,
            nint.Zero,
            nint.Zero);

        if (_magnifierWindow == nint.Zero)
        {
            throw new Win32Exception(
                System.Runtime.InteropServices.Marshal.GetLastWin32Error(),
                "Could not create the Windows magnifier control.");
        }

        var transform = MagTransform.Identity;
        if (!NativeMethods.MagSetWindowTransform(_magnifierWindow, ref transform))
        {
            throw new Win32Exception("Could not initialize the magnifier transform.");
        }

        ApplyColorEffectToMagnifier();

        var excludedWindows = new[] { Handle };
        NativeMethods.MagSetWindowFilterList(
            _magnifierWindow,
            NativeMethods.MwFilterModeExclude,
            excludedWindows.Length,
            excludedWindows);

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
            NativeMethods.DestroyWindow(_magnifierWindow);
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
        if (!NativeMethods.MagSetColorEffect(_magnifierWindow, ref colorEffect))
        {
            throw new Win32Exception(
                $"Could not apply the '{_transformId}' visual transform.");
        }
    }

    private void UpdateOverlay()
    {
        if (!_initialized || !NativeMethods.IsWindow(TargetHandle))
        {
            Close();
            return;
        }

        if (!NativeMethods.IsWindowVisible(TargetHandle) ||
            NativeMethods.IsIconic(TargetHandle) ||
            !IsTargetForeground())
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SwHide);
            return;
        }

        if (!OverlayBoundsResolver.TryResolve(
                TargetHandle,
                _overlayScope,
                out var geometry))
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SwHide);
            return;
        }

        var destination = geometry.Destination;
        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopMost,
            destination.Left,
            destination.Top,
            destination.Width,
            destination.Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);

        NativeMethods.SetWindowPos(
            _magnifierWindow,
            nint.Zero,
            0,
            0,
            destination.Width,
            destination.Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoZOrder);

        var source = geometry.Source;
        if (!NativeMethods.MagSetWindowSource(_magnifierWindow, source))
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SwHide);
            return;
        }

        NativeMethods.InvalidateRect(_magnifierWindow, nint.Zero, true);
    }

    private bool IsTargetForeground()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        foreground = NativeMethods.GetAncestor(foreground, NativeMethods.GaRoot);
        return foreground == TargetHandle;
    }
}
