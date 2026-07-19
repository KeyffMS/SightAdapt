# SightAdapt 0.4A.2 — User-defined visual profiles

## Purpose

0.4A.2 allows applications to use independent Soft Invert parameter sets instead of sharing only the built-in `Soft invert` definition.

## Profile types

### Built-in profiles

- `Exact invert` — fixed transformation; cannot be renamed or deleted.
- `Soft invert` — editable shared default; cannot be renamed or deleted.

### User-defined profiles

A user-defined profile:

- has a stable identifier beginning with `user-`;
- uses the Soft Invert transform;
- stores its own output-black, output-white, brightness, contrast, saturation, and hue values;
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

- names are trimmed, required, limited to 80 characters, and unique without regard to letter case;
- built-in profiles cannot be renamed or deleted;
- Exact Invert cannot be duplicated because it is not an editable Soft Invert profile;
- generated user identifiers are independent from display names;
- application assignments continue to reference profile identifiers, so renaming does not break assignments.

## Deletion behavior

Before deletion, the manager displays how many applications use the selected profile.

When a user-defined profile is deleted:

1. all affected application assignments are moved to the built-in `Soft invert` profile;
2. the user-defined profile is removed;
3. settings are saved atomically through the existing settings store;
4. open application and profile lists are refreshed.

## Automated validation

The 0.4A.2 tests cover:

- creation from Soft Invert defaults;
- independent profile identifiers;
- duplication of all tuning parameters;
- case-insensitive name uniqueness;
- protection of built-in profiles;
- assignment counting;
- deletion and fallback reassignment;
- deterministic suggested names;
- persistence of a custom profile and its application assignment.

The validated CI run completed with:

```text
build: 0 warnings, 0 errors
tests: 30 passed, 0 failed, 0 skipped
Windows x64 publish: passed
```

## Manual acceptance checks

1. Create two profiles with different parameters.
2. Assign each profile to a different application.
3. Confirm that editing one profile does not change the other application.
4. Duplicate a profile and confirm that its values match before editing.
5. Rename a user-defined profile and confirm that application assignments remain intact.
6. Confirm that built-in profile rename and delete actions are unavailable.
7. Delete a profile used by an application and confirm reassignment to `Soft invert`.
8. Restart SightAdapt and confirm that profiles, parameters, names, and assignments persist.
