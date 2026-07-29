# SightAdapt release naming

This document defines the public naming and attribution rules for SightAdapt release notes. Build, packaging, legal, and artifact validation remain covered by the release and compliance issues linked from the project tracker.

## Public release identity

Use the following format for a GitHub Release title:

`SightAdapt™ <product version>`

Example:

`SightAdapt™ 0.5.0.50 Alpha`

Use plain technical identifiers where punctuation is not appropriate:

- tag: `v0.5.0.50-alpha`;
- executable: `SightAdapt.exe`;
- archive: `SightAdapt-0.5.0.50-alpha-win-x64.zip`;
- package identifier: `KeyffMS.SightAdapt`.

## Release-note opening

Begin public release notes with the canonical one-line description:

> SightAdapt is a free, open-source Windows application for per-application visual accessibility and color correction.

Then identify:

- product version and release status;
- supported Windows versions and architecture;
- canonical website: `https://aiteracja.pl/sightadapt/`;
- source repository: `https://github.com/KeyffMS/SightAdapt`;
- publisher: `KeyffMS / aiteracja.pl`;
- license: `MIT License`.

## Required mark notice

Include this notice once in the release description or linked legal material:

> SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.

Do not use `®`.

## Consistency rules

- Spell the product identity only as `SightAdapt`.
- Use `SightAdapt™` for the release title and first prominent prose occurrence.
- Use plain `SightAdapt` in subsequent prose and all technical filenames and identifiers.
- Link public website references to `https://aiteracja.pl/sightadapt/`.
- Do not use `https://sightadapt.aiteracja.pl/` as a canonical URL; it is reserved as an optional redirect.
- Use `Publisher`, not alternating `Author`, `Vendor`, or `Company`, on public release surfaces.
- Follow [the product identity standard](BRAND.md) for descriptions, attribution, and mark usage.

## Minimal release header

```markdown
# SightAdapt™ <version>

SightAdapt is a free, open-source Windows application for per-application visual accessibility and color correction.

- Website: https://aiteracja.pl/sightadapt/
- Source: https://github.com/KeyffMS/SightAdapt
- Publisher: KeyffMS / aiteracja.pl
- License: MIT License

SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.
```
