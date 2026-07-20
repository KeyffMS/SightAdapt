# SightAdapt 0.4A.4 — interface correction requirements

## Status

**Implementation is complete on `agent/fix-audit-v0.4`. Manual Windows, screenshot, keyboard, resize, multi-monitor, and DPI acceptance remains pending.**

This document is the single source of truth for screenshot-driven `0.4A.4` interface requirements. Architecture remediation remains documented in [`ARCHITECTURE_REMEDIATION_0.4A.4.md`](ARCHITECTURE_REMEDIATION_0.4A.4.md).

## Engineering constraints

Every correction preserves:

- **Clean Code** — focused, named, testable responsibilities;
- **KISS** — the smallest WinForms solution satisfying the requirement;
- **SPoA** — one owner for each UI action and mutation;
- **SPoT** — product metadata, state, limits, transforms, and theme values come from canonical sources;
- **DRY** — no duplicated menu construction, version strings, state rules, transform formulas, or profile mutation logic;
- persisted values, color-processing semantics, emergency behavior, input transparency, keyboard access, accessibility metadata, DPI scaling, and the modern dark theme.

## Numbered requirements

### 001 — About window

Add `About SightAdapt...` to the tray menu. The modern-dark dialog shows the canonical icon, product name, current milestone, informational version, author, license, and repository information from `ProductInfo`. It supports keyboard closing, predictable focus, accessible names, and DPI scaling.

**Implementation:** `AboutForm` and the existing canonical tray icon.

### 002 — notification-area left click

Left click must open the same `ContextMenuStrip` instance as right click in every runtime state and after restart.

**Implementation:** `TrayPresenter` owns one menu and opens it from the left-click handler.

### 003 — configuration window

#### 003.1 — visual-profile selector

The closed selector, active editing state, dropdown button, opened list, hover, focus, selection, disabled state, and read-only state must remain modern-dark. Profile names must not clip or render stale values.

**Implementation:** `StableVisualProfileComboBoxColumn`, `ModernVisualProfileComboBoxCell`, `ModernVisualProfileEditingControl`, and a custom owner-drawn `ToolStripDropDown`. The native `DataGridViewComboBoxEditingControl` is not used.

#### 003.2 — activity lamp

Enabled applications use a green circular lamp. Disabled applications use a dark gray circle with a lighter outline. The underlying checkbox value remains the interaction and accessibility authority.

#### 003.3 — canonical product presentation

User-facing `Demo` wording is removed. The current milestone is defined once in assembly metadata and exposed through `ProductInfo`. Current milestone: `Alpha 0.4A.4`.

### 004 — visual-profile editor

#### 004.1 — profile identity

The supplied working profile name is shown prominently at the top.

#### 004.2 — modern sliders

`Output black`, `Output white`, `Brightness`, `Contrast`, `Saturation`, and `Hue shift` use modern sliders with canonical ranges, current values, units, mouse control, arrow keys, Page Up/Down, Home, End, focus cues, and accessible names.

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

#### 005.1 — GitHub link

About includes a visible, keyboard-focusable link showing `ProductInfo.RepositoryDisplay` and opening `ProductInfo.RepositoryUrl`.

#### 005.2 — About size

The dialog and identity layout are enlarged. Tagline, full informational version, author, repository, and license do not use ellipsis.

#### 005.3 — text contrast

Canonical `AppTheme.TextSecondary` and `AppTheme.TextMuted` are brighter across the application while remaining subordinate to primary text.

#### 005.4 — selector after activation

The active selector and its opened list remain custom modern-dark and never fall back to a native Windows combo appearance.

#### 005.5 — direct numeric input

Every slider includes a synchronized numeric `TextBox`. Both comma and dot decimal separators are accepted. Values are clamped and snapped to canonical limits and steps. Enter commits, Escape restores, and slider or keyboard changes update the field.

## Acceptance requirements

1. No clipping at 100%, 125%, 150%, 175%, or 200% DPI.
2. Keyboard-only operation has predictable tab order, focus, default, and cancel actions.
3. Selected application and profile remain stable through refresh.
4. No UI refresh occurs inside an active selector commit.
5. About, dropdown, lamp, sliders, direct input, previews, reset, cancel, and save work in the running Windows application.
6. Persisted values and transformation results remain unchanged except for explicitly edited fields.
7. Each item is manually accepted or receives a documented follow-up.

## Automated validation

The final CI evidence is recorded after the latest documentation head completes the Windows workflow. Focused source-level regression checks cover the repository link, About capacity, text contrast, custom dropdown editing control, and synchronized numeric input.

## Register

| ID | Implementation | Manual acceptance |
|---|---|---|
| `001` | Implemented and refined | Pending |
| `002` | Implemented | Pending |
| `003.1` | Implemented and refined | Pending |
| `003.2` | Implemented | Pending |
| `003.3` | Implemented | Pending |
| `004.1–004.6` | Implemented and refined | Pending |
| `005.1–005.5` | Implemented | Pending |
