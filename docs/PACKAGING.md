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
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | End-user and downstream-distributor conditions that keep Microsoft components separate from SightAdapt's MIT license |
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

Release mirrors must copy the verified archive without removing, renaming or replacing the legal files. A downstream distributor may redistribute Microsoft components only as part of an intact SightAdapt application package and must preserve terms that protect the Microsoft distributable code at least as much as the applicable Microsoft agreement.

## Publish behavior

`src/SightAdapt/SightAdapt.csproj` copies the legal baseline and redistribution notice into the publish directory. `tools/generate-dotnet-notices.ps1` replaces the baseline .NET files with exact-version material. `tools/generate-sbom.ps1` then evaluates dependency licenses and generates `DEPENDENCIES.md`, `SBOM.spdx.json` and `LICENSE-REPORT.json` before packaging.

The expected publish layout begins with:

```text
artifacts/win-x64/
├── SightAdapt.exe
├── LICENSE.txt
├── THIRD-PARTY-NOTICES.txt
├── DOTNET-LICENSE-NOTICE.txt
├── DOTNET-NOTICE-METADATA.json
├── MICROSOFT-DOTNET-REDISTRIBUTION.txt
├── DEPENDENCIES.md
├── SBOM.spdx.json
├── LICENSE-REPORT.json
└── PRIVACY.md
```

Additional reviewed runtime files may be present depending on the publish settings.

## Archive creation and validation

Create the archive from the contents of the publish directory so the required files remain at the ZIP root:

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'

Remove-Item $archive -Force -ErrorAction SilentlyContinue
Compress-Archive `
    -Path '.\artifacts\win-x64\*' `
    -DestinationPath $archive `
    -CompressionLevel Optimal

.\tools\test-release-package.ps1 -ArchivePath $archive
```

The validation script opens the final ZIP and checks the entries listed in `release/required-files.txt`. Required documents must be readable UTF-8 text. The exact .NET metadata must match the pinned release inputs and map the required runtime packs. SBOM generation has already failed the workflow if a dependency is absent from the reviewed policy, uses a different version or has a denied/unreviewed license.

## Microsoft .NET notices and redistribution position

SightAdapt is a self-contained Windows application. `global.json` pins the SDK, while `Directory.Build.props` records the expected SDK/runtime release mapping. The actual restore graph in `project.assets.json` identifies the exact runtime packs selected by the build.

The notice generator obtains the matching official .NET SDK ZIP through Microsoft's release metadata, verifies its published SHA-512 hash and imports `LICENSE.txt` and `ThirdPartyNotices.txt` from that archive. The SDK ZIP is used because the standalone Windows Desktop Runtime ZIP does not contain those legal files; the restore graph, not the SDK ZIP contents, remains the authority for which runtime packs are associated with the SightAdapt publication.

The reviewed redistribution position is documented in [Microsoft .NET redistribution analysis](legal/DOTNET-REDISTRIBUTION.md). `MICROSOFT-DOTNET-REDISTRIBUTION.txt` implements the recipient and downstream-distributor notice in each binary package. See also [Exact-version .NET notice generation](legal/DOTNET-NOTICE-GENERATION.md).

## SBOM and license review

`release/dependency-policy.json` records the reviewed component versions, suppliers, sources, scopes and license decisions. `tools/generate-sbom.ps1` combines that policy with the actual restore graph, project references, workflow actions, .NET notice metadata and final publish files.

The generated SPDX document contains component relationships and SHA-256 checksums for every separately shipped file. `SightAdapt.exe` is the documented single-file container for embedded managed and native runtime components. See [SBOM and dependency-license review](legal/SBOM-AND-LICENSE-REVIEW.md).

## Release checklist

Before publishing or mirroring a binary package:

1. verify the pinned release metadata;
2. verify the Microsoft .NET redistribution analysis and package notice;
3. restore, build and test the application;
4. publish into a clean staging directory;
5. generate exact-version .NET notices from the hash-verified official SDK package and actual restore graph;
6. generate the SPDX SBOM, license report and human-readable dependency inventory;
7. resolve every component or license-policy failure;
8. create the final archive or platform package;
9. validate the final archive with `tools/test-release-package.ps1`;
10. inspect the package manually to confirm that every legal document opens without running SightAdapt;
11. publish the same verified bytes to every official mirror.

Do not publish an official binary release when the legal bundle, redistribution notice, exact-version generation, SBOM, license report, package checksum, runtime mapping or final-archive validation is incomplete. Production or paid distribution additionally remains blocked until the qualified legal review required by Issue #93 is recorded.
