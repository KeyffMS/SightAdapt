# SightAdapt 0.4A.4 — interface correction requirements

## Status

**Requirement intake is active. Implementation planning is intentionally deferred until the user confirms that the screenshot list is complete.**

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

No implementation plan is approved by this document. Requirements may be appended or refined until intake is explicitly closed.

## Numbered requirements

### 001 — About window

Add an `About SightAdapt...` entry to the notification-area menu.

The command must open a dedicated modern-dark dialog containing a clean, balanced presentation of:

1. the canonical SightAdapt graphic, icon, or logo;
2. the product name;
3. the current product version or milestone label;
4. the author or company attribution.

The dialog must obtain product metadata from the canonical assembly/product metadata source instead of duplicating text in the form. It must support keyboard closing, predictable focus, DPI scaling, and accessible names.

### 002 — notification-area left click

A left click on the SightAdapt notification-area icon must open the same menu that is currently opened by a right click.

Acceptance details:

1. left and right click use the same menu instance and command definitions;
2. no separate or duplicated menu-building path is introduced;
3. the behavior remains the same while inactive, manually active, automatically active, faulted, or emergency-stopped;
4. the action remains correct after application restart.

### 003 — configuration window

#### 003.1 — modern visual-profile selector

Replace the visually inconsistent profile selector presentation with a modern-dark treatment covering:

1. the closed selector border and background;
2. the dropdown button and indicator;
3. the opened list background and border;
4. selected, hovered, focused, disabled, and read-only states;
5. readable profile names without clipping or stale selection rendering.

The existing stable selector behavior and profile-assignment semantics must not change.

#### 003.2 — application activity lamp

Replace the current checkbox-style activity indicator in the configured-applications table with a compact circular status lamp:

1. **enabled** — a clear green filled circle;
2. **disabled** — a dark gray circle with a slightly lighter outline;
3. both states must fit the modern dark theme and remain distinguishable at supported DPI scales;
4. the visual must expose an accessible enabled/disabled state and must not become a second authority for assignment state.

#### 003.3 — canonical product name and current version

Remove user-facing `Demo` wording such as `SightAdapt.Demo` from window titles and product presentation.

Display the current SightAdapt alpha milestone using canonical product metadata. The milestone text must be updated in one authoritative location when the release stage changes; forms and tray presentation must not contain independent hard-coded copies.

Captured examples of the intended progression:

- current historical label at requirement capture: `Alpha 0.4A.3.007`;
- later milestone example: `Alpha 0.4B`.

The actual displayed value at implementation time must reflect the current canonical milestone.

### 004 — visual-profile editor

#### 004.1 — edited profile identity

Show the name of the profile being edited prominently at the top of the editor. The identity must come from the supplied working profile and must not be duplicated or reconstructed from unrelated UI state.

#### 004.2 — modern slider controls

Replace the current spinner-style numeric inputs and up/down arrows with modern-dark sliders for profile adjustment.

Each slider must:

1. show its current numeric value and unit;
2. preserve the existing domain limits and stored-value precision;
3. support keyboard adjustment and accessible naming;
4. use canonical limits and descriptions rather than form-local copies.

#### 004.3 — editor size and layout capacity

The editor may be made moderately larger and must define a usable minimum size so the new layout remains unclipped at supported DPI scales. Resizing must remain predictable.

#### 004.4 — output black/white controls and conversion sample

Place `Output black` and `Output white` in the first control row.

Beside them, show a compact visual conversion sample that clearly demonstrates how a black-on-white source sample is transformed by the current output limits. The sample must update from the same working profile as the rest of the live preview and must not implement an independent transformation formula.

#### 004.5 — required editor section order

Arrange the editor in this order:

1. profile name and editor context;
2. `Output black`, `Output white`, and the black/white conversion sample;
3. the existing live grayscale and color comparison preview;
4. four adjustment sliders: `Brightness`, `Contrast`, `Saturation`, and `Hue shift`;
5. reset, cancel, and save actions with a clear primary-action hierarchy.

#### 004.6 — editing and persistence invariants

The redesigned editor must continue to edit a working copy and return a result through the existing profile-management authority. It must not mutate persisted settings directly, rewrite untouched fields through rounded UI values, or change transformation semantics.

## Cross-cutting acceptance requirements

1. Controls remain readable and unclipped at 100%, 125%, 150%, 175%, and 200% DPI.
2. Primary actions are usable with keyboard only, with predictable tab order, focus, default, and cancel behavior.
3. Selected application and profile state remain stable during refresh.
4. No UI refresh occurs inside an active combo-box commit.
5. Dark-theme colors and interaction states come from shared theme sources rather than repeated literals where practical.
6. Product metadata, profile limits, runtime state, and mutation behavior remain owned by their existing canonical authorities.
7. Automated architecture and behavioral tests remain green; new behavior receives focused regression coverage where practical.
8. Each screenshot-reported item is marked implemented and verified, deferred with a reason, or explicitly out of scope.
9. The implementation plan is written only after the user explicitly closes requirement intake.

## Intake register

| ID | Area | Status |
|---|---|---|
| `001` | About window | Captured |
| `002` | Notification-area left click | Captured |
| `003.1` | Visual-profile selector styling | Captured |
| `003.2` | Application activity lamp | Captured |
| `003.3` | Product name and milestone presentation | Captured |
| `004.1` | Edited profile identity | Captured |
| `004.2` | Modern slider controls | Captured |
| `004.3` | Editor size and layout capacity | Captured |
| `004.4` | Output-limit conversion sample | Captured |
| `004.5` | Editor section order | Captured |
| `004.6` | Editing and persistence invariants | Captured |
