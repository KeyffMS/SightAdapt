# SightAdapt 0.4 Alpha — Soft Color Profiles

## Purpose

SightAdapt 0.4 Alpha introduces configurable color correction profiles for individual Windows applications. The first implementation extends exact color inversion with **Soft Invert**, which prevents white from becoming pure black and black from becoming pure white.

The feature remains implemented through the Windows Magnification API color matrix. It does not capture all unique pixels, perform palette analysis, or use a per-pixel CPU loop.

## 0.4A delivery plan

The profile work is completed in four increments before palette analysis begins:

| Increment | Scope | Status |
|---|---|---|
| `0.4A.1` | Built-in Exact Invert and Soft Invert profiles, parameter editor, assignment, persistence, and active-overlay refresh | Implemented and under manual validation |
| `0.4A.2` | User-defined profiles: create, duplicate, rename, delete, reassign, and separate parameters per application | Next functional increment |
| `0.4A.3` | Profile lifecycle, persistence, migration, shortcut, overlay, and emergency-action regression | Planned |
| `0.4A.4` | Interface corrections, DPI and resizing checks, keyboard access, accessibility labels, selector stability, messages, and visual consistency | Planned final 0.4A increment |

`0.4B` palette analysis starts only after `0.4A.4` passes its interface and regression acceptance checks.

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

## Current user flow

1. Open **Configure applications and colors...** from the tray menu.
2. Add an application or select an existing row.
3. Choose `Exact invert` or `Soft invert` in the **VISUAL PROFILE** column.
4. Select a row using `Soft invert`.
5. Press **Edit Soft invert**.
6. Adjust the parameters while reviewing the grayscale and hue-spectrum preview.
7. Save the profile.

A saved change is applied immediately when an overlay using that profile is active.

## Planned user-defined profile flow — 0.4A.2

The next increment adds:

1. create a profile from Soft Invert defaults;
2. duplicate the selected editable profile;
3. give the copy a unique name and identifier;
4. edit and save parameters independently;
5. assign different profiles to different applications;
6. rename or delete user-defined profiles;
7. protect built-in profiles from rename and deletion;
8. reassign affected applications to a safe fallback before deletion.

## Shortcut behavior

SightAdapt continues to register exactly two global shortcuts:

| Shortcut | Behavior |
|---|---|
| `Ctrl+Alt+I` | Locally toggles visual correction for the active window without changing persistent settings |
| `Ctrl+Alt+Shift+I` | Adds, disables, or re-enables the persistent application assignment |

The local shortcut uses the visual profile assigned to the application when one exists. A disabled persistent assignment still supplies its selected visual profile for the local toggle, but it does not activate automatically.

Emergency shutdown remains available from the tray menu.

## Settings schema

Version 0.4 currently uses settings schema `3`.

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

## Current shared-profile behavior

The implemented 0.4A.1 slice contains shared built-in profiles:

- all applications assigned to `default-soft-invert` use the same Soft Invert parameters;
- editing the built-in Soft Invert profile affects every application assigned to it;
- Exact Invert remains fixed.

This limitation is removed by `0.4A.2`, which introduces user-defined copies with independent settings.

## 0.4A.4 interface-corrections scope

The interface-corrections increment will not add a new transformation. It will focus on:

- clipping, alignment, spacing, and control sizing;
- table columns, selected rows, profile selector states, and safe refresh behavior;
- supported DPI scales from 100% through 200%;
- resizing, minimum window dimensions, and multi-monitor movement;
- button hierarchy and enabled/disabled states;
- keyboard navigation, tab order, focus indicators, Enter, and Escape behavior;
- accessible names, descriptions, and text alternatives;
- consistent confirmation, validation, empty-state, and error messages;
- dark-theme consistency across normal, hover, selected, disabled, and error states;
- manual visual-regression checks before work starts on palette analysis.

## Current limitations

This implementation does not yet provide:

- user-defined independent Soft Invert profiles;
- create, duplicate, rename, or delete profile actions;
- analysis of colors present in a target window;
- dominant-color extraction or histogram display;
- source-color-to-output-color rules;
- tolerance ranges for selected colors;
- LUT import;
- a Direct3D shader renderer.

Those features remain assigned to later 0.4 increments and the subsequent renderer roadmap.

## Manual acceptance checks

### Current 0.4A.1 checks

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
11. Repeated profile selection does not corrupt names or raise a WinForms exception.

### Required before 0.4B

1. user-defined profiles complete their full lifecycle safely;
2. missing or deleted references recover to a documented fallback;
3. all regression tests from 0.4A.3 pass;
4. all interface acceptance checks from 0.4A.4 pass at supported DPI scales.
