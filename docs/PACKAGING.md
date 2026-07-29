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
| `THIRD-PARTY-NOTICES.txt` | Third-party notices and exact-version notice requirement |
| `DOTNET-LICENSE-NOTICE.txt` | Applicable Microsoft .NET redistribution terms and official references |
| `DEPENDENCIES.md` | Human-readable inventory of shipped, platform and development dependencies |
| `PRIVACY.md` | Application privacy and local-data notice |

A package is incomplete if a required file is missing, empty, unreadable or nested only inside another container that the recipient cannot inspect directly.

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

`src/SightAdapt/SightAdapt.csproj` copies the legal files into the final publish directory. The repository `LICENSE` file is linked into publication as `LICENSE.txt`; the remaining legal documents retain their repository filenames.

The expected publish layout begins with:

```text
artifacts/win-x64/
├── SightAdapt.exe
├── LICENSE.txt
├── THIRD-PARTY-NOTICES.txt
├── DOTNET-LICENSE-NOTICE.txt
├── DEPENDENCIES.md
└── PRIVACY.md
```

Additional runtime files may be present depending on the .NET SDK, runtime patch level and publish settings.

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

The validation script opens the final ZIP and checks the entries listed in `release/required-files.txt`. It does not merely check the repository or staging directory. Required text documents must contain readable UTF-8 text.

## Microsoft .NET notices

SightAdapt is a self-contained Windows application. The exact .NET runtime-pack contents can change when the SDK or runtime patch changes. The baseline notice files in the repository identify the applicable Microsoft terms and authoritative sources, but an official release must also complete the exact-version notice-generation and review process before publication.

The release record must identify:

- exact .NET SDK version;
- exact runtime-pack versions;
- authoritative notice source and revision;
- checksum of imported notice material;
- validation result for the final archive.

## Release checklist

Before publishing or mirroring a binary package:

1. build and test the application;
2. publish into a clean staging directory;
3. confirm the exact SDK and runtime versions;
4. refresh and review exact-version third-party notices;
5. create the final archive or platform package;
6. validate the final archive with `tools/test-release-package.ps1`;
7. inspect the package manually to confirm that every legal document opens without running SightAdapt;
8. publish the same verified bytes to every official mirror.

Do not publish an official binary release when the legal bundle or exact-version notice review is incomplete.
