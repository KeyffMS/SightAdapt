# SightAdapt 0.4 Alpha

SightAdapt 0.4 Alpha is the current executable Windows build. It adds configurable color profiles to per-window visual correction while preserving persistent application assignments, automatic activation, settings migration, two global toggle shortcuts, and the dark configuration interface.

## Implemented features

- runs in the Windows notification area;
- tracks the currently active top-level application window;
- applies an assigned visual profile in local or automatic mode;
- provides fixed `Exact invert` and editable `Soft invert` profiles;
- limits output black and output white levels;
- adjusts brightness, contrast, saturation, and hue;
- previews grayscale and hue-spectrum conversion in the profile editor;
- updates the color matrix of an active overlay without recreating the overlay;
- stores persistent per-application profile assignments;
- keeps the overlay aligned while the target window moves or resizes;
- passes mouse and keyboard input through to the original application;
- hides the overlay when the target is minimized or no longer active;
- stores schema-versioned JSON settings in `%LOCALAPPDATA%\SightAdapt\settings.json`;
- migrates older settings without changing existing Exact Invert assignments;
- provides emergency shutdown from the notification-area menu;
- supports per-monitor DPI awareness;
- targets Windows 10 version 2004 or newer and Windows 11.

## Keyboard controls

SightAdapt registers exactly two global shortcuts:

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Locally enable or disable visual correction for the active window without changing persistent settings |
| `Ctrl+Alt+Shift+I` | Add, disable, or re-enable the active application's persistent automatic assignment |

The local toggle uses the application's assigned visual profile when one exists. A disabled assignment can still provide its selected profile for local use, but it will not activate automatically.

The persistent toggle behaves as follows:

1. when no assignment exists, SightAdapt creates and enables one using Soft Invert;
2. when the assignment is enabled, SightAdapt disables it;
3. when the assignment is disabled, SightAdapt enables it again;
4. enabling an assignment also enables automatic mode;
5. disabling the assignment immediately stops an automatically activated overlay.

No alternative `Ctrl+Win` shortcuts or emergency keyboard shortcut are registered.

## Notification-area menu

The tray menu provides:

- local visual-correction toggle;
- persistent automatic-assignment toggle for the current application;
- global automatic-mode switch;
- application and color-profile configuration;
- emergency shutdown of all overlays and automatic mode;
- application exit.

Double-clicking the tray icon performs the local visual-correction toggle.

## Application and color profiles

Use **Configure applications and colors...** from the notification-area menu.

The configuration panel provides:

- a global automatic-mode switch;
- a dark, DPI-aware application table;
- adding the currently active application;
- selecting an executable manually;
- enabling or disabling individual automatic assignments;
- choosing `Exact invert` or `Soft invert` per application;
- opening the Soft Invert editor;
- removing application assignments.

Newly configured applications use Soft Invert by default. Existing assignments migrated from 0.3.1 retain Exact Invert.

### Soft Invert defaults

```text
Output black: 8%
Output white: 92%
Brightness: 0%
Contrast: 100%
Saturation: 100%
Hue shift: 0°
```

The built-in Soft Invert profile is currently shared. Editing it affects every application assigned to `default-soft-invert`.

Detailed behavior is documented in [`docs/SOFT_COLOR_PROFILES_0.4.md`](docs/SOFT_COLOR_PROFILES_0.4.md).

## Settings

Settings are stored at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

Version 0.4 uses schema `3`. The configuration uses `System.Text.Json` and contains no captured screen content.

## Build and tests

Requirements:

- Windows 10 or Windows 11;
- Visual Studio 2022 with the .NET desktop development workload, or the .NET 8 SDK;
- x64 build target.

From the repository root:

```powershell
dotnet restore src/SightAdapt.Demo/SightAdapt.Demo.csproj
dotnet build src/SightAdapt.Demo/SightAdapt.Demo.csproj -c Release
dotnet test tests/SightAdapt.Tests/SightAdapt.Tests.csproj -c Release
```

Run:

```powershell
dotnet run --project src/SightAdapt.Demo/SightAdapt.Demo.csproj -c Release
```

Create a self-contained x64 build:

```powershell
dotnet publish src/SightAdapt.Demo/SightAdapt.Demo.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -o artifacts/win-x64
```

## Technical scope

The current alpha uses the Windows Magnification API because it provides a dependency-free route to validating overlay interaction and affine color matrices. Soft Invert composes output limits, saturation, hue rotation, contrast, and brightness into one `MAGCOLOREFFECT` matrix.

Version 0.3.1 established the state, overlay, transform, profile, and migration boundaries described in [`docs/ARCHITECTURE_0.3.1.md`](docs/ARCHITECTURE_0.3.1.md). Version 0.4 builds on those boundaries without adding a dependency-injection framework or a speculative renderer abstraction.

This is not the final Light capture backend. The planned production architecture remains:

- Windows Graphics Capture;
- Direct3D 11;
- GPU shader and LUT processing;
- native overlay composition.

## Known limitations

- only one target window can be corrected at a time;
- the effect is visible only while the selected target is the foreground window;
- the current editor modifies one shared built-in Soft Invert profile;
- user-defined profile creation, duplication, renaming, deletion, import, and export are not implemented yet;
- palette capture, dominant-color analysis, targeted source-to-output color rules, and LUTs are not implemented yet;
- owned dialogs and pop-up windows are not automatically included;
- application identity is based on the executable path;
- protected or DRM-controlled content may not be capturable;
- elevated applications may not work from a non-elevated SightAdapt process;
- some GPU drivers or remote-desktop sessions may not support the magnifier control correctly;
- this alpha has not yet completed the endurance and compatibility testing required by `LIGHT.md`.

## Safety behavior

The overlay never intentionally receives input. If the target window becomes invalid, minimized, hidden, or inactive, the overlay is hidden or closed. The tray-menu emergency command removes the current overlay and disables automatic mode.
