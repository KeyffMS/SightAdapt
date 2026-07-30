# Build and package SightAdapt as a standalone EXE

These steps create a self-contained Windows x64 executable and verified ZIP.

## Prerequisites

Use Windows 10/11 x64 and the exact SDK pinned in `global.json`.

Current release inputs:

```text
.NET SDK 8.0.423
.NET Runtime 8.0.29
Windows Desktop Runtime 8.0.29
```

Verify:

```powershell
dotnet --version
```

SDK roll-forward is disabled.

## 1. Verify release metadata and maintainer decision

```powershell
.\tools\verify-release-metadata.ps1
```

`Directory.Build.props` is the canonical source for product, SDK, runtime, RID, publish-mode and artifact metadata. The check compares it with the project, `global.json`, the reviewed redistribution-template SHA-256 and the maintainer decision in `release/dotnet-redistribution-review.json`. A mismatched configuration or `blocked` decision stops the build.

## 2. Restore and test

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj
dotnet restore .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj

dotnet test .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj `
    --configuration Release `
    --no-restore
```

The application restore graph is the authority for runtime-pack inventory.

## 3. Publish

```powershell
dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore `
    --output .\artifacts\win-x64
```

The static legal baseline is copied to the publish directory. The Microsoft redistribution notice is generated later from reviewed metadata.

## 4. Generate exact-version Microsoft notices

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This verifies the restored runtime packs, downloads the matching official SDK ZIP, verifies SHA-512 and writes:

- `THIRD-PARTY-NOTICES.txt`;
- `DOTNET-LICENSE-NOTICE.txt`;
- `DOTNET-NOTICE-METADATA.json`.

## 5. Generate the maintainer-reviewed redistribution notice

```powershell
.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This verifies the reviewed configuration, template checksum, maintainer decision and exact-version metadata, then writes `MICROSOFT-DOTNET-REDISTRIBUTION.txt`.

No external legal audit is required by the project plan. The generated file remains an internal project notice, not legal advice or clearance.

## 6. Generate SBOM and license report

```powershell
.\tools\generate-sbom.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This creates `DEPENDENCIES.md`, `SBOM.spdx.json` and `LICENSE-REPORT.json`.

## 7. Inspect the staged directory

At minimum:

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

## 8. Run negative package checks

```powershell
.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The validator must reject an incomplete package and a package with stale redistribution metadata.

## 9. Create and verify the final archive

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'
$report = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64-compliance.json'

Compress-Archive `
    -Path '.\artifacts\win-x64\*' `
    -DestinationPath $archive `
    -CompressionLevel Optimal

.\tools\verify-release-compliance.ps1 `
    -DirectoryPath .\artifacts\win-x64 `
    -ArchivePath $archive `
    -ReportPath $report
```

Retain the compliance report with the archive.

## 10. Run and inspect the version

```powershell
.\artifacts\win-x64\SightAdapt.exe

$process = Get-Process SightAdapt
(Get-Item $process.Path).VersionInfo |
    Format-List ProductVersion, FileVersion
```

## Clean rebuild

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64.zip -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64-compliance.json -Force -ErrorAction SilentlyContinue
```

A release must not be published if metadata review, notice generation, the maintainer decision, negative checks or final-package validation fail.
