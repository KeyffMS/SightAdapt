# Faster overlay switching — 0.4B.2

## Status

Complete and locally accepted. Issue #12 is closed as completed.

## Goal

Reduce the perceived delay when the foreground application changes and avoid the white flash caused by disposing and recreating the overlay window.

## Foreground detection

`ForegroundWindowTracker` remains the foreground-detection authority.

- default polling interval: 75 ms;
- a supported foreground handle is published only when it differs from the last published handle;
- the last supported external window remains available for commands invoked while a SightAdapt window has focus;
- unchanged timer ticks do not re-run application discovery or overlay activation.

The polling model remains intentionally simple. A native foreground hook can be considered only if measured behavior later proves the accepted 75 ms model insufficient.

## Identity cache

`ApplicationDiscovery` uses a bounded process-ID cache backed by `ApplicationIdentityCache`.

- maximum default size: 64 processes;
- successful lookups are reused across top-level windows from the same process;
- least-recently-used entries are removed when capacity is reached;
- failed resolutions remove the corresponding process entry;
- cache eviction does not change persisted assignments.

The cache is a runtime optimization only. Committed settings remain the source of truth for application matching and behavior.

## Overlay lifetime

`OverlayController` keeps one `MagnifierOverlay` instance while visual correction remains active.

For a new configured target, it retargets the existing instance with:

- target handle;
- color effect;
- transform identifier;
- per-application overlay scope;
- resolved destination and source geometry.

The native magnifier child window is not recreated during a normal foreground switch.

Explicit disable, emergency shutdown, application exit, and controller disposal still close and dispose the overlay immediately.

## Transition grace

A previously rendered overlay may remain visible for at most 125 ms while the old target loses foreground status and the tracker resolves the next target.

Retargeting resets the grace period and immediately applies the new effect, scope, and geometry.

After the grace period:

- a destroyed target closes the overlay;
- an existing but unavailable target hides the overlay;
- no second overlay or parallel runtime state is created.

## Safety and boundaries

- `ForegroundWindowTracker` owns detection and deduplication only;
- `ApplicationIdentityCache` owns derived cache entries only;
- `ApplicationStateController.Current` remains the runtime product-state truth;
- `OverlayController` remains the overlay-resource authority;
- `MagnifierOverlay` owns target tracking and rendering details;
- emergency behavior and automatic suppression are unchanged;
- the overlay remains input-transparent and non-activating.

## Validation baseline

Automated regressions cover:

- the 75 ms default interval;
- duplicate foreground-handle suppression;
- bounded least-recently-used cache behavior;
- overlay retarget ownership;
- absence of normal overlay recreation;
- transition-grace constants and stale-target handling;
- architecture boundaries for detection, cache, and overlay lifecycle.

Local Windows acceptance confirmed:

- faster switching between configured applications;
- correct switching between different profiles and overlay scopes;
- new top-level windows from the same application;
- reduced or eliminated visible white flash;
- predictable cleanup after minimize, close, unconfigured target, and emergency shutdown.

## Build identity

```text
Product version: 0.4.0-alpha.6+<commit>
File version:    0.4.0.2
Milestone:       Alpha 0.4B.2 · Faster overlay switching
```