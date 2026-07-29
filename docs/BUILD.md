# Build and package SightAdapt as a standalone EXE

These steps create a self-contained Windows x64 executable and a verified ZIP archive. The published application is started directly as `SightAdapt.exe`; `dotnet run` is not required.

## 1. Install prerequisites

Use a 64-bit Windows 10 or Windows 11 computer and install one of:

- the .NET 8 SDK; or
- Visual Studio with the **.NET desktop development** workload.

Verify the SDK:

```powershell
dotnet --version
```

The displayed version should begin with `8.`. Record the exact version when producing an official release.

## 2. Clone the repository

```powershell
git clone https://github.com/KeyffMS/SightAdapt.git
cd SightAdapt
```

## 3. Restore dependencies

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj
dotnet restore .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj
```

## 4. Run the tests

```powershell
dotnet test .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj `
    --configuration Release `
    --no-restore
```

All tests must pass before publication.

## 5. Publish a self-contained single-file executable

```powershell
dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    --output .\artifacts\win-x64
```

The project automatically copies the required legal-document bundle into the publish directory.

## 6. Inspect the publish directory

At minimum, the directory must contain:

```text
artifacts\win-x64\
├── SightAdapt.exe
├── LICENSE.txt
├── THIRD-PARTY-NOTICES.txt
├── DOTNET-LICENSE-NOTICE.txt
├── DEPENDENCIES.md
└── PRIVACY.md
```

Additional runtime files may be present depending on the .NET SDK, runtime patch and publish settings. See [the binary packaging standard](PACKAGING.md).

## 7. Create and verify the final archive

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

The validation script opens the final ZIP and checks the canonical manifest in `release/required-files.txt`. It does not only inspect repository files or the staging directory.

## 8. Start the executable

```powershell
.\artifacts\win-x64\SightAdapt.exe
```

The application appears in the Windows notification area.

## 9. Verify the built version

While SightAdapt is running:

```powershell
$process = Get-Process SightAdapt

(Get-Item $process.Path).VersionInfo |
    Format-List ProductVersion, FileVersion
```

The expected values are generated from the single source of truth in `Directory.Build.props`:

```text
SightAdaptProductVersion
SightAdaptFileVersion
SightAdaptInformationalVersion
SightAdaptAssemblyVersion
```

`SightAdapt.csproj`, the executable metadata, CI artifact names, and the generated README version block consume or verify these properties rather than maintaining independent version numbers.

## Clean rebuild

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item .\artifacts\SightAdapt-*-win-x64.zip -Force -ErrorAction SilentlyContinue

dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    --output .\artifacts\win-x64
```

An official release must not be published until the exact .NET runtime notices have been generated and reviewed for that build.
