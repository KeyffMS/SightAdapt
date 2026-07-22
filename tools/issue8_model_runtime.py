from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SRC = ROOT / "src" / "SightAdapt.Demo"


def read(name: str) -> str:
    return (SRC / name).read_text(encoding="utf-8")


def write(name: str, content: str) -> None:
    (SRC / name).write_text(content, encoding="utf-8", newline="\n")


def replace_once(name: str, old: str, new: str) -> None:
    text = read(name)
    if old not in text:
        raise RuntimeError(f"Missing expected text in {name}: {old[:100]!r}")
    write(name, text.replace(old, new, 1))


# Persist one stable overlay-scope identifier per application.
replace_once(
    "ApplicationProfile.cs",
    "public const int CurrentSchemaVersion = 3;",
    "public const int CurrentSchemaVersion = 4;")
replace_once(
    "ApplicationProfile.cs",
    '''    public string VisualProfileId { get; set; } =
        VisualProfilePolicy.NewAssignmentProfileId;

    [JsonPropertyName("effect")]''',
    '''    public string VisualProfileId { get; set; } =
        VisualProfilePolicy.NewAssignmentProfileId;

    private string _overlayScopeId =
        OverlayScopePolicy.ToId(OverlayScopePolicy.Default);

    [JsonPropertyName("overlayScope")]
    public string OverlayScopeId
    {
        get => _overlayScopeId;
        set
        {
            OverlayScopePolicy.TryParseId(value, out var scope);
            _overlayScopeId = OverlayScopePolicy.ToId(scope);
        }
    }

    [JsonIgnore]
    public OverlayScope OverlayScope =>
        OverlayScopePolicy.ParseRequired(OverlayScopeId);

    [JsonPropertyName("effect")]''')
replace_once(
    "ApplicationProfile.cs",
    '''            Enabled = Enabled,
            VisualProfileId = VisualProfileId,
            LegacyEffect = LegacyEffect,''',
    '''            Enabled = Enabled,
            VisualProfileId = VisualProfileId,
            OverlayScopeId = OverlayScopeId,
            LegacyEffect = LegacyEffect,''')

# Keep all application-assignment mutations in one authority.
replace_once(
    "ApplicationProfileManagementService.cs",
    '''    public static void Remove(
        SightAdaptSettings settings,
        ApplicationProfile profile)''',
    '''    public static void SetOverlayScope(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        OverlayScope overlayScope)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        settings.EnsureCollections();
        EnsureMember(settings, profile);

        if (!OverlayScopePolicy.IsSupported(overlayScope))
        {
            throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");
        }

        profile.OverlayScopeId =
            OverlayScopePolicy.ToId(overlayScope);
    }

    public static void Remove(
        SightAdaptSettings settings,
        ApplicationProfile profile)''')

# Resolve all overlay geometry in one runtime authority.
write("OverlayBoundsResolver.cs", '''using System.ComponentModel;
using System.Runtime.InteropServices;

namespace SightAdapt.Demo;

internal readonly record struct OverlayGeometry(
    Rect Destination,
    Rect Source);

internal static class OverlayBoundsResolver
{
    public static bool TryResolve(
        nint targetWindow,
        OverlayScope scope,
        out OverlayGeometry geometry)
    {
        geometry = default;

        if (targetWindow == nint.Zero ||
            !NativeMethods.IsWindow(targetWindow) ||
            !OverlayScopePolicy.IsSupported(scope))
        {
            return false;
        }

        return scope switch
        {
            OverlayScope.ClientArea =>
                TryResolveClientArea(targetWindow, out geometry),
            OverlayScope.Window =>
                TryResolveWindow(targetWindow, out geometry),
            OverlayScope.Screen =>
                TryResolveScreen(targetWindow, out geometry),
            OverlayScope.AllScreens =>
                TryResolveAllScreens(out geometry),
            _ => false,
        };
    }

    private static bool TryResolveClientArea(
        nint targetWindow,
        out OverlayGeometry geometry)
    {
        geometry = default;
        if (!GetClientRect(targetWindow, out var clientRect))
        {
            return false;
        }

        var topLeft = new NativePoint(
            clientRect.Left,
            clientRect.Top);
        var bottomRight = new NativePoint(
            clientRect.Right,
            clientRect.Bottom);

        if (!ClientToScreen(targetWindow, ref topLeft) ||
            !ClientToScreen(targetWindow, ref bottomRight))
        {
            return false;
        }

        return TryCreateGeometry(
            new Rect
            {
                Left = topLeft.X,
                Top = topLeft.Y,
                Right = bottomRight.X,
                Bottom = bottomRight.Y,
            },
            out geometry);
    }

    private static bool TryResolveWindow(
        nint targetWindow,
        out OverlayGeometry geometry)
    {
        geometry = default;
        return NativeMethods.TryGetVisibleWindowBounds(
                   targetWindow,
                   out var bounds) &&
               TryCreateGeometry(bounds, out geometry);
    }

    private static bool TryResolveScreen(
        nint targetWindow,
        out OverlayGeometry geometry)
    {
        return TryCreateGeometry(
            ToRect(Screen.FromHandle(targetWindow).Bounds),
            out geometry);
    }

    private static bool TryResolveAllScreens(
        out OverlayGeometry geometry)
    {
        return TryCreateGeometry(
            ToRect(SystemInformation.VirtualScreen),
            out geometry);
    }

    private static bool TryCreateGeometry(
        Rect bounds,
        out OverlayGeometry geometry)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            geometry = default;
            return false;
        }

        geometry = new OverlayGeometry(bounds, bounds);
        return true;
    }

    private static Rect ToRect(Rectangle rectangle)
    {
        return new Rect
        {
            Left = rectangle.Left,
            Top = rectangle.Top,
            Right = rectangle.Right,
            Bottom = rectangle.Bottom,
        };
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(
        nint window,
        out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(
        nint window,
        ref NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }
}
''')

# Overlay resource owns the active target, transform, and scope.
write("OverlayController.cs", '''namespace SightAdapt.Demo;

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

    public OverlayScope ActiveScope => IsActive
        ? _overlay!.OverlayScope
        : OverlayScopePolicy.Default;

    public event EventHandler? OverlayClosed;

    public void Activate(
        nint targetWindow,
        VisualProfile visualProfile,
        OverlayScope overlayScope)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(visualProfile);

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

        var transform = _transformCatalog.GetRequired(
            visualProfile.TransformId);
        var colorEffect = transform.CreateColorEffect(visualProfile);

        if (IsActive && TargetWindow == targetWindow)
        {
            _overlay!.ApplyColorEffect(colorEffect, transform.Id);
            _overlay.ApplyOverlayScope(overlayScope);
            return;
        }

        Disable();

        var overlay = new MagnifierOverlay(
            targetWindow,
            colorEffect,
            transform.Id,
            overlayScope);
        overlay.FormClosed += HandleOverlayClosed;

        try
        {
            overlay.Show();
            _overlay = overlay;
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
            return;
        }

        var overlay = _overlay;
        _overlay = null;

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

    private void HandleOverlayClosed(
        object? sender,
        FormClosedEventArgs eventArgs)
    {
        if (!ReferenceEquals(sender, _overlay))
        {
            return;
        }

        var overlay = _overlay!;
        _overlay = null;

        overlay.FormClosed -= HandleOverlayClosed;
        overlay.Dispose();
        OverlayClosed?.Invoke(this, EventArgs.Empty);
    }
}
''')

# Apply destination/source geometry selected per application.
text = read("MagnifierOverlay.cs")
text = text.replace(
    '''    private string _transformId;
    private nint _magnifierWindow;''',
    '''    private string _transformId;
    private OverlayScope _overlayScope;
    private nint _magnifierWindow;''',
    1)
text = text.replace(
    '''        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId)''',
    '''        nint targetHandle,
        MagColorEffect colorEffect,
        string transformId,
        OverlayScope overlayScope)''',
    1)
text = text.replace(
    '''        _transformId = string.IsNullOrWhiteSpace(transformId)
            ? throw new ArgumentException("A transform identifier is required.", nameof(transformId))
            : transformId.Trim();''',
    '''        _transformId = string.IsNullOrWhiteSpace(transformId)
            ? throw new ArgumentException("A transform identifier is required.", nameof(transformId))
            : transformId.Trim();
        _overlayScope = OverlayScopePolicy.IsSupported(overlayScope)
            ? overlayScope
            : throw new ArgumentOutOfRangeException(
                nameof(overlayScope),
                overlayScope,
                "The overlay scope is not supported.");''',
    1)
text = text.replace(
    '''    public nint TargetHandle { get; }

    protected override bool ShowWithoutActivation => true;''',
    '''    public nint TargetHandle { get; }

    public OverlayScope OverlayScope => _overlayScope;

    protected override bool ShowWithoutActivation => true;''',
    1)
text = text.replace(
    '''    protected override void OnShown(EventArgs eventArgs)''',
    '''    public void ApplyOverlayScope(OverlayScope overlayScope)
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

    protected override void OnShown(EventArgs eventArgs)''',
    1)
old = '''        if (!NativeMethods.TryGetVisibleWindowBounds(TargetHandle, out var targetBounds) ||
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
        if (!NativeMethods.MagSetWindowSource(_magnifierWindow, source))'''
new = '''        if (!OverlayBoundsResolver.TryResolve(
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
        if (!NativeMethods.MagSetWindowSource(_magnifierWindow, source))'''
if old not in text:
    raise RuntimeError("MagnifierOverlay geometry block not found")
text = text.replace(old, new, 1)
write("MagnifierOverlay.cs", text)

# Pass the per-application scope through the existing activation orchestration.
replace_once(
    "SightAdaptContext.cs",
    "            _overlayController.Activate(target, visualProfile);",
    '''            _overlayController.Activate(
                target,
                visualProfile,
                assignment?.OverlayScope ?? OverlayScopePolicy.Default);''')

# Make the issue-8 build unmistakable in the executable and UI.
replace_once(
    "SightAdapt.Demo.csproj",
    "<Version>0.4.0-alpha.4</Version>",
    "<Version>0.4.0-alpha.5</Version>")
replace_once(
    "SightAdapt.Demo.csproj",
    "<FileVersion>0.4.0.0</FileVersion>",
    "<FileVersion>0.4.0.1</FileVersion>")
replace_once(
    "SightAdapt.Demo.csproj",
    "<InformationalVersion>0.4.0-alpha.4</InformationalVersion>",
    "<InformationalVersion>0.4.0-alpha.5</InformationalVersion>")
replace_once(
    "SightAdapt.Demo.csproj",
    '<AssemblyMetadata Include="Milestone" Value="Alpha 0.4A.4" />',
    '<AssemblyMetadata Include="Milestone" Value="Alpha 0.4B.1 · Overlay scope per app" />')
