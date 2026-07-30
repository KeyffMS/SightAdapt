# Exact-version .NET notice generation

This document defines the release process for Microsoft .NET license and third-party notice material included in a self-contained SightAdapt Windows package.

## Authoritative release inputs

The release inputs are intentionally pinned and reviewed in synchronized sources:

- `global.json` pins the .NET SDK and disables roll-forward;
- `Directory.Build.props` records the expected SDK version, runtime version, runtime identifier, publish mode and official .NET release-metadata URL;
- `release/dotnet-redistribution-review.json` records the SDK/runtime/TFM/RID/publish configuration covered by the redistribution analysis and the reviewed package-notice template checksum.

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

## Two authoritative evidence sources

The exact-version generator deliberately uses separate sources for separate facts:

1. `src/SightAdapt/obj/project.assets.json`, produced by the actual restore, identifies the exact runtime packs selected for the release build;
2. the official, hash-verified `.NET SDK 8.0.423 win-x64` ZIP supplies Microsoft's authoritative `LICENSE.txt` and `ThirdPartyNotices.txt` for that same release train.

The standalone Windows Desktop Runtime ZIP does not contain those two legal files. The matching SDK archive is therefore used as the legal-text source, while the actual application's restore graph remains the authority for which runtime packs and versions are associated with the SightAdapt build.

## Exact-version generator behavior

After `dotnet restore` and `dotnet publish`, run:

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The generator:

1. requires the exact SDK selected by `global.json`;
2. reads `project.frameworks.*.downloadDependencies` from the restored `project.assets.json`;
3. requires exact-version entries for `Microsoft.NETCore.App.Runtime.win-x64` and `Microsoft.WindowsDesktop.App.Runtime.win-x64`;
4. records the SDK-selected ASP.NET Core runtime pack when present, but does not claim that ASP.NET components are shipped unless they appear in the final package inventory;
5. fails if a runtime pack is outside the reviewed mapping, uses a non-exact version range or differs from the pinned runtime version;
6. confirms that Microsoft's release metadata maps runtime `8.0.29`, Windows Desktop Runtime `8.0.29` and SDK `8.0.423` to the same release;
7. locates the exact official SDK ZIP for `win-x64`;
8. downloads that SDK ZIP and verifies its published SHA-512 hash;
9. imports `LICENSE.txt` and `ThirdPartyNotices.txt` from the verified SDK archive;
10. records the source URL, package checksum, imported-file checksums, versions, RID, publish mode, restore framework and mapped runtime packs;
11. writes these package-root files:
    - `THIRD-PARTY-NOTICES.txt`;
    - `DOTNET-LICENSE-NOTICE.txt`;
    - `DOTNET-NOTICE-METADATA.json`.

The imported Microsoft text is not substantively rewritten. SightAdapt adds a metadata header so a recipient can identify the exact source and release mapping used by the build.

## Reviewed redistribution notice

After exact-version metadata exists, run:

```powershell
.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

This generator does not invent separate legal text. It renders the reviewed wording from `release/MICROSOFT-DOTNET-REDISTRIBUTION.template.txt` and inserts canonical release metadata. Before writing `MICROSOFT-DOTNET-REDISTRIBUTION.txt`, it verifies:

- the reviewed SDK/runtime/TFM/RID/publish configuration;
- the reviewed template SHA-256;
- the professional-review status and Issue #93 linkage;
- the previously generated exact-version metadata for the same artifact.

Product-version updates are rendered automatically. A change to SDK, runtime, target framework, RID, publish mode or reviewed wording fails until the review record is deliberately updated.

## Archive validation

`tools/test-release-package.ps1` opens the final ZIP and verifies that:

- all required files are present and readable;
- the notice metadata matches `Directory.Build.props`;
- the source package and imported-file checksums are recorded;
- both required runtime packs are mapped at the pinned version;
- `THIRD-PARTY-NOTICES.txt` is an exact-version generated file rather than the repository baseline;
- the generated redistribution notice headers match the product, SDK, runtime, TFM, RID, publish mode, review date and professional-review state.

The workflow runs this check before artifact upload. CI diagnostics also retain `project.assets.json` so the restore graph used by the generator can be reviewed after the run.

## Updating .NET

A .NET update must change all related inputs in one pull request:

1. update `global.json`;
2. update the expected .NET properties in `Directory.Build.props`;
3. confirm that official release metadata maps the selected runtime and Windows Desktop Runtime to the selected SDK;
4. update `release/dotnet-redistribution-review.json` after reviewing the new SDK/runtime/TFM/RID/publish configuration;
5. run restore, publish, exact-version notice generation and reviewed redistribution-notice generation;
6. inspect the restored `downloadDependencies`, `DOTNET-NOTICE-METADATA.json`, generated redistribution notice and imported notice text;
7. review any runtime pack that is not already in the explicit mapping;
8. run the stale-notice negative test and final-package validation;
9. update this document's reviewed-version table.

Changing only one version causes release-metadata verification, generation or final-package validation to fail.

## Review triggers

Repeat the notice and redistribution review when any of these changes:

- .NET SDK or runtime patch;
- target framework;
- runtime identifier;
- self-contained/framework-dependent setting;
- single-file, trimming, ReadyToRun or Native AOT setting;
- runtime pack or native component mapping;
- official Microsoft licensing source or distribution terms;
- reviewed redistribution wording.

Do not publish an official binary when the authoritative package cannot be downloaded, its hash does not match, its notice files are absent, the redistribution review record is stale, or a restored runtime component is not mapped to reviewed notice material.
