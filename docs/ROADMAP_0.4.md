# SightAdapt 0.4 Alpha roadmap

## Current position

The accepted feature stack currently ends at **0.4B.2 — Faster overlay switching**.

Completed and locally accepted:

- configurable built-in and user-defined visual profiles;
- transactional settings and migration hardening;
- accepted 0.4A interface baseline;
- corrected application-grid transaction boundary;
- per-application overlay scope;
- faster foreground switching with overlay reuse and reduced white flash.

The current work is a **release-integration checkpoint**: consolidate documentation, run fresh validation on the final branch, review the complete difference from `main`, and prepare one explicit pull request to `main`. No new product capability should be added during this checkpoint.

## Engineering principles

Development follows:

- **KISS** — implement the smallest design that satisfies the accepted behavior;
- **DRY** — define each rule, default, mutation, and geometry calculation once;
- **Clean Code** — use explicit names, focused responsibilities, deterministic failure behavior, and testable boundaries;
- **Single Point of Authority** — one component owns each state-changing operation;
- **Single Point of Truth** — one authoritative source defines each persisted value, runtime state, policy, and product requirement;
- avoid speculative frameworks and abstractions;
- preserve emergency shutdown and input transparency ahead of feature growth.

## Current authority and truth map

| Concern | Authority / source of truth |
|---|---|
| Committed settings transaction | `SettingsCoordinator` |
| Settings migration, normalization, and recovery | `SettingsStore.Normalize` |
| Application-assignment mutations, including overlay scope | `ApplicationProfileManagementService` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Persisted automatic-mode mutation | `AutomaticModeManagementService` |
| Runtime mode, active target, active profile, suppression, and message | `ApplicationStateController.Current` |
| Foreground detection and duplicate suppression | `ForegroundWindowTracker` |
| Derived application-identity cache | `ApplicationIdentityCache` through `ApplicationDiscovery` |
| Overlay geometry | `OverlayBoundsResolver` |
| Overlay lifetime and retargeting | `OverlayController` |
| Native overlay target, rendering, and transition grace | `MagnifierOverlay` |
| Application-table presentation and edit mechanics | `ApplicationProfilesGrid` |
| Configuration use-case orchestration and dialogs | `ConfigurationForm` |
| Notification-area presentation | `TrayPresenter` |
| Built-in profile IDs and fallback/name rules | `VisualProfilePolicy` |
| Canonical profile defaults | `VisualProfileDefaults` |
| Supported transforms and tuning capability | `VisualTransformCatalog` |
| Parameter limits | `VisualProfileLimits` |
| Persisted applications, profiles, automatic mode, and overlay scopes | `SightAdaptSettings` |
| Product name, version, milestone, repository, and license | project/assembly metadata exposed through `ProductInfo` |
| Current architecture documentation | `ARCHITECTURE_0.4.md` |

## Completed 0.4A increments

### 0.4A.1 — built-in profiles and Soft Invert editor

**Status: complete and manually validated.**

Delivered:

- built-in `Exact invert` and `Soft invert` profiles;
- output limits, brightness, contrast, saturation, and hue adjustment;
- application-to-profile assignment;
- grayscale and hue-spectrum preview;
- schema migration and atomic persistence;
- active-overlay profile refresh;
- stable custom profile selector.

### 0.4A.2 — user-defined profiles

**Status: complete and manually validated.**

Delivered:

- create, duplicate, rename, edit, and delete operations;
- independent tuning for different applications;
- protected built-in profiles;
- assignment counts and confirmed deletion;
- fallback reassignment to built-in Soft Invert;
- stable identifiers and unique case-insensitive names;
- persistence of custom profiles and assignments.

### 0.4A.3 — lifecycle and architecture hardening

**Status: complete.**

Delivered:

- deterministic malformed-settings recovery;
- canonical built-in restoration and reference repair;
- transaction rollback after domain or persistence failure;
- separate runtime fault and emergency states;
- emergency protection against automatic reactivation;
- named mutation authorities and architecture guardrails;
- focused foreground and tray components.

### 0.4A.4 — interface correction and closing audit

**Status: complete and manually accepted.**

Delivered:

- canonical product metadata and About dialog;
- shared left/right tray menu;
- modern dark selectors, status indicator, profile editor, and slider controls;
- keyboard and accessibility behavior;
- DPI-aware layout and visual acceptance;
- corrected issue #9 grid transaction boundary;
- extracted `ApplicationProfilesGrid` ownership and stable row keys.

## Completed 0.4B increments

### 0.4B.1 — per-application overlay scope

**Status: complete and locally accepted; issue #8 closed.**

Delivered four persisted choices per application:

1. client area without title bar and frame — default;
2. full visible window;
3. complete monitor containing the application;
4. complete Windows virtual desktop.

The settings schema advanced to `4`. Missing and invalid scope values recover to `client-area` without discarding valid assignments.

### 0.4B.2 — faster overlay switching

**Status: complete and locally accepted; issue #12 closed.**

Delivered:

- foreground polling reduced from 250 ms to 75 ms;
- duplicate foreground-handle suppression;
- bounded 64-entry least-recently-used application-identity cache;
- retargeting of one existing overlay instead of normal recreation;
- a 125 ms foreground-transition grace period;
- deterministic stale-target hide/close behavior;
- unchanged explicit disable and emergency behavior.

## Main-integration checkpoint

Before opening the pull request to `main`:

1. current documentation must agree on version, schema, implemented features, and active roadmap status;
2. temporary or superseded requirement documents must be removed or explicitly classified as historical;
3. current architecture boundaries must be documented independently from historical audits;
4. the final branch must pass build, tests, architecture checks, and Windows x64 publish;
5. the complete `main...branch` difference must be reviewed for generated files, temporary automation, and branch-specific artifacts;
6. the final executable must receive a local smoke test using its commit-derived `ProductVersion`;
7. the PR to `main` must state the exact expected head SHA and must not be merged without explicit approval.

## 0.4C — reserved

**Status: intentionally unassigned.**

This increment remains available for a concrete capability discovered during later work. It must not be used until its user-visible behavior, authority, source of truth, persistence, failure contract, and acceptance criteria are defined.

## 0.4D — palette analysis

**Status: planned after the main-integration checkpoint.**

Planned scope:

- capture one representative frame on explicit user request;
- calculate RGB and luminance histograms;
- reduce the frame to approximately 16, 32, or 64 dominant colors;
- show source and transformed color previews;
- show frequency or percentage of each dominant color;
- keep analysis preview-only and avoid retaining captured images;
- report protected, elevated, or unavailable targets clearly.

This work is expected to require Windows Graphics Capture and GPU-oriented processing rather than extending the current magnifier matrix path indefinitely.

## 0.4E — targeted color corrections

**Status: planned after stable palette analysis.**

Potential rule model:

- source color or source range;
- tolerance;
- target color or output range;
- blend strength;
- enabled state;
- deterministic ordering when rules overlap.

A LUT or GPU shader should be used when the requested mapping cannot be represented by the current 5×5 color matrix.

## Current execution order

```text
0.4A.1   built-in profiles and editor                 complete / accepted
0.4A.2   user-defined profiles                        complete / accepted
0.4A.3   lifecycle and architecture hardening         complete
0.4A.4   interface correction and grid closure        complete / accepted
0.4B.1   per-application overlay scope                complete / accepted
0.4B.2   faster overlay switching                     complete / accepted
integration documentation, final CI, main PR          current
0.4C     reserved                                     intentionally open
0.4D     palette analysis                             planned
0.4E     targeted color corrections                   planned after 0.4D
```

## Settings and migration baseline

The current 0.4 stack must:

- load older valid assignments without discarding them;
- migrate legacy `effect: "invert"` values to built-in Exact Invert;
- normalize settings to schema `4`;
- default missing or invalid overlay scopes to `client-area`;
- write settings atomically;
- publish committed settings only after successful persistence;
- keep settings in `%LOCALAPPDATA%\SightAdapt\settings.json` unless a documented migration changes the path.

## Future renderer direction

Palette analysis, LUT support, and targeted correction should move toward:

- Windows Graphics Capture;
- Direct3D 11;
- HLSL shaders;
- GPU-side LUT processing;
- preview tooling that does not copy every frame to CPU memory.

These changes should preserve the accepted 0.4 application assignment, visual-profile, safety, and overlay-scope models wherever possible.