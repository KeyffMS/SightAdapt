# SightAdapt binary packaging standard

This document defines the minimum contents and verification rules for every binary distribution of SightAdapt.

## Canonical required-file manifest

The machine-readable source of truth is `release/required-files.txt`.

Every package must place these files at the package root:

| File | Purpose |
|---|---|
| `SightAdapt.exe` | Application executable |
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `THIRD-PARTY-NOTICES.txt` | Exact-version Microsoft/.NET third-party notices |
| `DOTNET-LICENSE-NOTICE.txt` | Exact-version Microsoft .NET license text and source metadata |
| `DOTNET-NOTICE-METADATA.json` | SDK, runtime, RID, runtime-pack, source and checksum evidence |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated maintainer-reviewed Microsoft redistribution notice |
| `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` | Mark ownership, no-endorsement and DRM/access-control boundaries |
| `DEPENDENCIES.md` | Human-readable dependency inventory |
| `SBOM.spdx.json` | SPDX 2.3 component and shipped-file inventory |
| `LICENSE-REPORT.json` | Dependency-license policy result |
| `PRIVACY.md` | Application privacy and local-data notice |

A package is incomplete if a required file is missing, empty, unreadable, stale or inconsistent with the pinned build inputs.

## Distribution formats

The same bundle is required for Actions artifacts, manual ZIPs, installers, store packages, portable builds and mirrors. Platform metadata does not replace readable files in the installed or unpacked application directory.

Mirrors must publish the same verified bytes and compliance report without stripping or replacing notices.

## Publish behavior

1. `SightAdapt.csproj` copies the repository legal baseline.
2. `generate-dotnet-notices.ps1` imports exact-version Microsoft license/notice material.
3. `generate-dotnet-redistribution-notice.ps1` validates the reviewed configuration, template checksum and maintainer decision, then generates the package notice.
4. `generate-sbom.ps1` creates `DEPENDENCIES.md`, `SBOM.spdx.json` and `LICENSE-REPORT.json`.

Expected package root:

```text
SightAdapt.exe
LICENSE.txt
THIRD-PARTY-NOTICES.txt
DOTNET-LICENSE-NOTICE.txt
DOTNET-NOTICE-METADATA.json
MICROSOFT-DOTNET-REDISTRIBUTION.txt
THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt
DEPENDENCIES.md
SBOM.spdx.json
LICENSE-REPORT.json
PRIVACY.md
```

## Final package compliance gate

Create the archive from the publish directory and validate both representations:

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'
$report = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64-compliance.json'

Compress-Archive `
    -Path '.\artifacts\win-x64\*' `
    -DestinationPath $archive `
    -CompressionLevel Optimal

.\tools\verify-release-compliance.ps1 `
    -DirectoryPath '.\artifacts\win-x64' `
    -ArchivePath $archive `
    -ReportPath $report
```

The gate validates the manifest, staged files, final ZIP, release identity, exact .NET metadata, maintainer redistribution decision, license-policy result and SBOM coverage. It writes the final archive SHA-256 and decision evidence to the compliance report.

The negative check must also pass:

```powershell
.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory '.\artifacts\win-x64'
```

It proves that incomplete packages and stale redistribution notices are rejected.

## Microsoft .NET evidence

`global.json` pins the SDK, `Directory.Build.props` records the expected runtime/RID/publish inputs and `project.assets.json` identifies actual runtime packs.

The exact-version notice generator downloads the matching official SDK ZIP, verifies its SHA-512 and imports its license and third-party notice text. The restore graph remains the authority for selected runtime packs.

`release/dotnet-redistribution-review.json` records the exact reviewed configuration, notice-template SHA-256 and maintainer decision. No external legal audit is planned. A mismatch or `blocked` decision fails before packaging.

See [Microsoft .NET redistribution analysis](legal/DOTNET-REDISTRIBUTION.md) and [Exact-version notice generation](legal/DOTNET-NOTICE-GENERATION.md).

## Other notices and SBOM

The third-party names/DRM notice governs mark ownership, compatibility wording and protected-content limitations.

The dependency policy, restore graph, workflows, .NET metadata and final publish files feed the SPDX SBOM and license report. `SightAdapt.exe` is the documented single-file container for embedded runtime components.

## Installers, stores and mirrors

Every maintained packaging workflow must validate its final installed/unpacked file set against `release/required-files.txt` and retain the outer container checksum. Portable packages use the ZIP gate directly. Mirrors publish the same verified bytes.

## Release checklist

1. verify release metadata and the maintainer redistribution record;
2. verify the .NET analysis and reviewed notice template;
3. review compatibility/DRM wording;
4. restore, build and test;
5. publish into a clean staging directory;
6. generate exact-version .NET notices;
7. generate the maintainer-reviewed redistribution notice;
8. generate SBOM, license report and dependency inventory;
9. resolve license-policy failures;
10. confirm incomplete and stale packages are rejected;
11. create and verify the final package;
12. retain/publish the compliance report;
13. inspect legal documents for readability;
14. publish the same verified bytes to mirrors.

Do not publish when any package, notice, SBOM, license, metadata or maintainer-decision check fails. The project does not require an external audit; release risk is accepted, conditioned or blocked by the maintainer under the documented governance policy.
