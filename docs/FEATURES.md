
# SightAdapt 0.5 Alpha functionality

## Application operation

SightAdapt runs in the Windows notification area and enforces one process per user session. It tracks the active supported top-level window and applies the saved assignment when automatic mode and that assignment are enabled.

Only one foreground application session is corrected at a time. SightAdapt uses one persistent application overlay plus transient overlays for detected native popup menus. The overlays are separate from the target process, never intentionally receive input, and do not modify target files or memory.

## Application assignments

Every assignment stores:

- display name;
- executable name;
- executable path;
- enabled state;
- visual-profile identifier;
- optional native-menu visual-profile identifier;
- overlay-scope identifier.

Assignments are matched primarily by executable path without regard to letter case. A disabled assignment remains available for the local shortcut but does not activate automatically.

New assignments use:

- visual profile: `Soft invert`;
- native-menu profile: inherit the application visual profile;
- overlay scope: `Client area`.

## Visual profiles

### Exact invert

`Exact invert` is a fixed built-in profile. It cannot be edited, renamed, or deleted.

### Soft invert

The built-in `Soft invert` profile is editable and shared by every assignment that references it.

Default values:

```text
Output black: 8%
Output white: 92%
Brightness:   0%
Contrast:     100%
Saturation:   100%
Hue shift:    0°
```

The current matrix pipeline applies:

```text
soft inversion and output limits
→ saturation
→ hue rotation
→ contrast
→ brightness
```

All operations are composed into one Magnification API color-effect matrix.

### User-defined profiles

Users can create a profile from Soft Invert defaults or duplicate an editable profile. User-defined profiles have stable identifiers, independent tuning values, and unique case-insensitive names.

Supported operations:

- create;
- duplicate;
- rename;
- edit;
- assign;
- delete.

Deleting a user-defined profile reassigns affected applications to built-in Soft Invert before removing the profile. Built-in profiles are protected.

## Native popup-menu profiles

Each application assignment can optionally select a second visual profile for native Win32 popup-menu windows. Leaving the selector at `Same as application` stores no duplicate profile identifier and resolves the current application profile at runtime.

The initial implementation detects visible top-level windows with class `#32768` that belong to the foreground application's process or GUI thread. It covers standard menu drops, context menus, system menus, nested submenus, and owner-drawn menus that retain the native popup class. WPF, WinUI, Chromium/Electron, Qt, and other custom-rendered menus are outside this feature boundary.

One persistent overlay continues to render the application target. Zero or more transient menu overlays are created by popup HWND, use window bounds, and are removed when their native window disappears. Every active SightAdapt overlay is excluded from every magnifier source to prevent recursive capture.

## Overlay scope per application

| UI choice | Persisted ID | Result |
|---|---|---|
| Client area | `client-area` | Application content without title bar and frame; default |
| Full window | `window` | Complete visible application window |
| Current screen | `screen` | Complete monitor containing the target |
| All screens | `all-screens` | Complete Windows virtual desktop |

Changing one assignment does not modify another assignment's scope. Missing or invalid persisted scope values recover to `client-area`.

## Foreground switching

The foreground tracker polls every 75 ms by default and publishes only a changed supported top-level handle. Application identity is cached in a bounded 64-entry least-recently-used process cache. The cache contains derived runtime data only; saved assignments remain authoritative.

During normal application switching, SightAdapt reuses one persistent application overlay and retargets it with the new window handle, profile, scope, and geometry. Native popup menus are detected by WinEvent notifications with a 75 ms polling verification path and rendered through transient overlays keyed by HWND. The last application frame may remain visible for at most 125 ms while a new application target is resolved. Explicit disable and emergency shutdown bypass this grace period.

## Keyboard and tray controls

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Local correction toggle without changing saved settings |
| `Ctrl+Alt+Shift+I` | Add, disable, or re-enable the persistent assignment for the active application |

The notification-area menu provides:

- local correction toggle;
- persistent assignment toggle;
- automatic-mode switch;
- application and profile configuration;
- About dialog;
- emergency shutdown;
- application exit.

## Settings

Settings are stored at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

Schema `5` contains automatic mode, application assignments, application and native-menu profile references, overlay scopes, and visual profiles. Changes use a copy, mutate, normalize, save, and publish transaction. Failed domain operations or failed writes do not replace the committed in-memory state.

Older valid assignments are preserved where possible. Legacy `effect: "invert"` values migrate to built-in Exact Invert.

## Safety behavior

- overlay windows are layered, input-transparent, non-activating tool windows;
- emergency shutdown removes the overlay before attempting settings persistence;
- renderer fault and explicit emergency shutdown are separate runtime states;
- failed persistence cannot publish candidate settings;
- destroyed targets close the corresponding overlay;
- minimized, hidden, or unavailable targets hide it;
- native menu tracking or filter-list failure removes only transient menu overlays and leaves the primary correction active;
- every magnifier excludes all active SightAdapt overlay windows from its source;
- application exit and disposal release native overlay resources;
- no DLL injection or kernel driver is used.

## Limitations

- only one foreground application session is corrected at a time;
- separate menu profiles apply only to native `#32768` popup windows; custom-rendered menus remain part of the application overlay;
- the current Magnification API backend cannot provide a stable persistent filter for obscured background windows;
- minimized targets are not continuously rendered;
- profile import and export are not implemented;
- palette analysis, targeted per-color correction, and LUT import are not implemented;
- DRM, protected surfaces, elevated targets, remote sessions, and some graphics drivers may limit capture;
- endurance and broad compatibility testing remain incomplete.
