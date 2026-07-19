# SightAdapt 0.4A.3.007 — superseded architecture audit

## Status

This document previously presented an unconditional `10/10` result for KISS, DRY, Clean Code, Single Point of Authority, and Single Point of Truth. A subsequent source review found material gaps that were not covered by the original evidence. The former score and completion statement are withdrawn.

The remediation is documented in [`ARCHITECTURE_REMEDIATION_0.4A.4.md`](ARCHITECTURE_REMEDIATION_0.4A.4.md).

## What the original hardening established

The 0.4A.3 work created useful boundaries that remain part of the design:

| Responsibility | Domain component |
|---|---|
| Application assignment operations | `ApplicationProfileManagementService` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Persisted automatic-mode value | `AutomaticModeManagementService` |
| Settings recovery and canonicalization | `SettingsStore.Normalize` |
| Overlay ownership | `OverlayController` |
| Runtime transitions | `ApplicationStateController` |
| Built-in tuning defaults | `VisualProfileDefaults` |

Those components improved the baseline, but named services alone did not prove transactionality, complete runtime truth, or exclusive mutation authority.

## Findings missed by the original audit

### Safety and persistence

- Emergency shutdown performed settings synchronization and I/O before the unconditional overlay stop.
- UI and runtime mutated the shared settings object before attempting persistence.
- A failed write could leave a new state active only in memory.
- Multiple process instances could independently write the same settings file.

### Runtime source of truth

- Active mode and target were stored in `ApplicationStateController`.
- Active profile identity was stored separately in `OverlayController`.
- automatic suppression and last-window state were held in `SightAdaptContext` fields.
- changing the profile for the same target could therefore fail to produce a runtime-state event and leave stale tray text.
- renderer faults and explicit emergency shutdown shared one state even though their persistence semantics differed.

### DRY and policy

- supported transforms and tuning capability were repeated in policy, model, catalog, and preview code;
- profile limits and name length were repeated in the UI;
- application assignment creation and toggling duplicated most of their flow;
- product metadata appeared in both `ProductInfo` and the project file;
- tray icon documentation and runtime rendering both claimed or implied authority;
- user-defined profile limitations in `DEMO.md` contradicted the implemented manager.

### Clean Code and enforcement

- `SightAdaptContext` combined tray presentation, foreground tracking, persistence, runtime orchestration, and notifications;
- a custom combo-box column hid the framework `DataSource` property with incompatible semantics;
- the editor rounded all percentage values and rewrote unchanged fields when any field changed;
- expected failures were sometimes swallowed without diagnostics;
- source regex tests were useful guardrails but were treated as stronger proof than they provided.

## Corrected assessment of the pre-remediation implementation

| Principle | Corrected score | Main reason |
|---|---:|---|
| KISS | 6/10 | Several responsibilities and state fragments were concentrated in the application context. |
| DRY | 5/10 | Transform capabilities, UI limits, metadata, icon definitions, and mutation flows were duplicated. |
| Clean Code | 6/10 | Naming was generally good, but contracts and error handling were inconsistent. |
| Single Point of Authority | 4/10 | Services existed, but mutation and persistence were not one enforced transaction boundary. |
| Single Point of Truth | 4/10 | Runtime state, transform capabilities, metadata, documentation, and icon authority were split. |

These scores describe the source before the remediation branch. They are not a claim about the final remediated result.

## Evidence standard after remediation

A future closing assessment must distinguish:

1. **named responsibility** — a class is documented as owner;
2. **enforced authority** — callers cannot bypass the owner in normal product flows;
3. **transactional authority** — failed persistence cannot publish a partial state;
4. **single truth** — runtime presentation and behavior are derived from the same complete snapshot;
5. **automated evidence** — behavior tests validate failure paths, not only source text patterns;
6. **Windows validation** — build, tests, publish, and manual emergency behavior are verified on Windows.

No future audit should assign an unconditional perfect score while known limitations remain or while evidence consists only of source-pattern restrictions.
