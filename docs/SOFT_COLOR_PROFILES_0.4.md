# SightAdapt 0.4 Alpha — Soft Color Profiles

## Purpose

SightAdapt 0.4 Alpha introduces configurable color correction profiles for individual Windows applications. The first implementation extends exact color inversion with **Soft Invert**, which prevents white from becoming pure black and black from becoming pure white.

The feature remains implemented through the Windows Magnification API color matrix. It does not capture all unique pixels, perform palette analysis, or use a per-pixel CPU loop.

## Built-in visual profiles

### Exact invert

`Exact invert` preserves the behavior available before 0.4:

```text
black → white
white → black
```

It is fixed and cannot be edited.

### Soft invert

`Soft invert` is the default for newly configured applications:

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

Existing application assignments are not automatically changed from Exact Invert to Soft Invert during migration.

## Adjustable parameters

| Parameter | Range | Purpose |
|---|---:|---|
| Output black | 0–49% | Prevents the darkest output from reaching pure black |
| Output white | 51–100% | Limits the brightest output value |
| Brightness | -50–50% | Moves the whole output range |
| Contrast | 50–200% | Compresses or expands differences around mid-gray |
| Saturation | 0–200% | Moves from grayscale to amplified color |
| Hue shift | -180–180° | Rotates colors around the hue spectrum |

Values are validated and clamped before they are stored or used by the renderer. Invalid floating-point values are replaced with safe defaults.

## Processing order

The current matrix pipeline applies transformations in this order:

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

All operations are composed into one `MAGCOLOREFFECT` matrix before being applied to the overlay.

## User flow

1. Open **Configure applications and colors...** from the tray menu.
2. Add an application or select an existing row.
3. Choose `Exact invert` or `Soft invert` in the **VISUAL PROFILE** column.
4. Select a row using `Soft invert`.
5. Press **Edit Soft invert**.
6. Adjust the parameters while reviewing the grayscale and hue-spectrum preview.
7. Save the profile.

A saved change is applied immediately when an overlay using that profile is active.

## Shortcut behavior

SightAdapt continues to register exactly two global shortcuts:

| Shortcut | Behavior |
|---|---|
| `Ctrl+Alt+I` | Locally toggles visual correction for the active window without changing persistent settings |
| `Ctrl+Alt+Shift+I` | Adds, disables, or re-enables the persistent application assignment |

The local shortcut uses the visual profile assigned to the application when one exists. A disabled persistent assignment still supplies its selected visual profile for the local toggle, but it does not activate automatically.

Emergency shutdown remains available from the tray menu.

## Settings schema

Version 0.4 uses settings schema `3`.

Visual profiles contain:

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

Schema 2 files are upgraded by adding the built-in Soft Invert definition. Existing application assignments retain their previous visual-profile identifiers.

Legacy files using `effect: "invert"` continue to migrate to `default-invert`.

## Shared-profile behavior

The first 0.4 Alpha slice contains shared built-in profiles:

- all applications assigned to `default-soft-invert` use the same Soft Invert parameters;
- editing the built-in Soft Invert profile affects every application assigned to it;
- Exact Invert remains fixed.

Creating, duplicating, renaming, deleting, importing, and exporting user-defined visual profiles is planned for a later 0.4 increment.

## Current limitations

This implementation does not yet provide:

- analysis of colors present in a target window;
- dominant-color extraction or histogram display;
- source-color-to-output-color rules;
- tolerance ranges for selected colors;
- LUT import;
- a Direct3D shader renderer;
- separate Soft Invert copies for individual applications.

Those features remain part of the later 0.4 and 0.5 roadmap stages.

## Manual acceptance checks

1. Existing schema 2 settings load without losing application assignments.
2. A new application receives Soft Invert by default.
3. Switching between Exact Invert and Soft Invert changes the active overlay.
4. Output-black and output-white limits prevent pure black and pure white output.
5. Brightness, contrast, saturation, and hue changes affect the preview and the target application.
6. Canceling the editor leaves saved values unchanged.
7. Saving the editor persists values after restart.
8. The local shortcut uses the assigned profile without enabling automatic mode.
9. The persistent shortcut still performs add, disable, and re-enable toggling.
10. The tray emergency action removes the overlay and disables automatic mode.
