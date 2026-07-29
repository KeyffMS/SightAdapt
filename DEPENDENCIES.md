# SightAdapt dependency inventory

This inventory distinguishes components shipped in a SightAdapt binary distribution from build, test and operating-system dependencies. The machine-readable exact release evidence is stored in `DOTNET-NOTICE-METADATA.json` inside the final package.

## Components shipped in the Windows x64 package

| Component | Reviewed version | Purpose | Distribution status | License or terms |
|---|---|---|---|---|
| SightAdapt application | `0.5.0.50-alpha` | Per-application visual accessibility and color correction | Shipped as `SightAdapt.exe` | MIT License; see `LICENSE.txt` |
| Microsoft .NET runtime | `8.0.29` | Managed runtime, host and base class libraries | Embedded in the self-contained application | Exact Microsoft license and notices; see `DOTNET-LICENSE-NOTICE.txt`, `THIRD-PARTY-NOTICES.txt` and `DOTNET-NOTICE-METADATA.json` |
| Microsoft Windows Desktop runtime | `8.0.29` | Windows Forms and Windows desktop runtime components | Embedded in the self-contained application | Exact Microsoft license and notices; see the generated package files |
| Runtime third-party components | from the verified `8.0.29` Windows Desktop Runtime archive | Native and managed components included by the runtime packs | Embedded in the self-contained application | Imported exact-version notices in `THIRD-PARTY-NOTICES.txt` |

The application project currently has no direct third-party NuGet `PackageReference` dependencies.

The restored release inventory must include:

- `Microsoft.NETCore.App.Runtime.win-x64/8.0.29`;
- `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29`.

The notice generator rejects an unreviewed runtime or host package.

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
| .NET SDK | `8.0.423` | Restore, build, test and publish |
| Microsoft.NET.Test.Sdk | `17.11.1` | Test host |
| MSTest.TestAdapter | `3.6.4` | Test discovery and execution |
| MSTest.TestFramework | `3.6.4` | Test framework |
| GitHub Actions checkout | `actions/checkout@v4` | CI source checkout |
| GitHub Actions .NET setup | `actions/setup-dotnet@v4` | CI SDK installation |
| GitHub Actions artifact upload | `actions/upload-artifact@v4` | CI artifact publication |

Build and test dependencies are development infrastructure and are not part of the final SightAdapt binary archive unless explicitly listed in the shipped-components table.

## Release maintenance

For every official binary release, maintainers must:

1. keep `global.json` and the .NET properties in `Directory.Build.props` synchronized;
2. restore and review the exact runtime-pack inventory;
3. generate license and notice material from the hash-verified official Windows Desktop Runtime archive;
4. review `DOTNET-NOTICE-METADATA.json` and any newly mapped runtime component;
5. verify the final archive against `release/required-files.txt`.
