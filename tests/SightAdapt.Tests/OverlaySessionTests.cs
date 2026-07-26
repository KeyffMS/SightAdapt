using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
[DoNotParallelize]
public sealed class OverlaySessionTests
{
    private static readonly ResolvedVisualEffect PrimaryEffect = new(
        "primary",
        new MagColorEffect { M00 = 1.0f, M44 = 1.0f });

    private static readonly ResolvedVisualEffect MenuEffect = new(
        "menu",
        MagColorEffect.Invert);

    [TestMethod]
    public void ExistingSessionRetargetsOnePrimaryWindow()
    {
        var tracker = new FakeMenuWindowTracker();
        var factory = new FakeOverlayWindowFactory();
        var windows = new FakeNativeWindowApi();
        using var session = new OverlaySession(
            tracker,
            factory,
            windows);

        session.Activate(
            (nint)10,
            PrimaryEffect,
            MenuEffect,
            OverlayScope.ClientArea);
        session.Activate(
            (nint)20,
            PrimaryEffect,
            MenuEffect,
            OverlayScope.Window);

        Assert.AreEqual(1, factory.Created.Count);
        var primary = factory.Created.Single();
        Assert.AreEqual(1, primary.RetargetCount);
        Assert.AreEqual((nint)20, primary.TargetHandle);
        Assert.AreEqual(OverlayScope.Window, primary.Scope);
        Assert.AreEqual((nint)20, session.TargetWindow);
    }

    [TestMethod]
    public void MenuWindowsUseBidirectionalLifetimeTracking()
    {
        var tracker = new FakeMenuWindowTracker();
        var factory = new FakeOverlayWindowFactory();
        var windows = new FakeNativeWindowApi();
        windows.SetWindow((nint)101);
        windows.SetWindow((nint)102);
        using var session = new OverlaySession(
            tracker,
            factory,
            windows);

        session.Activate(
            (nint)10,
            PrimaryEffect,
            MenuEffect,
            OverlayScope.ClientArea);
        tracker.Raise((nint)101, (nint)102);

        Assert.AreEqual(2, session.MenuWindowCount);
        Assert.AreEqual(3, factory.Created.Count);
        var primary = factory.Created[0];
        var menus = factory.Created.Skip(1).ToArray();
        Assert.IsTrue(menus.All(menu =>
            ReferenceEquals(primary, menu.Owner)));
        Assert.IsTrue(factory.Created.All(window =>
            window.ExcludedWindows.Count == 3));

        menus[0].RaiseClosed();

        Assert.AreEqual(1, session.MenuWindowCount);
        Assert.IsTrue(menus[0].IsDisposed);
        Assert.IsTrue(primary.ExcludedWindows.Count == 2);
    }

    [TestMethod]
    public void RemovedMenuWindowIsClosedAndDisposed()
    {
        var tracker = new FakeMenuWindowTracker();
        var factory = new FakeOverlayWindowFactory();
        var windows = new FakeNativeWindowApi();
        windows.SetWindow((nint)101);
        windows.SetWindow((nint)102);
        using var session = new OverlaySession(
            tracker,
            factory,
            windows);

        session.Activate(
            (nint)10,
            PrimaryEffect,
            MenuEffect,
            OverlayScope.ClientArea);
        tracker.Raise((nint)101, (nint)102);
        var removed = factory.Created.Single(window =>
            window.TargetHandle == (nint)101);

        tracker.Raise((nint)102);

        Assert.AreEqual(1, session.MenuWindowCount);
        Assert.AreEqual(1, removed.CloseCount);
        Assert.AreEqual(1, removed.DisposeCount);
    }

    [TestMethod]
    public void MenuFailureDoesNotDisablePrimaryCorrection()
    {
        var tracker = new FakeMenuWindowTracker();
        var factory = new FakeOverlayWindowFactory
        {
            FailNextMenuFilter = true,
        };
        var windows = new FakeNativeWindowApi();
        windows.SetWindow((nint)101);
        var diagnostics = new RecordingDiagnosticSink();
        using var scope = Diagnostics.UseSink(diagnostics);
        using var session = new OverlaySession(
            tracker,
            factory,
            windows);

        session.Activate(
            (nint)10,
            PrimaryEffect,
            MenuEffect,
            OverlayScope.ClientArea);
        tracker.Raise((nint)101);

        Assert.IsTrue(session.IsActive);
        Assert.AreEqual((nint)10, session.TargetWindow);
        Assert.AreEqual(0, session.MenuWindowCount);
        Assert.IsTrue(diagnostics.Events.Any(item =>
            item.FailurePolicy ==
                DiagnosticFailurePolicy.Recovered));
    }

    [TestMethod]
    public void DisableAndDisposeAreIdempotent()
    {
        var tracker = new FakeMenuWindowTracker();
        var factory = new FakeOverlayWindowFactory();
        var windows = new FakeNativeWindowApi();
        windows.SetWindow((nint)101);
        var session = new OverlaySession(
            tracker,
            factory,
            windows);
        session.Activate(
            (nint)10,
            PrimaryEffect,
            MenuEffect,
            OverlayScope.ClientArea);
        tracker.Raise((nint)101);
        var created = factory.Created.ToArray();

        session.Disable();
        session.Disable();
        session.Dispose();
        session.Dispose();

        Assert.IsFalse(session.IsActive);
        Assert.IsTrue(created.All(window =>
            window.DisposeCount == 1));
        Assert.AreEqual(1, tracker.DisposeCount);
    }

    [TestMethod]
    public void PrimaryCloseEndsCompleteSessionOnce()
    {
        var tracker = new FakeMenuWindowTracker();
        var factory = new FakeOverlayWindowFactory();
        using var session = new OverlaySession(
            tracker,
            factory,
            new FakeNativeWindowApi());
        var closed = 0;
        session.Closed += (_, _) => closed++;
        session.Activate(
            (nint)10,
            PrimaryEffect,
            MenuEffect,
            OverlayScope.ClientArea);

        factory.Created[0].RaiseClosed();

        Assert.IsFalse(session.IsActive);
        Assert.AreEqual(1, closed);
        Assert.AreEqual(1, factory.Created[0].DisposeCount);
    }
}
