# SightAdapt 0.4A.4 — interface correction requirements

## Status

**Requirement intake is closed. The captured corrections are implemented on `agent/fix-audit-v0.4`; manual Windows, screenshot, and DPI acceptance remains pending.**

Implementation branch: `agent/fix-audit-v0.4`.

This document is the single source of truth for screenshot-driven interface corrections in the active `0.4A.4` interface phase. The completed architecture-remediation baseline remains documented separately in [`ARCHITECTURE_REMEDIATION_0.4A.4.md`](ARCHITECTURE_REMEDIATION_0.4A.4.md).

## Engineering constraints

Every correction must preserve:

- **Clean Code** — explicit names, focused responsibilities, readable control composition, and testable behavior;
- **KISS** — the smallest WinForms solution that satisfies the captured requirement;
- **Single Point of Authority** — one component owns each UI action and state-changing operation;
- **Single Point of Truth** — product name, version, author, state, profile data, and visual defaults come from their canonical sources;
- **DRY** — no duplicated menu construction, version strings, state rules, theme values, or profile-editing logic;
- existing color-processing semantics and persisted profile values;
- deterministic emergency behavior and input transparency;
- keyboard usability, accessibility metadata, supported DPI scaling, and the modern dark theme.

Implementation does not close `0.4A.4` by itself. The increment closes only after manual visual and interaction acceptance.

## Numbered requirements

### 001 — About window

Add an `About SightAdapt...` entry to the notification-area menu.

The command must open a dedicated modern-dark dialog containing a clean, balanced presentation of:

1. the canonical SightAdapt graphic, icon, or logo;
2. the product name;
3. the current product version or milestone label;
4. the author or company attribution.

The dialog must obtain product metadata from the canonical assembly/product metadata source instead of duplicating text in the form. It must support keyboard closing, predictable focus, DPI scaling, and accessible names.

**Implementation:** `AboutForm` uses the canonical tray icon and `ProductInfo` metadata. The tray exposes one `About SightAdapt...` command.

### 002 — notification-area left click

A left click on the SightAdapt notification-area icon must open the same menu that is currently opened by a right click.

Acceptance details:

1. left and right click use the same menu instance and command definitions;
2. no separate or duplicated menu-building path is introduced;
3. the behavior remains the same while inactive, manually active, automatically active, faulted, or emergency-stopped;
4. the action remains correct after application restart.

**Implementation:** `TrayPresenter` owns one `ContextMenuStrip`; the left-click handler shows that same instance.

### 003 — configuration window

#### 003.1 — modern visual-profile selector

Replace the visually inconsistent profile selector presentation with a modern-dark treatment covering:

1. the closed selector border and background;
2. the dropdown button and indicator;
3. the opened list background and border;
4. selected, hovered, focused, disabled, and read-only states;
5. readable profile names without clipping or stale selection rendering.

The existing stable selector behavior and profile-assignment semantics must not change.

**Implementation:** `StableVisualProfileComboBoxColumn` owns the modern closed-cell painting and dark owner-drawn dropdown while retaining the explicit `SetProfiles` API.

#### 003.2 — application activity lamp

Replace the current checkbox-style activity indicator in the configured-applications table with a compact circular status lamp:

1. **enabled** — a clear green filled circle;
2. **disabled** — a dark gray circle with a slightly lighter outline;
3. both states must fit the modern dark theme and remain distinguishable at supported DPI scales;
4. the visual must expose an accessible enabled/disabled state and must not become a second authority for assignment state.

**Implementation:** the existing checkbox value remains the interaction and accessibility authority; the grid paints it as the required status lamp.

#### 003.3 — canonical product name and current version

Remove user-facing `Demo` wording such as `SightAdapt.Demo` from window titles and product presentation.

Display the current SightAdapt alpha milestone using canonical product metadata. The milestone text must be updated in one authoritative location when the release stage changes; forms and tray presentation must not contain independent hard-coded copies.

Captured examples of the intended progression:

- historical label at requirement capture: `Alpha 0.4A.3.007`;
- later milestone example: `Alpha 0.4B`.

The actual displayed value at implementation time must reflect the current canonical milestone.

**Implementation:** the project metadata contains the current `Alpha 0.4A.4` milestone; `ProductInfo` composes all user-facing product labels from assembly metadata.

### 004 — visual-profile editor

#### 004.1 — edited profile identity

Show the name of the profile being edited prominently at the top of the editor. The identity must come from the supplied working profile and must not be duplicated or reconstructed from unrelated UI state.

**Implementation:** the editor header displays `_workingProfile.Name`.

#### 004.2 — modern slider controls

Replace the current spinner-style numeric inputs and up/down arrows with modern-dark sliders for profile adjustment.

Each slider must:

1. show its current numeric value and unit;
2. preserve the existing domain limits and stored-value precision;
3. support keyboard adjustment and accessible naming;
4. use canonical limits and descriptions rather than form-local copies.

**Implementation:** `ModernProfileSlider` supports mouse, arrow, Page Up/Down, Home, End, focus cues, value labels, units, and canonical `VisualProfileLimits`.

#### 004.3 — editor size and layout capacity

The editor may be made moderately larger and must define a usable minimum size so the new layout remains unclipped at supported DPI scales. Resizing must remain predictable.

**Implementation:** the editor uses a larger DPI-scaled table layout with an explicit minimum size.

#### 004.4 — output black/white controls and conversion sample

Place `Output black` and `Output white` in the first control row.

Beside them, show a compact visual conversion sample that clearly demonstrates how a black-on-white source sample is transformed by the current output limits. The sample must update from the same working profile as the rest of the live preview and must not implement an independent transformation formula.

**Implementation:** `OutputLimitPreview` uses the same canonical transform catalog and working profile as `ColorProfilePreview`.

#### 004.5 — required editor section order

Arrange the editor in this order:

1. profile name and editor context;
2. `Output black`, `Output white`, and the black/white conversion sample;
3. the existing live grayscale and color comparison preview;
4. four adjustment sliders: `Brightness`, `Contrast`, `Saturation`, and `Hue shift`;
5. reset, cancel, and save actions with a clear primary-action hierarchy.

**Implementation:** the editor root layout follows this exact order.

#### 004.6 — editing and persistence invariants

The redesigned editor must continue to edit a working copy and return a result through the existing profile-management authority. It must not mutate persisted settings directly, rewrite untouched fields through rounded UI values, or change transformation semantics.

**Implementation:** each slider updates only its corresponding field on `_workingProfile`; the form returns a working copy and persistence remains owned by `VisualProfileManagementService.UpdateTuning` through `SettingsCoordinator`.

## Cross-cutting acceptance requirements

1. Controls remain readable and unclipped at 100%, 125%, 150%, 175%, and 200% DPI.
2. Primary actions are usable with keyboard only, with predictable tab order, focus, default, and cancel behavior.
3. Selected application and profile state remain stable during refresh.
4. No UI refresh occurs inside an active combo-box commit.
5. Dark-theme colors and interaction states come from shared theme sources rather than repeated literals where practical.
6. Product metadata, profile limits, runtime state, and mutation behavior remain owned by their existing canonical authorities.
7. Automated architecture and behavioral tests remain green; new behavior receives focused regression coverage where practical.
8. Each screenshot-reported item is marked implemented and verified, deferred with a reason, or explicitly out of scope.
9. Manual Windows validation is required before `0.4A.4` acceptance.

## Automated validation

```text
head: b937f46b5b1f6304276be3e09a93bb38602a9d43
build: 0 warnings, 0 errors
tests: 82 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
workflow run: 29724659171
artifact: SightAdapt-0.4-Alpha-win-x64
artifact SHA-256: f128dfbe4d6f1282e5f174657db40db0b04884c58f2dd136f742cf2d7567f022
```

## Intake register

| ID | Area | Implementation status | Acceptance status |
|---|---|---|---|
| `001` | About window | Implemented | Manual verification pending |
| `002` | Notification-area left click | Implemented | Manual verification pending |
| `003.1` | Visual-profile selector styling | Implemented | Manual verification pending |
| `003.2` | Application activity lamp | Implemented | Manual verification pending |
| `003.3` | Product name and milestone presentation | Implemented | Manual verification pending |
| `004.1` | Edited profile identity | Implemented | Manual verification pending |
| `004.2` | Modern slider controls | Implemented | Manual verification pending |
| `004.3` | Editor size and layout capacity | Implemented | Manual verification pending |
| `004.4` | Output-limit conversion sample | Implemented | Manual verification pending |
| `004.5` | Editor section order | Implemented | Manual verification pending |
| `004.6` | Editing and persistence invariants | Implemented | Automated validation passed; manual regression pending |
