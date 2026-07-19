# SightAdapt 0.4 Alpha

SightAdapt 0.4 Alpha is the current executable Windows build. It applies configurable visual profiles to selected Windows application windows while preserving per-application assignments, automatic activation, settings migration, two global shortcuts, and an input-transparent overlay.

## Implemented features

- runs in the Windows notification area;
- enforces a single running process per user session;
- tracks the active top-level application window;
- applies assigned profiles in local or automatic mode;
- provides fixed `Exact invert` and editable `Soft invert` profiles;
- supports independent user-defined Soft Invert profiles;
- creates, duplicates, renames, edits, assigns, and deletes user-defined profiles;
- limits output black and output white levels;
- adjusts brightness, contrast, saturation, and hue;
- previews grayscale and hue-spectrum conversion;
- updates an active overlay without recreating it when only the profile changes;
- stores persistent per-application assignments;
- keeps the overlay aligned while the target moves or resizes;
- passes mouse and keyboard input through to the original application;
- hides the overlay when the target is minimized or inactive;
- stores schema-versioned JSON settings in `%LOCALAPPDATA%\SightAdapt\settings.json`;
- migrates older settings without changing existing Exact Invert assignments;
- commits settings through a write-before-publish transaction boundary;
- provides immediate emergency shutdown from the notification-area menu;
- distinguishes an explicit emergency shutdown from a renderer fault;
- supports per-monitor DPI awareness;
- targets Windows 10 version 2004 or newer and Windows 11.

## Keyboard controls

SightAdapt registers exactly two global shortcuts:

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Locally enable or disable visual correction for the active window without changing persistent settings |
| `Ctrl+Alt+Shift+I` | Add, disable, or re-enable the active application's persistent automatic assignment |

The local toggle uses the application's assigned profile when one exists. A disabled assignment remains available for local use but does not activate automatically.

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
- assigning any available visual profile;
- opening the editor for tunable profiles;
- creating, duplicating, renaming, editing, and deleting user-defined profiles;
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

The built-in Soft Invert profile is shared. Editing it affects every application assigned to `default-soft-invert`. Create or duplicate a user-defined profile when applications need independent parameter sets.

Detailed behavior is documented in [`docs/SOFT_COLOR_PROFILES_0.4.md`](docs/SOFT_COLOR_PROFILES_0.4.md) and [`docs/USER_DEFINED_PROFILES_0.4A.2.md`](docs/USER_DEFINED_PROFILES_0.4A.2.md).

## Settings consistency

Settings are stored at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

Version 0.4 uses schema `3`. Mutations are applied to a working snapshot, normalized, written atomically, and published to the running application only after the write succeeds. A failed domain operation or failed write leaves the last committed in-memory settings unchanged.

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

The current alpha uses the Windows Magnification API to validate overlay interaction and affine color matrices without third-party runtime dependencies. Soft Invert composes output limits, saturation, hue rotation, contrast, and brightness into one `MAGCOLOREFFECT` matrix.

The production Light architecture remains planned around Windows Graphics Capture, Direct3D 11, GPU shader and LUT processing, and native overlay composition.

## Known limitations

- only one target window can be corrected at a time;
- the effect is visible only while the selected target is the foreground window;
- profile import and export are not implemented yet;
- palette capture, dominant-color analysis, targeted source-to-output rules, and LUTs are not implemented yet;
- owned dialogs and pop-up windows are not automatically included;
- application identity is based on the executable path;
- protected or DRM-controlled content may not be capturable;
- elevated applications may not work from a non-elevated SightAdapt process;
- some GPU drivers or remote-desktop sessions may not support the magnifier control correctly;
- this alpha has not completed the endurance and compatibility testing required by `LIGHT.md`.

## Safety behavior

The overlay never intentionally receives input. If the target becomes invalid, minimized, hidden, or inactive, the overlay is hidden or closed. Emergency shutdown disables the overlay before any settings I/O, enters a persistent runtime emergency state, and then attempts to save automatic mode as disabled. A renderer failure uses a separate fault state and does not falsely report that automatic mode was persisted as off.
