
# SightAdapt 0.5 Alpha functionality

## Application operation

SightAdapt runs in the Windows notification area and enforces one process per user session. It tracks the active supported top-level window and applies the saved assignment when automatic mode and that assignment are enabled.

Only one foreground target is corrected at a time. The overlay is separate from the target process, never intentionally receives input, and does not modify target files or memory.

## Application assignments

Every assignment stores:

- display name;
- executable name;
- executable path;
- enabled state;
- visual-profile identifier;
- overlay-scope identifier.

Assignments are matched primarily by executable path without regard to letter case. A disabled assignment remains available for the local shortcut but does not activate automatically.

New assignments use:

- visual profile: `Soft invert`;
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

During normal switching, SightAdapt reuses one existing overlay instance and retargets it with the new window handle, profile, scope, and geometry. The last rendered frame may remain visible for at most 125 ms while the new target is resolved. Explicit disable and emergency shutdown bypass this grace period.

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

Schema `4` contains automatic mode, application assignments, overlay scopes, and visual profiles. Changes use a copy, mutate, normalize, save, and publish transaction. Failed domain operations or failed writes do not replace the committed in-memory state.

Older valid assignments are preserved where possible. Legacy `effect: "invert"` values migrate to built-in Exact Invert.

## Safety behavior

- overlay windows are layered, input-transparent, non-activating tool windows;
- emergency shutdown removes the overlay before attempting settings persistence;
- renderer fault and explicit emergency shutdown are separate runtime states;
- failed persistence cannot publish candidate settings;
- destroyed targets close the overlay;
- minimized, hidden, or unavailable targets hide it;
- application exit and disposal release native overlay resources;
- no DLL injection or kernel driver is used.

## Limitations

- only one foreground target is corrected at a time;
- the current Magnification API backend cannot provide a stable persistent filter for obscured background windows;
- minimized targets are not continuously rendered;
- profile import and export are not implemented;
- palette analysis, targeted per-color correction, and LUT import are not implemented;
- DRM, protected surfaces, elevated targets, remote sessions, and some graphics drivers may limit capture;
- endurance and broad compatibility testing remain incomplete.
