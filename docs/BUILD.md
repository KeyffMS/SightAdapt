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

## 3. Verify release metadata

```powershell
.\tools\verify-release-metadata.ps1
```

This check verifies the synchronized product, expected SDK/runtime, RID and publish-mode inputs.

## 4. Restore dependencies

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj
dotnet restore .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj
```

The application restore creates the authoritative runtime-pack inventory used by notice generation. The generator rejects runtime-pack versions that differ from `Directory.Build.props`.

## 5. Run the tests

```powershell
dotnet test .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj `
    --configuration Release `
    --no-restore
```

All tests must pass before publication.

## 6. Publish a self-contained single-file executable

The runtime identifier, self-contained mode and single-file setting are defined by `Directory.Build.props` and `SightAdapt.csproj`. The exact runtime patch is selected by the pinned SDK and verified against the restored `project.assets.json` before packaging.

```powershell
dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore `
    --output .\artifacts\win-x64
```

The project copies the repository legal-document baseline into the publish directory.

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

## 8. Inspect the publish directory

At minimum, the directory must contain:

```text
artifacts\win-x64\
├── SightAdapt.exe
├── LICENSE.txt
├── THIRD-PARTY-NOTICES.txt
├── DOTNET-LICENSE-NOTICE.txt
├── DOTNET-NOTICE-METADATA.json
├── DEPENDENCIES.md
└── PRIVACY.md
```

Additional runtime files may be present depending on reviewed publish settings. See [the binary packaging standard](PACKAGING.md).

## 9. Create and verify the final archive

Create the ZIP from the contents of the publish directory so the required files remain at the archive root:

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'

Remove-Item $archive -Force -ErrorAction SilentlyContinue
Compress-Archive `
    -Path '.\artifacts\win-x64\*' `
    -DestinationPath $archive `
    -CompressionLevel Optimal

.\tools\test-release-package.ps1 -ArchivePath $archive
```

The validation script opens the final ZIP, checks the canonical manifest, verifies notice metadata against the pinned release inputs and confirms that the runtime packs are mapped.

## 10. Start the executable

```powershell
.\artifacts\win-x64\SightAdapt.exe
```

The application appears in the Windows notification area.

## 11. Verify the built version

While SightAdapt is running:

```powershell
$process = Get-Process SightAdapt

(Get-Item $process.Path).VersionInfo |
    Format-List ProductVersion, FileVersion
```

The expected product values and exact .NET release inputs are generated from the sources of truth in `Directory.Build.props`, `global.json` and the restore graph.

## Clean rebuild

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64.zip -Force -ErrorAction SilentlyContinue

dotnet restore .\src\SightAdapt\SightAdapt.csproj

dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --no-restore `
    --output .\artifacts\win-x64

.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

An official release must not be published if exact-version notice generation or final-archive validation fails.
