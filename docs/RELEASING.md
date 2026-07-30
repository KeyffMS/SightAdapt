# SightAdapt release rules

This document defines public naming, claims and package-compliance rules.

## Public identity

GitHub Release title:

`SightAdapt™ <product version>`

Technical identifiers remain plain:

- tag: `v0.5.0.50-alpha`;
- executable: `SightAdapt.exe`;
- archive: `SightAdapt-0.5.0.50-alpha-win-x64.zip`;
- package identifier: `KeyffMS.SightAdapt`.

## Maintained distribution scope

The only maintained binary format is the Windows x64 portable ZIP. Its maintained producer contexts are listed in `release/distribution-channels.json`.

GitHub Releases are not yet an active publication channel; implementation is tracked in #98. Installers, store packages and official mirrors are also inactive until separately implemented with the reusable final-package gate.

## Release-note opening

Begin with:

> SightAdapt is a free, open-source Windows application for per-application visual accessibility and color correction.

Then identify version/status, supported Windows/architecture, website, repository, publisher and MIT License.

## Intended-purpose claims

SightAdapt is maintained as general-purpose accessibility and display-personalization software, not a medical product. Do not claim diagnosis, treatment, therapy, prevention, monitoring, clinical correction or clinical effectiveness.

Use where appropriate:

> SightAdapt is general-purpose accessibility and display-personalization software. It is not intended to diagnose, treat, prevent, monitor or clinically alleviate any disease, injury or disability, and it is not a substitute for professional medical or eye-care advice.

## Patent and legal statements

Before material paid distribution, enterprise commitments or large-scale commercial deployment, complete the internal patent-risk and maintainer release-risk decisions.

Do not state that SightAdapt is legally cleared, fully compliant, patent-free, patent-cleared, non-infringing, professionally audited or MDR approved.

## Third-party names and protected content

Use neutral compatibility wording such as `works with` or `tested with <product/version>`. Do not imply partnership, certification, support or endorsement without a written relationship.

When third-party compatibility is discussed, include:

> Third-party product names and trademarks are the property of their respective owners and are used only to identify applications selected or configured by the user. SightAdapt is not affiliated with, sponsored by or endorsed by Microsoft or those owners unless an explicit written relationship is identified. SightAdapt does not circumvent DRM or other access controls, and protected content may remain unavailable or unfilterable.

Do not use third-party logos, branded icons or trade dress without a documented basis.

## Mark notice

Include once in release or linked legal material:

> SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.

Do not use `®` unless an actual registration exists.

## Consistency rules

- Spell the product identity only as `SightAdapt`.
- Use `SightAdapt™` for the release title and first prominent prose occurrence.
- Use plain `SightAdapt` in technical filenames and identifiers.
- Use `https://aiteracja.pl/sightadapt/` as the canonical product URL.
- Use `Publisher` for `KeyffMS / aiteracja.pl`.

## Binary package gate

Every maintained binary package must contain every file in `release/required-files.txt` and be created through:

```powershell
.\tools\new-verified-release-package.ps1 `
    -DirectoryPath <staged-directory> `
    -ArchivePath <archive-path> `
    -ReportPath <compliance-report-path> `
    -DistributionChannel <maintained-channel>
```

The command creates the ZIP, runs the base compliance and component validators, compares every staged/archive file by SHA-256 and records commit/ref/workflow provenance.

Publish the ZIP and matching schema-3 report together. Do not publish when the report result is not `pass`.

A future GitHub Release workflow must activate `github-release` in `release/distribution-channels.json`, require an immutable tag and publish the exact verified ZIP and report. Until then the final gate rejects that channel.

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
