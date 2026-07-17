# SightAdapt

SightAdapt is an open-source visual accessibility application for Windows 10 and Windows 11. It changes how selected application windows are displayed, even when those applications do not provide suitable accessibility or appearance settings.

The project is developed in two stages:

1. **Light** — a fast and stable overlay that applies color inversion, color transforms, or LUTs to a selected application window.
2. **Hard** — an extended accessibility shell built only after the Light version is proven stable.

## Project goal

SightAdapt is intended to help people with low vision use applications that:

- do not provide a high-contrast mode;
- do not support accessibility themes;
- use unreadable color combinations;
- do not allow per-user image correction;
- do not provide suitable magnification or focus-highlighting options.

SightAdapt does not modify another application's files or process memory. It captures the target window, processes its image, and displays the result in a separate input-transparent overlay.

## Current demo

The repository contains **SightAdapt Demo 0.2** in [`src/SightAdapt.Demo`](src/SightAdapt.Demo). It runs in the notification area and applies color inversion to application windows through an input-transparent overlay.

Demo 0.2 adds:

- persistent per-application profiles;
- automatic inversion when a configured application becomes active;
- `Ctrl+Alt+Shift+I` to add the active application;
- a WinForms configuration panel;
- per-user JSON settings stored in `%LOCALAPPDATA%\SightAdapt\settings.json`;
- an emergency action that disables the overlay and automatic mode.

See [DEMO.md](DEMO.md) for controls, build instructions, profile behavior, architecture notes, and known limitations.

The demo uses the Windows Magnification API to validate the interaction model with minimal code and no third-party runtime dependencies. The production Light implementation is still planned around Windows Graphics Capture and Direct3D 11.

## Documentation

- [LIGHT.md](LIGHT.md) — scope, architecture, safety requirements, tests, and completion criteria for the Light version.
- [HARD.md](HARD.md) — target architecture for the extended accessibility shell.

## Core assumptions

- primary language: **C#**;
- current demo runtime: **.NET 8 x64**;
- planned production migration target: **.NET 10 x64**;
- current demo settings UI: **WinForms**;
- planned production settings UI: **WPF**;
- system integration: **Win32**;
- window capture: **Windows Graphics Capture**;
- rendering: **Direct3D 11 + HLSL**;
- overlay: native Win32 window backed by a GPU surface;
- initial compatibility target: **Windows 10 22H2**;
- parallel compatibility target: **Windows 11**;
- no DLL injection;
- no kernel drivers;
- no screen-image storage;
- no mandatory telemetry;
- all source code published on GitHub.

## Priorities

The project priorities are, in order:

1. user safety;
2. immediate emergency shutdown of all visual effects;
3. overlay stability;
4. no interference with the target application;
5. low latency;
6. low CPU and GPU overhead;
7. predictable behavior;
8. feature count only after the previous requirements are met.

## Demo 0.2 controls

| Shortcut | Action |
|---|---|
| `Ctrl+Win+2` or `Ctrl+Alt+I` | Manually enable or disable inversion for the active window |
| `Ctrl+Alt+Shift+I` | Add the active application to automatic inversion |
| `Ctrl+Win+Shift+2` or `Ctrl+Alt+Shift+F12` | Emergency shutdown and disable automatic mode |

The configuration panel and the same commands are also available from the notification-area icon. Future production shortcuts remain configurable by design.

## Development model

### Phase A — Light

The Light version must provide:

- capture of a selected application window;
- color inversion;
- LUT support;
- a global enable/disable shortcut;
- automatic tracking of window position and size;
- complete mouse and keyboard pass-through;
- flicker-free operation;
- multi-monitor support;
- mixed-DPI support;
- stable operation for many hours.

Development of Hard-only functionality must not begin before the stability criteria in [LIGHT.md](LIGHT.md) are met.

### Phase B — Hard

The Hard version adds a semantic accessibility layer and additional assistive tools:

- management of multiple active overlays;
- focus highlighting;
- magnification;
- a large-text panel;
- UI Automation integration;
- extended per-application profile rules;
- optional local OCR;
- optional text-to-speech;
- rules for dialogs, owned windows, and child windows.

## Proposed repository structure

```text
SightAdapt/
├── README.md
├── LICENSE
├── SECURITY.md
├── CONTRIBUTING.md
├── docs/
│   ├── LIGHT.md
│   ├── HARD.md
│   ├── architecture/
│   └── adr/
├── src/
│   ├── SightAdapt.App/
│   ├── SightAdapt.Core/
│   ├── SightAdapt.Platform.Win32/
│   ├── SightAdapt.Capture.Wgc/
│   ├── SightAdapt.Rendering.D3D11/
│   ├── SightAdapt.Overlays/
│   └── SightAdapt.Accessibility/
├── tests/
│   ├── SightAdapt.UnitTests/
│   ├── SightAdapt.IntegrationTests/
│   └── TestApplications/
└── tools/
```

## License

Recommended license: **MIT**.

It permits:

- private and commercial use;
- modification;
- redistribution;
- forks;
- integration with other accessibility projects.

The repository should also contain:

- `SECURITY.md`;
- a privacy policy;
- a threat-model document;
- a statement that protected or DRM-controlled content may not be capturable;
- a statement describing limitations when the target application runs with elevated privileges.

## Definition of done for Light

The Light version is ready for public release only when:

- normal operation does not require administrator privileges;
- it runs on Windows 10 22H2 and supported Windows 11 versions;
- the user can always disable overlays with the emergency shortcut;
- renderer failure does not block the desktop;
- overlays never capture input;
- the image pipeline does not copy every frame to CPU memory;
- the application correctly handles window move, resize, minimize, restore, and close events;
- an eight-hour endurance test shows no continuously increasing memory usage;
- the overlay is not recursively captured;
- a profile can be assigned to an application executable;
- settings can be imported and exported.

## Document status

This documentation defines the initial engineering baseline. Significant architecture decisions should be recorded as Architecture Decision Records in `docs/adr`.
