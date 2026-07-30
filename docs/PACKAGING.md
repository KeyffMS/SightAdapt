# SightAdapt binary packaging standard

## Maintained format

SightAdapt currently maintains one binary distribution format: a self-contained Windows x64 portable ZIP.

The allowed producer contexts are registered in `release/distribution-channels.json`. Both the GitHub Actions artifact and a maintainer-created local ZIP must use `tools/new-verified-release-package.ps1`.

Installers, store packages, GitHub Releases and mirrors are not maintained channels yet. Their future implementation must activate a registry entry and use the reusable final-package gate.

## Canonical required files

The machine-readable package manifest is `release/required-files.txt`.

Every portable ZIP must contain at its root:

| File | Purpose |
|---|---|
| `SightAdapt.exe` | Application executable and single-file container |
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `THIRD-PARTY-NOTICES.txt` | Exact .NET and package-specific notices |
| `DOTNET-LICENSE-NOTICE.txt` | Exact Microsoft .NET license text and source evidence |
| `DOTNET-NOTICE-METADATA.json` | Exact package/component/notice evidence |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Maintainer-reviewed redistribution summary |
| `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` | Affiliation, trademark and DRM boundaries |
| `DEPENDENCIES.md` | Human-readable dependency inventory |
| `SBOM.spdx.json` | SPDX 2.3 package and file inventory |
| `LICENSE-REPORT.json` | Complete license-evidence and policy result |
| `PRIVACY.md` | Local data and privacy notice |

The files must be readable without running SightAdapt.

## Publish sequence

1. restore, build and test;
2. publish into a clean staging directory;
3. import exact Microsoft notices;
4. generate component-level notice coverage;
5. generate the redistribution summary;
6. generate `DEPENDENCIES.md`, `LICENSE-REPORT.json` and `SBOM.spdx.json`;
7. run negative compliance tests;
8. call the reusable package entry point;
9. retain the ZIP and schema-3 compliance report together.

## Reusable package command

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'
$report = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64-compliance.json'

.\tools\new-verified-release-package.ps1 `
    -DirectoryPath '.\artifacts\win-x64' `
    -ArchivePath $archive `
    -ReportPath $report `
    -DistributionChannel 'local-portable-zip'
```

In GitHub Actions the channel is `github-actions-artifact`; GitHub environment variables supply commit/ref and workflow/run provenance.

## Final-package invariants

The reusable gate verifies:

- the channel is registered as maintained;
- the final package contains the canonical legal/compliance bundle;
- exact .NET notices, component coverage, SBOM and license report match the build;
- every staged path exists in the ZIP and no unexpected path was added;
- every staged file SHA-256 equals the corresponding ZIP entry SHA-256;
- source commit SHA matches repository HEAD;
- tag and source ref are consistent;
- the archive name, size and SHA-256 are recorded;
- the final report is schema 3 with `result: pass`.

The report is distributed alongside the ZIP, not inside it, because it records the final ZIP checksum.

## Negative checks

The workflow proves rejection of incomplete, stale, unmapped, unknown-license, byte-modified and provenance-inconsistent packages. It also proves that a planned channel cannot publish before activation.

## Adding a new format or channel

A new GitHub Release, installer, store package or mirror must be implemented in its own tracked work. The implementation must:

- define its final installed/unpacked staging tree;
- preserve the canonical legal bundle;
- activate its registry entry;
- call the reusable final-package gate;
- retain the verified container and report;
- add container-specific tests.

Do not describe a planned channel as maintained before those controls exist.
