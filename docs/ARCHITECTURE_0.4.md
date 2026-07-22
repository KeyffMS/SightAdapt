# SightAdapt 0.4 — current architecture

## Status and precedence

This document is the canonical architecture map for the accepted 0.4B.2 development stack.

Historical audits and remediation notes describe earlier repository states. When they differ from this document or the current code, the current code and this document take precedence.

## Product flow

```text
Foreground window
      |
      v
ForegroundWindowTracker
(detect + deduplicate)
      |
      v
ApplicationDiscovery
(process path + bounded identity cache)
      |
      v
ProfileResolver
(committed application assignment)
      |
      v
SightAdaptContext
(use-case orchestration)
      |
      v
OverlayController
(create once, retarget, disable)
      |
      v
MagnifierOverlay
(target + geometry + rendering)
```

Settings changes use a separate transaction flow:

```text
SettingsCoordinator.Current
      |
      v
CreateWorkingCopy
      |
      v
Domain-service mutation
      |
      v
SettingsStore.Save
(normalize + atomic replacement)
      |
      v
Current.ReplaceWith
      |
      v
one synchronous Changed event
```

A failed mutation or failed write does not replace committed settings and does not publish a settings change.

## Authorities

| Concern | Authority |
|---|---|
| Settings transaction | `SettingsCoordinator` |
| Migration, normalization, malformed-data recovery, and reference repair | `SettingsStore.Normalize` |
| Application assignment creation, enablement, profile assignment, overlay scope, and removal | `ApplicationProfileManagementService` |
| Visual-profile creation, duplication, rename, tuning, deletion, and assignment counting | `VisualProfileManagementService` |
| Automatic-mode mutation | `AutomaticModeManagementService` |
| Runtime product state and automatic suppression | `ApplicationStateController` |
| Foreground detection and duplicate notification suppression | `ForegroundWindowTracker` |
| Runtime application identity lookup and cache integration | `ApplicationDiscovery` |
| Bounded process-to-identity cache | `ApplicationIdentityCache` |
| Overlay geometry for client area, window, screen, and virtual desktop | `OverlayBoundsResolver` |
| Overlay resource lifetime, retargeting, and explicit disable | `OverlayController` |
| Native magnifier target, rendering, geometry refresh, and transition grace | `MagnifierOverlay` |
| Notification-area controls and presentation | `TrayPresenter` |
| Application-table construction, binding, selectors, painting, and edit events | `ApplicationProfilesGrid` |
| Configuration use cases, settings commits, and dialogs | `ConfigurationForm` |
| Generic dark selector editing contract | `ModernSelectorEditingControl` |

## Sources of truth

| Data or rule | Source of truth |
|---|---|
| Persisted automatic mode, applications, assignments, overlay scopes, and visual profiles | `SightAdaptSettings` committed through `SettingsCoordinator.Current` |
| Current runtime mode, target, active profile ID, suppression, and message | `ApplicationStateController.Current` |
| Actual overlay resource and its current target | `OverlayController` / active `MagnifierOverlay` |
| Per-application overlay scope | `ApplicationProfile.OverlayScopeId` |
| Overlay-scope identifiers, defaults, and display names | `OverlayScopePolicy` |
| Built-in profile IDs, assignment fallback, user-ID, and name rules | `VisualProfilePolicy` |
| Canonical profile names and tuning defaults | `VisualProfileDefaults` |
| Supported transforms and tuning capability | `VisualTransformCatalog` |
| Parameter ranges | `VisualProfileLimits` |
| Product name, version, milestone, repository, author, and license | project/assembly metadata exposed by `ProductInfo` |
| Current roadmap status | `ROADMAP_0.4.md` |

`ApplicationIdentityCache` is not a product source of truth. It stores derived runtime data only and may evict entries without affecting persisted behavior.

## Foreground switching

`ForegroundWindowTracker` polls every 75 ms by default and publishes only a changed supported top-level handle.

`ApplicationDiscovery` resolves the executable path and reuses a bounded process-ID cache where possible. Persisted assignment matching continues to use executable identity from committed settings.

When an enabled assignment exists, `SightAdaptContext` resolves its visual profile and overlay scope and asks `OverlayController` to activate the correction.

- no active overlay: create and show one `MagnifierOverlay`;
- active overlay: retarget the existing instance with the new handle, color effect, transform ID, and scope;
- no enabled assignment: disable an automatically active overlay;
- explicit local disable or emergency: remove the overlay immediately.

The overlay may preserve its last rendered frame for at most 125 ms while the foreground transition is resolved. This is a rendering grace period, not a second runtime state or settings authority.

## Overlay geometry

`OverlayBoundsResolver` is the only geometry authority for the four persisted scopes:

- `client-area` — client coordinates converted to screen coordinates;
- `window` — visible extended window bounds;
- `screen` — bounds of the monitor containing the target;
- `all-screens` — Windows virtual-screen bounds.

Destination and magnifier source use the same resolved rectangle for the current Magnification API implementation.

## Configuration grid boundary

`ApplicationProfilesGrid` owns presentation and editing mechanics:

- columns and rows;
- visual-profile and overlay-scope selector options;
- enabled-state painting;
- empty state and selection;
- stable executable-path keys in `DataGridViewRow.Tag`;
- typed value-change events;
- local row update or failed-cell restoration.

It does not know about persistence, `SettingsCoordinator`, or dialogs.

`ConfigurationForm` resolves the current assignment from committed settings and translates a typed grid event into a domain-service mutation wrapped by `SettingsCoordinator.Commit`. During a grid-originated commit it suppresses only its own synchronous full refresh, then updates the affected row after success or restores the affected cell after failure.

`ModernSelectorEditingControl` follows `IDataGridViewEditingControl`: it exposes display text as the formatted value and marks the cell dirty without directly writing `DataGridViewCell.Value`, forcing `EndEdit`, or controlling settings dispatch.

## Safety and failure behavior

- overlay windows are layered, input-transparent, tool windows, and non-activating;
- emergency shutdown disables the overlay before any settings I/O;
- emergency and fault are distinct runtime states;
- a failed settings write cannot publish candidate state;
- a destroyed overlay target closes the overlay after transition grace;
- an existing but unavailable target hides the overlay after transition grace;
- explicit disable, emergency, application exit, and disposal bypass transition grace;
- no DLL injection, kernel driver, or target-process memory modification is used.

## Intentionally absent mechanisms

The current architecture does not use:

- a dependency-injection container;
- an event bus;
- a repository layer over the settings store;
- global selector timers or `Application.Idle` guards;
- delayed settings dispatch to work around grid editing;
- reflection-based popup manipulation;
- multiple competing overlay-state authorities.

## Renderer boundary

The Magnification API implementation validates current interaction, profile, geometry, and lifecycle behavior with minimal dependencies. Palette analysis, LUTs, and targeted per-color mapping should move to Windows Graphics Capture, Direct3D 11, and HLSL when the 5×5 color matrix is no longer expressive enough.

That renderer change should preserve the current authorities for settings, application assignments, runtime state, overlay scope, emergency behavior, and UI transaction boundaries.