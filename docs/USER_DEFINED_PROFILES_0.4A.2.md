# SightAdapt 0.4A.2 — user-defined visual profiles

## Status

Complete and manually accepted. This behavior remains part of the current 0.4B.2 stack.

## Purpose

User-defined profiles allow applications to use independent Soft Invert parameter sets instead of sharing only the built-in `Soft invert` definition.

## Profile types

### Built-in profiles

- `Exact invert` — fixed transformation; cannot be edited, renamed, duplicated, or deleted.
- `Soft invert` — editable shared default; cannot be renamed or deleted.

### User-defined profiles

A user-defined profile:

- has a stable identifier beginning with `user-`;
- uses the Soft Invert transform;
- stores independent output-black, output-white, brightness, contrast, saturation, and hue values;
- can be assigned to one or more applications;
- can be renamed, duplicated, edited, and deleted.

## User workflow

1. Open **Configure applications and colors...**.
2. Select **Manage profiles**.
3. Create a profile from Soft Invert defaults or duplicate an existing editable profile.
4. Give the profile a unique name.
5. Edit its color parameters.
6. Close the manager and choose the new profile in an application's **VISUAL PROFILE** column.

## Validation rules

- names are trimmed, required, limited to 80 characters, and unique without regard to case;
- built-in profiles cannot be renamed or deleted;
- Exact Invert cannot be duplicated because it is not a tunable Soft Invert profile;
- generated user identifiers are independent from display names;
- application assignments reference profile identifiers, so renaming does not break assignments;
- invalid or missing profile references recover through the documented built-in fallback.

## Deletion behavior

Before deletion, the manager displays how many applications use the selected profile.

When a user-defined profile is deleted:

1. affected application assignments are moved to built-in `Soft invert`;
2. the user-defined profile is removed;
3. the candidate settings snapshot is normalized and saved atomically;
4. committed settings are published only after persistence succeeds;
5. open application and profile views refresh from committed settings.

A failed mutation or failed write leaves the last committed profile collection and assignments unchanged.

## Authorities

- `VisualProfileManagementService` owns create, duplicate, rename, tuning, delete, and assignment-count operations.
- `VisualProfilePolicy` owns built-in protection, identifier generation, name normalization, and fallback identifiers.
- `ApplicationProfileManagementService` owns application-to-profile assignment.
- `SettingsCoordinator` owns the copy → mutate → save → publish transaction.
- `SettingsCoordinator.Current.VisualProfiles` is the committed profile collection.

## Regression baseline

Automated validation covers:

- creation from Soft Invert defaults;
- independent stable identifiers;
- duplication of all tuning values;
- case-insensitive name uniqueness;
- built-in profile protection;
- assignment counting;
- deletion and fallback reassignment;
- deterministic suggested names;
- persistence of custom profiles and application assignments;
- rollback after domain or persistence failure;
- configuration-grid assignment without reentrant refresh.

## Manual acceptance baseline

1. Create two profiles with different parameters.
2. Assign each profile to a different application.
3. Confirm that editing one profile does not change the other application.
4. Duplicate a profile and confirm that its values match before editing.
5. Rename a user-defined profile and confirm that assignments remain intact.
6. Confirm that built-in rename and delete actions are unavailable.
7. Delete a profile used by an application and confirm reassignment to Soft Invert.
8. Restart SightAdapt and confirm that profiles, parameters, names, assignments, and per-application overlay scopes persist.