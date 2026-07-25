using System.Diagnostics;

namespace SightAdapt;

internal sealed class OverlayController :
    IRuntimeOverlay,
    IDisposable
{
    private readonly VisualTransformCatalog
        _transformCatalog;
    private readonly IWin32MenuWindowTracker
        _menuWindowTracker;
    private readonly Dictionary<nint, MagnifierOverlay>
        _menuOverlays = [];

    private MagnifierOverlay? _overlay;
    private MagColorEffect _menuColorEffect;
    private string _menuTransformId = string.Empty;
    private bool _synchronizingMenuOverlays;
    private bool _disposed;

    public OverlayController(
        VisualTransformCatalog transformCatalog)
        : this(
            transformCatalog,
            new Win32MenuWindowTracker())
    {
    }

    internal OverlayController(
        VisualTransformCatalog transformCatalog,
        IWin32MenuWindowTracker menuWindowTracker)
    {
        _transformCatalog = transformCatalog ??
            throw new ArgumentNullException(
                nameof(transformCatalog));
        _menuWindowTracker = menuWindowTracker ??
            throw new ArgumentNullException(
                nameof(menuWindowTracker));
        _menuWindowTracker.Changed +=
            MenuWindowTrackerChanged;
    }

    public event EventHandler? OverlayClosed;

    public bool IsActive =>
        _overlay is { IsDisposed: false };

    public nint TargetWindow =>
        IsActive
            ? _overlay!.TargetHandle
            : nint.Zero;

    public void Activate(
        nint targetWindow,
        VisualProfile visualProfile,
        OverlayScope overlayScope)
    {
        Activate(
            targetWindow,
            visualProfile,
            visualProfile,
            overlayScope);
    }

    public void Activate(
        nint targetWindow,
        VisualProfile visualProfile,
        VisualProfile menuVisualProfile,
        OverlayScope overlayScope)
    {
        ObjectDisposedException.ThrowIf(
            _disposed,
            this);
        ArgumentNullException.ThrowIfNull(
            visualProfile);
        ArgumentNullException.ThrowIfNull(
            menuVisualProfile);

        if (targetWindow == nint.Zero)
        {
            throw new ArgumentException(
                "A target window is required.",
                nameof(targetWindow));
        }

        if (!OverlayScopePolicy.IsSupported(
                overlayScope))
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlayScope));
        }

        var primaryTransform =
            _transformCatalog.GetRequired(
                visualProfile.TransformId);
        var primaryColorEffect =
            primaryTransform.CreateColorEffect(
                visualProfile);
        var menuTransform =
            _transformCatalog.GetRequired(
                menuVisualProfile.TransformId);
        var menuColorEffect =
            menuTransform.CreateColorEffect(
                menuVisualProfile);

        if (IsActive)
        {
            ActivateExistingOverlay(
                targetWindow,
                primaryColorEffect,
                primaryTransform.Id,
                menuColorEffect,
                menuTransform.Id,
                overlayScope);
            return;
        }

        ActivateNewOverlay(
            targetWindow,
            primaryColorEffect,
            primaryTransform.Id,
            menuColorEffect,
            menuTransform.Id,
            overlayScope);
    }

    public void Disable()
    {
        _menuWindowTracker.Stop();
        CloseMenuOverlays();

        if (_overlay is null)
        {
            return;
        }

        var overlay = _overlay;
        _overlay = null;
        overlay.FormClosed -= OverlayFormClosed;
        overlay.Close();
        overlay.Dispose();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _menuWindowTracker.Changed -=
            MenuWindowTrackerChanged;
        Disable();
        _menuWindowTracker.Dispose();
        _disposed = true;
    }

    private void ActivateExistingOverlay(
        nint targetWindow,
        MagColorEffect primaryColorEffect,
        string primaryTransformId,
        MagColorEffect menuColorEffect,
        string menuTransformId,
        OverlayScope overlayScope)
    {
        var targetChanged =
            _overlay!.TargetHandle != targetWindow;

        if (targetChanged)
        {
            _menuWindowTracker.Stop();
            CloseMenuOverlays();
        }

        _overlay!.Retarget(
            targetWindow,
            primaryColorEffect,
            primaryTransformId,
            overlayScope);
        _menuColorEffect = menuColorEffect;
        _menuTransformId = menuTransformId;

        if (!targetChanged)
        {
            try
            {
                RetargetMenuOverlays();
            }
            catch (Exception exception)
            {
                HandleMenuOverlayFailure(
                    "retarget native popup menu overlays",
                    exception);
            }
        }

        StartMenuTracking(targetWindow);
    }

    private void ActivateNewOverlay(
        nint targetWindow,
        MagColorEffect primaryColorEffect,
        string primaryTransformId,
        MagColorEffect menuColorEffect,
        string menuTransformId,
        OverlayScope overlayScope)
    {
        var overlay = new MagnifierOverlay(
            targetWindow,
            primaryColorEffect,
            primaryTransformId,
            overlayScope);
        overlay.FormClosed += OverlayFormClosed;

        try
        {
            overlay.SetExcludedWindows(Array.Empty<nint>());
            overlay.Show();

            if (overlay.IsDisposed)
            {
                throw new InvalidOperationException(
                    "The overlay target became unavailable " +
                    "during activation.");
            }

            _overlay = overlay;
            _menuColorEffect = menuColorEffect;
            _menuTransformId = menuTransformId;
            StartMenuTracking(targetWindow);
        }
        catch
        {
            overlay.FormClosed -= OverlayFormClosed;
            overlay.Dispose();
            throw;
        }
    }

    private void StartMenuTracking(
        nint targetWindow)
    {
        try
        {
            _menuWindowTracker.Start(
                targetWindow);
        }
        catch (Exception exception)
        {
            Debug.WriteLine(
                "SightAdapt native menu tracking could not " +
                $"start: {exception}");
        }
    }

    private void MenuWindowTrackerChanged(
        object? sender,
        Win32MenuWindowsChangedEventArgs eventArgs)
    {
        if (_disposed ||
            !IsActive ||
            _synchronizingMenuOverlays)
        {
            return;
        }

        Exception? failure = null;
        _synchronizingMenuOverlays = true;
        try
        {
            SynchronizeMenuOverlays(
                eventArgs.Windows);
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            _synchronizingMenuOverlays = false;
        }

        if (failure is not null)
        {
            HandleMenuOverlayFailure(
                "synchronize native popup menu overlays",
                failure);
        }
    }

    private void SynchronizeMenuOverlays(
        IReadOnlyList<nint> menuWindows)
    {
        var desiredOrder = menuWindows
            .Where(window =>
                window != nint.Zero &&
                NativeMethods.IsWindow(window))
            .Distinct()
            .ToArray();
        var desiredWindows =
            desiredOrder.ToHashSet();
        var changed = false;

        foreach (var existingWindow in
                 _menuOverlays.Keys.ToArray())
        {
            if (desiredWindows.Contains(
                    existingWindow))
            {
                continue;
            }

            RemoveMenuOverlay(existingWindow);
            changed = true;
        }

        var addedOverlays =
            new List<MagnifierOverlay>();

        foreach (var menuWindow in
                 desiredOrder.Reverse())
        {
            if (_menuOverlays.ContainsKey(
                    menuWindow))
            {
                continue;
            }

            var menuOverlay =
                CreateMenuOverlay(menuWindow);
            _menuOverlays.Add(
                menuWindow,
                menuOverlay);
            addedOverlays.Add(menuOverlay);
            changed = true;
        }

        if (!changed)
        {
            return;
        }

        ApplyExclusionLists();

        foreach (var menuOverlay in addedOverlays)
        {
            if (!menuOverlay.IsDisposed &&
                _overlay is { IsDisposed: false })
            {
                menuOverlay.Show();
            }
        }

        ApplyExclusionLists();
    }

    private MagnifierOverlay CreateMenuOverlay(
        nint menuWindow)
    {
        var menuOverlay = new MagnifierOverlay(
            menuWindow,
            _menuColorEffect,
            _menuTransformId,
            OverlayScope.Window,
            MagnifierOverlayTargetKind.TransientPopup);
        if (_overlay is { IsDisposed: false })
        {
            menuOverlay.Owner = _overlay;
        }

        menuOverlay.FormClosed +=
            MenuOverlayFormClosed;
        return menuOverlay;
    }

    private void RetargetMenuOverlays()
    {
        var removed = false;

        foreach (var pair in
                 _menuOverlays.ToArray())
        {
            if (pair.Value.IsDisposed ||
                !NativeMethods.IsWindow(pair.Key))
            {
                RemoveMenuOverlay(pair.Key);
                removed = true;
                continue;
            }

            pair.Value.Retarget(
                pair.Key,
                _menuColorEffect,
                _menuTransformId,
                OverlayScope.Window);
        }

        if (removed)
        {
            ApplyExclusionLists();
        }
    }

    private void ApplyExclusionLists()
    {
        var overlays =
            new List<MagnifierOverlay>();

        if (_overlay is { IsDisposed: false })
        {
            overlays.Add(_overlay);
        }

        overlays.AddRange(
            _menuOverlays.Values.Where(
                overlay => !overlay.IsDisposed));

        var overlayHandles = overlays
            .Select(overlay => overlay.Handle)
            .Where(handle => handle != nint.Zero)
            .Distinct()
            .ToArray();

        foreach (var overlay in overlays)
        {
            overlay.SetExcludedWindows(
                overlayHandles);
        }
    }

    private void CloseMenuOverlays()
    {
        var wasSynchronizing =
            _synchronizingMenuOverlays;
        _synchronizingMenuOverlays = true;
        try
        {
            foreach (var menuWindow in
                     _menuOverlays.Keys.ToArray())
            {
                RemoveMenuOverlay(menuWindow);
            }
        }
        finally
        {
            _synchronizingMenuOverlays =
                wasSynchronizing;
        }

        TryResetPrimaryExclusionList();
    }

    private void RemoveMenuOverlay(
        nint menuWindow)
    {
        if (!_menuOverlays.Remove(
                menuWindow,
                out var menuOverlay))
        {
            return;
        }

        menuOverlay.FormClosed -=
            MenuOverlayFormClosed;

        if (!menuOverlay.IsDisposed)
        {
            menuOverlay.Close();
            menuOverlay.Dispose();
        }
    }

    private void TryResetPrimaryExclusionList()
    {
        if (_overlay is not
            { IsDisposed: false })
        {
            return;
        }

        try
        {
            _overlay.SetExcludedWindows(Array.Empty<nint>());
        }
        catch (Exception exception)
        {
            Debug.WriteLine(
                "SightAdapt could not reset the primary " +
                $"overlay filter: {exception}");
        }
    }

    private void HandleMenuOverlayFailure(
        string operation,
        Exception exception)
    {
        Debug.WriteLine(
            $"SightAdapt could not {operation}; transient " +
            $"menu overlays were removed: {exception}");
        CloseMenuOverlays();
    }

    private void OverlayFormClosed(
        object? sender,
        FormClosedEventArgs eventArgs)
    {
        if (!ReferenceEquals(sender, _overlay))
        {
            return;
        }

        var overlay = _overlay;
        _overlay = null;
        _menuWindowTracker.Stop();
        CloseMenuOverlays();

        if (overlay is not null)
        {
            overlay.FormClosed -= OverlayFormClosed;
            overlay.Dispose();
        }

        OverlayClosed?.Invoke(
            this,
            EventArgs.Empty);
    }

    private void MenuOverlayFormClosed(
        object? sender,
        FormClosedEventArgs eventArgs)
    {
        if (sender is not
            MagnifierOverlay closedOverlay)
        {
            return;
        }

        nint closedWindow = nint.Zero;
        foreach (var pair in _menuOverlays)
        {
            if (ReferenceEquals(
                    pair.Value,
                    closedOverlay))
            {
                closedWindow = pair.Key;
                break;
            }
        }

        if (closedWindow == nint.Zero)
        {
            return;
        }

        _menuOverlays.Remove(closedWindow);
        closedOverlay.FormClosed -=
            MenuOverlayFormClosed;
        closedOverlay.Dispose();

        if (!_synchronizingMenuOverlays)
        {
            try
            {
                ApplyExclusionLists();
            }
            catch (Exception exception)
            {
                HandleMenuOverlayFailure(
                    "refresh popup overlay filters",
                    exception);
            }
        }
    }
}
