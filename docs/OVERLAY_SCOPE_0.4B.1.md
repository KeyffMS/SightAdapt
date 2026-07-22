# Overlay scope per application — 0.4B.1

## Status

Complete and locally accepted. Issue #8 is closed as completed.

## Persisted model

Every `ApplicationProfile` stores one normalized `overlayScope` identifier. The current settings schema is `4`.

Missing and invalid values recover to `client-area` without removing valid applications or visual-profile assignments.

## Scope choices

| UI choice | Persisted ID | Geometry |
|---|---|---|
| Client area | `client-area` | Target client content without title bar and frame; default |
| Full window | `window` | Complete visible target window |
| Current screen | `screen` | Complete monitor containing the target |
| All screens | `all-screens` | Complete Windows virtual desktop |

## Ownership

- `ApplicationProfile.OverlayScopeId` — persisted source of truth;
- `OverlayScopePolicy` — identifiers, display names, supported values, and default;
- `ApplicationProfileManagementService.SetOverlayScope` — mutation authority;
- `ApplicationProfilesGrid` — per-row selector and typed edit event;
- `ConfigurationForm` — committed settings transaction orchestration;
- `OverlayBoundsResolver` — single geometry authority;
- `OverlayController` — active overlay lifetime and selected scope;
- `MagnifierOverlay` — application of resolved destination and source geometry.

## Runtime behavior

Manual and automatic activation use the scope stored on the resolved application assignment.

A scope change for the active application is committed through `SettingsCoordinator`. The existing overlay instance receives the updated scope and geometry through the normal settings-change path.

The overlay remains input-transparent and non-activating. Target minimize, hide, close, and foreground transitions continue to follow the overlay lifecycle and transition-grace rules.

## Compatibility

Schema migration preserves:

- application display name and executable identity;
- enabled state;
- visual-profile assignment;
- automatic mode;
- valid custom visual profiles.

Assignments created before schema `4` receive `client-area`.

## Build identity

The feature was introduced in:

```text
Product version: 0.4.0-alpha.5+<commit>
File version:    0.4.0.1
Milestone:       Alpha 0.4B.1 · Overlay scope per app
```

The current development stack includes this behavior in 0.4B.2.