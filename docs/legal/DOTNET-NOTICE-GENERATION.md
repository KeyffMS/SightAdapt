# Exact-version .NET notice generation

This document defines the release process for Microsoft .NET license and third-party notice material included in a self-contained SightAdapt Windows package.

## Authoritative release inputs

The release inputs are intentionally pinned in two synchronized places:

- `global.json` pins the .NET SDK and disables roll-forward;
- `Directory.Build.props` records the SDK version, runtime version, runtime identifier, publish mode and official .NET release-metadata URL.

For the current release candidate:

| Input | Value |
|---|---|
| .NET SDK | `8.0.423` |
| .NET runtime | `8.0.29` |
| Windows Desktop Runtime | `8.0.29` |
| Runtime identifier | `win-x64` |
| Publish mode | self-contained single-file |

The official .NET 8 release metadata is obtained from:

`https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json`

The matching Microsoft release notes identify .NET Runtime `8.0.29` and SDK `8.0.423` as one release train:

`https://github.com/dotnet/core/blob/main/release-notes/8.0/8.0.29/8.0.29.md`

## Generator behavior

After `dotnet restore` and `dotnet publish`, run:

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The generator:

1. requires the exact SDK selected by `global.json`;
2. reads the restored package inventory from `src/SightAdapt/obj/project.assets.json`;
3. requires the pinned `Microsoft.NETCore.App.Runtime.win-x64` and `Microsoft.WindowsDesktop.App.Runtime.win-x64` packs;
4. fails if it finds a runtime or host package outside the reviewed mapping;
5. locates the exact Windows Desktop Runtime ZIP in Microsoft's release metadata;
6. downloads that official ZIP and verifies its published SHA-512 hash;
7. imports `LICENSE.txt` and `ThirdPartyNotices.txt` from the verified archive;
8. records the source URL, package checksum, imported-file checksums, versions, RID, publish mode and mapped runtime packs;
9. writes these package-root files:
   - `THIRD-PARTY-NOTICES.txt`;
   - `DOTNET-LICENSE-NOTICE.txt`;
   - `DOTNET-NOTICE-METADATA.json`.

The imported Microsoft text is not substantively rewritten. SightAdapt adds a metadata header so a recipient can identify the exact source used by the build.

## Archive validation

`tools/test-release-package.ps1` opens the final ZIP and verifies that:

- all required files are present and readable;
- the notice metadata matches `Directory.Build.props`;
- the source package and imported-file checksums are recorded;
- both required runtime packs are mapped;
- `THIRD-PARTY-NOTICES.txt` is an exact-version generated file rather than the repository baseline.

The workflow runs this check before artifact upload.

## Updating .NET

A .NET update must change all related inputs in one pull request:

1. update `global.json`;
2. update the .NET properties in `Directory.Build.props`;
3. confirm the official release metadata maps the selected runtime to the selected SDK;
4. run restore, publish and notice generation;
5. inspect `DOTNET-NOTICE-METADATA.json` and the imported notice text;
6. review any runtime package that is not already in the explicit mapping;
7. run the final-archive validation;
8. update this document's reviewed-version table.

Changing only one version causes release-metadata verification, generation or final-package validation to fail.

## Review triggers

Repeat the notice and redistribution review when any of these changes:

- .NET SDK or runtime patch;
- target framework;
- runtime identifier;
- self-contained/framework-dependent setting;
- single-file, trimming, ReadyToRun or Native AOT setting;
- runtime pack or native component mapping;
- official Microsoft licensing source or distribution terms.

Do not publish an official binary when the authoritative package cannot be downloaded, its hash does not match, its notice files are absent, or a restored runtime component is not mapped to reviewed notice material.
