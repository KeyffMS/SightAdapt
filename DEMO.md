# SightAdapt Demo 0.2

This executable proof of concept validates per-window inversion, persistent application profiles, automatic activation, and a modern dark user interface for SightAdapt.

## Implemented features

- runs in the Windows notification area;
- uses a modern dark interface for the tray menu and application-profile panel;
- tracks the currently active top-level application window;
- toggles color inversion for that window;
- keeps the overlay aligned while the target window moves or resizes;
- passes mouse and keyboard input through to the original application;
- hides the overlay when the target is minimized or no longer active;
- stores per-application profiles in the current user's local settings directory;
- automatically enables inversion when a configured application becomes active;
- supports adding the active application through a global shortcut;
- provides an emergency shortcut that disables the overlay and automatic mode;
- supports per-monitor DPI awareness;
- targets Windows 10 version 2004 or newer and Windows 11.

## Controls

| Action | Preferred shortcut | Fallback shortcut |
|---|---|---|
| Toggle inversion for the active window | `Ctrl+Win+2` | `Ctrl+Alt+I` |
| Add or enable the active application profile | `Ctrl+Win+Shift+I` | `Ctrl+Alt+Shift+I` |
| Emergency disable and turn off automatic mode | `Ctrl+Win+Shift+2` | `Ctrl+Alt+Shift+F12` |

The fallback is registered automatically when Windows or another application reserves the preferred shortcut. The same commands are available from the notification-area icon.

## Application profiles

Use **Configure applications...** from the notification-area menu to open the profile panel. The panel provides:

- a global automatic-mode switch;
- a dark, DPI-aware application list;
- adding the currently active application;
- selecting an application executable manually;
- enabling or disabling individual profiles;
- removing profiles.

You can also press the add-application shortcut while an application window is active. SightAdapt records the executable name and full path and enables automatic mode.

Settings are stored at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

The configuration uses `System.Text.Json` and contains no captured screen content.

## Build

Requirements:

- Windows 10 or Windows 11;
- Visual Studio 2022 with the .NET desktop development workload, or the .NET 8 SDK;
- x64 build target.

From the repository root:

```powershell
dotnet restore src/SightAdapt.Demo/SightAdapt.Demo.csproj
dotnet build src/SightAdapt.Demo/SightAdapt.Demo.csproj -c Release
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

The demo intentionally uses the Windows Magnification API because it provides the shortest dependency-free route to validating the overlay interaction model and inversion color matrix.

The interface uses Windows Forms with custom owner-drawn controls, a shared dark color palette, a dark notification-area menu renderer, and DWM dark-title-bar integration. It does not introduce third-party UI dependencies and remains compatible with the planned migration from .NET 8 to .NET 10.

This is not the final Light capture backend. The planned production architecture remains:

- Windows Graphics Capture;
- Direct3D 11;
- GPU shader and LUT processing;
- native overlay composition.

The demo code isolates the current overlay implementation so it can later be replaced without changing user-facing controls.

## Known limitations

- only one window can be inverted at a time;
- only color inversion is implemented;
- the effect is visible only while the selected target is the foreground window;
- owned dialogs and pop-up windows are not automatically included;
- application identity is based on the executable path;
- protected or DRM-controlled content may not be capturable;
- elevated applications may not work from a non-elevated SightAdapt process;
- some GPU drivers or remote-desktop sessions may not support the magnifier control correctly;
- this proof of concept has not yet completed the endurance and compatibility testing required by `LIGHT.md`.

## Safety behavior

The overlay never intentionally receives input. If the target window becomes invalid, minimized, hidden, or inactive, the overlay is hidden or closed. The emergency shortcut and tray command remove the current overlay and disable automatic mode.
