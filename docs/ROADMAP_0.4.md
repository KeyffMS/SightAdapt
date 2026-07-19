# SightAdapt 0.4 Alpha roadmap

## Purpose

SightAdapt 0.4 Alpha introduces configurable visual color profiles for individual Windows applications. The 0.4A sequence completes profile behavior, lifecycle reliability, and interface quality before palette analysis begins.

## Engineering principles

Development follows:

- **KISS** — use the smallest design that satisfies the current increment;
- **DRY** — keep each rule and mutation in one implementation;
- **Clean Code** — use explicit names, focused responsibilities, deterministic behavior, and testable boundaries;
- **Single Point of Authority** — one component owns each state-changing operation;
- **Single Point of Truth** — one authoritative source defines profile policy, settings, application state, and product metadata;
- no dependency-injection framework or speculative abstraction without a concrete requirement;
- emergency shutdown and input transparency remain higher priorities than feature count.

## Authority and truth map

| Concern | Authority / source of truth |
|---|---|
| Runtime application state | `ApplicationStateController` |
| Overlay lifetime | `OverlayController` |
| Profile lifecycle mutations | `VisualProfileManagementService` |
| Default IDs, fallback rules, names, and transform policy | `VisualProfilePolicy` |
| Persisted profile definitions and assignments | `SightAdaptSettings` |
| Settings recovery and migration | `SettingsStore.Normalize` |
| Product metadata | `ProductInfo` |

## 0.4A delivery increments

### 0.4A.1 — built-in profiles and Soft Invert editor

**Status: implemented and manually validated.**

Delivered:

- built-in `Exact invert` and `Soft invert` profiles;
- configurable output black and output white;
- brightness, contrast, saturation, and hue shift;
- application-to-profile assignment;
- grayscale and hue-spectrum preview;
- schema migration and atomic persistence;
- immediate refresh of an active overlay;
- stable, unbound WinForms profile selector.

### 0.4A.2 — user-defined profiles per application

**Status: implemented and manually validated.**

Delivered:

- create profiles from Soft Invert defaults;
- duplicate editable profiles with independent tuning;
- rename and delete user-defined profiles;
- protect built-in profiles from rename and deletion;
- display application assignment counts;
- reassign affected applications to built-in `Soft invert` before deletion;
- validate required, unique, case-insensitive names;
- generate stable `user-<guid>` identifiers;
- persist multiple independent profiles and assignments;
- provide a dedicated visual-profile manager.

### 0.4A.3 — profile lifecycle hardening and regression

**Status: implemented and ready for manual regression acceptance.**

Delivered:

- central `VisualProfilePolicy` for new assignments, deletion fallback, missing-reference fallback, supported transforms, built-in IDs, user IDs, and name limits;
- one lifecycle mutation authority for create, duplicate, rename, delete, assignment counting, and reassignment;
- canonical recovery of built-in `Exact invert` and `Soft invert` definitions;
- safe handling of `null` values and `null` collection entries from manually edited JSON;
- preservation of recoverable custom profiles by generating replacement IDs instead of deleting them;
- deterministic repair of duplicate IDs and duplicate names;
- recovery of unknown transforms to `Soft invert`;
- recovery of missing executable metadata from the executable path;
- explicit fallback for deleted or missing profile references;
- idempotent normalization after recovery;
- multiple-profile persistence round trips;
- repeated create, duplicate, rename, delete, and reassignment regression tests;
- emergency-state protection against automatic reactivation;
- explicit manual activation as the only direct emergency override;
- 43 automated tests passing with zero warnings and zero errors.

Manual acceptance focus:

1. restart with existing 0.3, 0.4A.1, and 0.4A.2 settings;
2. create, duplicate, rename, edit, assign, and delete profiles repeatedly;
3. verify separate tuning for multiple applications;
4. verify local toggle with enabled and disabled assignments;
5. verify persistent assignment toggle preserves a valid custom profile;
6. verify automatic mode changes between configured and unconfigured applications;
7. verify emergency shutdown removes the overlay and blocks automatic reactivation;
8. verify a later explicit manual toggle can resume correction;
9. verify restart persistence after all lifecycle operations;
10. verify no WinForms exceptions during repeated profile selection and refresh.

### 0.4A.4 — interface corrections

**Status: next planned increment.**

This stage improves interface quality without changing color-processing semantics.

Scope:

- correct clipping, alignment, spacing, and inconsistent control sizing;
- stabilize table columns, profile selectors, selected rows, and edit states;
- ensure readable labels and values at 100%, 125%, 150%, 175%, and 200% DPI;
- verify resizing, minimum window sizes, and multi-monitor movement;
- correct button hierarchy, enabled/disabled states, and destructive-action placement;
- improve keyboard navigation, tab order, focus indicators, and default/cancel actions;
- add or correct accessible names, descriptions, and text alternatives;
- make validation, confirmation, empty-state, and error messages consistent;
- preserve the dark theme across normal, hover, selected, disabled, and error states;
- verify that no interface refresh occurs inside an active combo-box commit;
- complete a manual visual-regression checklist before starting 0.4B.

Acceptance criteria:

1. profile names remain readable before and after repeated changes;
2. changing a profile never raises an unhandled WinForms exception;
3. controls remain usable and unclipped at supported DPI scales;
4. all primary workflows are usable with keyboard only;
5. destructive actions require clear confirmation;
6. selected application and profile state remain predictable after refresh;
7. interface corrections do not change transformation semantics or stored values.

## 0.4B — palette analysis

0.4B starts only after 0.4A.4 passes interface acceptance.

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

This stage will probably require a LUT or GPU shader and must not be forced into the current 5×5 color matrix when the matrix cannot represent the requested correction.

## Planned order

```text
0.4A.1  Built-in profiles and Soft Invert editor       complete
0.4A.2  User-defined profiles per application          complete
0.4A.3  Lifecycle hardening and regression              complete / manual acceptance
0.4A.4  Interface corrections                           next
0.4B    Palette analysis
0.4C    Targeted color corrections
```

## Settings and migration requirements

0.4 must:

- load existing 0.2 and 0.3 settings without losing valid application assignments;
- migrate legacy `effect: "invert"` to built-in exact inversion;
- write settings atomically;
- recover safely from invalid or partially written configuration;
- keep settings in `%LOCALAPPDATA%\SightAdapt\settings.json` unless a documented migration changes that path.

## Testing requirements

Automated tests cover:

- soft-invert matrix calculations;
- clamping and validation of profile values;
- profile creation, duplication, rename, deletion, and assignment;
- stable profile-selector refreshes;
- malformed and nullable settings values;
- duplicate and reserved identifiers;
- duplicate names and unknown transforms;
- missing profile references;
- multiple-profile persistence round trips;
- application-state transitions;
- emergency automatic-reactivation protection;
- manual and automatic assignment resolution;
- persistent assignment toggling.

Manual Windows testing covers:

- white and black source themes;
- text antialiasing, gradients, icons, and photographs;
- multiple DPI scales and monitors;
- move, resize, minimize, restore, close, and application switching;
- repeated profile lifecycle operations;
- long-running switching stability;
- keyboard-only operation and screen-reader labels;
- the full 0.4A.4 interface checklist.

## Definition of done

SightAdapt 0.4 Alpha is complete when:

1. users can create and save multiple independent visual profiles;
2. Soft Invert parameters produce deterministic output;
3. applications can be assigned to selected profiles;
4. old and malformed settings recover without losing valid data where deterministically possible;
5. manual and automatic activation continue to work;
6. emergency shutdown removes overlays and prevents automatic reactivation;
7. transformation logic remains independent from tray and configuration UI code;
8. automated calculation, lifecycle, recovery, state, selector, and migration tests pass;
9. manual Windows regression confirms readable output and stable overlay behavior;
10. 0.4A.4 interface acceptance passes at supported DPI scales.

## Future renderer direction

Advanced range mapping, LUT support, and targeted correction should move toward:

- Windows Graphics Capture;
- Direct3D 11;
- HLSL shaders;
- GPU-side LUT processing;
- preview tooling that does not copy every frame to CPU memory.

These changes should preserve the 0.4 visual-profile model and application assignments wherever possible.
