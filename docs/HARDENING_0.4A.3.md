# SightAdapt 0.4A.3 — lifecycle hardening and architectural completion

## Purpose

0.4A.3 hardens the profile model and runtime state without adding a new color-processing capability. It applies KISS, DRY, Clean Code, Single Point of Authority, and Single Point of Truth to profile policy, persisted mutations, settings recovery, lifecycle operations, and emergency behavior.

**Status: complete.** The functional baseline and `0.4A.3.001–0.4A.3.007` are implemented. `0.4A.4` is the active next increment.

## Final responsibility map

| Component | Responsibility |
|---|---|
| `VisualProfilePolicy` | Built-in IDs, fallback rules, supported transforms, user-ID generation, and name policy |
| `VisualProfileDefaults` | Canonical Exact Invert and Soft Invert names and tuning values |
| `VisualProfileManagementService` | Profile creation, duplication, rename, tuning, deletion, usage counting, and deletion orchestration |
| `ApplicationProfileManagementService` | Application assignment creation, removal, enablement, toggling, profile assignment, counting, and reassignment |
| `AutomaticModeManagementService` | Persisted automatic-mode mutation |
| `ProfileResolver` | Assignment lookup and visual-profile resolution |
| `SettingsStore.Normalize` | Migration, canonicalization, recovery, validation, and reference repair |
| `ApplicationStateController` | Runtime state transitions and emergency protection |
| `OverlayController` | Overlay creation, update, closure, disposal, target, and active profile identity |

Forms collect user intent and display results. They do not define profile policy or own persisted-domain mutation rules.

## Completed hardening baseline

### Profile policy

```text
new application assignment  -> default-soft-invert
deleted profile fallback     -> default-soft-invert
missing reference fallback   -> default-invert
maximum profile name length  -> 80 characters
user profile ID prefix       -> user-
```

### Settings recovery

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

### Lifecycle and emergency invariants

- Detached profile and assignment objects cannot be mutated through an authority.
- Built-in profiles cannot be renamed or deleted.
- Assignment requires an existing visual profile.
- Deletion validates the fallback before modifying assignments.
- A valid custom assignment survives disable and re-enable.
- Emergency state blocks automatic activation.
- Explicit manual activation remains the deliberate user override.

## Completed architectural sequence

### 0.4A.3.001 — application-assignment mutation authority

**Status: complete.**

Delivered:

- added `ApplicationProfileManagementService`;
- centralized add, remove, assign, enable, disable, toggle, count, and reassignment operations;
- removed `ApplicationProfileToggleService`;
- removed assignment mutation from forms and runtime context;
- retained Soft Invert for new assignments and Exact Invert for recovery of corrupted existing references.

### 0.4A.3.002 — visual-profile tuning authority

**Status: complete.**

Delivered:

- added `VisualProfileManagementService.UpdateTuning`;
- validates profile membership and editability;
- clamps persisted tuning values;
- changed `VisualProfileEditorForm.Edit` to return a working profile or `null`;
- removed direct mutation of persisted profiles from the editor;
- retained Save and Cancel semantics.

### 0.4A.3.003 — persisted automatic-mode authority

**Status: complete.**

Delivered:

- added `AutomaticModeManagementService`;
- routed configuration UI, tray state, shortcut behavior, profile enablement, and emergency shutdown through the same persisted-mode boundary;
- reused `ApplicationStateController.AllowsAutomaticActivation` in automatic evaluation.

### 0.4A.3.004 — canonical visual-profile defaults

**Status: complete.**

Delivered:

- added `VisualProfileDefaults` and `VisualProfileTuning`;
- centralized built-in names and Exact/Soft tuning values;
- used the same values in profile factories, tuning normalization, rendering, editor reset, settings canonicalization, and tests;
- removed the obsolete `CopyTuningFrom` mutation helper.

### 0.4A.3.005 — focused settings normalization stages

**Status: complete.**

`SettingsStore.Normalize` remains the single authority and now orchestrates:

```text
CanonicalizeBuiltInProfiles
NormalizeCustomProfiles
NormalizeApplications
RepairProfileReferences
```

A small private `SettingsNormalizationContext` owns shared profile IDs, executable paths, normalized collections, and the changed flag. No generic rule engine or framework was introduced.

### 0.4A.3.006 — architectural enforcement and regression

**Status: complete.**

Delivered:

- source-level tests for prohibited direct writes;
- authority tests for assignments, tuning, and automatic mode;
- detached, missing, and protected-entity tests;
- combined mutation persistence round trip;
- repeated create, assign, rename, delete, and fallback cycles;
- warnings-as-errors, complete test execution, and Windows x64 publishing.

Validated implementation:

```text
build: 0 warnings, 0 errors
tests: 64 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
artifact SHA-256: 98572fcc2522cdfd148569cf056f8d8a972a7187db1484d5b8de477bf5466c47
```

### 0.4A.3.007 — final 10/10 architecture audit

**Status: complete.**

The final report records objective evidence for:

| Principle | Result |
|---|---:|
| KISS | 10/10 |
| DRY | 10/10 |
| Clean Code | 10/10 |
| Single Point of Authority | 10/10 |
| Single Point of Truth | 10/10 |

See [`ARCHITECTURE_AUDIT_0.4A.3.007.md`](ARCHITECTURE_AUDIT_0.4A.3.007.md).

## Manual regression checklist

1. Load settings created by earlier alpha versions.
2. Create two custom profiles and assign them to different applications.
3. Edit one profile and verify the second remains unchanged.
4. Duplicate, rename, and delete profiles repeatedly.
5. Delete an assigned profile and verify fallback to Soft Invert.
6. Disable and re-enable an assignment with `Ctrl+Alt+Shift+I` and verify its valid custom profile remains selected.
7. Use `Ctrl+Alt+I` with an enabled assignment, a disabled assignment, and no assignment.
8. Switch automatic mode between configured and unconfigured applications.
9. Trigger emergency shutdown and verify no automatic reactivation occurs.
10. Use an explicit manual toggle after emergency and verify correction can resume.
11. Restart SightAdapt and verify profiles, names, tuning, enabled states, and assignments.
12. Repeat profile selector changes and editor operations without WinForms exceptions.

## Current execution order

```text
0.4A.3 baseline   lifecycle recovery and emergency hardening       complete
0.4A.3.001        application-assignment mutation authority        complete
0.4A.3.002        visual-profile tuning authority                  complete
0.4A.3.003        persisted automatic-mode authority               complete
0.4A.3.004        canonical visual-profile defaults                complete
0.4A.3.005        focused settings normalization stages            complete
0.4A.3.006        architectural enforcement and regression         complete
0.4A.3.007        final 10/10 architecture audit                    complete
0.4A.4            interface corrections                            active
```

## Next increment

`0.4A.4` covers DPI behavior, resizing, layout, control states, keyboard navigation, accessibility labels, message consistency, and visual regression. It must not change transformation semantics or persisted profile values.
