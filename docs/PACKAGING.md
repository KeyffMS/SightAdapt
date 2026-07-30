# SightAdapt binary packaging standard

This document defines the minimum contents and verification rules for every binary distribution of SightAdapt.

## Canonical required-file manifest

The machine-readable source of truth is:

```text
release/required-files.txt
```

Every package must place the following files at the package root so they can be read without starting SightAdapt:

| File | Purpose |
|---|---|
| `SightAdapt.exe` | Application executable |
| `LICENSE.txt` | SightAdapt MIT License; applies to SightAdapt project code, not Microsoft redistributables |
| `THIRD-PARTY-NOTICES.txt` | Exact-version notices imported from the reviewed Microsoft .NET release distribution |
| `DOTNET-LICENSE-NOTICE.txt` | Exact-version Microsoft .NET license text and source metadata |
| `DOTNET-NOTICE-METADATA.json` | Machine-readable SDK, runtime, RID, runtime-pack, source and checksum evidence |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated end-user and downstream-distributor conditions that keep Microsoft components separate from SightAdapt's MIT license |
| `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` | Third-party mark ownership, lack of affiliation/endorsement and protected-content/DRM limitations |
| `DEPENDENCIES.md` | Human-readable inventory generated from the evaluated dependency list |
| `SBOM.spdx.json` | SPDX 2.3 component and shipped-file inventory |
| `LICENSE-REPORT.json` | Automated dependency-license policy result |
| `PRIVACY.md` | Application privacy and local-data notice |

A package is incomplete if a required file is missing, empty, unreadable, stale, inconsistent with the pinned build inputs or nested only inside another container that the recipient cannot inspect directly.

## Distribution formats covered

The same legal-document bundle is required for:

- GitHub Actions artifacts;
- manually created ZIP archives;
- installers;
- store packages;
- portable builds;
- release mirrors and repackaged downloads.

Installers and store packages may additionally display or register legal information through platform-specific metadata, but that does not replace including the files in the installed or unpacked application directory.

Release mirrors must copy the verified archive without removing, renaming or replacing the legal files. A downstream distributor may redistribute Microsoft components only as part of an intact SightAdapt application package and must preserve terms that protect the Microsoft distributable code at least as much as the applicable Microsoft agreement. It must also preserve the third-party names/DRM notice and must not add unauthorized endorsement or circumvention claims.

## Publish behavior

`src/SightAdapt/SightAdapt.csproj` copies the repository legal baseline and third-party names/DRM notice into the publish directory. `tools/generate-dotnet-notices.ps1` replaces the baseline .NET files with exact-version material. `tools/generate-dotnet-redistribution-notice.ps1` then validates the reviewed configuration and template checksum and generates `MICROSOFT-DOTNET-REDISTRIBUTION.txt` from canonical release metadata. `tools/generate-sbom.ps1` evaluates dependency licenses and generates `DEPENDENCIES.md`, `SBOM.spdx.json` and `LICENSE-REPORT.json` before packaging.

The expected publish layout begins with:

```text
artifacts/win-x64/
├── SightAdapt.exe
├── LICENSE.txt
├── THIRD-PARTY-NOTICES.txt
├── DOTNET-LICENSE-NOTICE.txt
├── DOTNET-NOTICE-METADATA.json
├── MICROSOFT-DOTNET-REDISTRIBUTION.txt
├── THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt
├── DEPENDENCIES.md
├── SBOM.spdx.json
├── LICENSE-REPORT.json
└── PRIVACY.md
```

Additional reviewed runtime files may be present depending on the publish settings.

## Final package compliance gate

Create the archive from the contents of the publish directory so the required files remain at the ZIP root, then validate both the staged directory and final archive:

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'
$report = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64-compliance.json'

Remove-Item $archive -Force -ErrorAction SilentlyContinue
Remove-Item $report -Force -ErrorAction SilentlyContinue
Compress-Archive `
    -Path '.\artifacts\win-x64\*' `
    -DestinationPath $archive `
    -CompressionLevel Optimal

.\tools\verify-release-compliance.ps1 `
    -DirectoryPath '.\artifacts\win-x64' `
    -ArchivePath $archive `
    -ReportPath $report
```

The gate consumes the canonical manifest and existing notice, license-report and SBOM outputs. It verifies the staged directory, final ZIP, build identity, archive name, exact-version metadata, reviewed redistribution metadata, license-policy result and shipped-file coverage. It writes a machine-readable report containing the final archive SHA-256 and validation result.

The negative check must also pass after notice generation:

```powershell
.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory '.\artifacts\win-x64'
```

This proves that both an incomplete package and a package with deliberately stale .NET redistribution metadata are rejected.

See [Release compliance gate](legal/RELEASE-COMPLIANCE-GATE.md).

## Microsoft .NET notices and redistribution position

SightAdapt is a self-contained Windows application. `global.json` pins the SDK, while `Directory.Build.props` records the expected SDK/runtime release mapping. The actual restore graph in `project.assets.json` identifies the exact runtime packs selected by the build.

The exact-version notice generator obtains the matching official .NET SDK ZIP through Microsoft's release metadata, verifies its published SHA-512 hash and imports `LICENSE.txt` and `ThirdPartyNotices.txt` from that archive. The SDK ZIP is used because the standalone Windows Desktop Runtime ZIP does not contain those legal files; the restore graph, not the SDK ZIP contents, remains the authority for which runtime packs are associated with the SightAdapt publication.

`release/dotnet-redistribution-review.json` records the exact technical configuration reviewed by the maintainer, the reviewed notice-template SHA-256 and the current professional-review status. A mismatch in SDK, runtime, target framework, RID, publish mode or template checksum fails before release packaging. Product version is inserted automatically into the generated notice.

The reviewed redistribution position is documented in [Microsoft .NET redistribution analysis](legal/DOTNET-REDISTRIBUTION.md). The generated `MICROSOFT-DOTNET-REDISTRIBUTION.txt` implements the recipient and downstream-distributor notice in each binary package. See also [Exact-version .NET notice generation](legal/DOTNET-NOTICE-GENERATION.md).

## Third-party names and protected content

`THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` is the package notice for third-party mark ownership, identification-only use, lack of affiliation/endorsement, neutral compatibility wording and the fact that SightAdapt does not circumvent DRM or access controls. Protected content may remain unavailable or unfilterable.

See [Third-party names, affiliation and protected-content policy](legal/THIRD-PARTY-NAMES-AFFILIATION-AND-DRM.md).

## SBOM and license review

`release/dependency-policy.json` records the reviewed component versions, suppliers, sources, scopes and license decisions. `tools/generate-sbom.ps1` combines that policy with the actual restore graph, project references, workflow actions, .NET notice metadata and final publish files.

The generated SPDX document contains component relationships and SHA-256 checksums for every separately shipped file. `SightAdapt.exe` is the documented single-file container for embedded managed and native runtime components. See [SBOM and dependency-license review](legal/SBOM-AND-LICENSE-REVIEW.md).

## Installers, stores and mirrors

Every maintained installer or store workflow must run the same logical gate after the final installed/unpacked file set is staged. The installed application directory must satisfy `release/required-files.txt`, and the outer installer/store container checksum must be retained in its compliance report.

Portable packages use the ZIP gate directly. Release mirrors publish the same verified bytes and compliance report; they must not rebuild, strip or rename required legal/compliance files.

## Release checklist

Before publishing or mirroring a binary package:

1. verify the pinned release metadata and reviewed redistribution record;
2. verify the Microsoft .NET redistribution analysis and reviewed notice template;
3. review third-party compatibility wording and ensure the affiliation/DRM notice is included;
4. restore, build and run the maintained project checks;
5. publish into a clean staging directory;
6. generate exact-version .NET notices from the hash-verified official SDK package and actual restore graph;
7. generate the Microsoft .NET redistribution notice from the reviewed template and canonical release metadata;
8. generate the SPDX SBOM, license report and human-readable dependency inventory;
9. resolve every component or license-policy failure;
10. confirm the deliberately incomplete and stale-notice packages are rejected;
11. create the final archive or platform package;
12. run `verify-release-compliance.ps1` against the staged directory and final package;
13. retain and publish the compliance report with the package;
14. inspect the package manually to confirm that every legal document opens without running SightAdapt;
15. publish the same verified bytes to every official mirror.

Do not publish an official binary release when the legal bundle, redistribution notice, third-party affiliation/DRM notice, exact-version generation, SBOM, license report, package checksum, runtime mapping or final compliance gate is incomplete. Production or paid distribution additionally remains blocked while the professional-review status in `release/dotnet-redistribution-review.json` is `not-obtained`.
