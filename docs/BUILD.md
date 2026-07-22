# Build SightAdapt as a standalone EXE

These steps create a self-contained Windows x64 executable. The published application is started directly as `SightAdapt.exe`; `dotnet run` is not required.

## 1. Install prerequisites

Use a 64-bit Windows 10 or Windows 11 computer and install one of:

- the .NET 8 SDK; or
- Visual Studio with the **.NET desktop development** workload.

Verify the SDK:

```powershell
dotnet --version
```

The displayed version should begin with `8.`.

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

## 6. Start the executable

```powershell
.\artifacts\win-x64\SightAdapt.exe
```

The application appears in the Windows notification area.

## 7. Verify the built version

While SightAdapt is running:

```powershell
$process = Get-Process SightAdapt

(Get-Item $process.Path).VersionInfo |
    Format-List ProductVersion, FileVersion
```

Expected version prefix:

```text
ProductVersion: 0.5.0-alpha.1+<commit>
FileVersion:    0.5.0.0
```

The commit suffix identifies the exact source revision.

## Output directory

The publication directory contains `SightAdapt.exe` and any files required by the selected .NET publication mode:

```text
artifacts\win-x64\
```

To rebuild from a clean state:

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    --output .\artifacts\win-x64
```
