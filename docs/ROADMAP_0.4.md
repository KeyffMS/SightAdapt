# SightAdapt 0.4 Alpha roadmap

## Purpose

SightAdapt 0.4 Alpha will introduce configurable visual color profiles. The first goal is to move beyond strict black-to-white inversion and allow users to create a softer, more readable result for each application.

This roadmap assumes that version 0.3.1 first completes a focused architecture and clean-code pass. That work must prepare the code for visual profiles without adding abstractions that are not required by the 0.4 scope.

## Guiding principles

Development should follow:

- **KISS** — prefer the smallest design that supports the current milestone;
- **DRY** — keep shared transformation and profile logic in one place;
- **Single Point of Authority** — one owner decides the current application and overlay state;
- **Single Point of Truth** — product metadata, settings, active state, and profile definitions each have one authoritative source;
- no dependency-injection framework or speculative abstraction unless a concrete requirement justifies it;
- emergency shutdown and input transparency remain higher priorities than feature count.

## 0.3.1 prerequisite: architecture hardening

Before implementing the 0.4 editor, version 0.3.1 should establish:

1. a single application-state controller for `Inactive`, `ManualActive`, `AutomaticActive`, and `Emergency`;
2. an overlay controller separated from tray and settings UI concerns;
3. a visual-transform abstraction with the current inversion as its first implementation;
4. a visual-profile model separate from application assignment;
5. one `ProductInfo` source for product name, version, repository, author, and license;
6. settings migration support and tests for existing 0.2/0.3 user configuration;
7. unit tests for state transitions, profile persistence, transformation calculations, and emergency shutdown.

## 0.4A: soft color profiles

### User goal

A user should be able to invert an application without mapping pure white to pure black or pure black to pure white.

Example:

```text
white -> dark gray at 8%
black -> light gray at 92%
```

A linear soft-invert transform can be expressed as:

```text
output = outputWhite - input * (outputWhite - outputBlack)
```

For the example above:

```text
output = 0.92 - input * 0.84
```

### Profile parameters

The first visual-profile model should support:

- profile name;
- output black level;
- output white level;
- brightness;
- contrast;
- saturation;
- hue shift;
- enabled/disabled state;
- stable profile identifier.

Application profiles should reference a visual-profile identifier instead of storing a transformation name such as `invert` directly.

### Editor UX

The profile editor should provide:

- a dark, consistent SightAdapt interface;
- a tonal range control for output black and output white;
- brightness and contrast controls;
- a saturation control;
- a hue control displayed on a continuous color spectrum;
- numeric values beside visual controls for precision and accessibility;
- reset-to-default action;
- save, duplicate, rename, and delete actions;
- assignment of a selected visual profile to an application;
- explicit text labels so color is never the only state indicator.

The initial editor may use sliders and a hue spectrum. It does not need a full node editor or professional color-grading interface.

### Rendering scope

0.4A should use the existing Windows Magnification API color matrix where the requested transformation can be represented accurately enough.

The implementation must isolate transformation calculation from the current overlay backend so a later move to Windows Graphics Capture, Direct3D 11, HLSL, and LUT processing does not require changing the profile model or user-facing controls.

## 0.4B: palette analysis

Palette analysis should operate on a sampled frame, not on a raw list of every unique pixel color.

A captured application frame may contain thousands or millions of near-duplicate colors because of antialiasing, gradients, photographs, transparency, and subpixel rendering. Listing every unique color would be unstable and unusable.

The analysis stage should instead provide:

- capture of a single representative frame on explicit user request;
- RGB and luminance histograms;
- reduction to approximately 16, 32, or 64 dominant colors;
- source and transformed color preview;
- percentage or frequency of each dominant color;
- a preview-only workflow that does not store captured screen images;
- clear handling of protected, DRM-controlled, elevated, or otherwise unavailable windows.

No captured frame should be transmitted or retained after analysis unless a later feature explicitly adds an opt-in export action.

## 0.4C: targeted color corrections

After soft profiles and palette analysis are stable, SightAdapt may add targeted correction rules such as:

```text
light neutral colors -> dark graphite range
dark neutral colors -> light gray range
yellow range -> muted amber range
blue range -> selected dark-blue range
```

Each rule should define:

- source color or source color range;
- tolerance;
- target color or target range;
- blend strength;
- enabled state;
- deterministic ordering when multiple rules match.

This stage will likely require a LUT or GPU shader. It should not be forced into the current 5x5 color matrix when that model cannot represent the requested transformation correctly.

## Data model direction

A conceptual model:

```csharp
internal sealed class VisualProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "Soft invert";
    public float OutputBlack { get; set; } = 0.08f;
    public float OutputWhite { get; set; } = 0.92f;
    public float Brightness { get; set; }
    public float Contrast { get; set; } = 1.0f;
    public float Saturation { get; set; } = 1.0f;
    public float HueShift { get; set; }
}
```

Application assignment should reference `VisualProfile.Id`.

The final implementation may adjust names and types, but it should preserve the separation between:

- application identity;
- visual-profile definition;
- application-to-profile assignment;
- runtime overlay state;
- renderer implementation.

## Settings and migration

0.4 must:

- load existing 0.2 and 0.3 settings without losing application assignments;
- migrate the old `effect: "invert"` value to a built-in inversion profile;
- write settings atomically;
- preserve unknown future-compatible fields when practical;
- recover safely from invalid or partially written configuration;
- keep settings in `%LOCALAPPDATA%\SightAdapt\settings.json` unless a documented migration changes that path.

## Testing requirements

Automated tests should cover:

- soft-invert matrix calculations;
- clamping and validation of all profile values;
- profile creation, update, duplication, deletion, and assignment;
- migration of existing settings;
- application-state transitions;
- manual and automatic activation;
- emergency shutdown preventing immediate reactivation;
- invalid profile references;
- persistence round trips.

Manual Windows testing should cover:

- white and black UI themes;
- text antialiasing and readability;
- gradients, icons, and photographs;
- multiple DPI scales and monitors;
- move, resize, minimize, restore, close, and application switching;
- long-running stability;
- keyboard-only operation and screen-reader labels in the profile editor.

## Non-goals for the first 0.4 Alpha

The first 0.4 Alpha does not need:

- a table containing every unique color in a window;
- continuous frame-by-frame palette analysis;
- OCR or semantic UI understanding;
- DLL injection;
- kernel drivers;
- a full professional color-grading suite;
- arbitrary per-pixel rules implemented on the CPU;
- final Windows Graphics Capture and Direct3D renderer migration unless required by a feature that cannot be represented correctly by the current backend.

## Definition of done

SightAdapt 0.4 Alpha is complete when:

1. users can create and save multiple visual profiles;
2. a profile can perform soft inversion with configurable output black and white levels;
3. brightness, contrast, saturation, and hue controls produce deterministic results;
4. applications can be assigned to a selected visual profile;
5. old settings migrate without losing assignments;
6. manual and automatic activation continue to work;
7. the emergency action always removes the overlay and prevents automatic reactivation;
8. transformation logic is independent of tray and configuration UI code;
9. automated calculations and migration tests pass;
10. manual Windows tests confirm readable output and stable overlay behavior.

## Future direction after 0.4

Advanced range mapping, LUT support, and richer palette correction should move toward:

- Windows Graphics Capture;
- Direct3D 11;
- HLSL shaders;
- GPU-side LUT processing;
- preview tooling that does not copy every frame to CPU memory.

Those changes should preserve the 0.4 visual-profile model and application assignments wherever possible.
