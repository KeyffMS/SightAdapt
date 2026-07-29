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
| `LICENSE.txt` | SightAdapt MIT License |
| `THIRD-PARTY-NOTICES.txt` | Exact-version notices imported from the reviewed Microsoft .NET distribution |
| `DOTNET-LICENSE-NOTICE.txt` | Exact-version Microsoft .NET license text and source metadata |
| `DOTNET-NOTICE-METADATA.json` | Machine-readable SDK, runtime, RID, runtime-pack, source and checksum evidence |
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

Release mirrors must copy the verified archive without removing, renaming or replacing the legal files. A third-party repackager must preserve the notices and add any notices required by its own packaging layer.

## Publish behavior

`src/SightAdapt/SightAdapt.csproj` copies the legal baseline into the publish directory. `tools/generate-dotnet-notices.ps1` must then replace the baseline .NET files with exact-version material and add the metadata record before packaging.

The expected publish layout begins with:

```text
artifacts/win-x64/
├── SightAdapt.exe
├── LICENSE.txt
├── THIRD-PARTY-NOTICES.txt
├── DOTNET-LICENSE-NOTICE.txt
├── DOTNET-NOTICE-METADATA.json
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

The validation script opens the final ZIP and checks the entries listed in `release/required-files.txt`. Required documents must be readable UTF-8 text. The .NET metadata must match the exact release inputs in `Directory.Build.props`, contain valid checksums and map both required runtime packs.

## Microsoft .NET notices

SightAdapt is a self-contained Windows application. The exact SDK and runtime are pinned in `global.json` and `Directory.Build.props`. The generator obtains the exact official Windows Desktop Runtime ZIP through Microsoft's release metadata, verifies the published SHA-512 hash and imports the license and third-party notice files from that archive.

The package record identifies:

- exact .NET SDK version;
- exact runtime and Windows Desktop Runtime versions;
- runtime identifier and publish mode;
- restored runtime packages;
- authoritative release-metadata and package URLs;
- official package SHA-512;
- SHA-256 values for the imported license and notice text;
- generation time and product version.

See [Exact-version .NET notice generation](legal/DOTNET-NOTICE-GENERATION.md).

## Release checklist

Before publishing or mirroring a binary package:

1. verify the pinned release metadata;
2. restore, build and test the application;
3. publish into a clean staging directory;
4. generate exact-version .NET notices from the verified official package;
5. inspect the generated notice metadata and any newly mapped runtime component;
6. create the final archive or platform package;
7. validate the final archive with `tools/test-release-package.ps1`;
8. inspect the package manually to confirm that every legal document opens without running SightAdapt;
9. publish the same verified bytes to every official mirror.

Do not publish an official binary release when the legal bundle, exact-version generation, package checksum, runtime mapping or final-archive validation is incomplete.
