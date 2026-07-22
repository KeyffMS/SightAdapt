# SightAdapt 0.4A.4 — historical final architecture audit

## Document status

This file records the closing assessment of the 0.4A implementation. It is retained for engineering traceability and does **not** define the current 0.4B.2 architecture.

Current architecture: [ARCHITECTURE_0.4.md](ARCHITECTURE_0.4.md)

Current roadmap: [ROADMAP_0.4.md](ROADMAP_0.4.md)

Branch names, commit hashes, workflow identifiers, test counts, and proposed next work below describe the repository at the time of the audit.

## Scope at the time

The audit evaluated:

- Clean Code;
- KISS;
- DRY;
- Single Point of Authority;
- Single Point of Truth;
- settings transaction and rollback behavior;
- runtime-state consistency;
- UI acceptance through 0.4A.4;
- architecture and behavioral regression evidence.

The scores were engineering assessments for the completed 0.4A scope, not mathematical or permanent guarantees.

## Evidence standard

The audit distinguished:

1. named responsibility;
2. normal product flow through that responsibility;
3. transactional failure behavior;
4. behavioral regression evidence.

Source-string and regular-expression tests were treated as guardrails rather than complete proof.

## Closing 0.4A authority map

| Concern | Authority at 0.4A closure |
|---|---|
| Committed settings transaction | `SettingsCoordinator` |
| Settings migration and recovery | `SettingsStore.Normalize` |
| Application-assignment mutations | `ApplicationProfileManagementService` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Persisted automatic mode | `AutomaticModeManagementService` |
| Runtime mode, target, profile, suppression, and message | `ApplicationStateController.Current` |
| Overlay resource lifetime | `OverlayController` |
| Foreground-window polling | `ForegroundWindowTracker` |
| Notification-area presentation | `TrayPresenter` |

## Closing 0.4A assessment

| Principle | Score | Finding at closure |
|---|---:|---|
| KISS | 9/10 | No speculative DI, event-bus, repository, command, or renderer framework was introduced. |
| DRY | 9/10 | Domain mutations, defaults, limits, metadata, normalization, and runtime state had named central implementations. |
| Clean Code | 9/10 | Failure contracts and component responsibilities were explicit, with remaining complexity concentrated in concrete WinForms UI code. |
| SPoA | 9/10 | Normal mutations passed through named domain authorities and the settings transaction boundary. |
| SPoT | 9/10 | Redundant runtime profile identity was removed from `OverlayController`; product state remained in `ApplicationStateController.Current`. |

The audit intentionally did not claim permanent perfection.

## Non-blocking debt recorded at closure

1. Large WinForms composition and painting files should be split only when a concrete second responsibility or reuse case appears.
2. Repository-link launch/error handling was repeated in two forms.
3. Generic slider neutral-value conventions needed care before supporting new parameter types.
4. Source-level architecture guardrails needed continued behavioral-test support.

## Issue #9 supersession note

An intermediate post-audit attempt used deferred WinForms settings notification to avoid reentrant grid refresh. That approach was later removed and is **not** part of the current architecture.

The accepted issue #9 correction is documented in [CONFIGURATION_GRID_REFACTOR_0.4.md](CONFIGURATION_GRID_REFACTOR_0.4.md):

- `SettingsCoordinator.Changed` remains synchronous;
- `ModernSelectorEditingControl` follows the `IDataGridViewEditingControl` contract;
- `ApplicationProfilesGrid` owns grid mechanics and stable executable-path row keys;
- `ConfigurationForm` suppresses only its own full refresh during a grid-originated commit;
- success updates one row and failure restores one cell;
- global timers, delayed observer dispatch, `Application.Idle`, reflection, and modal-owner workarounds are absent.

This note replaces the obsolete deferred-dispatch description that appeared in an earlier version of this audit.

## Historical validation record

The final 0.4A audit was supported by a green Windows build, test, and self-contained publish at the audited source revision. Exact historical identifiers remain available in Git history and the corresponding workflow records.

Later increments added more tests and changed architecture in the following areas:

- configuration-grid ownership;
- generic selector reuse;
- per-application overlay scope;
- overlay geometry authority;
- foreground deduplication;
- application-identity caching;
- overlay retargeting and transition grace.

## Decision at the time

No known blocking Clean Code, KISS, DRY, SPoA, or SPoT violation remained in the accepted 0.4A scope, so 0.4B work could proceed.

That decision is historical. Current integration readiness must be judged against the current source, [ARCHITECTURE_0.4.md](ARCHITECTURE_0.4.md), the current roadmap, fresh CI, and local acceptance.