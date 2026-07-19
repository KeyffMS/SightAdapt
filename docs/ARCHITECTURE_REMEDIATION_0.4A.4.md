# SightAdapt 0.4A.4 — architecture remediation

## Scope

This increment remediates the independent KISS, DRY, Clean Code, Single Point of Authority, and Single Point of Truth review of `agent/alpha-v0.4`. It changes architecture and failure behavior without changing the intended color-matrix semantics or settings schema.

Implementation branch: `agent/fix-audit-v0.4`.

## Remediation map

| Finding | Change | Primary regression evidence |
|---|---|---|
| Emergency stopped the overlay after settings I/O | `EmergencyDisable` disables `OverlayController` before any commit attempt | `EmergencyStopsOverlayBeforePersistence` |
| Fault and emergency used one misleading state | Added distinct `Fault` and `Emergency` runtime states | `FaultAndEmergencyBlockAutomaticActivation`, state-source tests |
| Failed writes left uncommitted memory state | Added `SettingsCoordinator`: copy, mutate, normalize, save, publish | `SettingsCoordinatorTests` |
| Domain exception could leave a partial mutation | Mutations operate on a candidate snapshot | `FailedDomainMutationDoesNotPublishPartialChanges` |
| Multiple processes could write one settings file | Added a named per-session mutex in `Program` | `ProgramEnforcesSingleInstance` plus manual launch check |
| Runtime state omitted the active profile | `ApplicationState` now contains target, profile ID, suppression, mode, and message | `ProfileChangeOnSameTargetIsARealTransition` |
| Suppression was an unrelated context field | Suppression moved into `ApplicationStateController` | `SuppressionClearsAfterForegroundChanges` |
| Foreground polling overloaded the application context | Added `ForegroundWindowTracker` | architecture composition test |
| Tray construction and presentation overloaded the context | Added `TrayPresenter` | architecture composition test |
| Transform support was repeated | Added canonical `VisualTransformDefinition` entries in `VisualTransformCatalog` | catalog capability tests |
| UI repeated profile limits | Editor derives ranges and descriptions from `VisualProfileLimits` | profile-editor architecture test |
| Editing one field rewrote rounded values in all fields | Each input updates only its own property; percentage inputs retain two decimals | source regression plus manual precision check |
| Assignment add/toggle duplicated their algorithm | Added shared `MutateAssignment`, `GetOrCreate`, and finalization flow | existing assignment lifecycle tests |
| UI hid `DataSource` with incompatible behavior | Replaced it with `StableVisualProfileComboBoxColumn.SetProfiles` | combo-column regression |
| Product metadata was duplicated in code | `.csproj`/assembly attributes are authoritative; `ProductInfo` reads them | product metadata architecture test |
| Tray icon sources competed for authority | `TrayIconSet.cs` is authoritative; SVGs are reference exports | documentation/source review |
| User-defined profile documentation was stale | `DEMO.md` now lists implemented lifecycle operations | documentation review |
| Expected failures were silently swallowed | Expected discovery/grid/repository failures are diagnosed or shown | empty-catch architecture test |
| Regex tests were treated as complete proof | Retained source checks as guardrails and added behavioral rollback/state tests | test suite structure |

## Settings authority

`SettingsCoordinator` is the product-flow authority for committed settings:

```text
Current committed snapshot
        |
        v
CreateWorkingCopy
        |
        v
Apply domain mutation
        |
        v
SettingsStore.Save
(normalize + atomic file replacement)
        |
        v
Current.ReplaceWith
        |
        v
Changed event
```

A mutation or write failure returns a failed result without replacing `Current` and without raising `Changed`.

`SettingsStore.Normalize` remains the authority for migration, canonicalization, malformed-data recovery, and reference repair. Domain services remain the authorities for individual mutation categories. The coordinator provides the transaction boundary around them.

## Runtime truth

`ApplicationStateController.Current` now holds the complete product runtime snapshot needed by behavior and presentation:

- run state: inactive, manual, automatic, fault, or emergency;
- target window;
- active visual-profile ID;
- automatically suppressed window;
- state message.

`OverlayController` owns the actual overlay resource and validates the profile through the canonical transform catalog. `TrayPresenter` renders the runtime snapshot instead of reconstructing state from unrelated fields.

## Emergency contract

Emergency shutdown follows this order:

1. close and dispose the overlay;
2. enter runtime `Emergency` state;
3. stop transient fault-state timing;
4. attempt to persist automatic mode as disabled;
5. report whether persistence succeeded.

A failed settings write does not reactivate the overlay. The tray explicitly reports that the session is stopped even when automatic-mode persistence failed.

## KISS and component boundaries

No dependency-injection framework, event bus, repository abstraction, generic command framework, or speculative renderer layer was added. The extracted classes correspond to current responsibilities:

- `SettingsCoordinator` — committed settings transaction;
- `ForegroundWindowTracker` — foreground polling and target resolution;
- `TrayPresenter` — notification-area controls and runtime presentation;
- `ApplicationStateController` — complete runtime state;
- `OverlayController` — overlay resource lifecycle;
- `VisualTransformCatalog` — transform definitions and lookup.

## Automated checks

The branch adds or updates checks for:

- save-before-publish ordering;
- rollback after I/O failure;
- rollback after domain failure;
- emergency stop before persistence;
- same-target profile transitions;
- fault/emergency distinction;
- suppression lifecycle;
- transform capability truth;
- domain-derived editor limits;
- explicit combo-box API;
- assembly-derived product metadata;
- single-instance setup;
- absence of empty catch blocks;
- existing migration, profile lifecycle, assignment, matrix, persistence, and repeated mutation regression.

## Manual Windows checks

1. Start SightAdapt twice; verify the second instance exits with an explanatory message.
2. Activate a profile, trigger emergency shutdown, and verify the visual effect disappears immediately.
3. Make the settings path unwritable, repeat emergency shutdown, and verify the overlay remains stopped while the persistence failure is reported.
4. Force an overlay creation failure and verify the UI reports a fault rather than claiming automatic mode was saved as off.
5. Change the active profile for the same window and verify the tray text changes immediately.
6. Open a profile containing fractional percentage values, modify one different field, save, and verify untouched values retain their precision.
7. Create, duplicate, rename, edit, assign, and delete user-defined profiles.
8. Restart and verify the last successfully committed settings snapshot.

## Validation record

The closing workflow result, test count, publish status, and artifact name are recorded here only after the latest branch head completes Windows CI. Until then, this document describes implemented remediation and required evidence rather than asserting a perfect score.
