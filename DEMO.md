# SightAdapt 0.4B.2 Alpha

SightAdapt 0.4B.2 is the current accepted Windows development build. It applies configurable visual profiles and overlay geometry to selected application windows while preserving per-application assignments, automatic activation, settings migration, fast foreground switching, and an input-transparent overlay.

## Build identity

```text
Product version: 0.4.0-alpha.6+<commit>
File version:    0.4.0.2
Milestone:       Alpha 0.4B.2 · Faster overlay switching
Settings schema: 4
```

The commit suffix in `ProductVersion` identifies the exact source revision used to build the executable.

## Implemented features

- runs in the Windows notification area;
- enforces one running process per user session;
- detects the active supported top-level window every 75 ms;
- suppresses duplicate foreground notifications for an unchanged handle;
- reuses cached application identity data on the foreground hot path;
- applies assigned profiles in local or automatic mode;
- provides fixed `Exact invert`, editable `Soft invert`, and independent user-defined Soft Invert profiles;
- creates, duplicates, renames, edits, assigns, and deletes user-defined profiles;
- adjusts output black, output white, brightness, contrast, saturation, and hue;
- previews grayscale and hue-spectrum conversion;
- stores a separate overlay scope for every application assignment;
- reuses and retargets one native magnifier overlay during normal switching;
- keeps the last rendered frame briefly while a new foreground target is resolved;
- tracks target move, resize, minimize, restore, foreground, and close state;
- passes mouse and keyboard input through to the original application;
- stores settings atomically in `%LOCALAPPDATA%\SightAdapt\settings.json`;
- migrates and normalizes older settings without discarding valid assignments;
- provides immediate emergency shutdown before settings persistence;
- distinguishes renderer fault from explicit emergency shutdown;
- supports per-monitor DPI awareness;
- targets x64 Windows 10 version 2004 or newer and Windows 11.

## Keyboard controls

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Locally enable or disable visual correction for the active window without changing persistent settings |
| `Ctrl+Alt+Shift+I` | Add, disable, or re-enable the active application's persistent automatic assignment |

A disabled assignment remains available for local use but does not activate automatically. Enabling an assignment also enables automatic mode.

No alternative `Ctrl+Win` shortcuts or emergency keyboard shortcut are registered.

## Notification-area menu

The tray menu provides:

- local visual-correction toggle;
- persistent automatic-assignment toggle for the current application;
- global automatic-mode switch;
- application and color-profile configuration;
- About dialog with product and commit-derived version information;
- emergency shutdown of the active overlay and automatic mode;
- application exit.

Left and right tray-icon clicks open the same menu. Double-clicking performs the local visual-correction toggle.

## Application configuration

Open **Configure applications and colors...** from the notification-area menu.

The application table supports:

- adding the currently active application;
- selecting an executable manually;
- enabling or disabling automatic assignment;
- assigning any available visual profile;
- selecting an independent overlay scope;
- editing a tunable profile;
- managing user-defined profiles;
- removing an application assignment.

The row stores only the executable path as its stable UI key. Committed settings remain the model source of truth.

### Overlay scope per application

| UI choice | Persisted ID | Result |
|---|---|---|
| Client area | `client-area` | Application content without title bar and frame; default |
| Full window | `window` | Complete visible application window |
| Current screen | `screen` | Complete monitor containing the target window |
| All screens | `all-screens` | Complete Windows virtual desktop |

Changing one row does not modify another application's scope. A scope change for the active application is applied through the existing committed-settings path.

## Visual profiles

### Built-in profiles

- `Exact invert` — fixed and protected from rename or deletion;
- `Soft invert` — editable shared default and protected from rename or deletion.

### Soft Invert defaults

```text
Output black: 8%
Output white: 92%
Brightness: 0%
Contrast: 100%
Saturation: 100%
Hue shift: 0°
```

The built-in Soft Invert profile is shared. Create or duplicate a user-defined profile when applications require independent values.

See:

- [`docs/SOFT_COLOR_PROFILES_0.4.md`](docs/SOFT_COLOR_PROFILES_0.4.md)
- [`docs/USER_DEFINED_PROFILES_0.4A.2.md`](docs/USER_DEFINED_PROFILES_0.4A.2.md)

## Settings consistency

Settings are stored at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

Schema `4` contains visual profiles, application assignments, automatic mode, and per-application `overlayScope` values. Mutations are applied to a working copy, normalized, saved atomically, and published only after the write succeeds. A failed domain operation or failed write leaves the last committed in-memory settings unchanged.

Older schemas are normalized to the current schema. Legacy `effect: "invert"` values migrate to `default-invert`, and assignments without an overlay scope receive `client-area`.

## Build and tests

Requirements:

- Windows 10 or Windows 11;
- Visual Studio with the .NET desktop workload, or the .NET 8 SDK;
- x64 target.

From the repository root:

```powershell
dotnet restore src/SightAdapt.Demo/SightAdapt.Demo.csproj
dotnet build src/SightAdapt.Demo/SightAdapt.Demo.csproj -c Release
dotnet test tests/SightAdapt.Tests/SightAdapt.Tests.csproj -c Release
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

## Verify the running build

```powershell
$process = Get-Process SightAdapt.Demo
$process.Path

(Get-Item $process.Path).VersionInfo |
    Format-List ProductVersion, FileVersion
```

For the current increment, `ProductVersion` begins with `0.4.0-alpha.6+` and `FileVersion` is `0.4.0.2`.

## Safety behavior

The overlay is created with input-transparent, non-activating window styles. Explicit disable and emergency shutdown remove it immediately. Emergency shutdown disables the overlay before attempting settings I/O and blocks automatic reactivation through the runtime emergency state.

During normal foreground switching, one existing overlay is retargeted. A rendered frame may remain visible for at most 125 ms while the next target is resolved. After that period, an invalid target closes the overlay and an unavailable target hides it.

## Known limitations

- only one foreground target is corrected at a time;
- profile import and export are not implemented;
- palette capture, dominant-color analysis, targeted color rules, and LUT import are not implemented;
- application identity is based on executable path, with a process-ID cache used only as a runtime optimization;
- protected or DRM-controlled content may not be capturable;
- elevated applications may not work from a lower-integrity SightAdapt process;
- some graphics drivers and remote-desktop sessions may not support the magnifier control correctly;
- the current alpha has not completed the endurance and compatibility requirements defined in `LIGHT.md`.