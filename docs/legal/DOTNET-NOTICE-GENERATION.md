# Exact-version .NET notice generation

This document defines the release process for Microsoft .NET license and third-party notice material included in a self-contained SightAdapt Windows package.

## Authoritative release inputs

- `global.json` pins the .NET SDK and disables roll-forward;
- `Directory.Build.props` records expected SDK/runtime/RID/publish metadata and the official release-metadata URL;
- `release/dotnet-redistribution-review.json` records the reviewed SDK/runtime/TFM/RID/publish configuration, notice-template SHA-256 and maintainer decision.

Current inputs:

| Input | Value |
|---|---|
| .NET SDK | `8.0.423` |
| .NET runtime | `8.0.29` |
| Windows Desktop Runtime | `8.0.29` |
| RID | `win-x64` |
| Publish mode | self-contained single-file |

## Evidence sources

1. `project.assets.json` identifies exact runtime packs selected by restore.
2. The official, hash-verified matching SDK ZIP supplies Microsoft's `LICENSE.txt` and `ThirdPartyNotices.txt`.

The restore graph identifies selected packs; the SDK archive supplies the authoritative legal text for the same release train.

## Exact-version generator

Run after restore and publish:

```powershell
.\tools\generate-dotnet-notices.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

The generator:

- requires the exact pinned SDK;
- reads exact runtime-pack versions from the restore graph;
- rejects unreviewed or mismatched packs;
- confirms the SDK/runtime release mapping;
- downloads the official SDK ZIP and verifies SHA-512;
- imports `LICENSE.txt` and `ThirdPartyNotices.txt`;
- records URLs, versions, packs and checksums;
- writes `THIRD-PARTY-NOTICES.txt`, `DOTNET-LICENSE-NOTICE.txt` and `DOTNET-NOTICE-METADATA.json`.

## Maintainer-reviewed redistribution notice

Then run:

```powershell
.\tools\generate-dotnet-redistribution-notice.ps1 `
    -PublishDirectory .\artifacts\win-x64
```

It renders `release/MICROSOFT-DOTNET-REDISTRIBUTION.template.txt` using canonical release metadata. Before writing the package notice, it verifies:

- reviewed SDK/runtime/TFM/RID/publish values;
- reviewed template SHA-256;
- maintainer decision status, owner and issue record;
- exact-version metadata for the same artifact.

A `blocked` maintainer decision fails packaging. No external legal audit is required or planned.

## Archive validation

`tools/test-release-package.ps1` verifies:

- required files and UTF-8 readability;
- exact notice metadata and checksums;
- required runtime-pack mapping;
- generated third-party notice identity;
- redistribution notice headers for product, SDK, runtime, TFM, RID, publish mode, review date and maintainer decision.

CI runs the validator before artifact upload and retains `project.assets.json` in diagnostics.

## Updating .NET

A .NET update must:

1. update `global.json` and `Directory.Build.props`;
2. confirm the official SDK/runtime mapping;
3. update `release/dotnet-redistribution-review.json` after maintainer review;
4. regenerate exact-version and redistribution notices;
5. inspect restore dependencies, metadata and generated text;
6. review new runtime packs;
7. run stale-notice negative validation and final-package validation;
8. update reviewed-version documentation.

## Review triggers

Repeat the maintainer review when SDK/runtime, TFM, RID, publish settings, runtime packs, Microsoft licensing sources or redistribution wording change.

Do not publish when the authoritative package cannot be verified, notice files are absent, the review record is stale, the maintainer decision is blocked or a runtime component is not mapped to reviewed notice material.
