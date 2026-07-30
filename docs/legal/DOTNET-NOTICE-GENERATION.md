# Exact-version .NET notice generation

This document defines the Microsoft .NET license and third-party notice evidence included in a self-contained SightAdapt Windows package.

## Authoritative inputs

- `global.json` pins the .NET SDK and disables roll-forward;
- `Directory.Build.props` records SDK, runtime, RID, publish mode and release-metadata URL;
- `project.assets.json` records exact runtime packs selected by restore;
- MSBuild `PrepareForBundle` and `FilesToBundle` provide the exact inputs embedded in the single-file executable;
- `release/dotnet-redistribution-review.json` records the maintainer-reviewed configuration and redistribution wording.

Current reviewed inputs:

| Input | Value |
|---|---|
| .NET SDK | `8.0.423` |
| .NET runtime | `8.0.29` |
| Windows Desktop Runtime | `8.0.29` |
| RID | `win-x64` |
| Publish mode | self-contained single-file |

## Publish-time component inventory

`SightAdapt.csproj` runs `CaptureSightAdaptFilesToBundle` between `PrepareForBundle` and `GenerateSingleFileBundle`. It writes `artifacts/dotnet-files-to-bundle.tsv` from the SDK-provided `FilesToBundle` item list.

The temporary manifest is build evidence only. Absolute build-machine paths are not copied into the release archive.

After publish, `tools/generate-dotnet-notices.ps1`:

1. resolves each exact runtime pack in the restored NuGet cache;
2. verifies its NuGet SHA-512 evidence and records repository metadata when present;
3. maps every `FilesToBundle` entry originating from a runtime pack;
4. records the bundle-relative output path, package asset path, component kind and SHA-256;
5. scans loose binary files in the final publish directory;
6. matches each loose runtime binary by filename and SHA-256 to an exact runtime-pack asset;
7. fails when a bundled package-cache file or loose binary cannot be mapped to a reviewed runtime pack.

This covers both components embedded in `SightAdapt.exe` and separately shipped native libraries.

## Official notice material

The generator confirms the SDK/runtime release mapping through Microsoft's release metadata, downloads the matching official SDK ZIP, verifies its published SHA-512 and imports:

- `LICENSE.txt` into `DOTNET-LICENSE-NOTICE.txt`;
- `ThirdPartyNotices.txt` into `THIRD-PARTY-NOTICES.txt`.

The imported texts are not substantively modified. SightAdapt adds an exact-build header and points recipients to `DOTNET-NOTICE-METADATA.json` for component-level coverage evidence.

## Metadata schema

`DOTNET-NOTICE-METADATA.json` schema 2 records:

- product, SDK, runtime, RID and publish mode;
- exact runtime-pack identities and NuGet SHA-512 values;
- official release/package URLs and imported notice hashes;
- SHA-256 of the `FilesToBundle` manifest;
- embedded and loose runtime-component counts;
- one entry per mapped runtime component containing disposition, output path, runtime pack, package asset path, kind and SHA-256;
- `unmappedExternalComponentCount`, which must be zero.

All runtime components use the mapping identifier `exact-release-dotnet-bundle`, tied to the imported exact-release license and third-party notice files.

## Final-package validation

`tools/test-release-package.ps1` independently verifies that:

- metadata uses schema 2 and matches canonical release values;
- both required runtime packs have published components and NuGet SHA-512 evidence;
- component totals match the detailed inventory;
- every loose binary in the ZIP has a component mapping and matching SHA-256;
- embedded runtime components are represented by the captured `FilesToBundle` evidence;
- no external component is reported as unmapped;
- notice-mapping hashes equal the imported official-text hashes.

The negative test removes one real loose runtime mapping while leaving the binary in the package. CI must reject that archive, in addition to rejecting incomplete and stale-notice packages.

## Maintenance

A .NET or publish change must update the pinned inputs and maintainer review, then regenerate all evidence. Repeat the review when any of these change:

- SDK or runtime version;
- target framework, RID or architecture;
- self-contained, single-file, trimming, ReadyToRun, Native AOT or extraction settings;
- restored runtime packs;
- bundled or loose runtime-component inventory;
- Microsoft release metadata, license or notice source.

Do not publish when package hashes, runtime-pack mapping, `FilesToBundle` capture, loose-binary matching, official notice import or final ZIP validation fails.
