# SightAdapt Demo

This is the first executable proof of concept for SightAdapt.

## Implemented features

- runs in the Windows notification area;
- tracks the currently active top-level application window;
- toggles color inversion for that window;
- keeps the overlay aligned while the target window moves or resizes;
- passes mouse and keyboard input through to the original application;
- hides the overlay when the target is minimized or no longer active;
- provides an emergency shortcut that disables the overlay;
- supports per-monitor DPI awareness;
- targets Windows 10 version 2004 or newer and Windows 11.

## Controls

| Action | Preferred shortcut | Fallback shortcut |
|---|---|---|
| Toggle inversion for the active window | `Ctrl+Win+2` | `Ctrl+Alt+I` |
| Disable every overlay | `Ctrl+Win+Shift+2` | `Ctrl+Alt+Shift+I` |

The fallback is registered automatically when Windows or another application reserves the preferred shortcut. The same commands are available from the notification-area icon.

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
- protected or DRM-controlled content may not be capturable;
- elevated applications may not work from a non-elevated SightAdapt process;
- some GPU drivers or remote-desktop sessions may not support the magnifier control correctly;
- this proof of concept has not yet completed the endurance and compatibility testing required by `LIGHT.md`.

## Safety behavior

The overlay never intentionally receives input. If the target window becomes invalid, minimized, hidden, or inactive, the overlay is hidden or closed. The emergency shortcut and tray command always remove the current overlay.
