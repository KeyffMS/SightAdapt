# SightAdapt 0.4A.4 — final architecture audit

## Scope

This audit evaluates the completed `0.4A` implementation on `agent/fix-audit-v0.4` before work begins on `0.4B`.

The assessment covers:

- Clean Code;
- KISS;
- DRY;
- Single Point of Authority (SPoA);
- Single Point of Truth (SPoT);
- transaction and failure boundaries;
- runtime-state consistency;
- architecture and behavioral regression evidence;
- the UI changes completed through requirement `007`;
- the post-acceptance profile-selector reentrancy fix tracked by issue `#9`.

The scores are engineering assessments for the current `0.4A` scope. They are not universal or permanent guarantees.

## Evidence standard

The audit distinguishes four levels of evidence:

1. **Named responsibility** — a component is documented as an owner.
2. **Enforced product flow** — normal application paths use that owner.
3. **Transactional behavior** — a failed mutation or write does not publish partial state.
4. **Regression evidence** — behavioral tests and focused source guardrails protect the boundary.

Source-string and regex tests are treated as guardrails, not as complete proof by themselves.

## Closing finding and remediation

The review found one material SPoT duplication before scoring:

- active visual-profile identity was present in both `ApplicationStateController.Current.VisualProfileId` and `OverlayController.VisualProfileId`.

`OverlayController.VisualProfileId` was redundant and was not required by product behavior. It was removed. The resulting boundary is now explicit:

- `ApplicationStateController.Current` owns product runtime identity and presentation state;
- `OverlayController` owns only the actual overlay resource and its target handle.

A regression test now rejects reintroduction of profile identity into `OverlayController`.

## Authority map

| Mutation or resource | Authority |
|---|---|
| Committed settings transaction | `SettingsCoordinator` |
| Settings migration, canonicalization, and recovery | `SettingsStore.Normalize` |
| Application assignment add, toggle, enable, assignment, reassignment, and removal | `ApplicationProfileManagementService` |
| Visual-profile creation, duplication, rename, tuning, deletion, and assignment count | `VisualProfileManagementService` |
| Persisted automatic-mode mutation | `AutomaticModeManagementService` |
| Runtime application state and suppression | `ApplicationStateController` |
| Overlay resource creation, update, closure, and disposal | `OverlayController` |
| Foreground-window polling and target resolution | `ForegroundWindowTracker` |
| Notification-area presentation | `TrayPresenter` |

`SettingsCoordinator` wraps domain authorities in a copy → mutate → save → publish transaction. Failed domain operations or persistence do not replace the committed in-memory snapshot and do not raise the settings-changed event.

## Truth map

| Data or rule | Source of truth |
|---|---|
| Persisted applications, profiles, assignments, and automatic-mode value | `SightAdaptSettings` |
| Current runtime mode, target, profile ID, suppression, and message | `ApplicationStateController.Current` |
| Actual overlay resource and target handle | `OverlayController` |
| Built-in profile IDs, assignment fallbacks, user-ID format, and profile-name rules | `VisualProfilePolicy` |
| Canonical Exact Invert and Soft Invert names and tuning defaults | `VisualProfileDefaults` |
| Supported transform definitions and tuning capability | `VisualTransformCatalog` |
| Profile parameter limits | `VisualProfileLimits` |
| Product name, milestone, version, author, repository, and license | project/assembly metadata exposed through `ProductInfo` |
| Accepted `0.4A.4` interface requirements | `UI_REQUIREMENTS_0.4A.4.md` |

## Assessment

| Principle | Score | Evidence and reasoning |
|---|---:|---|
| KISS | **9/10** | No DI container, event bus, repository layer, command framework, or speculative rendering abstraction was added. Extracted components correspond to current product responsibilities. The remaining custom WinForms controls are substantial, but they implement concrete interaction requirements. |
| DRY | **9/10** | Domain mutations, transform capabilities, defaults, limits, metadata, normalization, and runtime state each have a central implementation. Minor duplication remains in repository-link launch/error presentation across two forms. |
| Clean Code | **9/10** | Names, failure contracts, transaction order, and component responsibilities are explicit. `SightAdaptContext` is now an orchestrator rather than the owner of tray and foreground mechanics. Some UI files remain large because they contain detailed WinForms composition and painting. |
| SPoA | **9/10** | All normal product mutations pass through a named domain authority and the transactional coordinator. Architecture tests guard direct writes. The mutable settings model is internal rather than type-system-enforced as immutable, so authority still depends partly on review and regression checks. |
| SPoT | **9/10** | The duplicate runtime profile identity was removed. Product metadata, runtime state, transform capability, defaults, limits, and requirements have explicit truths. The generic slider still contains a convenience heuristic for a neutral point; current editor values are deterministic, but future parameters should declare neutral semantics explicitly. |

## Non-blocking technical debt

The following items do not block `0.4B`, but should remain visible:

1. `ConfigurationForm` and the custom visual-profile selector are large UI implementations. Split them only when a concrete second responsibility or reuse case appears; avoid abstraction solely to reduce line count.
2. Repository-link launch and error handling are repeated in `AboutForm` and `ConfigurationForm`. A small shared launcher may remove this duplication during a future UI maintenance pass.
3. `ModernProfileSlider` infers common neutral values from range and unit. New parameter types must define neutral semantics explicitly rather than relying on that convention.
4. Source-level architecture tests are useful guardrails but must continue to be paired with behavioral tests for transaction rollback, state transitions, persistence, and lifecycle behavior.

## Post-audit regression fix — issue #9

Changing an application's visual profile could raise `InvalidOperationException` because `SettingsCoordinator.Changed` invoked `ConfigurationForm.SettingsChanged` synchronously while the custom `DataGridView` editing control was still committing its cell value. `RefreshProfiles()` then attempted `Rows.Clear()` inside the active grid commit stack.

The correction keeps the transaction and runtime ordering intact while removing UI reentrancy:

- non-Control observers, including `SightAdaptContext`, continue to receive `Changed` synchronously;
- WinForms `Control` observers are dispatched with `BeginInvoke` after the current grid commit stack returns;
- disposed-control races are ignored only when the observer handle has already been destroyed and are written to diagnostics;
- `SettingsCoordinatorUiDispatchTests.WinFormsObserverRunsAfterCommitStackCompletes` verifies that a Control observer is not called inside `Commit`, then receives exactly one notification from the Windows message queue.

This preserves one settings transaction authority while making UI presentation explicitly non-reentrant.

## Validation evidence

```text
implementation head: 9d86a853f456f777d306d48bc8de77aedd045d32
workflow run: 29746408418
build: 0 warnings, 0 errors
tests: 92 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
artifact: SightAdapt-0.4-Alpha-win-x64
artifact SHA-256: d4cd27a734a5603ce8934ccd4f25008a42086d1de5e9ba29eb75a0f78bd0f69b
```

The 92-test suite includes transaction rollback, state transitions, emergency ordering, settings recovery, profile lifecycle, assignment authority, transform behavior, selector contracts, slider behavior, closing audit regressions, and deferred WinForms settings notification.

## Decision

No known blocking Clean Code, KISS, DRY, SPoA, or SPoT violation remains in the current `0.4A` scope.

`0.4A.4 / 007` has been manually accepted, and issue `#9` has an automated regression fix. `0.4A` remains closed and `0.4B` may proceed after a local smoke test of profile switching.

This decision does not mean that the code is permanently perfect. New `0.4B` work must define its authority, truth, persisted model, cleanup contract, failure behavior, and acceptance matrix before implementation.
