# SightAdapt binary packaging standard

This document defines the minimum contents and verification rules for every binary distribution of SightAdapt.

## Canonical required-file manifest

The machine-readable source of truth is `release/required-files.txt`.

Every package must place these files at the package root:

| File | Purpose |
|---|---|
| `SightAdapt.exe` | Application executable and single-file container |
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `THIRD-PARTY-NOTICES.txt` | Exact-version Microsoft/.NET third-party notices |
| `DOTNET-LICENSE-NOTICE.txt` | Exact-version Microsoft .NET license text and source metadata |
| `DOTNET-NOTICE-METADATA.json` | Exact runtime-pack, component, source and checksum evidence |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated maintainer-reviewed Microsoft redistribution notice |
| `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` | Mark ownership, no-endorsement and DRM/access-control boundaries |
| `DEPENDENCIES.md` | Human-readable dependency inventory |
| `SBOM.spdx.json` | SPDX 2.3 component and shipped-file inventory |
| `LICENSE-REPORT.json` | Dependency-license policy result |
| `PRIVACY.md` | Application privacy and local-data notice |

A package is incomplete if a required file is missing, empty, unreadable, stale, inconsistent with pinned inputs or contains a binary component without notice coverage evidence.

## Distribution formats

The same bundle is required for Actions artifacts, manual ZIPs, installers, store packages, portable builds and mirrors. Platform metadata does not replace readable files in the installed or unpacked application directory.

Mirrors must publish the same verified bytes and compliance report without stripping or replacing notices.

## Publish behavior

1. `SightAdapt.csproj` copies the repository legal baseline.
2. During single-file publication, `CaptureSightAdaptFilesToBundle` records the exact SDK `FilesToBundle` inventory in a temporary build manifest.
3. `generate-dotnet-notices.ps1` maps every embedded runtime-pack input and every separately shipped runtime binary, verifies package hashes and imports exact Microsoft license/notice material.
4. `generate-dotnet-redistribution-notice.ps1` validates the reviewed configuration, template checksum and maintainer decision, then generates the package notice.
5. `generate-sbom.ps1` creates `DEPENDENCIES.md`, `SBOM.spdx.json` and `LICENSE-REPORT.json`.

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

Exact loose native runtime files may also be present. They are permitted only when `DOTNET-NOTICE-METADATA.json` maps each file by output path and SHA-256 to an exact runtime-pack asset.

## Runtime component coverage

Schema 2 of `DOTNET-NOTICE-METADATA.json` is the component-level source of truth. It records:

- SHA-256 of the temporary MSBuild `FilesToBundle` manifest;
- exact NuGet SHA-512 evidence for restored runtime packs;
- one sanitized entry per embedded or loose runtime component;
- component output path, package identity, package-relative asset path, kind and SHA-256;
- the imported exact-release license and third-party-notice hashes;
- an unmapped-component count that must be zero.

Absolute paths from the build machine are not retained in the release artifact.

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

The gate validates the manifest, staged files, final ZIP, release identity, exact .NET metadata, component coverage, maintainer redistribution decision, license-policy result and SBOM coverage. It writes the final archive SHA-256 and decision evidence to the compliance report.

The negative check must also pass:

```powershell
.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory '.\artifacts\win-x64'
```

It proves rejection of:

- an incomplete package;
- a stale redistribution notice;
- a package that still contains a runtime binary after its notice mapping is removed.

## Microsoft .NET evidence

`global.json` pins the SDK, `Directory.Build.props` records expected runtime/RID/publish inputs, `project.assets.json` identifies exact runtime packs and MSBuild `FilesToBundle` identifies the exact embedded single-file inputs.

The exact-version generator downloads the matching official SDK ZIP, verifies SHA-512 and imports its license and third-party notice text. Runtime-pack NuGet hashes and component hashes connect that text to the actual published inventory.

`release/dotnet-redistribution-review.json` records the reviewed configuration, notice-template SHA-256 and maintainer decision. No external legal audit is planned. A mismatch or `blocked` decision fails before packaging.

See [Microsoft .NET redistribution analysis](legal/DOTNET-REDISTRIBUTION.md) and [Exact-version notice generation](legal/DOTNET-NOTICE-GENERATION.md).

## Other notices and SBOM

The third-party names/DRM notice governs mark ownership, compatibility wording and protected-content limitations.

The dependency policy, restore graph, workflows, exact .NET component metadata and final publish files feed the SPDX SBOM and license report. `SightAdapt.exe` is the documented container for embedded runtime components.

## Installers, stores and mirrors

Every maintained packaging workflow must validate its final installed/unpacked file set against `release/required-files.txt` and retain the outer container checksum. Portable packages use the ZIP gate directly. Mirrors publish the same verified bytes.

## Release checklist

1. verify release metadata and the maintainer redistribution record;
2. verify the .NET analysis and reviewed notice template;
3. review compatibility/DRM wording;
4. restore, build and test;
5. publish and capture `FilesToBundle`;
6. generate exact-version .NET notices and component coverage;
7. generate the maintainer-reviewed redistribution notice;
8. generate SBOM, license report and dependency inventory;
9. resolve license-policy or component-mapping failures;
10. confirm incomplete, stale and unmapped-component packages are rejected;
11. create and verify the final package;
12. retain/publish the compliance report;
13. inspect legal documents for readability;
14. publish the same verified bytes to mirrors.

Do not publish when any package, notice, component map, SBOM, license, metadata or maintainer-decision check fails. The project does not require an external audit; release risk is accepted, conditioned or blocked by the maintainer under the documented governance policy.
