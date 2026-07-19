# SightAdapt 0.4 Alpha roadmap

## Purpose

SightAdapt 0.4 Alpha introduces configurable visual color profiles for individual Windows applications. The 0.4A sequence completes profile behavior, lifecycle reliability, architectural consistency, and interface quality before palette analysis begins.

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
| Product metadata | `ProductInfo` |

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

**Status: complete and closed by the `0.4A.3.007` architecture audit.**

Delivered baseline:

- deterministic malformed and nullable settings recovery;
- duplicate ID and name repair;
- unknown-transform recovery;
- canonical built-in restoration;
- missing-reference fallback;
- idempotent normalization;
- repeated lifecycle and persistence regression;
- emergency protection against automatic reactivation.

Architectural closure:

| Subincrement | Result |
|---|---|
| `0.4A.3.001` | Application-assignment mutation authority complete |
| `0.4A.3.002` | Visual-profile tuning authority complete |
| `0.4A.3.003` | Persisted automatic-mode authority and UI mutation cleanup complete |
| `0.4A.3.004` | Canonical visual-profile defaults complete |
| `0.4A.3.005` | Focused settings normalization stages complete |
| `0.4A.3.006` | Architectural enforcement and regression complete |
| `0.4A.3.007` | Final KISS / DRY / Clean Code / SPoA / SPoT 10/10 audit complete |

Validated implementation baseline:

```text
build: 0 warnings, 0 errors
tests: 64 passed, 0 failed, 0 skipped
publish: self-contained Windows x64 succeeded
```

The final evidence and authority/truth maps are recorded in [`ARCHITECTURE_AUDIT_0.4A.3.007.md`](ARCHITECTURE_AUDIT_0.4A.3.007.md).

### 0.4A.4 — interface corrections

**Status: active next increment.**

This stage improves interface quality without changing color-processing semantics.

Scope:

- correct clipping, alignment, spacing, and inconsistent control sizing;
- stabilize tables, selectors, selected rows, and edit states;
- verify 100%, 125%, 150%, 175%, and 200% DPI;
- verify resizing, minimum sizes, and multi-monitor movement;
- correct button hierarchy and destructive-action placement;
- improve keyboard navigation, focus, default, and cancel actions;
- complete accessible names, descriptions, and text alternatives;
- standardize validation, confirmation, empty-state, and error messages;
- preserve the dark theme across all interaction states;
- confirm that UI refresh never occurs inside an active combo-box commit;
- complete manual visual regression before 0.4B.

Acceptance criteria:

1. Profile names remain readable after repeated changes.
2. Changing a profile never raises an unhandled WinForms exception.
3. Controls remain usable and unclipped at supported DPI scales.
4. Primary workflows are usable with keyboard only.
5. Destructive actions require clear confirmation.
6. Selected application and profile remain predictable after refresh.
7. Interface corrections do not change stored values or transformation semantics.

## 0.4B — palette analysis

**Starts only after 0.4A.4 acceptance.**

Planned scope:

- capture one representative frame on explicit user request;
- calculate RGB and luminance histograms;
- reduce the frame to approximately 16, 32, or 64 dominant colors;
- show source and transformed color previews;
- show percentage or frequency of each dominant color;
- keep analysis preview-only and avoid retaining captured images;
- handle protected, DRM-controlled, elevated, or unavailable windows clearly.

## 0.4C — targeted color corrections

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
0.4A.3.007      final 10/10 architecture audit                   complete
0.4A.4          interface corrections                           active
0.4B            palette analysis                                after 0.4A.4
0.4C            targeted color corrections                      later
```

## Settings and migration requirements

0.4 must:

- load existing 0.2 and 0.3 settings without losing valid assignments;
- migrate legacy `effect: "invert"` to built-in Exact Invert;
- write settings atomically;
- recover safely from invalid or partially written configuration;
- keep settings in `%LOCALAPPDATA%\SightAdapt\settings.json` unless a documented migration changes that path.

## Definition of done

SightAdapt 0.4 Alpha is complete when:

1. Users can create and save multiple independent visual profiles.
2. Soft Invert parameters produce deterministic output.
3. Applications can be assigned to selected profiles.
4. Old and malformed settings recover without losing deterministically valid data.
5. Manual and automatic activation continue to work.
6. Emergency shutdown removes overlays and prevents automatic reactivation.
7. KISS, DRY, Clean Code, SPoA, and SPoT remain enforced by the completed 0.4A.3 audit and tests.
8. 0.4A.4 interface acceptance passes at supported DPI scales.
9. Automated calculation, lifecycle, recovery, state, selector, migration, and architecture tests pass.
10. Manual Windows regression confirms readable output and stable overlay behavior.

## Future renderer direction

Advanced range mapping, LUT support, and targeted correction should move toward:

- Windows Graphics Capture;
- Direct3D 11;
- HLSL shaders;
- GPU-side LUT processing;
- preview tooling that does not copy every frame to CPU memory.

These changes should preserve the 0.4 visual-profile model and application assignments wherever possible.
