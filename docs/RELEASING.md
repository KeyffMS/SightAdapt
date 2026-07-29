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

## Third-party names and protected content

When release or store materials name a third-party application, use only neutral factual compatibility wording such as `works with` or `tested with <product/version>`. Do not imply official integration, partnership, certification, support or endorsement without a written relationship.

Include this summary whenever third-party compatibility is discussed:

> Third-party product names and trademarks are the property of their respective owners and are used only to identify applications selected or configured by the user. SightAdapt is not affiliated with, sponsored by or endorsed by Microsoft or those owners unless an explicit written relationship is identified. SightAdapt does not circumvent DRM or other access controls, and protected content may remain unavailable or unfilterable.

Do not use third-party logos, branded icons, trade dress or promotional assets without permission or another documented lawful basis. Follow [the third-party names, affiliation and protected-content policy](legal/THIRD-PARTY-NAMES-AFFILIATION-AND-DRM.md).

## Formal legal release gate

Repository notices, SBOMs, policies and CI checks are not a professional legal opinion. Before describing a release as production-ready, selling/licensing it, publishing a production package through a major store, supplying enterprise legal warranties/indemnities or claiming complete legal clearance, follow [the formal legal review and release gate](legal/LEGAL-RELEASE-GATE.md).

A valid approval requires a dated non-confidential record based on [the legal sign-off template](legal/LEGAL-SIGNOFF-TEMPLATE.md) and tied to the exact artifact checksum, territories, channels, revenue model, customer groups and materials reviewed. Privileged advice remains outside the public repository.

The current alpha stage has no formal counsel sign-off. Do not use `legally cleared`, `fully compliant`, `non-infringing`, `production approved` or equivalent wording.

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

Every binary distribution must comply with [the binary packaging standard](PACKAGING.md). The final archive or platform package must include every file listed in `release/required-files.txt`, including the license, exact-version notices, Microsoft redistribution notice, third-party names/DRM notice, privacy notice, dependency inventory, SBOM and license report.

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

Third-party names identify user-selected applications only. SightAdapt is not affiliated with or endorsed by those owners and does not circumvent DRM or other access controls.

SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.
```
