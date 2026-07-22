# Contributing to SightAdapt

SightAdapt is in an alpha stage. Contributions should prioritize user safety, predictable behavior, and Windows 10/11 compatibility.

## Development setup

- Windows 10 or Windows 11
- Visual Studio with .NET desktop development, or the .NET 8 SDK
- x64 target

Build the application with:

```powershell
dotnet build src/SightAdapt/SightAdapt.csproj -c Release
```

Create a self-contained executable with the steps in [docs/BUILD.md](docs/BUILD.md).

## Pull requests

- keep changes focused;
- describe Windows versions and DPI configurations tested;
- do not add DLL injection, kernel drivers, or screen-content telemetry;
- preserve the emergency overlay shutdown path;
- document known limitations;
- include manual test steps for visual or input-related changes.

## Bug reports

Include:

- Windows version;
- display scaling and monitor arrangement;
- target application and framework, when known;
- whether the target application was elevated;
- exact steps to reproduce;
- whether the emergency tray command still worked.

Do not attach screenshots containing private information.
