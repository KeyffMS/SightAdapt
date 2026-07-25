# SightAdapt current architecture

## Product flow

```text
Foreground application window
      ↓
ForegroundWindowTracker
(ignore native popup menus, detect and deduplicate)
      ↓
ApplicationDiscovery
(process lifetime, path, and bounded cache)
      ↓
ProfileResolver
(application profile plus optional menu profile)
      ↓
SightAdaptContext
(lifecycle and composition)
      ↓
RuntimeCoordinator
(use-case orchestration)
      ↓
OverlayController
(one persistent overlay plus transient menu overlays)
      ↑
Win32MenuWindowTracker
(WinEvent hint plus polling verification for `#32768`)
      ↓
MagnifierOverlay
(target kind, geometry, cross-filtering, rendering)
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
      ↓
SettingsNormalizer.Normalize
      ↓
atomic file replacement
      ↓
Current.ReplaceWith
      ↓
one synchronous Changed event
```

A failed mutation or failed write does not replace committed settings and does not publish a settings change. `SettingsCoordinator.Current` returns a defensive snapshot, so consumers cannot mutate the committed in-memory object outside a transaction.

## Authorities

| Concern | Authority |
|---|---|
| Settings transaction and published snapshots | `SettingsCoordinator` |
| Settings JSON persistence and atomic replacement | `SettingsStore` |
| Migration, scope canonicalization, normalization, recovery, and reference repair | `SettingsNormalizer` |
| Runtime use-case orchestration | `RuntimeCoordinator` |
| Application assignment mutations and overlay scope | `ApplicationProfileManagementService` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Automatic-mode mutation | `AutomaticModeManagementService` |
| Runtime mode, target, profile, suppression, and message | `ApplicationStateController` |
| Foreground detection, native-menu exclusion, and duplicate suppression | `ForegroundWindowTracker` |
| Native popup-menu detection and association | `Win32MenuWindowTracker` and `Win32MenuWindowPolicy` |
| Runtime identity resolution | `ApplicationDiscovery` |
| Bounded process identity cache | `ApplicationIdentityCache` |
| Overlay geometry | `OverlayBoundsResolver` |
| Persistent and transient overlay lifetime, retargeting, and cross-filtering | `OverlayController` |
| Native call failure classification and diagnostics | `NativeCall` |
| Native target kind, rendering, geometry refresh, and transition grace | `MagnifierOverlay` |
| Notification-area presentation | `TrayPresenter` |
| Application-table presentation and edit mechanics | `ApplicationProfilesGrid` |
| Configuration use cases and dialogs | `ConfigurationForm` |
| Selector editing contract | `ModernSelectorEditingControl` |

## Sources of truth

| Data or rule | Source of truth |
|---|---|
| Persisted automatic mode, applications, assignments, scopes, and profiles | `SightAdaptSettings` committed through `SettingsCoordinator` |
| Runtime mode, target, active profile, suppression, and message | `ApplicationStateController.Current` |
| Actual overlay resource and target | `OverlayController` and active `MagnifierOverlay` |
| Per-application overlay scope | `ApplicationProfile.OverlayScopeId` |
| Optional native-menu profile reference and inheritance sentinel | `ApplicationProfile.MenuVisualProfileId` and `ApplicationMenuProfilePolicy` |
| Scope enum values, canonical identifiers, aliases, default, and display names | `OverlayScopePolicy` definition table |
| Profile IDs, fallback, user-ID, and name rules | `VisualProfilePolicy` |
| Canonical profile values | `VisualProfileDefaults` |
| Supported transforms and tuning capability | `VisualTransformCatalog` |
| Parameter ranges | `VisualProfileLimits` |
| Product name, version, milestone, repository, author, and license | project and assembly metadata exposed through `ProductInfo` |

`ApplicationIdentityCache` is an optimization, not a product source of truth. Entries are keyed by both PID and process creation time so a reused PID cannot inherit another process lifetime's identity.

## Foreground and overlay lifecycle

The foreground tracker polls every 75 ms and publishes only a changed supported application handle. Native `#32768` popup-menu windows are deliberately rejected as application targets, so opening a menu does not replace the active assignment. When an enabled assignment exists, the context resolves the application profile, the inherited or explicit menu profile, and the overlay scope.

- without an active application overlay, `OverlayController` creates one persistent `MagnifierOverlay`;
- with an active application overlay, it retargets the same instance;
- the built-in `None` profile retains the application session with an identity color effect, allowing an explicit menu profile to correct only native popup menus;
- `Win32MenuWindowTracker` combines out-of-context WinEvent menu notifications with 75 ms `EnumWindows` verification;
- visible associated `#32768` HWNDs create transient window-scope overlays, including nested menus;
- disappearing or destroyed menu HWNDs remove only their transient overlays;
- every active overlay handle is installed in every magnifier filter list before a new menu overlay is shown;
- without an enabled assignment, an automatically active overlay session is disabled;
- local disable, emergency shutdown, exit, and disposal remove the complete overlay session immediately.

A rendered application frame may remain visible for at most 125 ms during application-target transition. Native popup overlays do not use this grace period. This rendering grace is not a second runtime state.

## Geometry

`OverlayBoundsResolver` is the only authority for:

- client-area bounds converted to screen coordinates;
- full visible window bounds;
- containing monitor bounds;
- Windows virtual-screen bounds.

The current backend uses the same rectangle for the magnifier source and overlay destination.

## Native call failure policy

`NativeCall` classifies fallible Win32 and Magnification API operations explicitly:

- **critical** initialization and effect calls throw a `Win32Exception` containing the operation name and native error code;
- **transient** geometry, positioning, and source-update failures are diagnosed, hide the overlay, and allow a later timer tick to recover;
- **best effort** cleanup failures are diagnosed without replacing the primary application failure.

Native menu detection, menu-overlay creation, and cross-filter refresh are subordinate operations. Their failure closes transient menu overlays and restores the primary overlay's self-filter instead of disabling the application correction.

`ShowWindow` and `InvalidateRect` are handled explicitly at their call sites because their Boolean return values do not represent a standard extended-error success contract.

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
- separate menu profiles intentionally cover only associated native `#32768` popup windows;
- the Magnification API backend intentionally corrects only the active foreground application session and its detected native popup menus.

## Architecture test strategy

Architecture checks are behavior-first. Transaction publication, defensive settings snapshots, failed persistence, expected and unexpected transaction failures, emergency ordering, runtime state transitions, native call classification, transform catalog consistency, overlay-scope and menu-profile recovery, grid commits, native menu policy, menu roles, preview caching, and profile-manager refresh behavior are exercised through executable tests.

Source inspection is retained only for exhaustive negative rules that cannot be proven by a finite runtime scenario:

- collection and property writes must remain inside their mutation authorities;
- UI and runtime components must not instantiate the persistence store;
- empty catch blocks are forbidden;
- removed legacy mutation services must not return.

These focused scans intentionally avoid asserting field names, statement ordering, formatting, RGB literals, or the exact internal spelling of valid implementations.
