# Exact-version .NET notice generation

This document defines the Microsoft .NET and package notice evidence included in a self-contained SightAdapt Windows package.

## Authoritative inputs

- `global.json` pins the .NET SDK and disables roll-forward;
- `Directory.Build.props` records SDK, runtime, RID, publish mode and release-metadata URL;
- `project.assets.json` records exact runtime packs selected by restore;
- MSBuild `PrepareForBundle` and `FilesToBundle` provide the exact inputs embedded in the single-file executable;
- `release/dependency-policy.json` records reviewed package versions, shipped status and license treatment;
- `release/dotnet-redistribution-review.json` records the maintainer-reviewed release configuration and wording.

## Stage 1: exact official .NET notice import

After restore and publish, run:

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This stage:

- requires the exact pinned SDK;
- validates exact runtime-pack versions from the restore graph;
- confirms the SDK/runtime release mapping through official Microsoft metadata;
- downloads the matching official SDK ZIP;
- verifies its published SHA-512;
- imports `LICENSE.txt` and `ThirdPartyNotices.txt` without substantive modification;
- writes base `DOTNET-NOTICE-METADATA.json` schema 1.

## Stage 2: exact published-component coverage

`SightAdapt.csproj` captures the SDK-generated `FilesToBundle` list during single-file publication. Then run:

```powershell
.\tools\generate-dotnet-component-coverage.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This stage:

1. identifies every NuGet package contributing an embedded component;
2. requires a reviewed dependency-policy entry and correct shipped classification;
3. records exact NuGet package SHA-512 and repository metadata;
4. reads exact `.nuspec` license metadata;
5. maps every embedded package asset with package-relative path and SHA-256;
6. maps every loose binary by filename and SHA-256 to an exact restored package asset;
7. adds package-specific sections to `THIRD-PARTY-NOTICES.txt` when components are not covered by the official .NET release bundle;
8. upgrades `DOTNET-NOTICE-METADATA.json` to schema 2 with package, notice-mapping and component inventories;
9. fails when a package is unreviewed, marked non-shipped or cannot be mapped.

The temporary `FilesToBundle` manifest may contain absolute build paths. Only its SHA-256 and sanitized component records are retained in the release metadata.

## Current component evidence

The reviewed alpha build maps 452 package components across three exact packages:

| Package | Components | Notice mapping |
|---|---:|---|
| `Microsoft.NETCore.App.Runtime.win-x64/8.0.29` | 165 | Official exact-release .NET license and notices |
| `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29` | 285 | Official exact-release .NET license and notices |
| `Microsoft.Windows.SDK.NET.Ref/10.0.19041.56` | 2 | Exact package SHA-512, policy license and `.nuspec` license URL |

The inventory contains 447 embedded components and 5 loose native DLLs. `unmappedExternalComponentCount` must remain zero.

## Stage 3: maintainer redistribution summary

Run:

```powershell
.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This renders `MICROSOFT-DOTNET-REDISTRIBUTION.txt` after validating the reviewed SDK/runtime/TFM/RID/publish configuration, template SHA-256, maintainer decision and exact-version metadata.

## Final-package validation

Two independent validators run on the final ZIP:

- `test-release-package.ps1` checks package completeness, official notice identity and redistribution headers;
- `verify-dotnet-component-coverage.ps1` checks schema-2 component evidence, package hashes, notice mappings, all loose binary SHA-256 values and absence of unmapped binaries.

The negative package test proves rejection of:

- an incomplete package;
- stale redistribution wording;
- a package where one real loose binary remains but its component mapping is removed.

## Maintenance

Repeat generation and review when SDK/runtime, TFM, RID, publish settings, restored packages, `FilesToBundle`, loose binaries, package license metadata or Microsoft legal sources change.

Do not publish when official-text import, package policy, package hashes, component mapping, notice mapping or final ZIP validation fails.
