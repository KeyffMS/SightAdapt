# SightAdapt release naming

This document defines public naming, attribution, intended-purpose and package-compliance rules for SightAdapt releases.

## Public release identity

GitHub Release title:

`SightAdapt™ <product version>`

Technical identifiers remain plain, for example:

- tag: `v0.5.0.50-alpha`;
- executable: `SightAdapt.exe`;
- archive: `SightAdapt-0.5.0.50-alpha-win-x64.zip`;
- package identifier: `KeyffMS.SightAdapt`.

## Release-note opening

Begin with:

> SightAdapt is a free, open-source Windows application for per-application visual accessibility and color correction.

Then identify version/status, supported Windows/architecture, website, repository, publisher and MIT License.

## Intended-purpose and store claims

All release, store, directory, screenshot, metadata and support materials must follow [the intended-purpose policy](legal/INTENDED-PURPOSE-AND-MDR.md).

The maintained scope is general-purpose accessibility/display personalization, not a medical purpose. Do not:

- select a medical-device category;
- target a named disease, disorder, impairment or patient group;
- claim diagnosis, treatment, therapy, prevention, monitoring or clinical alleviation;
- describe display/color correction as clinical vision correction;
- claim clinical effectiveness or professional endorsement.

Use where appropriate:

> SightAdapt is general-purpose accessibility and display-personalization software. It is not intended to diagnose, treat, prevent, monitor or clinically alleviate any disease, injury or disability, and it is not a substitute for professional medical or eye-care advice.

Features or marketing that create a medical purpose are outside the maintained SightAdapt scope and must not be released under the current policy.

## Patent and commercialization statements

Before material paid distribution, enterprise commitments or large-scale commercial deployment, complete the internal [patent-risk review](legal/PATENT-FTO-GATE.md) and the maintainer release-risk decision.

Do not state that SightAdapt is patent-free, patent-cleared or non-infringing. Do not provide patent warranties or indemnities. The project does not plan an external patent audit.

## Third-party names and protected content

Use neutral factual compatibility wording such as `works with` or `tested with <product/version>`. Do not imply official integration, partnership, certification, support or endorsement without a written relationship.

When third-party compatibility is discussed, include:

> Third-party product names and trademarks are the property of their respective owners and are used only to identify applications selected or configured by the user. SightAdapt is not affiliated with, sponsored by or endorsed by Microsoft or those owners unless an explicit written relationship is identified. SightAdapt does not circumvent DRM or other access controls, and protected content may remain unavailable or unfilterable.

Do not use third-party logos, branded icons, trade dress or promotional assets without a documented basis.

## Maintainer release-risk gate

Repository notices, SBOMs, policies and CI checks are internal project controls, not a professional audit or complete legal clearance.

Before a materially different production, paid, store or enterprise release, follow [the maintainer release-risk review](legal/LEGAL-RELEASE-GATE.md). Record the decision using [the maintainer template](legal/MAINTAINER-RELEASE-REVIEW-TEMPLATE.md), tied to the exact artifact checksum and defined scope.

Do not use `legally cleared`, `fully compliant`, `non-infringing`, `professionally audited`, `MDR approved` or equivalent wording.

## Required mark notice

Include once in the release description or linked legal material:

> SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.

Do not use `®` unless an actual registration exists.

## Consistency rules

- Spell the product identity only as `SightAdapt`.
- Use `SightAdapt™` for the release title and first prominent prose occurrence.
- Use plain `SightAdapt` in technical filenames and identifiers.
- Use `https://aiteracja.pl/sightadapt/` as the canonical product URL.
- Use `Publisher` for `KeyffMS / aiteracja.pl`.
- Follow [the product identity standard](BRAND.md).

## Binary release gate

Every binary distribution must comply with [the binary packaging standard](PACKAGING.md) and contain every file in `release/required-files.txt`.

Validate the staged directory and final package:

```powershell
.\tools\verify-release-compliance.ps1 `
    -DirectoryPath <staged-directory> `
    -ArchivePath <archive-path> `
    -ReportPath <compliance-report-path>
```

Publish the package and compliance report together. Do not publish when the report result is not `pass`.

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
