# SightAdapt

SightAdapt is an open-source visual-accessibility application for Windows 10 and Windows 11. It applies configurable visual correction to selected application windows through a separate input-transparent overlay.

SightAdapt does not modify another application's files or process memory. The current alpha uses the Windows Magnification API as a compact implementation of the Light interaction model.

## Current development build

The current accepted development increment is **SightAdapt 0.4B.2 — Faster overlay switching**.

Build identity:

```text
Product version: 0.4.0-alpha.6+<commit>
File version:    0.4.0.2
Milestone:       Alpha 0.4B.2 · Faster overlay switching
Settings schema: 4
```

The current alpha provides:

- persistent per-application visual-profile assignments;
- automatic correction when an enabled application becomes active;
- built-in `Exact invert` and editable `Soft invert` profiles;
- independent user-defined Soft Invert profiles;
- output-black, output-white, brightness, contrast, saturation, and hue controls;
- a grayscale and hue-spectrum profile preview;
- per-application overlay scope: client area, full window, current screen, or all screens;
- foreground-window detection with a 75 ms polling interval and duplicate suppression;
- a bounded runtime application-identity cache;
- reuse and retargeting of one overlay instance during normal application switching;
- a short transition grace period that reduces visible white flashes;
- immediate explicit and emergency overlay shutdown;
- schema-versioned JSON settings stored in `%LOCALAPPDATA%\SightAdapt\settings.json`;
- automated build, test, architecture, migration, and Windows publish validation.

New application assignments use Soft Invert and client-area scope by default. Existing settings are normalized without discarding valid assignments.

## Quick start

Requirements:

- Windows 10 version 2004 or newer, or Windows 11;
- .NET 8 SDK or Visual Studio with the .NET desktop workload;
- x64 runtime.

From the repository root:

```powershell
dotnet restore src/SightAdapt.Demo/SightAdapt.Demo.csproj
dotnet build src/SightAdapt.Demo/SightAdapt.Demo.csproj -c Release
dotnet test tests/SightAdapt.Tests/SightAdapt.Tests.csproj -c Release
dotnet run --project src/SightAdapt.Demo/SightAdapt.Demo.csproj -c Release
```

Create a self-contained build:

```powershell
dotnet publish src/SightAdapt.Demo/SightAdapt.Demo.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o artifacts/win-x64
```

Detailed controls and local verification steps are in [DEMO.md](DEMO.md).

## Keyboard controls

SightAdapt registers exactly two global shortcuts:

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Locally enable or disable visual correction for the active window without changing saved settings |
| `Ctrl+Alt+Shift+I` | Add, disable, or re-enable the active application's persistent automatic assignment |

Emergency shutdown is available from the notification-area menu.

## Documentation

- [Documentation index](docs/README.md)
- [Current 0.4 architecture](docs/ARCHITECTURE_0.4.md)
- [0.4 Alpha roadmap](docs/ROADMAP_0.4.md)
- [Current executable controls and build notes](DEMO.md)
- [Soft color profiles](docs/SOFT_COLOR_PROFILES_0.4.md)
- [Per-application overlay scope](docs/OVERLAY_SCOPE_0.4B.1.md)
- [Faster overlay switching](docs/OVERLAY_SWITCHING_0.4B.2.md)
- [Light scope and completion criteria](LIGHT.md)
- [Hard target architecture](HARD.md)

## Architecture summary

The current implementation keeps one authority for each mutable concern:

- `SettingsCoordinator` — committed settings transaction;
- domain services — application, profile, and automatic-mode mutations;
- `ApplicationStateController` — runtime product state;
- `ForegroundWindowTracker` — foreground detection and deduplication;
- `ApplicationDiscovery` and `ApplicationIdentityCache` — derived runtime identity resolution;
- `OverlayBoundsResolver` — overlay geometry;
- `OverlayController` — overlay lifetime and retargeting;
- `MagnifierOverlay` — native overlay rendering and target tracking;
- `ApplicationProfilesGrid` — application-table presentation and edit mechanics;
- `ConfigurationForm` — UI use-case orchestration.

The complete current map is maintained in [docs/ARCHITECTURE_0.4.md](docs/ARCHITECTURE_0.4.md). Historical audits remain available for traceability but are not the source of truth for the current architecture.

## Current limitations

- only one foreground target is corrected at a time;
- profile import and export are not implemented;
- palette analysis, dominant-color extraction, targeted color rules, and LUT import are not implemented;
- protected or DRM-controlled content may not be capturable;
- elevated applications may require SightAdapt to run at a compatible integrity level;
- remote-desktop sessions and some graphics drivers may not support the magnifier control correctly;
- the current Magnification API renderer has not completed the endurance and compatibility requirements defined for a public Light release.

## Project direction

The current alpha validates application assignment, settings, safety, UI, overlay geometry, and switching behavior with minimal dependencies. Advanced palette analysis, LUT processing, and targeted color correction are expected to require Windows Graphics Capture, Direct3D 11, and HLSL rather than extending the current 5×5 color matrix beyond its representational limits.

## License

SightAdapt is licensed under the MIT License. See [LICENSE](LICENSE).