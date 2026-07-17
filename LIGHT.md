# SightAdapt Light

## 1. Purpose

SightAdapt Light is the minimal standalone edition of SightAdapt. Its task is to change the visual appearance of one or more application windows without modifying the source process.

Primary user flow:

1. The user activates any application window.
2. The user presses `Ctrl+Win+2`.
3. SightAdapt resolves the active top-level window.
4. SightAdapt starts capturing that specific window.
5. The active visual profile is applied on the GPU.
6. The processed image is displayed in an overlay aligned with the source window.
7. Pressing the shortcut again disables the effect.

The first milestone is intentionally narrow: make per-window inversion and LUT processing reliable enough to use throughout a normal workday.

## 2. Light scope

### 2.1 Required features

- capture a specific window identified by `HWND`;
- invert colors;
- load and apply LUT files;
- adjust brightness;
- adjust contrast;
- adjust saturation;
- optionally convert the image to grayscale;
- enable or disable processing instantly;
- register configurable global keyboard shortcuts;
- create per-application profiles;
- align the overlay with the source window;
- react to move, resize, minimize, restore, hide, show, and close events;
- support multiple monitors;
- support mixed DPI scaling;
- run in the background;
- provide a system tray icon;
- optionally start when the user signs in;
- write diagnostic logs that never contain captured images;
- store configuration in versioned JSON;
- import and export profiles.

### 2.2 Explicitly out of scope

The Light version will not implement:

- OCR;
- text-to-speech;
- direct modification of fonts inside another application;
- layout or control-size modification;
- automated clicking;
- remapping mouse coordinates for a scaled interactive view;
- DLL injection;
- process-memory modification;
- kernel-mode drivers;
- screen recording;
- remote image transfer;
- a full accessibility-shell editor;
- semantic UI Automation features beyond basic target-window identification.

## 3. Supported platforms

### 3.1 Baseline platform

Initial test baseline:

- Windows 10 22H2 x64.

Windows 10 is the first compatibility target because many potential users remain dependent on applications and workstations deployed on that system.

### 3.2 Windows 11 compatibility

The implementation must preserve Windows 11 compatibility wherever possible by following these rules:

- use documented Win32, WinRT, DirectX, and .NET APIs;
- avoid undocumented window-manager structures;
- do not assume fixed border or title-bar dimensions;
- use per-monitor DPI awareness;
- detect optional features at runtime instead of relying only on OS version checks;
- do not depend on the visual design or location of the taskbar;
- keep capture and rendering backends behind interfaces;
- test every public release on Windows 10 and Windows 11.

## 4. Technology decisions

### 4.1 Language and runtime

- C#;
- .NET 10;
- x64 only;
- self-contained publishing as the default release format.

An x86 build is not planned. Native x64 reduces interoperability complexity and avoids limitations present in older magnification and capture paths.

### 4.2 User interface

- WPF for settings and profile management;
- native Win32 windows for overlays;
- system tray integration;
- no permanently visible main window.

WPF is used only for configuration. The overlay renderer must not depend on WPF composition.

### 4.3 Win32 interoperability

Recommended approach:

- `Microsoft.Windows.CsWin32` for generated P/Invoke declarations;
- small project-owned wrappers around handles and unmanaged resources;
- `SafeHandle` where practical;
- deterministic disposal for COM and GPU objects;
- no large hand-maintained P/Invoke layer unless an API is unavailable through generation.

### 4.4 Capture backend

Primary backend:

- Windows Graphics Capture;
- `IGraphicsCaptureItemInterop::CreateForWindow`;
- `Direct3D11CaptureFramePool`;
- frames retained as GPU textures.

Planned fallback after the primary backend is stable:

- Desktop Duplication API.

The fallback is not part of the first MVP because it captures a monitor rather than a logical window and requires additional cropping, occlusion, and multi-monitor logic.

### 4.5 Rendering

- Direct3D 11;
- DXGI;
- HLSL pixel shaders;
- Vortice.Windows as the preferred .NET binding;
- a single full-window quad rendered into the overlay surface;
- no full-frame copy into managed arrays;
- no CPU-based per-pixel loop.

### 4.6 Overlay window

Each overlay is a separate native window that is:

- borderless;
- non-activating;
- excluded from `Alt+Tab`;
- unable to receive keyboard focus;
- input-transparent;
- synchronized with the source window;
- hidden while the source is minimized or hidden;
- destroyed when its source `HWND` becomes invalid.

The default visual scale is 1:1. Whole-window interactive magnification is not part of Light because scaling breaks the relationship between visible coordinates and the original application's input coordinates.

## 5. Architecture

```text
Global Hotkey
     │
     ▼
Active Window Resolver
     │
     ▼
Profile Matcher
     │
     ▼
Overlay Session Manager
     │
     ├── Window Tracker
     ├── Capture Session
     ├── Effect Pipeline
     └── Overlay Window
```

### 5.1 Modules

#### `SightAdapt.App`

Responsibilities:

- process startup and shutdown;
- system tray;
- settings window;
- profile editor;
- global shortcuts;
- autostart configuration;
- user-facing error messages.

#### `SightAdapt.Core`

Responsibilities:

- profile models;
- configuration schema;
- validation;
- matching rules;
- session state;
- platform-independent business logic.

`SightAdapt.Core` must not reference WPF, Win32 UI types, or Direct3D.

#### `SightAdapt.Platform.Win32`

Responsibilities:

- window enumeration;
- active-window resolution;
- process identification;
- executable-path lookup;
- WinEvent subscriptions;
- DPI handling;
- visible window bounds;
- overlay styles;
- Z-order synchronization;
- global hotkey registration.

#### `SightAdapt.Capture.Wgc`

Responsibilities:

- create a `GraphicsCaptureItem` from `HWND`;
- create and manage the frame pool;
- expose new GPU frames;
- react to source-size changes;
- stop capture cleanly;
- handle capture-session termination;
- recover from device-loss scenarios when possible.

#### `SightAdapt.Rendering.D3D11`

Responsibilities:

- create the Direct3D device;
- create swap chains and render targets;
- compile or load shaders;
- update effect constant buffers;
- load and cache LUT textures;
- render and present frames;
- collect CPU/GPU timing metrics.

#### `SightAdapt.Overlays`

Responsibilities:

- create overlay windows;
- attach render surfaces;
- maintain correct position and dimensions;
- maintain appropriate Z-order;
- show, hide, and dispose overlays safely;
- prevent recursive capture where the platform allows it.

## 6. Window tracking

The implementation should be event-driven.

Primary system events:

- `EVENT_SYSTEM_FOREGROUND`;
- `EVENT_OBJECT_LOCATIONCHANGE`;
- `EVENT_OBJECT_SHOW`;
- `EVENT_OBJECT_HIDE`;
- `EVENT_OBJECT_DESTROY`;
- minimize and restore events.

Use `SetWinEventHook` with an out-of-context callback. The callback must do minimal work and enqueue updates for a dedicated application thread.

A low-frequency verification timer, for example once per second, may be used only to detect missed events. It must not be the main tracking mechanism.

### 6.1 Window bounds

Preferred order:

1. `DwmGetWindowAttribute` with `DWMWA_EXTENDED_FRAME_BOUNDS`;
2. fallback to `GetWindowRect`;
3. apply DPI-aware correction where required.

The implementation must not assume that `GetWindowRect` exactly matches the visible frame.

## 7. DPI model

The process must be per-monitor DPI aware.

Requirements:

- declare DPI awareness in the application manifest;
- use physical pixel coordinates for overlay placement and capture surfaces;
- react when the source window moves to a monitor with different DPI;
- test 100%, 125%, 150%, 175%, and 200% scaling;
- support monitor arrangements with different scaling values;
- avoid mixing logical WPF coordinates with physical Win32 coordinates without explicit conversion.

## 8. Effect pipeline

Recommended processing order:

```text
Source texture
    ↓
Input normalization
    ↓
Color matrix
    ↓
Brightness and contrast
    ↓
Gamma
    ↓
LUT
    ↓
Optional sharpening
    ↓
Output surface
```

The first public MVP needs only:

- inversion;
- grayscale;
- brightness;
- contrast;
- saturation;
- LUT.

### 8.1 Color inversion

Basic inversion:

```hlsl
rgb = 1.0 - rgb;
```

The alpha channel must not be inverted.

The project may later add luminance-preserving or perceptual inversion profiles, but they are not required for the initial implementation.

### 8.2 LUT support

Initial supported format:

- `.cube` 3D LUT.

Optional internal representation:

- versioned JSON describing matrices, curves, and parameters.

Requirements:

- validate dimensions and data count;
- enforce a maximum file size;
- reject malformed values;
- show a clear error message;
- never execute code from a profile or LUT file;
- cache uploaded LUT textures;
- release unused GPU resources.

## 9. Profile model

Example:

```json
{
  "schemaVersion": 1,
  "id": "accounting-high-contrast",
  "name": "Accounting — high contrast",
  "enabled": true,
  "match": {
    "executablePath": "C:\\Program Files\\Vendor\\App.exe",
    "processName": "App",
    "windowClass": null,
    "titleRegex": null
  },
  "scope": "activeWindow",
  "effects": {
    "invert": true,
    "grayscale": 0.0,
    "brightness": 0.0,
    "contrast": 1.2,
    "saturation": 1.0,
    "gamma": 1.0,
    "lutPath": null
  },
  "hotkey": null
}
```

### 9.1 Matching priority

1. full executable path;
2. process name;
3. window class;
4. window-title rule.

Window titles are unstable and should be used only as an optional additional condition. Regular expressions must have a timeout to prevent catastrophic backtracking.

## 10. Session lifecycle

One overlay session corresponds to one source top-level window.

Suggested lifecycle:

```text
Created
  ↓
Starting
  ↓
Running
  ↓
Suspended
  ↓
Stopping
  ↓
Disposed
```

Expected failure reasons:

- `TargetWindowClosed`;
- `CaptureUnavailable`;
- `DeviceLost`;
- `OverlayCreationFailed`;
- `ProtectedContent`;
- `PermissionDenied`;
- `UnsupportedSystem`.

Every failure path must hide or destroy the overlay before showing an error.

## 11. User safety

### 11.1 Emergency shortcut

`Ctrl+Win+Shift+2` must:

- hide or destroy every overlay immediately;
- stop every capture session;
- work even when the settings UI is unresponsive;
- avoid blocking on profile persistence;
- remain registered whenever the main process is running.

### 11.2 Internal watchdog

The application should:

- detect when frame presentation has stopped;
- hide a stale overlay after a short timeout;
- recreate Direct3D resources after recoverable device loss;
- never leave a frozen image covering the target application;
- terminate a failed session independently of other sessions.

### 11.3 Non-interference policy

Forbidden in Light:

- global mouse hooks;
- DLL injection;
- target-process memory modification;
- undocumented messages;
- modification of source-window styles;
- administrator elevation without a specific documented need.

## 12. Privacy

- captured frames are not saved;
- captured frames remain on the GPU whenever possible;
- no screenshot feature in Light;
- no upload functionality;
- no default telemetry;
- logs contain only technical data such as error codes, application version, and GPU information;
- executable paths should be redactable in diagnostic reports;
- debug capture tools must never be enabled in production by default.

## 13. Background operation

- run as a normal interactive user process;
- provide a system tray icon;
- optionally start at sign-in;
- do not use a Windows service for capture or overlays;
- do not require administrator rights for normal targets;
- start rendering only for enabled sessions;
- suspend or stop rendering when the target is hidden or minimized.

## 14. Performance requirements

Target for one 1920×1080 overlay on a typical office computer:

- average CPU usage below 5% during ordinary operation;
- GPU-based image processing;
- no managed allocation per frame;
- no full-frame GPU-to-CPU copy;
- additional latency preferably below one display frame;
- resource use that returns to baseline when a session stops.

Profiling tools:

- PIX;
- Windows Performance Recorder;
- Windows Performance Analyzer;
- Event Tracing for Windows;
- application-level frame and session counters.

Performance targets are engineering goals, not guarantees for every GPU or application.

## 15. Testing

### 15.1 Test applications

Minimum compatibility set:

- Notepad;
- File Explorer;
- a classic Win32/GDI test application;
- WinForms;
- WPF;
- a Chromium-based browser;
- an Electron application;
- a Qt application;
- a DirectX-rendered application;
- an application running with administrator privileges, to verify documented limitations.

### 15.2 Required scenarios

- repeated enable and disable actions;
- close the source window while capture is active;
- minimize and restore;
- maximize and unmaximize;
- snap layouts and edge snapping;
- move between monitors;
- mixed DPI values;
- lock and unlock the session;
- sleep and resume;
- GPU device reset or driver restart where testable;
- connect and disconnect a display;
- rotate a display;
- eight-hour endurance run;
- one hundred consecutive session starts and stops;
- target-window title change;
- target-window recreation by the source application;
- multiple overlapping windows;
- protected or non-capturable content.

## 16. Completion criteria for Light

Light is considered complete when:

1. color inversion is stable across the compatibility test set;
2. LUT processing runs without a full-frame CPU copy;
3. overlays never receive mouse or keyboard input;
4. the emergency shortcut works in all normal desktop scenarios;
5. no known failure leaves a stale overlay covering the target;
6. move and resize tracking is acceptably smooth;
7. the application runs on Windows 10 22H2 and Windows 11;
8. an eight-hour endurance test shows no continuously growing memory leak;
9. settings are versioned and migratable;
10. matching and profile logic have automated tests;
11. release packages include checksums;
12. the repository contains build and bug-reporting instructions;
13. a public build has been evaluated with people who have low vision;
14. known limitations are documented.

## 17. Architectural preparation for Hard

Light must already provide:

- `IWindowFrameSource`;
- `IEffectPipeline`;
- `IOverlaySession`;
- a versioned profile model;
- effect extensibility independent of the capture backend;
- strict separation between settings UI and renderer;
- internal support for multiple sessions even if the first UI emphasizes one active window;
- stable source-window and process identification;
- event channels for window position, visibility, and focus changes;
- a `Core` project with no WPF dependency.

These interfaces are preparation points only. Hard-specific features must not be implemented before Light meets its completion criteria.
