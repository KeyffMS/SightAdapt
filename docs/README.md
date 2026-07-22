# SightAdapt documentation

This index separates current documentation from historical engineering records.

For the current 0.4 implementation, the sources of truth are:

1. product behavior in the current source code and tests;
2. [Current 0.4 architecture](ARCHITECTURE_0.4.md);
3. [0.4 Alpha roadmap](ROADMAP_0.4.md);
4. current feature documents listed below.

Historical audits describe the repository at a specific earlier point. They remain useful for traceability but do not override current code or current architecture documentation.

## Current product and architecture

- [Current 0.4 architecture](ARCHITECTURE_0.4.md) — current authorities, truths, settings transaction, foreground switching, overlay lifecycle, UI boundaries, and safety contracts.
- [SightAdapt 0.4 Alpha roadmap](ROADMAP_0.4.md) — completed 0.4A and 0.4B increments, current main-integration checkpoint, and later palette and targeted-correction work.
- [Current application controls and build notes](../DEMO.md) — user controls, configuration, build, version verification, persistence, and limitations.
- [Soft color profiles](SOFT_COLOR_PROFILES_0.4.md) — current Exact Invert and Soft Invert model, processing order, parameters, persistence, and limitations.
- [User-defined visual profiles](USER_DEFINED_PROFILES_0.4A.2.md) — profile lifecycle, validation, deletion fallback, and acceptance behavior.
- [Configuration grid ownership](CONFIGURATION_GRID_REFACTOR_0.4.md) — final grid transaction and component boundaries after issue #9.
- [Per-application overlay scope](OVERLAY_SCOPE_0.4B.1.md) — four persisted scope choices and geometry ownership.
- [Faster overlay switching](OVERLAY_SWITCHING_0.4B.2.md) — 75 ms detection, deduplication, identity cache, overlay retargeting, and transition grace.
- [0.4A.4 interface requirements](UI_REQUIREMENTS_0.4A.4.md) — accepted UI behavior and manual validation baseline.
- [Tray icon specification](TRAY_ICON.md) — current tray states and reference exports.

## Historical engineering records

These files document earlier architecture states, audits, and remediation work. Branch names, test counts, and proposed work inside them are historical evidence rather than current status.

- [0.3.1 architecture](ARCHITECTURE_0.3.1.md)
- [0.4A.3 lifecycle hardening](HARDENING_0.4A.3.md)
- [Superseded 0.4A.3.007 architecture audit](ARCHITECTURE_AUDIT_0.4A.3.007.md)
- [0.4A.4 architecture remediation](ARCHITECTURE_REMEDIATION_0.4A.4.md)
- [Historical 0.4A.4 final architecture audit](ARCHITECTURE_AUDIT_0.4A.4_FINAL.md)

## Core project documents

- [Light scope and completion criteria](../LIGHT.md)
- [Hard target architecture](../HARD.md)
- [Security policy](../SECURITY.md)
- [Contribution guide](../CONTRIBUTING.md)
- [License](../LICENSE)