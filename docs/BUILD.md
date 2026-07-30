# Build and package SightAdapt as a standalone EXE

These steps create a self-contained Windows x64 executable and verified ZIP.

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

`Directory.Build.props` is the canonical source for product, SDK, runtime, RID, publish mode and artifact metadata. A mismatched reviewed configuration or blocked maintainer decision stops the build.

## 2. Restore and test

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj
dotnet restore .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj

dotnet test .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj `
    --configuration Release `
    --no-restore
```

## 3. Publish and capture bundle inputs

```powershell
dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore `
    --output .\artifacts\win-x64
```

The project captures the SDK-provided `FilesToBundle` list in `artifacts\dotnet-files-to-bundle.tsv`. This temporary file identifies exact single-file inputs; absolute paths are not copied into the release ZIP.

## 4. Import exact official .NET notices

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This verifies the runtime release train and official SDK ZIP SHA-512, then writes the exact Microsoft license, third-party notices and base metadata.

## 5. Generate component-level notice coverage

```powershell
.\tools\generate-dotnet-component-coverage.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This maps every embedded package asset and every loose runtime binary to:

- exact package and version;
- package SHA-512;
- package-relative asset path;
- component SHA-256;
- reviewed notice mapping.

The step also adds exact package notice sections for components not covered by the official .NET release bundle. Unreviewed, non-shipped or unmapped package components fail the build.

## 6. Generate redistribution summary

```powershell
.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

## 7. Generate SBOM and license report

```powershell
.\tools\generate-sbom.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

## 8. Run negative checks

```powershell
.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The validators must reject incomplete, stale and deliberately unmapped packages.

## 9. Create and verify the final ZIP

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

.\tools\verify-dotnet-component-coverage.ps1 `
    -ArchivePath $archive
```

Retain the verified archive and compliance report together.

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

Additional loose native runtime DLLs are allowed only when schema-2 metadata maps each file by exact output path and SHA-256.

## Clean rebuild

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\dotnet-files-to-bundle.tsv -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64.zip -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64-compliance.json -Force -ErrorAction SilentlyContinue
```

Do not publish when metadata review, notice import, component coverage, SBOM/license review, negative checks or final-package validation fails.
