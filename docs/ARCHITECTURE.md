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
(small command/event façade)
      ↓
RuntimeOverlayActivator + AutomaticActivationService
(focused runtime use cases)
      ↓
OverlayController
(profile-to-effect façade)
      ↓
OverlaySession
(primary and transient overlay aggregate)
      ↑
Win32MenuWindowTracker
(signal source + enumerator + pure policy)
      ↓
MagnifierOverlay
(native lifetime delegated to availability and frame-rendering policies)
```

## Settings transaction

```text
SettingsCoordinator.Current
(read-only snapshot contract)
      ↓
CreateWorkingCopy
      ↓
SettingsUseCaseContext
(shared snapshot/event/commit plumbing)
      ↓
Domain-service mutation
      ↓
SettingsNormalizer.Normalize
(explicit idempotent passes)
      ↓
PersistedSettingsMapper.FromDomain
      ↓
SettingsStore atomic file replacement
      ↓
Current.ReplaceWith
      ↓
one synchronous Changed event
```

A failed mutation or failed write does not replace committed settings and does not publish a settings change. `SettingsCoordinator.Current` exposes `IReadOnlySightAdaptSettings` and returns a defensive snapshot. Mutable `SightAdaptSettings` is confined to explicit transaction callbacks and normalization/persistence boundaries. `SettingsUseCaseContext` centralizes snapshot, change-event and commit plumbing for application use-case façades without sharing their domain commands. `Commit` and `PersistCurrent` use one persistence execution path; only `Commit` publishes the synchronous `Changed` event after a successful write.

## Authorities

| Concern | Authority |
|---|---|
| Settings transaction and published read-only snapshots | `SettingsCoordinator` |
| Shared use-case snapshot/event/commit plumbing | `SettingsUseCaseContext` |
| Settings JSON persistence and atomic replacement | `SettingsStore` |
| Persisted JSON DTOs and legacy-field migration | `PersistedSettingsMapper` |
| Schema, profile, assignment and reference normalization | explicit `ISettingsNormalizationPass` implementations |
| Runtime command/event façade | `RuntimeCoordinator` |
| Runtime target discovery and identity resolution | `IRuntimeTargetResolver` |
| Runtime user feedback and automatic-mode synchronization | `IRuntimeFeedback` |
| Overlay activation and automatic evaluation use cases | `RuntimeOverlayActivator` and `AutomaticActivationService` |
| Application assignment mutations and overlay scope | `ApplicationAssignmentService` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Automatic-mode mutation | `AutomaticModeManagementService` |
| Runtime mode, target, profile, suppression, and message | `ApplicationStateController` |
| Foreground detection, native-menu exclusion, and duplicate suppression | `ForegroundWindowTracker` |
| Native popup-menu coordination | `Win32MenuWindowTracker` |
| Native-menu signals, enumeration and association policy | `WinEventMenuRefreshSignalSource`, `NativeMenuWindowEnumerator`, and `Win32MenuWindowPolicy` |
| Runtime identity resolution | `ApplicationDiscovery` |
| Bounded process identity cache | `ApplicationIdentityCache` |
| Overlay geometry | `OverlayBoundsResolver` |
| Profile-to-effect overlay façade | `OverlayController` |
| Persistent and transient overlay lifetime, retargeting, and cross-filtering | `OverlaySession` |
| Raw P/Invoke declarations | `NativeInterop` |
| Domain-focused native operations | native API interfaces and adapters in `NativeApis` |
| Native call failure classification | `NativeCall` |
| Structured production diagnostics | `Diagnostics` and `IDiagnosticSink` |
| Runtime timing defaults | `RuntimeTimingPolicy` |
| Target availability and frame positioning/source updates | availability strategies and `MagnifierFrameRenderer` |
| Native window/control lifetime | `MagnifierOverlay` |
| Notification-area presentation | `TrayPresenter` |
| Application-table selection, binding, row updates and typed publication | `ApplicationAssignmentsGrid` |
| Assignment-grid columns and selector options | `ApplicationAssignmentsGridColumns` |
| Assignment-grid cell-to-command mapping | `ApplicationAssignmentCellChangeMapper` |
| Assignment enabled-state painting | `ApplicationAssignmentEnabledCellRenderer` |
| Assignment selector data-error classification | `ApplicationAssignmentSelectorErrorPolicy` |
| Configuration use cases and dialogs | `ConfigurationForm` |
| Selector editing contract | `ModernSelectorEditingControl` |

## Sources of truth

| Data or rule | Source of truth |
|---|---|
| Persisted JSON shape and legacy compatibility | `PersistedSightAdaptSettings` and `PersistedSettingsMapper` |
| Committed automatic mode, assignments, scopes, and profiles | `SightAdaptSettings` committed through `SettingsCoordinator` |
| Runtime mode, target, active profile, suppression, and message | `ApplicationStateController.Current` |
| Actual overlay session, primary target and popup resources | `OverlaySession` |
| Per-application overlay scope | `ApplicationAssignment.OverlayScopeId` |
| Optional native-menu profile reference and inheritance sentinel | `ApplicationAssignment.MenuVisualProfileId` and `ApplicationMenuProfilePolicy` |
| Scope enum values, canonical identifiers, aliases, default, and display names | `OverlayScopePolicy` definition table |
| Built-in profile IDs, names, transforms, ordering, tuning capability, canonical tuning and normalization policy | `VisualProfileCatalog` definition table |
| Assignment defaults, fallbacks, user-ID and user-name rules | `VisualProfilePolicy` |
| Built-in tuning constants and shared numeric helpers | `VisualProfileDefaults` |
| Parameter ranges | `VisualProfileLimits` |
| Runtime polling, refresh, transition and fault-recovery intervals | `RuntimeTimingPolicy.Default` |
| Product name, version, milestone, repository, author, and license | project and assembly metadata exposed through `ProductInfo` |

## Visual-profile tuning authority

Each registered `VisualProfileDefinition` is the authority for its transform's editable status, canonical/reset tuning and normalization function. `VisualProfileManagementService.UpdateTuning` resolves the target profile's transform definition before normalizing submitted values, and `VisualProfileEditorForm` resolves the same definition for reset wording and canonical reset values. Adding another editable transform therefore does not inherit Soft Invert normalization or defaults implicitly.

`VisualProfileDefaults` retains the built-in numeric constants and reusable normalization helpers used by the current definitions; it is not a second transform registry.

`ApplicationIdentityCache` is an optimization, not a product source of truth. Entries are keyed by both PID and process creation time so a reused PID cannot inherit another process lifetime's identity.

## Runtime ports

Runtime orchestration uses two narrow ports instead of one mixed environment abstraction:

- `IRuntimeTargetResolver` owns target-window resolution, target support checks and application identity lookup;
- `IRuntimeFeedback` owns user notifications and synchronization of the tray's automatic-mode state.

`SightAdaptContext` is the composition root that creates the concrete delegate adapters. Runtime services can substitute either collaborator independently in tests, and `RuntimeCoordinator` no longer exposes the previous five-delegate constructor.

## Foreground and overlay lifecycle

The foreground tracker polls every 75 ms and publishes only a changed supported application handle. Native `#32768` popup-menu windows are deliberately rejected as application targets, so opening a menu does not replace the active assignment. When an enabled assignment exists, the context resolves the application profile, the inherited or explicit menu profile, and the overlay scope.

- `OverlayController` resolves profiles to immutable `ResolvedVisualEffect` values and delegates lifetime work;
- without an active application overlay, `OverlaySession` creates one persistent overlay window;
- with an active application overlay, `OverlaySession` retargets the same primary resource;
- the built-in `None` profile retains the application session with an identity color effect, allowing an explicit menu profile to correct only native popup menus;
- `Win32MenuWindowTracker` coordinates an isolated WinEvent signal source and menu-window enumerator, with the polling interval supplied by `RuntimeTimingPolicy`;
- visible associated `#32768` HWNDs create transient window-scope overlays, including nested menus;
- disappearing or destroyed menu HWNDs remove only their transient overlays;
- every active overlay handle is installed in every magnifier filter list before a new menu overlay is shown;
- without an enabled assignment, an automatically active overlay session is disabled;
- local disable, emergency shutdown, exit, and disposal remove the complete overlay session immediately.

A rendered application frame may remain visible during the transition grace configured by `RuntimeTimingPolicy`. Native popup overlays do not use this grace period. Availability strategies determine whether a foreground target or popup is renderable, while `MagnifierFrameRenderer` owns positioning, source updates, and repaint requests.

## Geometry

`OverlayBoundsResolver` is the only authority for:

- client-area bounds converted to screen coordinates;
- full visible window bounds;
- containing monitor bounds;
- Windows virtual-screen bounds.

The current backend uses the same rectangle for the magnifier source and overlay destination.

## Native call failure policy

Raw imports exist only in `NativeInterop`. Production components depend on focused window, process, menu-event, DWM, and Magnification API adapters. `NativeCall` classifies fallible native operations explicitly:

- **critical** initialization and effect calls throw a `Win32Exception` containing the operation name and native error code;
- **transient** geometry, positioning, and source-update failures are diagnosed, hide the overlay, and allow a later timer tick to recover;
- **best effort** cleanup failures are diagnosed without replacing the primary application failure.

Every native failure is emitted through the structured `Diagnostics` authority. `RuntimeOverlayActivator` also classifies top-level activation failures: expected native or target-availability failures are diagnosed, cleaned up best-effort, transitioned into a safe `Fault` state and surfaced through `IRuntimeFeedback`; contract or programming failures are diagnosed, cleaned up without replacing the primary exception, and rethrown. Automatic activation retains the failed target as the suppression target so a failing foreground window is not immediately retried in a loop.

Native menu detection, menu-overlay creation, and cross-filter refresh are subordinate operations. Their failure closes transient menu overlays and restores the primary overlay's self-filter instead of disabling the application correction.

`ShowWindow` and `InvalidateRect` are handled explicitly at their call sites because their Boolean return values do not represent a standard extended-error success contract.

## Configuration grid boundary

`ApplicationAssignmentsGrid` remains the UserControl boundary and owns selection, binding, row updates, empty-state visibility, stable executable-path keys and typed change publication. Stable column/selector construction lives in `ApplicationAssignmentsGridColumns`; pure cell-to-command decoding lives in `ApplicationAssignmentCellChangeMapper`; enabled-state indicator geometry/painting lives in `ApplicationAssignmentEnabledCellRenderer`; selector data-error classification and diagnostics live in `ApplicationAssignmentSelectorErrorPolicy`. The grid does not know about persistence or dialogs.

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

Architecture checks are behavior-first. Transaction publication, coherent settings snapshots, runtime contracts, overlay-session lifetime, target availability, frame rendering, menu tracking, native call classification, structured diagnostics, transform catalog consistency, overlay-scope and menu-profile recovery, grid commits, menu roles, preview caching, and profile-manager refresh behavior are exercised through executable tests.

Source inspection is retained only for exhaustive negative rules that cannot be proven by a finite runtime scenario:

- collection and property writes must remain inside their mutation authorities;
- UI and runtime components must not instantiate the persistence store;
- empty catch blocks are forbidden;
- removed legacy mutation services must not return.

These focused scans intentionally avoid asserting field names, statement ordering, formatting, RGB literals, or the exact internal spelling of valid implementations.
