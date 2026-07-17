# SightAdapt Hard

## 1. Purpose

SightAdapt Hard is the extended accessibility shell built on top of the stable Light capture, rendering, and overlay foundation.

It is intended for applications that remain difficult to use even after color correction. Hard adds semantic information, magnification tools, focus visualization, large-text presentation, optional OCR, and optional speech output.

Hard does not replace Light. The Light visual-processing path remains the reliable core and must continue to work when every semantic feature is disabled or unavailable.

Development of Hard begins only after all Light completion criteria have been met.

## 2. Primary architecture rule

Hard must not weaken the guarantees provided by Light:

- overlays remain input-transparent unless the user explicitly enters an interactive editing mode;
- the source application is not patched or modified;
- semantic-feature failure cannot disable color processing;
- every additional feature can be disabled independently;
- UI Automation, OCR, and speech run outside the render-critical path;
- the emergency shortcut always disables every visual layer;
- a stale accessibility layer must never block access to the original application.

## 3. Target features

### 3.1 Advanced profiles

A profile may contain:

- inversion and color matrices;
- LUT selection;
- brightness, contrast, gamma, and saturation;
- local-contrast enhancement;
- sharpening;
- edge enhancement;
- glare-reduction filters;
- dimming outside the active window;
- a reading ruler;
- cursor highlighting;
- text-caret highlighting;
- focus highlighting;
- a large-text panel;
- magnifier settings;
- rules for dialogs and owned windows;
- optional rules for specific accessible controls.

Profiles must remain declarative. They must not contain executable scripts.

### 3.2 UI Automation

UI Automation is used as a semantic data source, not as a universal method for styling another application's controls.

Potential uses:

- detect the focused element;
- retrieve its accessible name;
- retrieve its control type;
- retrieve its value or selection state;
- read selected text where supported;
- retrieve the on-screen bounding rectangle;
- follow focus changes;
- identify editable text fields;
- show a large-text representation;
- position focus and caret overlays;
- optionally send information to text-to-speech.

Limitations:

- some applications expose incomplete or incorrect automation trees;
- custom-rendered applications may expose almost no semantic data;
- elevated applications may not be accessible to a normal process;
- Chromium and Electron may produce very large trees;
- remote providers may hang or respond slowly;
- every request requires cancellation and timeout handling;
- UI Automation calls must never run on the renderer thread.

### 3.3 Large-text panel

The panel may display:

- the accessible name of the focused control;
- the current value;
- selected text;
- editable text near the caret;
- text underneath the pointer when available;
- OCR output from a selected region.

Panel options:

- user-selected font family;
- large font sizes;
- custom foreground and background colors;
- increased letter spacing;
- increased line spacing;
- word wrapping;
- current-word highlighting;
- docked, floating, or pinned placement;
- monitor selection;
- automatic hiding when no usable text is available.

The panel displays a copy of information. It does not change the actual font or layout inside the source application.

### 3.4 Magnifier

Preferred modes:

- magnifier near the pointer;
- focused-control magnifier;
- docked magnifier;
- optional full-screen magnifier mode;
- a magnified region from only the selected source window.

The initial Hard magnifier is non-interactive. The user continues to click and type in the original application.

Interactive coordinate remapping may be investigated later, but it is experimental because transformed coordinates, nested windows, raw input, drag operations, menus, and application-specific hit testing make general input forwarding unreliable.

### 3.5 Focus highlighting

Possible visual forms:

- thick outline;
- semi-transparent fill;
- directional pointer;
- focus-order number;
- label containing the accessible name;
- animated transition that can be disabled.

Preferred location sources:

1. UI Automation bounding rectangle;
2. WinEvent accessibility events;
3. image analysis or OCR only as a final fallback.

Focus rectangles must be stabilized to avoid flicker from rapidly changing or inconsistent provider data.

### 3.6 Text caret

Possible data sources:

- UI Automation `TextPattern` and related text ranges;
- system accessibility events;
- application-specific accessibility providers;
- optional image analysis as a fallback.

Caret highlighting must tolerate missing, delayed, or invalid position data. It must disappear rather than remain at a stale location.

### 3.7 Reading ruler

Modes:

- a horizontal band following the pointer;
- a horizontal band following the focused line;
- dimming above and below the active line;
- adjustable height and opacity;
- keyboard-controlled vertical movement;
- automatic text-line alignment when reliable semantic geometry is available.

### 3.8 OCR

OCR is optional and local.

Use cases:

- applications without usable UI Automation;
- text rendered into a canvas;
- legacy applications;
- scanned documents;
- remote desktop content.

Requirements:

- process only a selected region;
- limit analysis frequency;
- cache recent results;
- support cancellation;
- expose a complete off switch;
- show when OCR is active;
- do not save source images;
- keep OCR failure isolated from all other features;
- clearly identify OCR output as potentially inaccurate.

### 3.9 Text-to-speech

Optional capabilities:

- announce the focused control;
- read selected text;
- read OCR results;
- read from a selected position;
- stop speech immediately;
- configure voice, rate, and volume;
- prioritize urgent system messages over background reading.

Speech is a separate module and must be disabled by default.

## 4. Capabilities the project must not promise

SightAdapt cannot guarantee universal ability to:

- change the real font size inside an arbitrary application;
- reflow or rearrange another application's layout;
- resize all controls correctly;
- change the style of every control;
- interact with protected desktop surfaces;
- capture DRM-protected content;
- support every exclusive full-screen game;
- bypass capture restrictions;
- produce reliable semantic data when the source application exposes none.

Instead, SightAdapt provides external adaptations:

- processed visual output;
- magnification;
- large-text presentation;
- focus and caret highlighting;
- semantic reading when available;
- optional speech output.

## 5. Extended architecture

```text
                    ┌────────────────────┐
                    │  Profile Manager   │
                    └─────────┬──────────┘
                              │
                ┌─────────────▼──────────────┐
                │ Accessibility Orchestrator │
                └────────┬──────────┬────────┘
                         │          │
          ┌──────────────▼───┐  ┌───▼────────────────┐
          │  Visual Pipeline  │  │ Semantic Pipeline  │
          └────────┬──────────┘  └────────┬───────────┘
                   │                      │
        ┌──────────▼──────────┐   ┌───────▼──────────┐
        │ Capture + Direct3D  │   │ UIA / OCR / TTS  │
        └──────────┬──────────┘   └───────┬──────────┘
                   │                      │
          ┌────────▼──────────────────────▼───────┐
          │ Composite Overlay Session             │
          └────────────────────────────────────────┘
```

## 6. Modules

### `SightAdapt.Accessibility.Uia`

Responsibilities:

- dedicated UI Automation worker thread;
- request queue;
- cancellation and timeout enforcement;
- element caching;
- runtime-ID tracking;
- focus-event subscription;
- safe event-handler removal;
- isolation from unresponsive providers;
- conversion of UIA objects into project-owned immutable snapshots.

### `SightAdapt.Accessibility.Ocr`

Responsibilities:

- request a selected image region;
- perform optional preprocessing;
- run local OCR;
- return text and word rectangles;
- cache recent results;
- enforce frequency and size limits;
- support cancellation;
- expose privacy status to the UI.

### `SightAdapt.Accessibility.Speech`

Responsibilities:

- speech queue;
- message priorities;
- interruption;
- voice configuration;
- keyboard commands;
- output-state reporting;
- prevention of overlapping announcements.

### `SightAdapt.Accessibility.Focus`

Responsibilities:

- aggregate UI Automation and WinEvent data;
- determine the best available focus rectangle;
- stabilize position;
- filter short-lived changes;
- produce render-ready focus-decoration state;
- expose fallback reasons for diagnostics.

### `SightAdapt.Magnifier`

Responsibilities:

- select a source rectangle;
- scale on the GPU;
- choose nearest or linear sampling;
- apply optional sharpening;
- render docked or pointer-following output;
- ensure the magnified view never becomes a recursive capture source.

## 7. Multi-profile model

### 7.1 Profile scopes

A profile may apply to:

- only the active window;
- all windows belonging to one process;
- a specific window class;
- dialogs or owned windows;
- a selected monitor;
- a configured schedule;
- manual activation only.

### 7.2 Inheritance

Potential model:

```text
Base profile
  └── Application profile
       └── Window-specific profile
            └── Temporary session override
```

Inheritance should remain simple in the first Hard release. Complex cascading rules can make behavior difficult to explain and debug.

### 7.3 Rule priority

1. explicit disable rule;
2. exact window rule;
3. full executable-path rule;
4. process-name rule;
5. window-class rule;
6. default profile.

The profile editor must show which rule won and why.

## 8. UIAccess and elevation

Full semantic interaction with applications running as administrator may require:

- a digitally signed executable;
- installation in a trusted location such as `Program Files`;
- a manifest containing `uiAccess=true`;
- an optional dedicated accessibility broker process.

Proposed model:

```text
SightAdapt.App
    │
    ├── normal UI and renderer
    │
    └── optional signed UIAccess broker
```

The broker:

- does not render captured images;
- exposes only narrowly scoped accessibility operations;
- communicates through authenticated local IPC;
- validates every request;
- contains as little code as possible;
- is not required by Light.

## 9. Processes and reliability

Potential process split for a mature Hard version:

- main process: tray, settings, profiles, orchestration;
- renderer process: Direct3D and overlays;
- optional UIAccess broker;
- optional OCR worker;
- optional speech worker if required by implementation constraints.

Benefits:

- an OCR crash does not close the renderer;
- the renderer can restart without losing settings;
- privilege boundaries are clearer;
- memory leaks can be contained;
- modules can have independent watchdogs.

This split should be introduced only when profiling and fault testing justify the added complexity.

## 10. Overlay composition

Suggested layer order:

1. processed source image;
2. outside-window dimming;
3. reading ruler;
4. focus outline;
5. caret or pointer highlight;
6. large-text panel;
7. magnifier;
8. SightAdapt status and error notifications.

Layers should be composed on the GPU.

Avoid a separate top-level `HWND` for every simple visual element when one composited surface can provide correct behavior.

## 11. Interaction model

All accessibility layers are click-through by default.

Explicit interactive modes:

- reposition the text panel;
- resize the magnifier;
- select an OCR region;
- edit a profile;
- inspect an accessible element.

Interactive mode must be clearly indicated and must automatically end after the operation or when the user presses Escape.

## 12. Internal APIs

Example interfaces:

```csharp
public interface IAccessibilityFeature
{
    string Id { get; }
    Task StartAsync(
        AccessibilityContext context,
        CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public interface IFocusedElementSource
{
    event EventHandler<FocusedElementChangedEventArgs> FocusedElementChanged;
    Task<AccessibleElement?> GetCurrentAsync(
        CancellationToken cancellationToken);
}

public interface ITextSource
{
    Task<TextSnapshot?> ReadAsync(
        TextRequest request,
        CancellationToken cancellationToken);
}
```

Rules:

- modules must not directly depend on WPF;
- public results should be immutable project-owned records;
- platform objects must not escape their owning thread;
- all potentially slow operations require cancellation;
- render code consumes snapshots, never live UI Automation objects.

## 13. Telemetry and diagnostics

Default behavior:

- no telemetry;
- local structured logs;
- manual generation of a diagnostic package;
- no screenshots in diagnostic packages;
- user choice over inclusion of application names and paths;
- window handles treated as temporary technical identifiers;
- clear indication when OCR or speech modules are active.

Optional telemetry may be introduced only as explicit opt-in and must never include captured image content.

## 14. Accessibility of SightAdapt itself

An accessibility product must have an accessible interface.

Requirements:

- full keyboard operation;
- correct UI Automation names and roles;
- support for Windows text scaling;
- compatibility with high-contrast themes;
- no information conveyed only by color;
- visible keyboard focus;
- configurable shortcuts;
- optional animation reduction;
- a simplified configuration mode;
- import and export through keyboard-accessible dialogs;
- error messages that explain both the failure and the next action;
- screen-reader testing before stable releases.

## 15. User participation

The project should be developed with people who have low vision, not only for them.

Recommended process:

- public usability scenarios;
- structured feedback on visual profiles;
- moderated usability testing;
- accessibility-specific issue templates;
- community profile sharing;
- clear separation between product feedback and medical information;
- no collection of diagnoses unless strictly necessary and explicitly consented to.

## 16. Security

### 16.1 Threat model

The project must consider:

- exposure of sensitive on-screen content;
- malicious or malformed LUT files;
- regular-expression denial of service;
- an unresponsive UI Automation provider;
- target-window spoofing;
- unauthorized local IPC requests;
- profile-file replacement;
- unauthorized launch of the UIAccess broker;
- stale overlays after a crash;
- excessive GPU-memory allocation;
- dependency or update-channel compromise.

### 16.2 Mitigations

- validate every input file;
- enforce size and complexity limits;
- use timeouts and cancellation;
- publish signed releases when feasible;
- publish SHA-256 checksums;
- restrict IPC with appropriate access control;
- never allow executable scripts in profiles;
- avoid loading arbitrary native libraries from profile directories;
- provide a safe startup mode with profiles disabled;
- provide a command-line emergency shutdown or reset option;
- isolate elevated components;
- document the security-reporting process.

## 17. Extensions and plugins

A plugin system is not recommended for the first Hard releases.

Preferred early extension mechanisms:

- versioned JSON profiles;
- LUT files;
- built-in effect combinations;
- declarative matching and behavior rules.

If executable plugins are introduced later, they should use:

- a separate host process;
- explicit capabilities;
- versioned APIs;
- user trust decisions or signing;
- independent disable controls;
- no direct access to the renderer internals;
- no execution inside the UIAccess broker.

## 18. Hard testing

Additional test categories:

- applications with good UI Automation support;
- applications with incomplete or incorrect UI Automation data;
- applications with no meaningful UI Automation data;
- very large Chromium accessibility trees;
- blocked or hanging providers;
- elevated applications;
- many active windows;
- rapid focus changes;
- multilingual OCR;
- combined magnifier, LUT, ruler, and focus overlays;
- restart of one module without restarting the full application;
- emergency shutdown while every module is active;
- Windows 10 and Windows 11 behavior comparisons;
- screen-reader use of SightAdapt's own interface.

## 19. Hard development stages

### Hard 0.1

- focus outline;
- focused-control name panel;
- UI Automation bounding rectangle;
- one docked magnifier;
- no OCR.

### Hard 0.2

- large-text panel;
- `TextPattern` support;
- reading ruler;
- layered profile settings;
- UI Automation cache and timeout hardening.

### Hard 0.3

- local OCR;
- selectable OCR region;
- text support for custom-rendered applications;
- optional text-to-speech.

### Hard 0.4

- signed UIAccess broker;
- improved elevated-application support;
- advanced profile rules;
- process isolation and restart handling.

### Hard 1.0

- stable public feature set;
- complete user and developer documentation;
- published threat model and privacy policy;
- user testing;
- maintained Windows 10 compatibility profile;
- primary Windows 11 validation profile.

## 20. Entry criteria for Hard development

Hard development may begin when Light:

- meets its complete definition of done;
- has a stable public profile schema;
- has a stable renderer;
- has no unresolved critical overlay defects;
- has multi-monitor and mixed-DPI tests;
- recovers from GPU device loss where technically possible;
- has results from testing with users who have low vision;
- has at least one stable public release.

## 21. Completion criteria for Hard

Hard 1.0 is complete when:

- every optional feature can be disabled independently;
- UI Automation failure does not affect color transformation;
- OCR failure does not affect the main UI or renderer;
- core functionality works without administrator privileges;
- the emergency shortcut disables every layer;
- the settings interface is usable with keyboard and screen readers;
- all semantic modules implement timeout and cancellation behavior;
- known limitations are public;
- captured images are never stored without a deliberate user action;
- Windows 10 and Windows 11 tests pass according to the support matrix;
- the feature set has been evaluated with target users.
