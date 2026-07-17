# SightAdapt 0.3.1 Alpha architecture

## Purpose

Version 0.3.1 is a focused architecture-hardening release. It preserves the behavior of 0.3 Alpha while preparing the codebase for configurable visual profiles in 0.4.

The refactor follows KISS and DRY and defines explicit single points of authority and truth. It intentionally avoids a dependency-injection framework and abstractions that are not required by the 0.4 roadmap.

## Responsibility map

| Responsibility | Authority |
|---|---|
| Product name, version, author, license, and repository | `ProductInfo` |
| Current inactive/manual/automatic/emergency state | `ApplicationStateController` |
| Overlay creation, lifetime, target, and selected visual profile | `OverlayController` |
| Available image transformations | `VisualTransformCatalog` |
| Application-to-profile matching | `ProfileResolver` |
| Persisted settings and schema migration | `SettingsStore` |
| Tray, hotkeys, foreground tracking, and UI orchestration | `SightAdaptContext` |

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
- inversion matrix values.

GitHub Actions must restore, build, run tests, and publish the Windows x64 artifact in that order.

## Manual regression tests

Before 0.3.1 is accepted, verify on Windows 10:

1. tray startup and all three icon states;
2. manual inversion toggle;
3. automatic activation for configured applications;
4. manual suppression until the foreground application changes;
5. add-current-application shortcut;
6. profile-panel add, browse, enable, disable, and remove operations;
7. move, resize, minimize, restore, and close behavior;
8. emergency shutdown and automatic-mode disable;
9. restart with an existing 0.3 settings file;
10. restart after automatic migration of an older 0.2 settings file.
