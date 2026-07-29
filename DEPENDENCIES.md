# SightAdapt dependency inventory

This inventory distinguishes components shipped in a SightAdapt binary distribution from build, test and operating-system dependencies. Exact versions for an official release must be recorded by the release process.

## Components shipped in the Windows x64 package

| Component | Purpose | Distribution status | License or terms |
|---|---|---|---|
| SightAdapt application | Per-application visual accessibility and color correction | Shipped as `SightAdapt.exe` | MIT License; see `LICENSE.txt` |
| Microsoft .NET 8 runtime | Managed runtime and base class libraries for the self-contained application | Embedded in or published with the application | Microsoft .NET Library License on Windows; see `DOTNET-LICENSE-NOTICE.txt` |
| Microsoft Windows Desktop runtime | Windows Forms and Windows desktop runtime components | Embedded in or published with the application | Microsoft .NET Library License and component notices; see `DOTNET-LICENSE-NOTICE.txt` and `THIRD-PARTY-NOTICES.txt` |
| Runtime third-party components | Native and managed components included by the selected .NET runtime packs | May be embedded in or published with the application | Exact-version notices must be generated from authoritative .NET notice material |

The application project currently has no direct third-party NuGet `PackageReference` dependencies.

## Operating-system components used but not redistributed

SightAdapt calls Windows APIs supplied by the operating system, including:

- Windows Forms platform services;
- Windows Magnification API;
- Win32 window, monitor, process and input APIs;
- Windows notification-area and shell services.

These components are required at runtime but are not copied into the SightAdapt package as independent libraries.

## Build and test dependencies not shipped

| Component | Current repository version or range | Purpose |
|---|---|---|
| .NET SDK | `8.0.x` in CI | Restore, build, test and publish |
| Microsoft.NET.Test.Sdk | `17.11.1` | Test host |
| MSTest.TestAdapter | `3.6.4` | Test discovery and execution |
| MSTest.TestFramework | `3.6.4` | Test framework |
| GitHub Actions checkout | `actions/checkout@v4` | CI source checkout |
| GitHub Actions .NET setup | `actions/setup-dotnet@v4` | CI SDK installation |
| GitHub Actions artifact upload | `actions/upload-artifact@v4` | CI artifact publication |

Build and test dependencies are development infrastructure and are not part of the final SightAdapt binary archive unless explicitly listed in the shipped-components table.

## Release maintenance

For every official binary release, maintainers must:

1. record the exact .NET SDK and runtime-pack versions selected by the build;
2. review the files in the final publish directory and archive;
3. refresh exact-version third-party notices from official Microsoft/.NET sources;
4. verify that all files listed in `release/required-files.txt` are present and non-empty in the final archive.
