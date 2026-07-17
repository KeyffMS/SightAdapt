# SightAdapt 0.3.1 Alpha

SightAdapt 0.3.1 Alpha is the current executable Windows build. It validates per-window inversion, persistent application profiles, automatic activation, settings migration, a modern dark interface, and the architecture boundary required for configurable color profiles in 0.4.

## Implemented features

- runs in the Windows notification area;
- uses a modern dark interface for the tray menu and application-profile panel;
- tracks the currently active top-level application window;
- toggles color inversion locally for that window;
- stores persistent per-application profile assignments;
- automatically enables inversion when an enabled application profile becomes active;
- toggles the current application's saved profile with a dedicated shortcut;
- keeps the overlay aligned while the target window moves or resizes;
- passes mouse and keyboard input through to the original application;
- hides the overlay when the target is minimized or no longer active;
- stores versioned per-user JSON settings in `%LOCALAPPDATA%\SightAdapt\settings.json`;
- migrates older `effect: invert` settings to visual-profile assignments;
- provides emergency shutdown from the notification-area menu;
- supports per-monitor DPI awareness;
- targets Windows 10 version 2004 or newer and Windows 11.

## Keyboard controls

SightAdapt registers exactly two global shortcuts:

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Locally enable or disable inversion for the active window without changing its saved profile |
| `Ctrl+Alt+Shift+I` | Toggle the active application's persistent automatic profile in settings |

The persistent toggle behaves as follows:

1. when no assignment exists, SightAdapt creates and enables one;
2. when the assignment is enabled, SightAdapt disables it;
3. when the assignment is disabled, SightAdapt enables it again;
4. enabling an assignment also enables automatic mode;
5. disabling the assignment immediately stops its automatically activated overlay.

No alternative `Ctrl+Win` shortcuts or emergency keyboard shortcut are registered.

## Notification-area menu

The tray menu provides:

- local inversion toggle;
- persistent automatic-profile toggle for the current application;
- global automatic-mode switch;
- application configuration panel;
- emergency shutdown of all overlays and automatic mode;
- application exit.

Double-clicking the tray icon performs the local inversion toggle.

## Application profiles

Use **Configure applications...** from the notification-area menu to open the profile panel. The panel provides:

- a global automatic-mode switch;
- a dark, DPI-aware application list;
- adding the currently active application;
- selecting an application executable manually;
- enabling or disabling individual profiles;
- removing profiles.

Settings are stored at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

The configuration uses `System.Text.Json` and contains no captured screen content.

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

The current alpha uses the Windows Magnification API because it provides a dependency-free route to validating overlay interaction and color matrices.

Version 0.3.1 separates application state, overlay lifetime, visual transformations, persistent assignments, visual profiles, product metadata, and settings migration. See [`docs/ARCHITECTURE_0.3.1.md`](docs/ARCHITECTURE_0.3.1.md).

This is not the final Light capture backend. The planned production architecture remains:

- Windows Graphics Capture;
- Direct3D 11;
- GPU shader and LUT processing;
- native overlay composition.

## Known limitations

- only one window can be inverted at a time;
- only color inversion is implemented;
- the effect is visible only while the selected target is the foreground window;
- owned dialogs and pop-up windows are not automatically included;
- application identity is based on the executable path;
- protected or DRM-controlled content may not be capturable;
- elevated applications may not work from a non-elevated SightAdapt process;
- some GPU drivers or remote-desktop sessions may not support the magnifier control correctly;
- this alpha has not yet completed the endurance and compatibility testing required by `LIGHT.md`.

## Safety behavior

The overlay never intentionally receives input. If the target window becomes invalid, minimized, hidden, or inactive, the overlay is hidden or closed. The tray-menu emergency command removes the current overlay and disables automatic mode.
