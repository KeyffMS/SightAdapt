# SightAdapt tray icon

This document defines the notification-area icon and its visual states.

## Canonical source

`src/SightAdapt.Demo/TrayIconSet.cs` is the single authoritative implementation. It renders each state at all required Windows icon sizes and packages the generated PNG frames into one multi-resolution icon stream at runtime.

The SVG files in `docs/assets/icons` are documentation reference exports only. They are not build inputs and must not be edited as an independent product definition. Any intentional design change starts in `TrayIconSet.cs`; reference exports may then be regenerated to illustrate the current result.

## Concept

The icon combines an **eye** with an **adaptive color lens**:

- the eye represents visual accessibility and window-image processing;
- the split iris represents inversion, color transforms, and visual profiles;
- the adjustment notch indicates that the displayed image is transformed;
- the dark rounded background keeps the silhouette readable on light and dark taskbars.

The design avoids letters and fine outlines so it remains recognizable at Windows tray sizes.

## States

| State | Meaning |
|---|---|
| **Active** | A manual or automatic visual correction is active. |
| **Inactive** | SightAdapt is running without an active overlay. |
| **Emergency / attention** | Explicit emergency shutdown, renderer fault, or another state requiring attention. |

Reference exports:

- [`sightadapt-tray-active.svg`](assets/icons/sightadapt-tray-active.svg)
- [`sightadapt-tray-inactive.svg`](assets/icons/sightadapt-tray-inactive.svg)
- [`sightadapt-tray-emergency.svg`](assets/icons/sightadapt-tray-emergency.svg)

## Runtime sizes

The canonical renderer produces frames for:

- 16×16 px;
- 20×20 px;
- 24×24 px;
- 32×32 px;
- 40×40 px;
- 48×48 px;
- 64×64 px;
- 128×128 px;
- 256×256 px.

The 16×16, 20×20, 24×24, and 32×32 results must be inspected manually at 100% scale after a visual change.

## Usage guidelines

- Use **Active** only while an overlay is active.
- Use **Inactive** while the process is running without an overlay.
- Use **Emergency / attention** for explicit emergency shutdown and renderer faults; accompanying text must distinguish those states.
- Keep the eye shape, pupil position, and outer silhouette identical across states.
- Do not add text, badges, or extra symbols to tray-size variants.

## Accessibility considerations

Color is never the only state indicator. Menus, tooltips, status text, and notifications explicitly state **Active**, **Inactive**, **All overlays stopped**, or the fault message. Emergency shutdown remains visible until an explicit user action resumes correction; a transient renderer fault may return to inactive presentation after its notification interval.
