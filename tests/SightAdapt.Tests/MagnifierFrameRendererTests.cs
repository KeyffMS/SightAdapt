using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
[DoNotParallelize]
public sealed class MagnifierFrameRendererTests
{
    [TestMethod]
    public void RenderPositionsOverlayAndUpdatesSource()
    {
        var windows = new FakeNativeWindowApi();
        var magnification = new FakeNativeMagnificationApi();
        var geometry = new FakeOverlayGeometryResolver();
        var renderer = new MagnifierFrameRenderer(
            windows,
            magnification,
            geometry);

        var rendered = renderer.TryRender(
            new MagnifierFrameRequest(
                (nint)1,
                (nint)2,
                (nint)3,
                OverlayScope.Window,
                PreservePopupZOrder: false));

        Assert.IsTrue(rendered);
        Assert.AreEqual(2, windows.PositionCalls.Count);
        Assert.AreEqual(
            NativeConstants.HwndTopMost,
            windows.PositionCalls[0].InsertAfter);
        Assert.AreEqual(100, windows.PositionCalls[0].Width);
        Assert.AreEqual(200, windows.PositionCalls[0].Height);
        Assert.AreEqual(1, magnification.SourceCalls.Count);
        Assert.AreEqual(1, windows.InvalidatedWindows.Count);
    }

    [TestMethod]
    public void PopupRetargetPreservesExistingZOrder()
    {
        var windows = new FakeNativeWindowApi();
        var renderer = new MagnifierFrameRenderer(
            windows,
            new FakeNativeMagnificationApi(),
            new FakeOverlayGeometryResolver());

        Assert.IsTrue(renderer.TryRender(
            new MagnifierFrameRequest(
                (nint)1,
                (nint)2,
                (nint)3,
                OverlayScope.Window,
                PreservePopupZOrder: true)));

        Assert.AreEqual(
            nint.Zero,
            windows.PositionCalls[0].InsertAfter);
        Assert.AreNotEqual(
            0u,
            windows.PositionCalls[0].Flags &
                NativeConstants.SwpNoZOrder);
    }

    [TestMethod]
    public void MissingGeometryStopsBeforeNativePositioning()
    {
        var windows = new FakeNativeWindowApi();
        var geometry = new FakeOverlayGeometryResolver
        {
            Succeeds = false,
        };
        var renderer = new MagnifierFrameRenderer(
            windows,
            new FakeNativeMagnificationApi(),
            geometry);

        Assert.IsFalse(renderer.TryRender(
            new MagnifierFrameRequest(
                (nint)1,
                (nint)2,
                (nint)3,
                OverlayScope.Window,
                PreservePopupZOrder: false)));
        Assert.AreEqual(0, windows.PositionCalls.Count);
    }

    [TestMethod]
    public void PositionFailureStopsFramePipeline()
    {
        var windows = new FakeNativeWindowApi
        {
            PositionSucceeds = false,
        };
        var magnification = new FakeNativeMagnificationApi();
        var renderer = new MagnifierFrameRenderer(
            windows,
            magnification,
            new FakeOverlayGeometryResolver());

        using var diagnostics = Diagnostics.UseSink(
            new RecordingDiagnosticSink());
        Assert.IsFalse(renderer.TryRender(
            new MagnifierFrameRequest(
                (nint)1,
                (nint)2,
                (nint)3,
                OverlayScope.Window,
                PreservePopupZOrder: false)));
        Assert.AreEqual(1, windows.PositionCalls.Count);
        Assert.AreEqual(0, magnification.SourceCalls.Count);
    }
}
