# Overlay scope per application — 0.4B.1 requirements

## User-visible choices

Each configured application stores exactly one overlay scope:

1. **Client area** — application content without the title bar and frame; default.
2. **Full window** — visible window including title bar and frame.
3. **Current screen** — the complete monitor containing the target window.
4. **All screens** — the complete Windows virtual desktop.

## Ownership

- `ApplicationProfile` is the persisted source of truth.
- `ApplicationProfileManagementService` is the mutation authority.
- `ApplicationProfilesGrid` owns the per-row selector.
- `ConfigurationForm` translates a selector change into one settings transaction.
- `OverlayBoundsResolver` is the single geometry authority.
- `OverlayController` owns the active overlay resource and active scope.

## Compatibility

Existing settings migrate to **Client area** without losing applications or visual-profile assignments. Invalid persisted values recover to the same default.

## Runtime constraints

The overlay remains input-transparent. It is hidden when the target application is minimized, invisible, or no longer foreground. Scope changes for the active application must update the active overlay without creating a second runtime or settings authority.

## Build identity

The implementation build must display `Alpha 0.4B.1 · Overlay scope per app` and report product version `0.4.0-alpha.5+<commit>`.
