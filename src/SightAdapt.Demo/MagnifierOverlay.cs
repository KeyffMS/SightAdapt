using System.ComponentModel;
using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class MagnifierOverlay : Form
{
    private readonly System.Windows.Forms.Timer _updateTimer;
    private readonly IVisualTransform _visualTransform;
    private nint _magnifierWindow;
    private bool _initialized;

    public MagnifierOverlay(nint targetHandle, IVisualTransform visualTransform)
    {
        if (targetHandle == nint.Zero)
        {
            throw new ArgumentException("A target window is required.", nameof(targetHandle));
        }

        TargetHandle = targetHandle;
        _visualTransform = visualTransform
            ?? throw new ArgumentNullException(nameof(visualTransform));

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

        var colorEffect = _visualTransform.CreateColorEffect();
        if (!NativeMethods.MagSetColorEffect(_magnifierWindow, ref colorEffect))
        {
            throw new Win32Exception(
                $"Could not apply the '{_visualTransform.Id}' visual transform.");
        }

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

        if (!NativeMethods.TryGetVisibleWindowBounds(TargetHandle, out var targetBounds) ||
            targetBounds.Width <= 0 ||
            targetBounds.Height <= 0)
        {
            NativeMethods.ShowWindow(Handle, NativeMethods.SwHide);
            return;
        }

        NativeMethods.SetWindowPos(
            Handle,
            NativeMethods.HwndTopMost,
            targetBounds.Left,
            targetBounds.Top,
            targetBounds.Width,
            targetBounds.Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow);

        NativeMethods.SetWindowPos(
            _magnifierWindow,
            nint.Zero,
            0,
            0,
            targetBounds.Width,
            targetBounds.Height,
            NativeMethods.SwpNoActivate | NativeMethods.SwpNoZOrder);

        var source = targetBounds;
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
