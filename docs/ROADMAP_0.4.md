# SightAdapt 0.4 Alpha roadmap

## Purpose

SightAdapt 0.4 Alpha introduces configurable visual color profiles for individual Windows applications. The 0.4A sequence completes profile behavior, lifecycle reliability, architectural consistency, and interface quality. Later increments add window-frame manipulation, leave room for an additional reserved capability, and then continue with palette analysis and targeted color corrections.

## Engineering principles

Development follows:

- **KISS** — use the smallest design that satisfies the current increment;
- **DRY** — implement each product rule, default, and mutation once;
- **Clean Code** — use explicit names, focused responsibilities, deterministic behavior, and testable boundaries;
- **Single Point of Authority** — one component owns each state-changing operation;
- **Single Point of Truth** — one authoritative source defines persisted data, defaults, policy, runtime state, and product metadata;
- no speculative framework or abstraction without a concrete requirement;
- emergency shutdown and input transparency remain higher priorities than feature count.

## Authority and truth map

| Concern | Authority / source of truth |
|---|---|
| Runtime application state | `ApplicationStateController` |
| Overlay lifetime and active overlay identity | `OverlayController` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Application-assignment mutations | `ApplicationProfileManagementService` |
| Persisted automatic mode | `AutomaticModeManagementService` |
| Built-in IDs, fallback rules, transform policy, user IDs, and name rules | `VisualProfilePolicy` |
| Canonical Exact Invert and Soft Invert names and tuning values | `VisualProfileDefaults` |
| Persisted applications, profiles, assignments, and automatic-mode value | `SightAdaptSettings` |
| Settings recovery and migration | `SettingsStore.Normalize` |
| Product metadata and milestone | `ProductInfo` backed by assembly metadata |
| Captured 0.4A.4 interface requirements and acceptance state | `UI_REQUIREMENTS_0.4A.4.md` |

## 0.4A delivery increments

### 0.4A.1 — built-in profiles and Soft Invert editor

**Status: complete and manually validated.**

Delivered:

- built-in `Exact invert` and `Soft invert` profiles;
- configurable output black, output white, brightness, contrast, saturation, and hue shift;
- application-to-profile assignment;
- grayscale and hue-spectrum preview;
- schema migration and atomic persistence;
- active-overlay refresh without recreation;
- stable, unbound WinForms profile selector.

### 0.4A.2 — user-defined profiles per application

**Status: complete and manually validated.**

Delivered:

- create, duplicate, rename, edit, and delete user-defined profiles;
- independent tuning for different applications;
- protected built-in profiles;
- assignment counts and confirmed deletion;
- fallback reassignment to built-in `Soft invert`;
- required, unique, case-insensitive names;
- stable `user-<guid>` identifiers;
- dedicated visual-profile manager;
- persistence of multiple profiles and assignments.

### 0.4A.3 — lifecycle hardening and architectural completion

**Status: complete; the subsequent independent remediation is recorded in `ARCHITECTURE_REMEDIATION_0.4A.4.md`.**

Delivered baseline:

- deterministic malformed and nullable settings recovery;
- duplicate ID and name repair;
- unknown-transform recovery;
- canonical built-in restoration;
- missing-reference fallback;
- idempotent normalization;
- repeated lifecycle and persistence regression;
- emergency protection against automatic reactivation.

Architectural closure and remediation:

| Subincrement | Result |
|---|---|
| `0.4A.3.001` | Application-assignment mutation authority complete |
| `0.4A.3.002` | Visual-profile tuning authority complete |
| `0.4A.3.003` | Persisted automatic-mode authority and UI mutation cleanup complete |
| `0.4A.3.004` | Canonical visual-profile defaults complete |
| `0.4A.3.005` | Focused settings normalization stages complete |
| `0.4A.3.006` | Architectural enforcement and regression complete |
| `0.4A.3.007` | Historical architecture audit completed; later independent findings remediated on `agent/fix-audit-v0.4` |

Current refactored and interface-implementation validation:

```text
build: 0 warnings, 0 errors
tests: 82 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
```

The remediation boundaries and evidence are recorded in [`ARCHITECTURE_REMEDIATION_0.4A.4.md`](ARCHITECTURE_REMEDIATION_0.4A.4.md).

### 0.4A.4 — interface corrections

**Status: implementation complete; manual Windows, screenshot, keyboard, and DPI acceptance pending.**

The captured corrections are implemented without changing color-processing semantics or direct persistence ownership. The complete numbered register, implementation notes, CI evidence, and manual acceptance state are maintained in [`UI_REQUIREMENTS_0.4A.4.md`](UI_REQUIREMENTS_0.4A.4.md). That document is the single source of truth for this interface increment.

Implemented scope:

- modern About dialog using canonical icon and product metadata;
- left click on the notification-area icon opens the same menu instance as right click;
- modern dark visual-profile selector and dropdown;
- enabled/disabled application state rendered as a circular status lamp;
- user-facing product name and `Alpha 0.4A.4` milestone sourced from assembly metadata;
- redesigned visual-profile editor with profile identity, output-limit conversion sample, live preview, and modern sliders;
- keyboard-operable slider controls with accessible names and focus cues;
- working-copy editing and existing persistence authorities preserved.

Remaining acceptance scope:

- verify 100%, 125%, 150%, 175%, and 200% DPI;
- verify resizing, minimum sizes, and multi-monitor movement;
- verify keyboard-only workflows, tab order, default, and cancel actions;
- verify selector, status-lamp, About, and editor interaction states in the running Windows application;
- complete screenshot comparison and record any follow-up defects before accepting 0.4A.4.

Acceptance criteria:

1. Profile names remain readable after repeated changes.
2. Changing a profile never raises an unhandled WinForms exception.
3. Controls remain usable and unclipped at supported DPI scales.
4. Primary workflows are usable with keyboard only.
5. Destructive actions require clear confirmation.
6. Selected application and profile remain predictable after refresh.
7. Interface corrections do not change stored values or transformation semantics.
8. Every captured requirement is manually verified or receives a documented follow-up.

## 0.4B — window-frame manipulation

**Starts only after 0.4A.4 manual acceptance.**

Planned scope:

- manipulate the target window frame or surrounding chrome independently from color-profile processing;
- preserve predictable target-window position, size, focus, and input behavior;
- handle move, resize, minimize, maximize, restore, DPI changes, and multi-monitor transitions;
- define supported window types and explicit fallback behavior for unsupported, elevated, protected, or custom-framed windows;
- guarantee deterministic cleanup and emergency restoration;
- keep palette analysis outside this increment.

## 0.4C — reserved

**Intentionally unassigned.**

This letter remains available for an additional capability that may emerge during 0.4A.4 or 0.4B work. No implementation should use `0.4C` until its scope, authority, source of truth, and acceptance criteria are explicitly defined.

## 0.4D — palette analysis

**Starts after 0.4B and any deliberately introduced 0.4C increment are accepted.**

Planned scope:

- capture one representative frame on explicit user request;
- calculate RGB and luminance histograms;
- reduce the frame to approximately 16, 32, or 64 dominant colors;
- show source and transformed color previews;
- show percentage or frequency of each dominant color;
- keep analysis preview-only and avoid retaining captured images;
- handle protected, DRM-controlled, elevated, or unavailable windows clearly.

## 0.4E — targeted color corrections

After palette analysis is stable, SightAdapt may add ordered rules containing:

- source color or source range;
- tolerance;
- target color or target range;
- blend strength;
- enabled state;
- deterministic ordering when rules overlap.

This stage will probably require a LUT or GPU shader and must not be forced into the current 5×5 color matrix when that model cannot represent the requested correction.

## Current execution order

```text
0.4A.1          built-in profiles and editor                    complete
0.4A.2          user-defined profiles                           complete
0.4A.3 baseline lifecycle recovery and emergency hardening      complete
0.4A.3.001      application-assignment mutation authority       complete
0.4A.3.002      visual-profile tuning authority                 complete
0.4A.3.003      persisted-mode authority and UI cleanup         complete
0.4A.3.004      canonical visual-profile defaults               complete
0.4A.3.005      focused settings normalization                  complete
0.4A.3.006      architectural enforcement and regression        complete
0.4A.3.007      historical audit / later remediation             complete
0.4A.4          interface corrections                           implemented / manual acceptance pending
0.4B            window-frame manipulation                       after 0.4A.4 acceptance
0.4C            reserved                                        intentionally open
0.4D            palette analysis                                after 0.4B / optional 0.4C
0.4E            targeted color corrections                      after 0.4D
```

## Settings and migration requirements

0.4 must:

- load existing 0.2 and 0.3 settings without losing valid assignments;
- migrate legacy `effect: "invert"` to built-in Exact Invert;
- write settings atomically;
- recover safely from invalid or partially written configuration;
- keep settings in `%LOCALAPPDATA%\SightAdapt\settings.json` unless a documented migration changes that path.

## 0.4A definition of done

SightAdapt 0.4A is complete when:

1. Users can create and save multiple independent visual profiles.
2. Soft Invert parameters produce deterministic output.
3. Applications can be assigned to selected profiles.
4. Old and malformed settings recover without losing deterministically valid data.
5. Manual and automatic activation continue to work.
6. Emergency shutdown removes overlays and prevents automatic reactivation.
7. KISS, DRY, Clean Code, SPoA, and SPoT remain enforced by tests and the remediated architecture boundaries.
8. 0.4A.4 interface acceptance passes at supported DPI scales.
9. Automated calculation, lifecycle, recovery, state, selector, migration, UI-contract, and architecture tests pass.
10. Manual Windows regression confirms readable output and stable overlay behavior.

## Future renderer direction

Advanced range mapping, LUT support, and targeted correction should move toward:

- Windows Graphics Capture;
- Direct3D 11;
- HLSL shaders;
- GPU-side LUT processing;
- preview tooling that does not copy every frame to CPU memory.

These changes should preserve the 0.4 visual-profile model and application assignments wherever possible.
