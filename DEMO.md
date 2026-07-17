# SightAdapt Demo 0.2

This is the second executable proof of concept for SightAdapt. It adds persistent per-application profiles and automatic inversion to the original active-window overlay demo.

## Implemented features

- runs in the Windows notification area;
- tracks the currently active top-level application window;
- toggles color inversion for that window manually;
- stores per-application inversion profiles;
- automatically enables inversion when a configured application becomes active;
- adds the active application with a global keyboard shortcut;
- provides a configuration panel for adding, enabling, disabling, and removing profiles;
- allows selecting an application executable through a file picker;
- stores settings in `%LOCALAPPDATA%\SightAdapt\settings.json`;
- keeps the overlay aligned while the target window moves or resizes;
- passes mouse and keyboard input through to the original application;
- hides the overlay when the target is minimized or no longer active;
- provides an emergency shortcut that disables the overlay and automatic mode;
- supports per-monitor DPI awareness;
- targets Windows 10 version 2004 or newer and Windows 11.

## Controls

| Action | Preferred shortcut | Fallback shortcut |
|---|---|---|
| Toggle inversion for the active window | `Ctrl+Win+2` | `Ctrl+Alt+I` |
| Add the active application to automatic inversion | `Ctrl+Alt+Shift+I` | Tray menu or configuration panel |
| Emergency disable and turn off automatic mode | `Ctrl+Win+Shift+2` | `Ctrl+Alt+Shift+F12` |

The toggle and emergency commands use their fallback shortcut when Windows or another application reserves the preferred shortcut. The add-application shortcut is intentionally fixed to `Ctrl+Alt+Shift+I`.

The same commands are available from the notification-area icon. The tray menu also contains:

- **Automatic mode** — globally enables or disables automatic profiles;
- **Configure applications...** — opens the profile panel;
- **Add current application** — adds the most recently active supported application;
- **Emergency disable all overlays** — removes the current overlay and turns automatic mode off.

## Profile behavior

Pressing `Ctrl+Alt+Shift+I` while an application is active:

1. resolves the application's process and full executable path;
2. adds or updates its inversion profile;
3. enables that profile;
4. enables automatic mode;
5. saves the settings file.

Profiles are matched primarily by full executable path. This avoids applying a profile to an unrelated executable that happens to use the same file name.

Manual inversion remains available through `Ctrl+Alt+I`. Manually disabling an automatically applied overlay suppresses it until the foreground application changes.

## Settings file

Settings are stored per Windows user at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

Example:

```json
{
  "automaticMode": true,
  "applications": [
    {
      "displayName": "Notepad",
      "executableName": "notepad.exe",
      "executablePath": "C:\\Windows\\System32\\notepad.exe",
      "enabled": true,
      "effect": "invert"
    }
  ]
}
```

The file is written atomically through a temporary file to reduce the risk of partial settings after interruption.

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

The demo targets .NET 8. Its implementation uses standard .NET APIs, WinForms, `System.Text.Json`, and explicit Win32 interop so that a later move to .NET 10 does not require a platform redesign.

## Technical scope

The demo intentionally uses the Windows Magnification API because it provides the shortest dependency-free route to validating the overlay interaction model and inversion color matrix.

This is not the final Light capture backend. The planned production architecture remains:

- Windows Graphics Capture;
- Direct3D 11;
- GPU shader and LUT processing;
- native overlay composition.

The demo code isolates the current overlay implementation so it can later be replaced without changing user-facing controls or the profile format.

## Known limitations

- only one window can be inverted at a time;
- only color inversion is implemented;
- the effect is visible only while the selected target is the foreground window;
- owned dialogs and pop-up windows are not automatically included;
- the current profile resolver is intended primarily for classic desktop `.exe` applications;
- packaged Microsoft Store applications and applications that use launcher or host processes may require additional identity handling;
- protected or DRM-controlled content may not be capturable;
- elevated applications may not work from a non-elevated SightAdapt process;
- some GPU drivers or remote-desktop sessions may not support the magnifier control correctly;
- this proof of concept has not yet completed the endurance and compatibility testing required by `LIGHT.md`.

## Safety behavior

The overlay never intentionally receives input. If the target window becomes invalid, minimized, hidden, or inactive, the overlay is hidden or closed. The emergency shortcut and tray command remove the current overlay and turn automatic mode off so a configured application cannot immediately reactivate the effect.
