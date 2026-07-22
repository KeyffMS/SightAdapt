# Faster overlay switching — 0.4B.2

## Goal

Reduce the perceived delay when the foreground application changes and avoid the white flash caused by disposing and recreating the overlay window.

## Detection

`ForegroundWindowTracker` remains the foreground detection authority.

- default polling interval: 75 ms;
- a foreground handle is published only when it differs from the last published handle;
- the last supported external window remains available for commands invoked while a SightAdapt window has focus.

## Identity cache

`ApplicationDiscovery` uses a bounded process-ID cache backed by `ApplicationIdentityCache`.

- maximum default size: 64 processes;
- successful lookups are reused across top-level windows from the same process;
- least-recently-used entries are removed when capacity is reached;
- failed resolutions remove the corresponding process entry.

The cache is an optimization only. Persisted application assignments remain the source of truth in `SettingsCoordinator.Current`.

## Overlay lifetime

`OverlayController` keeps one `MagnifierOverlay` instance while visual correction remains active.

For a new target it updates:

- target handle;
- color effect;
- transform identifier;
- per-application overlay scope;
- destination and source geometry.

The native magnifier child window is not recreated during a normal foreground switch.

## Transition grace

A previously rendered overlay remains visible for at most 125 ms while the old target loses foreground status and the tracker resolves the new target. Retargeting resets this period and immediately updates geometry.

After the grace period:

- a destroyed target closes the overlay;
- an existing but unavailable target hides the overlay;
- emergency disable and explicit disable still remove it immediately.

## Build identity

- product version: `0.4.0-alpha.6+<commit>`;
- file version: `0.4.0.2`;
- milestone: `Alpha 0.4B.2 · Faster overlay switching`.
