# SightAdapt binary packaging standard

This document defines the minimum contents and verification rules for every binary distribution of SightAdapt.

## Canonical required files

The machine-readable source of truth is `release/required-files.txt`.

| File | Purpose |
|---|---|
| `SightAdapt.exe` | Application executable and single-file container |
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `THIRD-PARTY-NOTICES.txt` | Exact .NET notices and package-specific notice sections |
| `DOTNET-LICENSE-NOTICE.txt` | Exact Microsoft .NET license text and source metadata |
| `DOTNET-NOTICE-METADATA.json` | Exact package, component, notice and checksum evidence |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated maintainer-reviewed redistribution notice |
| `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` | Mark ownership, no-endorsement and DRM/access-control boundaries |
| `DEPENDENCIES.md` | Human-readable dependency inventory |
| `SBOM.spdx.json` | SPDX 2.3 component and shipped-file inventory |
| `LICENSE-REPORT.json` | Dependency-license policy result |
| `PRIVACY.md` | Application privacy and local-data notice |

A package is incomplete if a required file is missing, unreadable, stale, inconsistent with pinned inputs or contains a binary without exact component notice evidence.

## Publish sequence

1. `SightAdapt.csproj` copies the static legal baseline and captures SDK `FilesToBundle` inputs.
2. `generate-dotnet-notices.ps1` imports exact official .NET legal text.
3. `generate-dotnet-component-coverage.ps1` maps all embedded and loose package components and adds package-specific notices.
4. `generate-dotnet-redistribution-notice.ps1` renders the reviewed redistribution summary.
5. `generate-sbom.ps1` generates the dependency inventory, SPDX SBOM and license report.

## Exact component coverage

Schema-2 `DOTNET-NOTICE-METADATA.json` records:

- SHA-256 of the temporary `FilesToBundle` manifest;
- exact package identities and NuGet SHA-512 values;
- repository and license metadata;
- notice mappings for exact .NET release text and package-specific terms;
- one sanitized record per embedded or loose component;
- package-relative asset path and component SHA-256;
- zero unmapped external components.

The reviewed alpha contains 452 mapped package components across:

- `Microsoft.NETCore.App.Runtime.win-x64/8.0.29`;
- `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29`;
- `Microsoft.Windows.SDK.NET.Ref/10.0.19041.56`.

`Microsoft.Windows.SDK.NET.Ref` is classified as `shipped-embedded`, because `Microsoft.Windows.SDK.NET.dll` and `WinRT.Runtime.dll` are present in `FilesToBundle`.

## Final package validation

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

.\tools\verify-dotnet-component-coverage.ps1 `
    -ArchivePath $archive
```

The general compliance gate validates package, metadata, license-report and SBOM invariants. The focused component validator independently verifies package evidence, notice mappings, exact loose-binary hashes and absence of unmapped binaries.

## Negative checks

```powershell
.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory '.\artifacts\win-x64'
```

The workflow proves rejection of:

- an incomplete package;
- stale redistribution metadata;
- a package containing a real runtime DLL after its component mapping is deliberately removed.

## Distribution formats

The same legal/compliance bundle is required for Actions artifacts, manual ZIPs, installers, store packages, portable builds and mirrors. Each maintained format must validate its final installed or unpacked file set. Mirrors publish the same verified bytes and report.

## Release checklist

1. verify canonical release metadata and maintainer decision;
2. restore, build and test;
3. publish and capture `FilesToBundle`;
4. import exact official .NET notices;
5. generate exact package/component coverage;
6. generate the redistribution summary;
7. generate SBOM, license report and dependency inventory;
8. resolve every policy or component-mapping failure;
9. prove incomplete, stale and unmapped packages are rejected;
10. create and run both validators against the final package;
11. retain the package and compliance report;
12. publish identical verified bytes to official mirrors.

Do not publish when any notice, package hash, component mapping, SBOM, license, metadata or maintainer-decision check fails.
