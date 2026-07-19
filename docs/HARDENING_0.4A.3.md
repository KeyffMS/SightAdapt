# SightAdapt 0.4A.3 — lifecycle hardening

## Status

The 0.4A.3 sequence established the first profile lifecycle, recovery, and mutation-service boundaries. Its former claim of final architectural completion and `10/10` scores has been withdrawn after a later source review found transactionality, runtime-state, DRY, and documentation gaps.

The historical work remains valid as a baseline. The follow-up changes are recorded in [`ARCHITECTURE_REMEDIATION_0.4A.4.md`](ARCHITECTURE_REMEDIATION_0.4A.4.md), and the superseded assessment is explained in [`ARCHITECTURE_AUDIT_0.4A.3.007.md`](ARCHITECTURE_AUDIT_0.4A.3.007.md).

## Baseline responsibility map

| Component | Responsibility established in 0.4A.3 |
|---|---|
| `VisualProfilePolicy` | Built-in IDs, fallback rules, user-ID generation, and name policy |
| `VisualProfileDefaults` | Canonical Exact Invert and Soft Invert tuning defaults |
| `VisualProfileManagementService` | Profile creation, duplication, rename, tuning, deletion, and usage counting |
| `ApplicationProfileManagementService` | Assignment creation, removal, enablement, toggle, profile assignment, counting, and reassignment |
| `AutomaticModeManagementService` | Automatic-mode value mutation |
| `ProfileResolver` | Assignment lookup and visual-profile resolution |
| `SettingsStore.Normalize` | Migration, canonicalization, recovery, validation, and reference repair |
| `ApplicationStateController` | Runtime state transitions |
| `OverlayController` | Overlay resource lifecycle |

These were useful responsibility boundaries, not by themselves proof of an atomic settings authority or complete runtime source of truth.

## Profile policy baseline

```text
new application assignment  -> default-soft-invert
deleted profile fallback     -> default-soft-invert
missing reference fallback   -> default-invert
maximum profile name length  -> 80 characters
user profile ID prefix       -> user-
```

## Settings recovery baseline

`SettingsStore.Normalize` handles malformed but parseable data deterministically:

- restores canonical built-in identities and transforms;
- preserves valid Soft Invert tuning;
- creates replacement IDs for recoverable custom profiles;
- repairs duplicate IDs and names;
- recovers unknown transforms to `soft-invert`;
- reconstructs missing names and executable metadata where possible;
- migrates legacy `effect: "invert"` to `default-invert`;
- repairs missing profile references to the compatibility fallback;
- clamps numeric values and replaces non-finite values with safe defaults;
- becomes idempotent after one successful recovery pass.

## Lifecycle invariants established

- Detached profile and assignment objects cannot be mutated through the lifecycle services.
- Built-in profiles cannot be renamed or deleted.
- Assignment requires an existing visual profile.
- Deletion validates the fallback before modifying assignments.
- A valid custom assignment survives disable and re-enable.
- Explicit manual activation can override an emergency runtime state.

## Historical subincrements

| Subincrement | Delivered baseline |
|---|---|
| `0.4A.3.001` | Added `ApplicationProfileManagementService` and removed the competing toggle service. |
| `0.4A.3.002` | Added validated tuning mutation and a working-copy editor result. |
| `0.4A.3.003` | Added `AutomaticModeManagementService`. |
| `0.4A.3.004` | Added `VisualProfileDefaults` and `VisualProfileTuning`. |
| `0.4A.3.005` | Split settings normalization into focused deterministic stages. |
| `0.4A.3.006` | Added source-level architecture checks and lifecycle regression. |
| `0.4A.3.007` | Published an assessment later found to overstate the evidence. |

## Evidence limitation discovered later

The original architecture checks restricted selected source patterns but did not establish that:

- mutations were committed only after successful persistence;
- emergency shutdown stopped the overlay before I/O;
- one process exclusively owned the per-user settings file;
- runtime state contained the active profile and suppression state;
- transform capabilities and UI limits had one source;
- product metadata and icon design had one authority;
- documentation matched implemented user-defined profile operations.

Those gaps are addressed in 0.4A.4 with behavioral rollback tests, a settings transaction coordinator, a complete runtime snapshot, focused presentation/tracking components, canonical transform definitions, and corrected documentation.

## Manual baseline checklist

1. Load settings created by earlier alpha versions.
2. Create two custom profiles and assign them to different applications.
3. Edit one profile and verify the second remains unchanged.
4. Duplicate, rename, and delete profiles repeatedly.
5. Delete an assigned profile and verify fallback to Soft Invert.
6. Disable and re-enable an assignment and verify its valid custom profile remains selected.
7. Use the local toggle with enabled, disabled, and missing assignments.
8. Switch automatic mode between configured and unconfigured applications.
9. Trigger emergency shutdown and verify no automatic reactivation occurs.
10. Restart and verify profiles, tuning, enabled states, and assignments.

The extended safety and transaction checklist is maintained in the 0.4A.4 remediation document.
