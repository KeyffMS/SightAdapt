# SightAdapt 0.4 — visual profiles

## Status

Built-in and user-defined visual profiles are complete and manually accepted in the current 0.4 stack.

The current renderer uses one Windows Magnification API `MAGCOLOREFFECT` matrix. It does not perform palette analysis, per-pixel CPU processing, LUT evaluation, or targeted source-color replacement.

## Profile types

### Exact invert

`Exact invert` is a fixed built-in profile:

```text
black → white
white → black
```

It cannot be edited, renamed, duplicated, or deleted.

### Soft invert

`Soft invert` is the editable built-in default for newly configured applications:

```text
black → output white
white → output black
```

Default values:

```text
output black: 8%
output white: 92%
brightness: 0%
contrast: 100%
saturation: 100%
hue shift: 0°
```

The built-in Soft Invert profile is shared by every assignment that references `default-soft-invert`.

### User-defined profiles

User-defined profiles use stable `user-<guid>` identifiers and independent Soft Invert parameters. They can be created, duplicated, renamed, edited, assigned, and deleted.

Deleting a user-defined profile reassigns affected applications to the built-in Soft Invert profile before removal. Detailed lifecycle behavior is documented in [USER_DEFINED_PROFILES_0.4A.2.md](USER_DEFINED_PROFILES_0.4A.2.md).

## Adjustable parameters

| Parameter | Range | Purpose |
|---|---:|---|
| Output black | 0–49% | Prevent the darkest output from reaching pure black |
| Output white | 51–100% | Limit the brightest output value |
| Brightness | -50–50% | Move the whole output range |
| Contrast | 50–200% | Compress or expand differences around mid-gray |
| Saturation | 0–200% | Move from grayscale to amplified color |
| Hue shift | -180–180° | Rotate colors around the hue spectrum |

Values are validated and normalized before persistence and rendering. Invalid floating-point values recover to safe defaults.

## Processing order

The current matrix pipeline composes transformations in this order:

```text
source RGB
  ↓
soft inversion and output limits
  ↓
saturation
  ↓
hue rotation
  ↓
contrast
  ↓
brightness
  ↓
Windows Magnification API output
```

All operations are combined into one matrix before being applied to the active overlay.

## User flow

1. Open **Configure applications and colors...** from the tray menu.
2. Add an application or select an existing row.
3. Choose a profile in the **VISUAL PROFILE** column.
4. Use **Edit color profile** for a tunable profile.
5. Adjust values while reviewing the grayscale and hue-spectrum preview.
6. Save the profile.

A successful save updates an active overlay through the committed-settings change path. The overlay instance is reused when possible.

Use **Manage profiles** to create, duplicate, rename, edit, and delete user-defined profiles.

## Authorities and truths

- `VisualProfileManagementService` owns profile lifecycle and tuning mutations.
- `VisualProfilePolicy` owns built-in IDs, user-ID format, name validation, and fallback rules.
- `VisualProfileDefaults` owns canonical built-in values.
- `VisualProfileLimits` owns parameter ranges.
- `VisualTransformCatalog` owns transform support and tuning capability.
- `SettingsCoordinator.Current.VisualProfiles` is the committed profile collection.
- application assignments store profile identifiers, so renaming a user-defined profile does not break assignment references.

## Persistence

The current settings schema is `4`.

A visual profile is stored in this form:

```json
{
  "id": "default-soft-invert",
  "name": "Soft invert",
  "transformId": "soft-invert",
  "outputBlack": 0.08,
  "outputWhite": 0.92,
  "brightness": 0.0,
  "contrast": 1.0,
  "saturation": 1.0,
  "hueShiftDegrees": 0.0
}
```

Schema `4` also stores `overlayScope` on each application assignment. Overlay scope is independent from visual-profile values.

Older settings are normalized as follows:

- legacy `effect: "invert"` becomes `default-invert`;
- missing built-in profiles are restored canonically;
- missing or invalid profile references use the documented fallback;
- valid existing application assignments are preserved;
- missing or invalid overlay scopes default to `client-area`.

## Shortcut behavior

| Shortcut | Behavior |
|---|---|
| `Ctrl+Alt+I` | Locally toggles visual correction for the active window without changing persistent settings |
| `Ctrl+Alt+Shift+I` | Adds, disables, or re-enables the persistent application assignment |

The local shortcut uses the assigned profile when one exists. A disabled assignment remains available for local use but does not activate automatically.

## Current limitations

The current visual-profile implementation does not provide:

- profile import or export;
- analysis of colors present in a target window;
- RGB or luminance histograms;
- dominant-color extraction;
- targeted source-color-to-output-color rules;
- tolerance ranges for selected colors;
- LUT import;
- a Direct3D shader renderer.

These capabilities are assigned to later roadmap increments and should not be forced into the current matrix model when it cannot represent them deterministically.

## Acceptance baseline

The accepted behavior includes:

1. existing valid assignments survive migration;
2. a new application receives Soft Invert by default;
3. Exact Invert and Soft Invert switch deterministically;
4. output limits prevent pure black and pure white output when configured accordingly;
5. brightness, contrast, saturation, and hue affect preview and target output;
6. cancel leaves committed values unchanged;
7. save persists values and updates an active overlay;
8. independent user-defined profiles do not share tuning values;
9. deleting a used user-defined profile performs fallback reassignment;
10. repeated selector use does not corrupt names or rebuild the active grid transaction.