# SightAdapt 0.3.1 Alpha architecture

## Purpose

Version 0.3.1 is a focused architecture-hardening release. It preserves the user-facing capabilities of 0.3 Alpha while preparing the codebase for configurable visual profiles in 0.4.

The refactor follows KISS and DRY and defines explicit single points of authority and truth. It intentionally avoids a dependency-injection framework and abstractions that are not required by the 0.4 roadmap.

## Responsibility map

| Responsibility | Authority |
|---|---|
| Product name, version, author, license, and repository | `ProductInfo` |
| Current inactive/manual/automatic/emergency state | `ApplicationStateController` |
| Overlay creation, lifetime, target, and selected visual profile | `OverlayController` |
| Available image transformations | `VisualTransformCatalog` |
| Application-to-profile matching | `ProfileResolver` |
| Persistent add/disable/re-enable profile operation | `ApplicationProfileToggleService` |
| Persisted settings and schema migration | `SettingsStore` |
| Tray, hotkeys, foreground tracking, and UI orchestration | `SightAdaptContext` |

## Command model

The current alpha registers exactly two global keyboard commands:

| Command | Shortcut | Scope |
|---|---|---|
| Local visual toggle | `Ctrl+Alt+I` | Enables or disables the overlay for the active window without changing its saved assignment |
| Persistent profile toggle | `Ctrl+Alt+Shift+I` | Creates, disables, or re-enables the active application's assignment in settings |

No fallback shortcuts are registered. In particular, the application no longer registers `Ctrl+Win+2`, `Ctrl+Win+Shift+I`, `Ctrl+Win+Shift+2`, or `Ctrl+Alt+Shift+F12`.

Emergency shutdown remains a tray-menu command. It disables every overlay and turns automatic mode off.

`ApplicationProfileToggleService` is the single implementation of persistent toggle behavior:

1. missing assignment → create and enable;
2. enabled assignment → disable;
3. disabled assignment → enable;
4. re-enabling preserves the selected `VisualProfileId`;
5. enabling also turns global automatic mode on;
6. disabling an automatically active assignment causes the orchestration layer to stop that overlay.

## Application state

`ApplicationStateController` is the single authority for these states:

- `Inactive`;
- `ManualActive`;
- `AutomaticActive`;
- `Emergency`.

The tray icon, tooltip, status text, and configuration-window icon are projections of this state. They do not maintain an independent overlay mode.

## Overlay boundary

`OverlayController` owns the `MagnifierOverlay` instance and:

- creates it for a target window;
- selects the transform from the assigned visual profile;
- prevents duplicate recreation for the same target and profile;
- disposes the overlay safely;
- reports unexpected overlay closure.

`SightAdaptContext` decides when an overlay should be active, but it does not own the overlay form directly.

## Visual profiles and transforms

Application assignments and visual profiles are separate models:

```text
ApplicationProfile
└── VisualProfileId
    └── VisualProfile
        └── TransformId
            └── IVisualTransform
```

0.3.1 registers one transform:

```text
invert
```

The model is deliberately minimal. Brightness, output black/white levels, contrast, saturation, hue, LUTs, and targeted color rules belong to the 0.4 implementation.

## Settings schema

The current schema version is `2`.

Legacy settings containing:

```json
{
  "effect": "invert"
}
```

are migrated to:

```json
{
  "visualProfileId": "default-invert"
}
```

The legacy `effect` field is not written back. Settings remain per-user in:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

Writes remain atomic through a temporary file.

## Automated tests

The test project covers:

- initial and changed application states;
- target validation for active states;
- emergency-state behavior;
- legacy settings migration;
- current-schema persistence;
- duplicate application normalization;
- executable-path matching;
- disabled assignment behavior;
- default-profile fallback;
- transform-catalog error handling;
- inversion matrix values;
- creation of an enabled persistent assignment;
- disabling an existing assignment;
- re-enabling an assignment without losing its visual-profile selection.

GitHub Actions must restore, build, run tests, and publish the Windows x64 artifact in that order.

## Manual regression tests

Before 0.3.1 is accepted, verify on Windows 10:

1. tray startup and all three icon states;
2. `Ctrl+Alt+I` local enable/disable behavior;
3. `Ctrl+Alt+Shift+I` creates and enables a missing assignment;
4. pressing `Ctrl+Alt+Shift+I` again disables the assignment and stops an automatic overlay;
5. pressing it a third time re-enables the same assignment;
6. automatic activation for configured applications;
7. local suppression until the foreground application changes;
8. profile-panel add, browse, enable, disable, and remove operations;
9. move, resize, minimize, restore, and close behavior;
10. tray-menu emergency shutdown and automatic-mode disable;
11. confirmation that removed shortcuts do not trigger SightAdapt;
12. restart with an existing 0.3 settings file;
13. restart after automatic migration of an older 0.2 settings file.
