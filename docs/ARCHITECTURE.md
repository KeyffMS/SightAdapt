
# SightAdapt current architecture

## Product flow

```text
Foreground window
      ↓
ForegroundWindowTracker
(detect and deduplicate)
      ↓
ApplicationDiscovery
(process lifetime, path, and bounded cache)
      ↓
ProfileResolver
(committed assignment)
      ↓
SightAdaptContext
(use-case orchestration)
      ↓
OverlayController
(create once, retarget, disable)
      ↓
MagnifierOverlay
(target, geometry, rendering)
```

## Settings transaction

```text
SettingsCoordinator.Current
      ↓
CreateWorkingCopy
      ↓
Domain-service mutation
      ↓
SettingsStore.Save
(normalize and atomic replacement)
      ↓
Current.ReplaceWith
      ↓
one synchronous Changed event
```

A failed mutation or failed write does not replace committed settings and does not publish a settings change.

## Authorities

| Concern | Authority |
|---|---|
| Settings transaction | `SettingsCoordinator` |
| Migration, scope canonicalization, normalization, recovery, and reference repair | `SettingsStore.Normalize` |
| Application assignment mutations and overlay scope | `ApplicationProfileManagementService` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Automatic-mode mutation | `AutomaticModeManagementService` |
| Runtime mode, target, profile, suppression, and message | `ApplicationStateController` |
| Foreground detection and duplicate suppression | `ForegroundWindowTracker` |
| Runtime identity resolution | `ApplicationDiscovery` |
| Bounded process identity cache | `ApplicationIdentityCache` |
| Overlay geometry | `OverlayBoundsResolver` |
| Overlay resource lifetime and retargeting | `OverlayController` |
| Native target, rendering, geometry refresh, and transition grace | `MagnifierOverlay` |
| Notification-area presentation | `TrayPresenter` |
| Application-table presentation and edit mechanics | `ApplicationProfilesGrid` |
| Configuration use cases and dialogs | `ConfigurationForm` |
| Selector editing contract | `ModernSelectorEditingControl` |

## Sources of truth

| Data or rule | Source of truth |
|---|---|
| Persisted automatic mode, applications, assignments, scopes, and profiles | `SightAdaptSettings` committed through `SettingsCoordinator.Current` |
| Runtime mode, target, active profile, suppression, and message | `ApplicationStateController.Current` |
| Actual overlay resource and target | `OverlayController` and active `MagnifierOverlay` |
| Per-application overlay scope | `ApplicationProfile.OverlayScopeId` |
| Scope enum values, canonical identifiers, aliases, default, and display names | `OverlayScopePolicy` definition table |
| Profile IDs, fallback, user-ID, and name rules | `VisualProfilePolicy` |
| Canonical profile values | `VisualProfileDefaults` |
| Supported transforms and tuning capability | `VisualTransformCatalog` |
| Parameter ranges | `VisualProfileLimits` |
| Product name, version, milestone, repository, author, and license | project and assembly metadata exposed through `ProductInfo` |

`ApplicationIdentityCache` is an optimization, not a product source of truth. Entries are keyed by both PID and process creation time so a reused PID cannot inherit another process lifetime's identity.

## Foreground and overlay lifecycle

The foreground tracker polls every 75 ms and publishes only a changed supported top-level handle. When an enabled assignment exists, the context resolves its profile and scope and activates correction.

- without an active overlay, `OverlayController` creates one `MagnifierOverlay`;
- with an active overlay, it retargets the same instance;
- without an enabled assignment, an automatically active overlay is disabled;
- local disable, emergency shutdown, exit, and disposal remove it immediately.

A rendered frame may remain visible for at most 125 ms during target transition. This is a rendering grace period, not a second runtime state.

## Geometry

`OverlayBoundsResolver` is the only authority for:

- client-area bounds converted to screen coordinates;
- full visible window bounds;
- containing monitor bounds;
- Windows virtual-screen bounds.

The current backend uses the same rectangle for the magnifier source and overlay destination.

## Configuration grid boundary

`ApplicationProfilesGrid` owns columns, rows, selectors, status painting, selection, empty state, stable executable-path keys, separate typed change events, row updates, and failed-cell restoration. It does not know about persistence or dialogs.

`ConfigurationForm` resolves current committed assignments and translates typed grid events into domain-service mutations wrapped by `SettingsCoordinator.Commit`. It suppresses only its own synchronous full refresh during a grid-originated commit.

`ModernSelectorEditingControl` exposes display text as its formatted value and marks the cell dirty. It does not write directly to a grid cell, force edit completion, or control settings dispatch.

## Safety and intentional constraints

- overlay windows do not accept input or activate themselves;
- emergency shutdown disables rendering before settings I/O;
- fault and emergency are distinct states;
- no dependency-injection container, event bus, repository layer, global selector guard, delayed settings workaround, or reflection-based popup control is used;
- no DLL injection, kernel driver, or target-process memory modification is used;
- the Magnification API backend intentionally corrects only the active foreground target.
