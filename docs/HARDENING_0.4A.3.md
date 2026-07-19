# SightAdapt 0.4A.3 — lifecycle hardening and architectural completion

## Purpose

0.4A.3 hardens the profile model and runtime state without adding a new color-processing feature. The increment applies KISS, DRY, Clean Code, Single Point of Authority, and Single Point of Truth to profile policy, settings recovery, lifecycle mutations, and emergency behavior.

The functional hardening baseline is implemented and validated. A final architectural-completion sequence, numbered `0.4A.3.001` through `0.4A.3.007`, closes the remaining direct-mutation, duplicated-default, and method-complexity gaps before 0.4A.4 begins.

## Responsibility map

| Component | Responsibility |
|---|---|
| `VisualProfilePolicy` | Default IDs, fallback rules, built-in IDs, supported transforms, user ID generation, name validation limits, and unique-name generation |
| `VisualProfileManagementService` | Visual-profile lifecycle and tuning mutations |
| `ApplicationProfileManagementService` | Application assignment creation, removal, enablement, toggling, and visual-profile assignment |
| Automatic-mode authority | The single owner of persisted automatic-mode changes, introduced in `0.4A.3.003` |
| `ProfileResolver` | Find application assignments and resolve profile IDs to profile definitions |
| `SettingsStore.Normalize` | Migrate, canonicalize, repair, and validate persisted settings through focused normalization stages |
| `ApplicationStateController` | Own runtime state transitions and block automatic activation during emergency state |
| `OverlayController` | Own overlay creation, update, closure, and disposal |

Forms collect user intent and display results. They must not define profile policy, perform lifecycle validation, or directly mutate persisted domain state.

## Implemented hardening baseline

### Profile policy

The following rules are explicit constants in `VisualProfilePolicy`:

```text
new application assignment  -> default-soft-invert
deleted profile fallback     -> default-soft-invert
missing reference fallback   -> default-invert
maximum profile name length  -> 80 characters
user profile ID prefix       -> user-
```

The missing-reference fallback remains Exact Invert for compatibility and predictable recovery of legacy assignments. New assignments and deliberate deletions use Soft Invert.

### Settings recovery

`SettingsStore.Normalize` handles malformed but parseable JSON deterministically:

- built-in profiles are restored to canonical identities and transforms;
- recoverable custom profiles receive replacement IDs instead of being discarded;
- duplicate IDs and names are repaired deterministically;
- unknown transforms recover to `soft-invert`;
- missing names and executable metadata are reconstructed where possible;
- legacy `effect: "invert"` migrates to `default-invert`;
- missing profile references recover to `default-invert`;
- numeric values are clamped and non-finite values use safe defaults;
- a second normalization pass is idempotent.

### Lifecycle and emergency invariants

- Detached profile objects cannot be renamed or deleted.
- Built-in profiles cannot be renamed or deleted.
- Deletion validates its fallback before modifying assignments.
- Emergency state blocks automatic activation.
- Explicit manual activation remains the deliberate user override.

### Validation baseline

```text
build: 0 warnings, 0 errors
tests: 43 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
```

## Architectural completion plan

### 0.4A.3.001 — application-assignment mutation authority

**Status: next.**

Create `ApplicationProfileManagementService` as the sole authority for persisted application assignments.

Scope:

- add or enable an application assignment;
- remove an assignment;
- assign a visual profile after validating that it exists;
- set enabled state;
- toggle an existing assignment;
- preserve a valid custom profile during disable and re-enable;
- absorb or replace `ApplicationProfileToggleService` so two services do not own the same mutation;
- move assignment mutation out of `ConfigurationForm` and `SightAdaptContext`.

Done when:

- no form directly writes `ApplicationProfile.VisualProfileId` or `ApplicationProfile.Enabled`;
- no form directly adds to or removes from `SightAdaptSettings.Applications`;
- all assignment mutations have focused unit tests.

### 0.4A.3.002 — visual-profile tuning authority

**Status: planned after `.001`.**

Extend `VisualProfileManagementService` with a validated `UpdateTuning` operation.

Scope:

- accept a source profile and working values;
- verify that the source belongs to current settings;
- reject tuning of non-editable transforms;
- validate and clamp values through the same policy/default source used by persistence and rendering;
- make `VisualProfileEditorForm` return user intent or a working copy instead of mutating the source profile directly.

Done when:

- the editor no longer calls `CopyTuningFrom` on persisted state;
- tuning can only change through `VisualProfileManagementService`;
- save and cancel behavior remain covered by tests.

### 0.4A.3.003 — persisted-mode authority and UI mutation cleanup

**Status: planned after `.002`.**

Introduce one authority for persisted automatic-mode changes and complete the UI mutation audit.

Scope:

- centralize `SightAdaptSettings.AutomaticMode` changes;
- route tray, shortcuts, configuration UI, and emergency shutdown through the same operation;
- search for direct writes to persisted profile, assignment, and automatic-mode state;
- retain direct writes only inside named authorities, normalization, deserialization-compatible models, and profile factories;
- keep runtime-only state inside `ApplicationStateController` and `OverlayController`.

Done when:

- forms contain no direct persisted-domain mutations;
- automatic mode has one mutation authority;
- emergency behavior and both shortcuts retain their current semantics.

### 0.4A.3.004 — canonical visual-profile defaults

**Status: planned after `.003`.**

Create one source of truth for canonical Exact Invert and Soft Invert values.

Scope:

- centralize output black, output white, brightness, contrast, saturation, and hue defaults;
- centralize canonical built-in names and transform IDs where practical;
- use the same definitions in profile factories, normalization, renderer fallbacks, editor reset, and tests;
- remove duplicated literals such as `0.08`, `0.92`, and `1.0` where they express product defaults rather than calculation constants.

Done when:

- changing a product default requires one authoritative edit;
- normalization and factories cannot drift apart;
- matrix and reset tests derive expectations from the canonical definitions where appropriate.

### 0.4A.3.005 — focused settings normalization stages

**Status: planned after `.004`.**

Keep `SettingsStore.Normalize` as the single public authority while decomposing its implementation into focused private stages.

Required stages:

```text
CanonicalizeBuiltInProfiles
NormalizeCustomProfiles
NormalizeApplications
RepairProfileReferences
```

Scope:

- preserve current recovery behavior and schema;
- use a small normalization context for shared ID and name sets when this reduces parameter noise;
- keep each helper deterministic and independently testable through the public normalization contract;
- avoid introducing a framework, pipeline abstraction, or generic rule engine.

Done when:

- `Normalize` reads as orchestration rather than a complete implementation;
- each helper has one reason to change;
- all existing recovery and idempotence tests pass unchanged or with clearer assertions.

### 0.4A.3.006 — architectural enforcement and regression suite

**Status: planned after `.005`.**

Add tests and repository checks that prevent the architecture from drifting back.

Scope:

- test every assignment and tuning mutation authority;
- test missing, detached, and protected entities;
- test round trips after every mutation category;
- test automatic-mode authority and emergency protection;
- add a lightweight source-level architecture test or documented CI audit for prohibited direct writes;
- preserve build warnings as errors;
- run repeated lifecycle and selector regression.

Done when:

- all functional and architectural tests pass;
- no known direct-mutation path bypasses an authority;
- the final test count and artifact digest are recorded in the PR and documentation.

### 0.4A.3.007 — final 10/10 architecture audit

**Status: planned after `.006`.**

Perform the closing audit and document objective evidence for each principle.

Required report:

| Principle | Required evidence for completion |
|---|---|
| KISS | No speculative framework or abstraction; every added type has a concrete current responsibility |
| DRY | Each product rule, default, and mutation is implemented once |
| Clean Code | Focused methods and classes, explicit names, deterministic errors, and no oversized orchestration implementation |
| SPoA | One owner for each mutation category: runtime state, overlay, visual profiles, application assignments, and automatic mode |
| SPoT | One authoritative source for persisted data, defaults, policy, runtime state, and product metadata |

Done when:

- the audit identifies no known violation in the 0.4A scope;
- the authority map matches the implementation;
- CI and manual regression pass;
- the roadmap marks `0.4A.3.001–0.4A.3.007` complete;
- 0.4A.4 becomes the active increment.

## Execution order

```text
0.4A.3 baseline   lifecycle recovery and emergency hardening       complete
0.4A.3.001        application-assignment mutation authority        next
0.4A.3.002        visual-profile tuning authority                  planned
0.4A.3.003        persisted-mode authority and UI cleanup          planned
0.4A.3.004        canonical visual-profile defaults                planned
0.4A.3.005        focused settings normalization stages            planned
0.4A.3.006        architectural enforcement and regression         planned
0.4A.3.007        final 10/10 architecture audit                    planned
0.4A.4            interface corrections                            after .007
```

## Manual regression checklist

1. Load existing settings created by earlier alpha versions.
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

## Next increment

`0.4A.4` begins only after `0.4A.3.001–0.4A.3.007` are complete. It performs interface corrections: DPI behavior, resizing, layout, control states, keyboard navigation, accessibility labels, message consistency, and visual regression.