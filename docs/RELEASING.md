# SightAdapt release naming

This document defines the public naming, attribution, intended-purpose and minimum package-compliance rules for SightAdapt releases.

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

## Intended-purpose and store claims

Release notes, store listings, directories, screenshots, metadata and support materials must follow [the intended-purpose and medical-device claims policy](legal/INTENDED-PURPOSE-AND-MDR.md).

The current approved position is general-purpose accessibility and display-personalization software, not software intended for a medical purpose. Do not:

- select a medical-device category;
- target a named disease, disorder, impairment or patient group;
- claim diagnosis, treatment, therapy, prevention, monitoring or clinical alleviation;
- describe display/color correction as clinical vision correction;
- claim clinical effectiveness or professional endorsement.

Where a purpose clarification is appropriate, use:

> SightAdapt is general-purpose accessibility and display-personalization software. It is not intended to diagnose, treat, prevent, monitor or clinically alleviate any disease, injury or disability, and it is not a substitute for professional medical or eye-care advice.

A feature, category or marketing change that creates a potential medical purpose requires a new written assessment from a qualified EU medical-device regulatory professional before publication.

## Commercialization and patent statements

The current release is approved only for free/open-source development and alpha distribution. Before paid licensing, enterprise warranties, investor representations or large-scale commercial deployment, reopen and complete [the patent FTO commercialization gate](legal/PATENT-FTO-GATE.md).

Do not state that SightAdapt is patent-free, patent-cleared or non-infringing. Do not provide a contractual patent warranty or indemnity without a written professional opinion covering the exact version, activity and territory.

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
- Follow [the product identity standard](BRAND.md) for descriptions, attribution, mark usage and claims control.

## Binary release gate

Every binary distribution must comply with [the binary packaging standard](PACKAGING.md). The final archive or platform package must include every file listed in `release/required-files.txt`, including the license, exact-version notices, Microsoft redistribution notice, privacy notice, dependency inventory, SBOM and license report.

Before publication, validate the final staged directory and package and retain the resulting report:

```powershell
.\tools\verify-release-compliance.ps1 `
    -DirectoryPath <staged-directory> `
    -ArchivePath <archive-path> `
    -ReportPath <compliance-report-path>
```

The package and compliance report must be published together. GitHub Releases, installers, store packages, portable packages and mirrors must use the same canonical manifest and equivalent final-package gate. Do not upload or publish when the report result is not `pass`.

Do not publish an official binary release until the exact .NET SDK and runtime-pack versions have been recorded and the corresponding exact-version notice material has been generated and reviewed.

## Minimal release header

```markdown
# SightAdapt™ <version>

SightAdapt is a free, open-source Windows application for per-application visual accessibility and color correction.

- Website: https://aiteracja.pl/sightadapt/
- Source: https://github.com/KeyffMS/SightAdapt
- Publisher: KeyffMS / aiteracja.pl
- License: MIT License

SightAdapt is general-purpose accessibility and display-personalization software. It is not intended to diagnose, treat, prevent, monitor or clinically alleviate any disease, injury or disability.

SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.
```
