# Build and package SightAdapt

These steps create the maintained self-contained Windows x64 portable ZIP and its schema-3 compliance report.

## Prerequisites

Use Windows 10/11 x64 and the exact SDK pinned in `global.json`.

```text
.NET SDK 8.0.423
.NET Runtime 8.0.29
Windows Desktop Runtime 8.0.29
```

## 1. Verify metadata

```powershell
.\tools\verify-release-metadata.ps1
```

`Directory.Build.props` is the canonical source for product, SDK, runtime, RID, publish mode and artifact metadata.

## 2. Restore, build and test

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj
dotnet restore .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj

dotnet build .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore

dotnet test .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj `
    --configuration Release `
    --no-restore
```

Both `project.assets.json` files are consumed by the complete SBOM/license inventory.

## 3. Publish

```powershell
dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore `
    --output .\artifacts\win-x64
```

The publish target captures SDK `FilesToBundle` inputs in `artifacts\dotnet-files-to-bundle.tsv`. The temporary file is not included in the ZIP.

## 4. Generate legal and supply-chain evidence

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64

.\tools\generate-dotnet-component-coverage.ps1 `
    -PublishDirectory .\artifacts\win-x64

.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64

.\tools\generate-sbom.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

These steps generate exact Microsoft notices, component mappings, `DEPENDENCIES.md`, schema-2 `LICENSE-REPORT.json` and SPDX 2.3 `SBOM.spdx.json`.

## 5. Run negative checks

```powershell
.\tools\test-sbom-license-negative.ps1 `
    -PublishDirectory .\artifacts\win-x64

.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory .\artifacts\win-x64

.\tools\test-final-package-gate-negative.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The tests reject unknown licenses, incomplete/stale/unmapped packages, changed archive bytes, incorrect commit provenance, inconsistent tags and inactive channels.

## 6. Create the maintained portable ZIP

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'
$report = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64-compliance.json'

.\tools\new-verified-release-package.ps1 `
    -DirectoryPath '.\artifacts\win-x64' `
    -ArchivePath $archive `
    -ReportPath $report `
    -DistributionChannel 'local-portable-zip'
```

Do not create an official package with a separate `Compress-Archive` command. The reusable entry point creates the archive, runs all final validators and records per-file SHA-256 equality and Git provenance.

GitHub Actions uses the same command with channel `github-actions-artifact`; commit/ref and workflow/run values are read from the GitHub environment.

## Expected package root

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

The compliance report is kept next to the ZIP because it contains the final archive SHA-256.

## Maintained scope

`release/distribution-channels.json` currently allows only the GitHub Actions portable ZIP and the local maintainer portable ZIP. GitHub Releases, installers, store packages and mirrors remain inactive until separately implemented with the same final gate.

## Clean rebuild

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\dotnet-files-to-bundle.tsv -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64.zip -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64-compliance.json -Force -ErrorAction SilentlyContinue
```

Do not publish when any generator, negative test or final-package validation fails.
