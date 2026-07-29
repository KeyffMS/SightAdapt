# SightAdapt dependency inventory

This repository copy documents the currently reviewed dependency set. Release builds regenerate `DEPENDENCIES.md` inside the final package from `release/dependency-policy.json`, the actual restore graph, exact .NET notice metadata, project references and workflow actions. The same evaluated inventory is written to `SBOM.spdx.json` and `LICENSE-REPORT.json`.

## Components shipped in the Windows x64 package

| Component | Reviewed version | Purpose | Distribution status | License or terms |
|---|---|---|---|---|
| SightAdapt application | `0.5.0.50-alpha` | Per-application visual accessibility and color correction | Shipped as `SightAdapt.exe` | MIT License; see `LICENSE.txt` |
| Microsoft .NET runtime | `8.0.29` | Managed runtime, host and base class libraries | Embedded in the self-contained application | Exact Microsoft license and notices; see `DOTNET-LICENSE-NOTICE.txt`, `THIRD-PARTY-NOTICES.txt` and `DOTNET-NOTICE-METADATA.json` |
| Microsoft Windows Desktop runtime | `8.0.29` | Windows Forms and Windows desktop runtime components | Embedded in the self-contained application | Exact Microsoft license and notices; see the generated package files |
| Runtime third-party components | mapped to the `.NET 8.0.29 / SDK 8.0.423` release train | Native and managed components included by the runtime packs | Embedded in the self-contained application | Exact legal text imported from the hash-verified official SDK archive and mapped to the actual restore graph |

The application project currently has no direct third-party NuGet `PackageReference` dependencies.

The restored release inventory must include:

- `Microsoft.NETCore.App.Runtime.win-x64/8.0.29`;
- `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29`.

The SDK-selected ASP.NET Core runtime pack is recorded when present in restore metadata, but it is not classified as shipped unless corresponding components appear in the final package inventory. The notice generator rejects an unreviewed runtime pack, a non-exact range or a version different from `8.0.29`.

## Operating-system components used but not redistributed

SightAdapt calls Windows APIs supplied by the operating system, including:

- Windows Forms platform services;
- Windows Magnification API;
- Win32 window, monitor, process and input APIs;
- Windows notification-area and shell services.

These components are required at runtime but are not copied into the SightAdapt package as independent libraries.

## Build and test dependencies not shipped

| Component | Current repository version | Purpose |
|---|---|---|
| .NET SDK | `8.0.423` | Restore, build, test, publish and authoritative legal-text source for the matching release train |
| Microsoft Windows SDK .NET reference package | `10.0.19041.56` | Compile-time Windows API references |
| Microsoft.NET.Test.Sdk | `17.11.1` | Test host |
| MSTest.TestAdapter | `3.6.4` | Test discovery and execution |
| MSTest.TestFramework | `3.6.4` | Test framework |
| GitHub Actions checkout | `actions/checkout@v4` | CI source checkout |
| GitHub Actions .NET setup | `actions/setup-dotnet@v4` | CI SDK installation |
| GitHub Actions artifact upload | `actions/upload-artifact@v4` | CI artifact publication |

Build and test dependencies are development infrastructure and are not part of the final SightAdapt binary archive unless explicitly marked as shipped by the generated inventory.

## Release maintenance

For every official binary release, maintainers must:

1. keep `global.json` and the expected .NET properties in `Directory.Build.props` synchronized;
2. restore and review the exact runtime-pack inventory in `project.assets.json`;
3. verify the official SDK ZIP SHA-512 and import its license and third-party notice files;
4. update `release/dependency-policy.json` for every dependency, version, supplier or license change;
5. generate and review `SBOM.spdx.json`, `LICENSE-REPORT.json` and the package `DEPENDENCIES.md`;
6. verify the final archive against `release/required-files.txt`.
