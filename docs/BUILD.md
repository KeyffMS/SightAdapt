# Build and package SightAdapt as a standalone EXE

These steps create a self-contained Windows x64 executable and a verified ZIP archive. The published application is started directly as `SightAdapt.exe`; `dotnet run` is not required.

## 1. Install prerequisites

Use a 64-bit Windows 10 or Windows 11 computer and install the exact SDK selected in `global.json`.

For the current release candidate:

```text
.NET SDK 8.0.423
.NET Runtime 8.0.29
Windows Desktop Runtime 8.0.29
```

Verify the SDK:

```powershell
dotnet --version
```

The command must print `8.0.423`. SDK roll-forward is disabled so an unreviewed SDK cannot silently change the release composition.

## 2. Clone the repository

```powershell
git clone https://github.com/KeyffMS/SightAdapt.git
cd SightAdapt
```

## 3. Verify release metadata and redistribution review

```powershell
.\tools\verify-release-metadata.ps1
```

This check verifies the synchronized product, expected SDK/runtime, target framework, RID and publish-mode inputs. It also compares them with `release/dotnet-redistribution-review.json` and verifies the SHA-256 of the reviewed redistribution-notice template. A changed reviewed input cannot proceed until the review record is deliberately updated.

## 4. Restore dependencies

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj
dotnet restore .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj
```

The application restore creates the authoritative runtime-pack inventory used by notice generation. The exact-version generator rejects runtime-pack versions that differ from `Directory.Build.props`.

## 5. Run the tests

```powershell
dotnet test .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj `
    --configuration Release `
    --no-restore
```

All maintained checks must pass before publication.

## 6. Publish a self-contained single-file executable

The runtime identifier, self-contained mode and single-file setting are defined by `Directory.Build.props` and `SightAdapt.csproj`. The exact runtime patch is selected by the pinned SDK and verified against the restored `project.assets.json` before packaging.

```powershell
dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore `
    --output .\artifacts\win-x64
```

The project copies the static repository legal-document baseline into the publish directory. The Microsoft .NET redistribution notice is intentionally not copied as a static file; it is generated after exact-version .NET metadata exists.

## 7. Generate exact-version .NET notices

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The generator verifies the runtime packs selected by restore, downloads the matching official Microsoft .NET SDK ZIP, verifies its published SHA-512 hash and replaces the baseline .NET files with exact-version legal material. It writes:

- `THIRD-PARTY-NOTICES.txt`;
- `DOTNET-LICENSE-NOTICE.txt`;
- `DOTNET-NOTICE-METADATA.json`.

The process and update rules are documented in [Exact-version .NET notice generation](legal/DOTNET-NOTICE-GENERATION.md).

## 8. Generate the reviewed Microsoft redistribution notice

```powershell
.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This step:

- verifies that the SDK, runtime, target framework, RID and publish mode still match the reviewed configuration;
- verifies the reviewed template SHA-256;
- confirms that exact-version .NET notice metadata was generated first;
- inserts the current product and technical metadata into the reviewed template;
- writes `MICROSOFT-DOTNET-REDISTRIBUTION.txt` without unresolved placeholders.

A technical configuration or template change fails until `release/dotnet-redistribution-review.json` is updated as a deliberate review action.

## 9. Generate the SBOM and license report

```powershell
.\tools\generate-sbom.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This creates `DEPENDENCIES.md`, `SBOM.spdx.json` and `LICENSE-REPORT.json` from the evaluated release inputs and final staged files.

## 10. Inspect the publish directory

At minimum, the directory must contain:

```text
artifacts\win-x64\
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

Additional runtime files may be present depending on reviewed publish settings. See [the binary packaging standard](PACKAGING.md).

## 11. Run negative package checks

```powershell
.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The check proves that the validator rejects both a missing legal bundle and a generated redistribution notice with a deliberately stale runtime version.

## 12. Create and verify the final archive

Create the ZIP from the contents of the publish directory so the required files remain at the archive root:

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
    -DirectoryPath .\artifacts\win-x64 `
    -ArchivePath $archive `
    -ReportPath $report
```

The validation opens the final ZIP, checks the canonical manifest, compares the generated redistribution headers with canonical and reviewed metadata, verifies exact .NET notice metadata, and confirms SBOM/file coverage. Retain the compliance report with the archive.

## 13. Start the executable

```powershell
.\artifacts\win-x64\SightAdapt.exe
```

The application appears in the Windows notification area.

## 14. Verify the built version

While SightAdapt is running:

```powershell
$process = Get-Process SightAdapt

(Get-Item $process.Path).VersionInfo |
    Format-List ProductVersion, FileVersion
```

The expected product values and exact .NET release inputs are generated from the sources of truth in `Directory.Build.props`, `global.json`, the redistribution review record and the restore graph.

## Clean rebuild

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64.zip -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64-compliance.json -Force -ErrorAction SilentlyContinue

dotnet restore .\src\SightAdapt\SightAdapt.csproj

dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore `
    --output .\artifacts\win-x64

.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64

.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64

.\tools\generate-sbom.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

An official release must not be published if the reviewed redistribution configuration, exact-version notice generation, stale-notice negative test or final-package validation fails. Production and paid distribution remain blocked while the professional-review status is `not-obtained`.
