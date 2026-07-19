# SightAdapt 0.4A.3 — lifecycle hardening and regression

## Purpose

0.4A.3 hardens the profile model and runtime state without adding a new color-processing feature. The increment applies KISS, DRY, Clean Code, Single Point of Authority, and Single Point of Truth to profile policy, settings recovery, lifecycle mutations, and emergency behavior.

## Responsibility map

| Component | Responsibility |
|---|---|
| `VisualProfilePolicy` | Default IDs, fallback rules, built-in IDs, supported transforms, user ID generation, name validation limits, and unique-name generation |
| `VisualProfileManagementService` | Create, duplicate, rename, delete, count assignments, and fallback reassignment |
| `ProfileResolver` | Find application assignments and resolve profile IDs to profile definitions |
| `SettingsStore.Normalize` | Migrate, canonicalize, repair, and validate persisted settings |
| `ApplicationStateController` | Own runtime state transitions and block automatic activation during emergency state |
| `OverlayController` | Own overlay creation, update, closure, and disposal |

The forms collect user intent and display results. They do not define profile policy or directly implement lifecycle validation.

## Profile policy

The following rules are explicit constants in `VisualProfilePolicy`:

```text
new application assignment  -> default-soft-invert
deleted profile fallback     -> default-soft-invert
missing reference fallback   -> default-invert
maximum profile name length  -> 80 characters
user profile ID prefix       -> user-
```

The missing-reference fallback remains Exact Invert for compatibility and predictable recovery of legacy assignments. New assignments and deliberate deletions use Soft Invert.

## Settings recovery

`SettingsStore.Normalize` now handles malformed but parseable JSON deterministically.

### Built-in profiles

- `default-invert` is restored to the canonical name, transform, and exact matrix values.
- `default-soft-invert` is restored to the canonical name and transform while retaining valid user tuning.
- Additional profiles that illegally reuse a built-in ID receive a new user ID instead of being discarded.

### User-defined profiles

- missing IDs receive a new `user-<guid>` ID;
- duplicate IDs are reidentified while the first valid owner keeps the original ID;
- unknown transforms recover to `soft-invert`;
- empty names recover to `Custom Soft Invert`;
- duplicate names receive deterministic numeric suffixes;
- names longer than 80 characters are safely shortened;
- numeric values are clamped and non-finite values use safe defaults.

### Application assignments

- `null` entries are removed;
- whitespace is trimmed;
- missing executable names are derived from the executable path;
- missing display names are derived from the executable name;
- duplicate executable paths remain first-wins;
- legacy `effect: "invert"` migrates to `default-invert`;
- missing profile references recover to `default-invert`.

After one recovery pass, normalization is idempotent: running it again does not change the settings.

## Lifecycle authority

`VisualProfileManagementService` now rejects mutation of detached profile objects. A profile must belong to the current `SightAdaptSettings.VisualProfiles` collection before it can be duplicated, renamed, or deleted.

Deletion follows this order:

1. verify that the selected profile belongs to current settings;
2. reject protected built-in profiles;
3. resolve and validate the fallback profile;
4. collect affected assignments;
5. reassign them to the fallback ID;
6. remove the user profile.

A missing fallback leaves the settings unchanged.

## Emergency state

`ApplicationStateController` exposes whether automatic activation is allowed. While the state is `Emergency`:

- automatic activation throws and cannot replace the emergency state;
- foreground tracking already skips automatic activation;
- explicit manual activation remains available as a deliberate user override;
- returning to `Inactive` permits automatic activation again.

## Automated regression

The suite contains 43 passing tests covering:

- color matrix calculations;
- profile selector stability;
- application assignment lookup;
- local versus automatic assignment behavior;
- persistent assignment toggling;
- profile creation, duplication, rename, deletion, and reassignment;
- detached mutation rejection;
- repeated lifecycle operations;
- built-in profile protection;
- null and malformed settings recovery;
- canonical built-in restoration;
- duplicate and reserved IDs;
- duplicate names;
- unknown transforms;
- missing profile references;
- idempotent normalization;
- multiple-profile persistence round trips;
- application-state transitions;
- emergency automatic-reactivation protection.

CI validation:

```text
build: 0 warnings, 0 errors
tests: 43 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
```

## Manual acceptance checklist

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

`0.4A.4` performs interface corrections: DPI behavior, resizing, layout, control states, keyboard navigation, accessibility labels, message consistency, and visual regression.
