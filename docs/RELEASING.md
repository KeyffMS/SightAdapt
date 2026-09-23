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

GitHub Releases use the gated producer in `.github/workflows/release.yml` and the reusable final-package gate. Publication remains blocked unless `release/release-request.json` explicitly confirms the required manual Windows smoke test and repository release-immutability setting. Installers, store packages and official mirrors remain inactive until separately implemented with the reusable final-package gate.

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

`github-release` is registered as a maintained portable-ZIP producer. The release workflow consumes the exact successful CI artifact for the requested source commit, re-runs the final package gate under the `github-release` channel, creates the version tag only after verification, and publishes the verified ZIP, schema-3 report, checksums, SBOM and legal/privacy evidence. It refuses publication unless the maintainer has confirmed both manual Windows smoke testing and GitHub release immutability.

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


## Prerelease and stable policy

Versions whose canonical product version contains a prerelease suffix such as `-alpha`, `-beta` or `-rc` are published as GitHub prereleases. A stable release must use a product version without a prerelease suffix and requires a separate maintainer decision that the current testing, documentation and compatibility evidence support stable status.

The release request is authoritative for the requested tag and prerelease flag, but it must match the canonical product version in `Directory.Build.props`. A release workflow must never reinterpret or silently rewrite the requested version.

## Immutable release and tag policy

Before the first published GitHub Release, the repository administrator must enable GitHub's **release immutability** setting. The release request must not claim that confirmation until it has been checked in repository settings.

After publication:

- never force-update, delete and recreate, or reuse a published version tag;
- never replace release assets under an existing version;
- treat the GitHub Release URL, tag and asset hashes as a permanent publication record;
- publish corrections as a new version.

The workflow verifies GitHub's reported `isImmutable` state after publication.

## Emergency withdrawal

If a release is unsafe or compromised, do not replace its bytes or move its tag. Stop promoting the affected download, record that the version is withdrawn in release history and public download surfaces, and publish a corrected version under a new immutable tag. Preserve enough dated evidence to explain which bytes were withdrawn and why.

## Release request

The canonical request is `release/release-request.json`. Changing `publish` to `true` is a publication action, not ordinary metadata maintenance. Follow [the GitHub Release checklist](../release/RELEASE-CHECKLIST.md) before doing so.

Release history is retained in [RELEASE-HISTORY.md](RELEASE-HISTORY.md) and links to the authoritative GitHub Release/tag records.
