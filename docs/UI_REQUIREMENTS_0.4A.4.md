# SightAdapt 0.4A.4 — interface correction requirements

## Status

**Complete and manually accepted.**

The screenshot-driven `0.4A.4` interface stage is closed on `agent/fix-audit-v0.4`. The user confirmed that the previously reviewed interface was correct except for the final slider-width issue; that issue was corrected by returning numeric inputs to the parameter-header line and restoring full-width slider tracks.

This document is the single source of truth for the completed `0.4A.4` interface requirements. Architecture remediation remains documented in [`ARCHITECTURE_REMEDIATION_0.4A.4.md`](ARCHITECTURE_REMEDIATION_0.4A.4.md).

## Engineering constraints

Every correction preserves:

- **Clean Code** — focused, named, testable responsibilities;
- **KISS** — the smallest WinForms solution satisfying the requirement;
- **SPoA** — one owner for each UI action and mutation;
- **SPoT** — product metadata, state, limits, transforms, and theme values come from canonical sources;
- **DRY** — no duplicated menu construction, version strings, state rules, transform formulas, or profile mutation logic;
- persisted values, color-processing semantics, emergency behavior, input transparency, keyboard access, accessibility metadata, DPI scaling, and the modern dark theme.

## Completed requirements

### 001 — About window

`About SightAdapt...` is available from the tray menu. The modern-dark dialog shows the canonical icon, product name, milestone, informational version, author, license, and a keyboard-focusable GitHub repository link from `ProductInfo`. The enlarged layout does not truncate metadata.

### 002 — notification-area left click

Left click opens the same `ContextMenuStrip` instance as right click in every runtime state and after restart.

### 003 — configuration window

#### 003.1 — visual-profile selector

The closed selector, active editing state, dropdown button, opened list, hover, focus, selection, disabled state, and read-only state use the modern-dark presentation. The native `DataGridViewComboBoxEditingControl` is not used.

#### 003.2 — activity lamp

Enabled applications use a green circular lamp. Disabled applications use a dark gray circle with a lighter outline. The underlying checkbox value remains the interaction and accessibility authority.

#### 003.3 — canonical product presentation

User-facing `Demo` wording is removed. The current milestone is defined once in assembly metadata and exposed through `ProductInfo`.

### 004 — visual-profile editor

#### 004.1 — profile identity

The supplied working-profile name is shown prominently at the top.

#### 004.2 — modern sliders and direct input

`Output black`, `Output white`, `Brightness`, `Contrast`, `Saturation`, and `Hue shift` provide:

- full-width modern slider tracks;
- editable numeric fields on the same line as the parameter title;
- synchronized slider and numeric values;
- comma and dot decimal-separator support;
- canonical clamping, precision, and step snapping;
- mouse, arrow, Page Up/Down, Home, End, Enter, and Escape behavior;
- focus cues and accessible names.

#### 004.3 — layout capacity

The editor is larger, DPI-scaled, resizable, and has an explicit minimum size.

#### 004.4 — output conversion sample

`Output black` and `Output white` appear first beside a black-on-white source/output sample. The sample and live strips use the same working profile and canonical `VisualTransformCatalog`.

#### 004.5 — section order

1. profile identity;
2. output-limit controls and sample;
3. live grayscale and hue preview;
4. brightness, contrast, saturation, and hue controls;
5. reset, cancel, and save actions.

#### 004.6 — persistence invariants

The form edits a working copy. Each control updates only its corresponding field. Persistence remains owned by `SettingsCoordinator` and `VisualProfileManagementService.UpdateTuning`. Transformation semantics and untouched values remain unchanged.

### 005 — first-review refinements

- visible GitHub link in About;
- enlarged About layout without clipping;
- brighter canonical secondary and muted text;
- custom dark selector during active editing and dropdown display;
- synchronized direct numeric entry for every slider.

### 006 — final slider-width refinement

Numeric input was moved from the slider row into the parameter-header row. `ModernProfileSlider` now owns one full-width `ProfileSliderTrack` and exposes its synchronized value editor for placement beside the title. This preserves one value authority while avoiding the narrow-track regression.

## Acceptance decision

The final screenshot review established that the remaining interface was acceptable and identified only requirement `006`. After its correction:

1. the slider track again uses the full available card width;
2. the editable value remains aligned with the parameter title;
3. numeric and slider input remain synchronized;
4. profile semantics and persistence authority remain unchanged;
5. automated build, tests, and Windows publish pass.

`0.4A.4` is therefore closed and `0.4B` may begin.

## Final automated validation

```text
final head: cddb8c91cc5e1b77a016ac129074e0aadb878ece
workflow run: 29729239085
build: 0 warnings, 0 errors
tests: 86 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
artifact: SightAdapt-0.4-Alpha-win-x64
artifact SHA-256: 8ff29da91f53bffe2f4245ce0addf31dd32ed46ab5d636d8be9b17ba6ee5ab7f
```

## Register

| ID | Implementation | Acceptance |
|---|---|---|
| `001` | Implemented and refined | Accepted |
| `002` | Implemented | Accepted |
| `003.1` | Implemented and refined | Accepted |
| `003.2` | Implemented | Accepted |
| `003.3` | Implemented | Accepted |
| `004.1–004.6` | Implemented and refined | Accepted |
| `005.1–005.5` | Implemented | Accepted |
| `006` | Implemented | Accepted |
