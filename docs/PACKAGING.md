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
| `DEPENDENCIES.md` | Human-readable inventory of shipped, platform and development dependencies |
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

`src/SightAdapt/SightAdapt.csproj` copies the legal baseline and redistribution notice into the publish directory. `tools/generate-dotnet-notices.ps1` then replaces the baseline .NET files with exact-version material and adds the metadata record before packaging.

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

The validation script opens the final ZIP and checks the entries listed in `release/required-files.txt`. Required documents must be readable UTF-8 text. The .NET metadata must match the exact release inputs in `Directory.Build.props`, contain valid checksums and map both required runtime packs. The redistribution notice must identify the same product, SDK, runtime, target framework, RID and publish mode, clearly separate Microsoft's terms from MIT, prohibit standalone redistribution and disclaim Microsoft endorsement.

## Microsoft .NET notices and redistribution position

SightAdapt is a self-contained Windows application. `global.json` pins the SDK, while `Directory.Build.props` records the expected SDK/runtime release mapping. The actual restore graph in `project.assets.json` identifies the exact runtime packs selected by the build.

The generator obtains the matching official .NET SDK ZIP through Microsoft's release metadata, verifies its published SHA-512 hash and imports `LICENSE.txt` and `ThirdPartyNotices.txt` from that archive. The SDK ZIP is used because the standalone Windows Desktop Runtime ZIP does not contain those legal files; the restore graph, not the SDK ZIP contents, remains the authority for which runtime packs are associated with the SightAdapt publication.

The package record identifies:

- exact .NET SDK version;
- exact runtime and Windows Desktop Runtime versions;
- runtime identifier and publish mode;
- restored runtime packages and framework;
- authoritative release-metadata and SDK-package URLs;
- official SDK package SHA-512;
- SHA-256 values for the imported license and notice text;
- generation time and product version.

The reviewed redistribution position is documented in [Microsoft .NET redistribution analysis](legal/DOTNET-REDISTRIBUTION.md). `MICROSOFT-DOTNET-REDISTRIBUTION.txt` implements the recipient and downstream-distributor notice in each binary package. See also [Exact-version .NET notice generation](legal/DOTNET-NOTICE-GENERATION.md).

## Release checklist

Before publishing or mirroring a binary package:

1. verify the pinned release metadata;
2. verify the Microsoft .NET redistribution analysis and package notice;
3. restore, build and test the application;
4. publish into a clean staging directory;
5. generate exact-version .NET notices from the hash-verified official SDK package and actual restore graph;
6. inspect the generated notice metadata and any newly mapped runtime component;
7. create the final archive or platform package;
8. validate the final archive with `tools/test-release-package.ps1`;
9. inspect the package manually to confirm that every legal document opens without running SightAdapt;
10. publish the same verified bytes to every official mirror.

Do not publish an official binary release when the legal bundle, redistribution notice, exact-version generation, package checksum, runtime mapping or final-archive validation is incomplete. Production or paid distribution additionally remains blocked until the qualified legal review required by Issue #93 is recorded.
