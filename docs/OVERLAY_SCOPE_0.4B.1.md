# Overlay scope per application — 0.4B.1

## Status

Implementation complete on `agent/issue-8-overlay-scope-per-app-v0.4`. Automated validation is executed against the final source tree; local Windows acceptance determines final completion of issue #8.

## Persisted model

Every `ApplicationProfile` stores one normalized `overlayScope` identifier. Existing and invalid values recover to `client-area` during deserialization. Settings schema version 4 records the new model.

## Scope choices

- `client-area` — target client content without frame/title bar; default;
- `window` — full visible target window;
- `screen` — full monitor containing the target;
- `all-screens` — full Windows virtual desktop.

## Ownership

- `ApplicationProfile` — persisted source of truth;
- `ApplicationProfileManagementService` — mutation authority;
- `ApplicationProfilesGrid` — per-row selector and typed edit event;
- `ConfigurationForm` — settings transaction orchestration;
- `OverlayBoundsResolver` — geometry authority;
- `OverlayController` — active overlay resource and scope.

## Runtime

Manual and automatic activation use the assigned application's scope. A settings change updates the active overlay through the existing settings-change path. The overlay remains input-transparent and remains tied to the target application's foreground lifecycle.

## Build identity

The issue-8 build displays `Alpha 0.4B.1 · Overlay scope per app` and reports `0.4.0-alpha.5+<commit>`.
