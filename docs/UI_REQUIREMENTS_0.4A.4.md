# SightAdapt 0.4A.4 — interface correction requirements

## Status

**Complete and manually accepted.**

The screenshot-driven `0.4A.4` interface stage is closed on `agent/fix-audit-v0.4`. Requirements `001–007` were implemented and accepted in the running Windows application.

This document is the single source of truth for `0.4A.4` interface requirements and acceptance. The closing engineering assessment is recorded in [`ARCHITECTURE_AUDIT_0.4A.4_FINAL.md`](ARCHITECTURE_AUDIT_0.4A.4_FINAL.md).

## Engineering constraints

Every correction preserves:

- **Clean Code** — focused, named, testable responsibilities;
- **KISS** — the smallest WinForms solution satisfying the requirement;
- **SPoA** — one owner for each UI action and mutation;
- **SPoT** — product metadata, state, limits, transforms, theme values, and slider values come from canonical sources;
- **DRY** — no duplicated menu construction, version strings, state rules, transform formulas, range mapping, or profile mutation logic;
- persisted values, color-processing semantics, emergency behavior, input transparency, keyboard access, accessibility metadata, DPI scaling, and the modern dark theme.

## Accepted requirements

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

`Output black`, `Output white`, `Brightness`, `Contrast`, `Saturation`, and `Hue shift` provide editable numeric fields, synchronized values, comma and dot decimal separators, canonical clamping and precision, mouse and keyboard operation, focus cues, and accessible names.

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

### 006 — numeric-field placement

Numeric input is aligned beside the parameter title. The slider occupies a separate row and retains one value authority with the text field.

### 007 — full-width neutral-centered slider behavior

#### 007.1 — complete visual rail

The unfilled rail is clearly visible across the full available width of each parameter card. The active segment is visually distinct from the total slider width.

#### 007.2 — neutral reference points

The four color-adjustment sliders expose a visible neutral marker:

- `Brightness`: `0%`;
- `Contrast`: `100%`;
- `Saturation`: `100%`;
- `Hue shift`: `0°`.

Neutral values are visually centered. Asymmetric domain ranges use one piecewise mapping so the neutral value remains at the midpoint without changing the stored numeric domain.

#### 007.3 — gentle magnetic snapping

During mouse dragging, the thumb snaps to the neutral value inside a small three-percent rail window. Direct numeric input and keyboard stepping retain exact canonical step behavior.

#### 007.4 — visual direction

For controls with a neutral point, the accent segment runs between the neutral marker and the current thumb. Positive and negative deviation remain readable while the complete rail stays visible.

#### 007.5 — responsibility split

`ProfileSliderControl.cs` owns slider input, mapping, neutral reference, magnetic snapping, and track painting. `ProfileEditorControls.cs` owns profile previews.

## Acceptance decision

The user confirmed the final full-width rail, neutral markers, centered mapping, and magnetic snapping in the running application. No additional screenshot defect remains open in `0.4A.4`.

## Automated validation

```text
code audit head: 74f6609581810584b853f80e12eb9a6ad1cd05da
workflow run: 29740090542
build: 0 warnings, 0 errors
tests: 91 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
artifact: SightAdapt-0.4-Alpha-win-x64
artifact SHA-256: 3d684f7d1f1175a811e14d1ceba54f64189a69f43a095581fffc764a3a8f47a7
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
| `007.1–007.5` | Implemented and validated | Accepted |
