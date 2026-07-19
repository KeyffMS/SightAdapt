# SightAdapt 0.4 Alpha roadmap

## Purpose

SightAdapt 0.4 Alpha introduces configurable visual color profiles for individual Windows applications. The 0.4A sequence completes profile behavior, lifecycle reliability, architectural consistency, and interface quality before palette analysis begins.

## Engineering principles

Development follows:

- **KISS** — use the smallest design that satisfies the current increment;
- **DRY** — keep each product rule, default, and mutation in one implementation;
- **Clean Code** — use explicit names, focused responsibilities, deterministic behavior, and testable boundaries;
- **Single Point of Authority** — one component owns each state-changing operation;
- **Single Point of Truth** — one authoritative source defines profile policy, defaults, settings, runtime state, and product metadata;
- no dependency-injection framework or speculative abstraction without a concrete requirement;
- emergency shutdown and input transparency remain higher priorities than feature count.

## Authority and truth map

| Concern | Authority / source of truth |
|---|---|
| Runtime application state | `ApplicationStateController` |
| Overlay lifetime | `OverlayController` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Application assignment mutations | `ApplicationProfileManagementService` after `0.4A.3.001` |
| Persisted automatic-mode mutation | Dedicated authority after `0.4A.3.003` |
| Default IDs, fallback rules, names, transforms, and user IDs | `VisualProfilePolicy` |
| Canonical profile values | Central defaults introduced in `0.4A.3.004` |
| Persisted profile definitions and assignments | `SightAdaptSettings` |
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

**Functional baseline: complete and manually validated.**

Delivered baseline:

- central profile policy;
- malformed and nullable settings recovery;
- duplicate ID and name repair;
- unknown-transform recovery;
- canonical built-in restoration;
- missing-reference fallback;
- idempotent normalization;
- repeated lifecycle and persistence regression;
- emergency protection against automatic reactivation;
- 43 automated tests with zero warnings and zero errors.

The remaining architectural closure is divided into seven numbered subincrements.

#### 0.4A.3.001 — application-assignment mutation authority

**Status: next.**

- create `ApplicationProfileManagementService`;
- centralize add, remove, assign, enable, disable, and toggle operations;
- absorb or replace `ApplicationProfileToggleService`;
- remove direct assignment mutations from forms and context code;
- validate referenced profiles before assignment.

#### 0.4A.3.002 — visual-profile tuning authority

**Status: planned.**

- add `VisualProfileManagementService.UpdateTuning`;
- validate ownership, editability, and tuning values;
- stop the editor from directly mutating persisted profiles;
- preserve save and cancel behavior.

#### 0.4A.3.003 — persisted-mode authority and UI mutation cleanup

**Status: planned.**

- centralize persisted `AutomaticMode` changes;
- route tray, shortcuts, configuration UI, and emergency shutdown through one authority;
- audit direct writes to persisted domain state;
- leave forms as intent collection and presentation only.

#### 0.4A.3.004 — canonical visual-profile defaults

**Status: planned.**

- create one source for canonical Exact Invert and Soft Invert values;
- use it in factories, normalization, renderer fallbacks, editor reset, and tests;
- remove duplicated product-default literals;
- prevent factories and recovery logic from drifting apart.

#### 0.4A.3.005 — focused settings normalization stages

**Status: planned.**

Keep `SettingsStore.Normalize` as the public authority and divide its implementation into:

```text
CanonicalizeBuiltInProfiles
NormalizeCustomProfiles
NormalizeApplications
RepairProfileReferences
```

No generic rule engine, framework, or speculative pipeline is planned.

#### 0.4A.3.006 — architectural enforcement and regression

**Status: planned.**

- test every assignment, tuning, and automatic-mode mutation authority;
- test detached, missing, and protected entities;
- test persistence after every mutation category;
- add a lightweight source-level audit for prohibited direct writes;
- preserve warnings-as-errors and full Windows x64 publishing.

#### 0.4A.3.007 — final 10/10 architecture audit

**Status: planned.**

The closing report must provide objective evidence that:

- KISS has no speculative framework or unnecessary abstraction;
- DRY has one implementation for every rule, default, and mutation;
- Clean Code has focused methods, explicit names, and deterministic errors;
- SPoA has one owner for every mutation category;
- SPoT has one source for persisted data, defaults, policy, runtime state, and product metadata;
- CI and manual regression pass;
- no known 0.4A architectural violation remains.

### 0.4A.4 — interface corrections

**Status: planned after `0.4A.3.007`.**

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

1. profile names remain readable after repeated changes;
2. changing a profile never raises an unhandled WinForms exception;
3. controls remain usable and unclipped at supported DPI scales;
4. primary workflows are usable with keyboard only;
5. destructive actions require clear confirmation;
6. selected application and profile remain predictable after refresh;
7. interface corrections do not change stored values or transformation semantics.

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
0.4A.3.001      application-assignment mutation authority       next
0.4A.3.002      visual-profile tuning authority                 planned
0.4A.3.003      persisted-mode authority and UI cleanup         planned
0.4A.3.004      canonical visual-profile defaults               planned
0.4A.3.005      focused settings normalization                  planned
0.4A.3.006      architectural enforcement and regression        planned
0.4A.3.007      final 10/10 architecture audit                   planned
0.4A.4          interface corrections                           after .007
0.4B            palette analysis
0.4C            targeted color corrections
```

## Settings and migration requirements

0.4 must:

- load existing 0.2 and 0.3 settings without losing valid assignments;
- migrate legacy `effect: "invert"` to built-in Exact Invert;
- write settings atomically;
- recover safely from invalid or partially written configuration;
- keep settings in `%LOCALAPPDATA%\SightAdapt\settings.json` unless a documented migration changes that path.

## Testing requirements

Automated coverage includes or will include:

- color-matrix calculations and value clamping;
- profile creation, duplication, tuning, rename, deletion, and assignment;
- stable selector refreshes;
- malformed and nullable settings;
- duplicate and reserved identifiers;
- duplicate names and unknown transforms;
- missing profile references;
- multiple-profile persistence round trips;
- application-state transitions;
- emergency automatic-reactivation protection;
- local and automatic assignment resolution;
- persistent assignment toggling;
- architectural mutation-boundary enforcement.

Manual Windows testing includes:

- white and black source themes;
- text antialiasing, gradients, icons, and photographs;
- multiple DPI scales and monitors;
- move, resize, minimize, restore, close, and application switching;
- repeated profile lifecycle operations;
- long-running switching stability;
- keyboard-only operation and screen-reader labels;
- the complete 0.4A.4 interface checklist.

## Definition of done

SightAdapt 0.4 Alpha is complete when:

1. users can create and save multiple independent visual profiles;
2. Soft Invert parameters produce deterministic output;
3. applications can be assigned to selected profiles;
4. old and malformed settings recover without losing deterministically valid data;
5. manual and automatic activation continue to work;
6. emergency shutdown removes overlays and prevents automatic reactivation;
7. KISS, DRY, Clean Code, SPoA, and SPoT pass the `0.4A.3.007` audit;
8. 0.4A.4 interface acceptance passes at supported DPI scales;
9. automated calculation, lifecycle, recovery, state, selector, migration, and architecture tests pass;
10. manual Windows regression confirms readable output and stable overlay behavior.

## Future renderer direction

Advanced range mapping, LUT support, and targeted correction should move toward:

- Windows Graphics Capture;
- Direct3D 11;
- HLSL shaders;
- GPU-side LUT processing;
- preview tooling that does not copy every frame to CPU memory.

These changes should preserve the 0.4 visual-profile model and application assignments wherever possible.