# SightAdapt 0.4 Alpha roadmap

## Purpose

SightAdapt 0.4 Alpha introduces configurable visual profiles for individual Windows applications, completes the reliability and interface baseline, then proceeds to window-frame manipulation, reserved capacity, palette analysis, and targeted color correction.

## Engineering principles

Development follows:

- **KISS** — use the smallest design that satisfies the current increment;
- **DRY** — implement each product rule, default, and mutation once;
- **Clean Code** — use explicit names, focused responsibilities, deterministic behavior, and testable boundaries;
- **Single Point of Authority** — one component owns each state-changing operation;
- **Single Point of Truth** — one authoritative source defines persisted data, defaults, policy, runtime state, product metadata, and accepted requirements;
- no speculative framework or abstraction without a concrete requirement;
- emergency shutdown and input transparency remain higher priorities than feature count.

## Authority and truth map

| Concern | Authority / source of truth |
|---|---|
| Committed settings transaction | `SettingsCoordinator` |
| Runtime application mode, target, active profile, suppression, and message | `ApplicationStateController.Current` |
| Overlay resource lifetime and target handle | `OverlayController` |
| Foreground target discovery | `ForegroundWindowTracker` |
| Notification-area presentation | `TrayPresenter` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Application-assignment mutations | `ApplicationProfileManagementService` |
| Persisted automatic mode | `AutomaticModeManagementService` |
| Built-in IDs, fallback rules, user IDs, and name rules | `VisualProfilePolicy` |
| Canonical Exact Invert and Soft Invert values | `VisualProfileDefaults` |
| Supported transforms and tuning capability | `VisualTransformCatalog` |
| Profile parameter limits | `VisualProfileLimits` |
| Persisted applications, profiles, assignments, and automatic mode | `SightAdaptSettings` |
| Settings recovery and migration | `SettingsStore.Normalize` |
| Product metadata and milestone | project/assembly metadata exposed by `ProductInfo` |
| Completed 0.4A.4 requirements and acceptance | `UI_REQUIREMENTS_0.4A.4.md` |

## 0.4A delivery increments

### 0.4A.1 — built-in profiles and Soft Invert editor

**Status: complete and manually validated.**

Delivered:

- built-in `Exact invert` and `Soft invert` profiles;
- configurable output limits, brightness, contrast, saturation, and hue shift;
- application-to-profile assignment;
- grayscale and hue-spectrum preview;
- schema migration and atomic persistence;
- active-overlay refresh without recreation;
- stable unbound WinForms profile selector.

### 0.4A.2 — user-defined profiles per application

**Status: complete and manually validated.**

Delivered:

- create, duplicate, rename, edit, and delete user-defined profiles;
- independent tuning for different applications;
- protected built-in profiles;
- assignment counts and confirmed deletion;
- fallback reassignment to built-in `Soft invert`;
- required unique case-insensitive names;
- stable `user-<guid>` identifiers;
- dedicated visual-profile manager;
- persistence of multiple profiles and assignments.

### 0.4A.3 — lifecycle hardening and architectural completion

**Status: complete. Independent remediation is recorded in `ARCHITECTURE_REMEDIATION_0.4A.4.md`.**

Delivered:

- deterministic malformed and nullable settings recovery;
- duplicate ID and name repair;
- unknown-transform recovery;
- canonical built-in restoration;
- missing-reference fallback;
- idempotent normalization;
- repeated lifecycle and persistence regression;
- emergency protection against automatic reactivation;
- completed mutation authorities, canonical defaults, focused normalization stages, and architecture enforcement.

### 0.4A.4 — interface corrections and closing audit

**Status: complete and manually accepted.**

Delivered and accepted:

- modern About dialog with canonical metadata and GitHub link;
- left and right tray clicks opening one shared menu;
- custom modern-dark profile selector in resting, active, and dropdown states;
- circular enabled/disabled application lamp;
- canonical `SightAdapt · Alpha 0.4A.4` presentation without user-facing `Demo` wording;
- brighter shared secondary and muted text;
- redesigned profile editor with profile identity, output conversion sample, live preview, and direct numeric entry;
- full-width slider rails, neutral markers, centered neutral mapping, and gentle mouse-only snapping;
- keyboard and accessibility behavior;
- working-copy editing and existing persistence authorities preserved.

The closing assessment is recorded in [`ARCHITECTURE_AUDIT_0.4A.4_FINAL.md`](ARCHITECTURE_AUDIT_0.4A.4_FINAL.md).

Closing assessment:

| Principle | Score |
|---|---:|
| KISS | 9/10 |
| DRY | 9/10 |
| Clean Code | 9/10 |
| SPoA | 9/10 |
| SPoT | 9/10 |

No known blocking violation remains in the current `0.4A` scope. The audit records minor non-blocking technical debt rather than claiming permanent perfection.

Validation:

```text
code audit head: 74f6609581810584b853f80e12eb9a6ad1cd05da
workflow run: 29740090542
build: 0 warnings, 0 errors
tests: 91 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
artifact SHA-256: 3d684f7d1f1175a811e14d1ceba54f64189a69f43a095581fffc764a3a8f47a7
```

## 0.4B — window-frame manipulation

**Status: active next increment for requirements and architecture definition.**

Planned scope:

- manipulate the target window frame or surrounding chrome independently from color-profile processing;
- preserve predictable target-window position, size, focus, and input behavior;
- handle move, resize, minimize, maximize, restore, DPI changes, and multi-monitor transitions;
- define supported window types and explicit fallback behavior for unsupported, elevated, protected, or custom-framed windows;
- guarantee deterministic cleanup and emergency restoration;
- keep palette analysis outside this increment.

Before implementation, `0.4B` must define:

1. the exact user-visible behavior;
2. the runtime authority;
3. the source of truth;
4. whether any value is persisted;
5. cleanup and emergency restoration order;
6. unsupported-window and privilege behavior;
7. a manual Windows acceptance matrix.

## 0.4C — reserved

**Intentionally unassigned.**

This letter remains available for an additional capability discovered during later work. No implementation may use `0.4C` until its scope, authority, source of truth, and acceptance criteria are explicitly defined.

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
0.4A.4          interface corrections and closing audit          complete / accepted
0.4B            window-frame manipulation                       active requirements phase
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

SightAdapt 0.4A is complete because:

1. users can create and save multiple independent visual profiles;
2. Soft Invert parameters produce deterministic output;
3. applications can be assigned to selected profiles;
4. old and malformed settings recover without losing deterministically valid data;
5. manual and automatic activation continue to work;
6. emergency shutdown removes overlays and prevents automatic reactivation;
7. architecture authority and truth boundaries are documented and regression-tested;
8. all captured interface requirements are manually accepted;
9. calculation, lifecycle, recovery, state, selector, migration, UI-contract, transaction, and architecture tests pass;
10. manual screenshot review confirms readable and stable interface behavior.

## Future renderer direction

Advanced range mapping, LUT support, and targeted correction should move toward:

- Windows Graphics Capture;
- Direct3D 11;
- HLSL shaders;
- GPU-side LUT processing;
- preview tooling that does not copy every frame to CPU memory.

These changes should preserve the 0.4 visual-profile model and application assignments wherever possible.
