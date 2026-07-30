# Microsoft .NET redistribution analysis

## Status

| Field | Value |
|---|---|
| Maintainer review record | `release/dotnet-redistribution-review.json` |
| Reviewed configuration | Exact SDK, runtime, target framework, runtime identifier and publish mode recorded in the review record |
| Reviewed notice wording | `release/MICROSOFT-DOTNET-REDISTRIBUTION.template.txt`, protected by the SHA-256 in the review record |
| Component evidence | Schema-2 `DOTNET-NOTICE-METADATA.json`, generated from `FilesToBundle`, restored packages and final loose binaries |
| Package notice | Generated as `MICROSOFT-DOTNET-REDISTRIBUTION.txt` from the reviewed template and canonical release metadata |
| Decision owner | KeyffMS / aiteracja.pl |
| External legal audit | Not planned |

This document records the project's internal technical and business-risk decision. It is not legal advice and does not claim legal clearance or review by outside counsel.

## Authoritative sources

The review uses:

1. official [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md);
2. official [.NET Library License Terms](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm);
3. exact .NET release metadata recorded in `Directory.Build.props`;
4. the hash-verified official SDK archive identified in `DOTNET-NOTICE-METADATA.json`;
5. the actual `project.assets.json` restore graph;
6. exact NuGet package SHA-512 and `.nuspec` license metadata for every package contributing published components;
7. MSBuild `PrepareForBundle` / `FilesToBundle` evidence and final loose-binary hashes.

The generated files preserve the exact evidence. This analysis paraphrases project treatment and does not replace the source terms.

## Published Microsoft-origin component inventory

The reviewed `0.5.0.50-alpha` build contains **452 mapped package components**:

| Exact package | Mapped components | Disposition | Notice treatment |
|---|---:|---|---|
| `Microsoft.NETCore.App.Runtime.win-x64/8.0.29` | 165 | Embedded in `SightAdapt.exe` | Exact official SDK `LICENSE.txt` and `ThirdPartyNotices.txt` for the same release train |
| `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29` | 285 | 280 embedded, 5 loose native DLLs | Exact official SDK `LICENSE.txt` and `ThirdPartyNotices.txt` for the same release train |
| `Microsoft.Windows.SDK.NET.Ref/10.0.19041.56` | 2 | Embedded in `SightAdapt.exe` | Exact package SHA-512, project policy license and `.nuspec` license URL recorded in `THIRD-PARTY-NOTICES.txt` and metadata |

The two Windows SDK package components are:

- `Microsoft.Windows.SDK.NET.dll`;
- `WinRT.Runtime.dll`.

They were previously described as build-reference-only. Publish-time `FilesToBundle` evidence proves that they are embedded in the final executable, so `release/dependency-policy.json` now classifies the package as `shipped-embedded`.

The five loose native files are mapped by exact SHA-256 to `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29`:

- `D3DCompiler_47_cor3.dll`;
- `PenImc_cor3.dll`;
- `PresentationNative_cor3.dll`;
- `vcruntime140_cor3.dll`;
- `wpfgfx_cor3.dll`.

`DOTNET-NOTICE-METADATA.json` is the source of truth for the complete component list, package-relative paths, dispositions and hashes. Absolute CI paths are not retained.

## Inputs not redistributed as product components

| Component | Role | Distribution status |
|---|---|---|
| Pinned .NET SDK | Restore, compile, test and publish; source of matching legal text | Build infrastructure only |
| Test SDK/adapter/framework | Automated testing | Test-only |
| GitHub Actions | CI infrastructure | Not shipped |
| Windows system APIs/DLLs not copied by publish | Operating-system services | Supplied by Windows |

## Maintainer redistribution controls

For the reviewed configuration:

1. Microsoft components are distributed in object-code form only as part of SightAdapt.
2. Every package contributing a published component has exact identity, SHA-512, policy license and source metadata.
3. Every embedded or loose component has package-relative identity and SHA-256 evidence.
4. The package preserves exact official .NET license/notice material plus package-specific notice sections where required.
5. SightAdapt's MIT License applies only to SightAdapt project code.
6. Recipients receive the complete package-root legal bundle.
7. Downstream packages must preserve the legal bundle and must not imply Microsoft sponsorship or endorsement.
8. An unreviewed package, unmapped bundled component or unmapped loose binary blocks publication.

## Package implementation

Every binary package includes:

| File | Function |
|---|---|
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated maintainer-reviewed redistribution notice |
| `DOTNET-LICENSE-NOTICE.txt` | Exact Microsoft .NET license text from the hash-verified SDK archive |
| `THIRD-PARTY-NOTICES.txt` | Exact .NET notices plus exact package-specific sections |
| `DOTNET-NOTICE-METADATA.json` | Exact package/component identities, sources and checksums |
| `DEPENDENCIES.md` | Human-readable dependency inventory |

Generation and validation are separated:

- `generate-dotnet-notices.ps1` imports exact official .NET legal text;
- `generate-dotnet-component-coverage.ps1` creates component-level package and notice mappings;
- `generate-dotnet-redistribution-notice.ps1` renders the reviewed redistribution summary;
- `test-release-package.ps1` validates general package/notice consistency;
- `verify-dotnet-component-coverage.ps1` validates every package/component mapping in the final ZIP.

## Review triggers

Repeat the maintainer review when any of these change:

- SDK, runtime, Windows Desktop Runtime, TFM, RID or architecture;
- self-contained, single-file, trimming, ReadyToRun, Native AOT or extraction settings;
- restored packages or `FilesToBundle` inventory;
- loose native binaries;
- package license metadata or policy classification;
- distribution channels or Microsoft compatibility claims;
- Microsoft licensing sources or reviewed notice wording.

The negative package checks prove that incomplete, stale and deliberately unmapped runtime packages are rejected.

## Decision

The maintainers accept the documented redistribution approach for the recorded configuration and current distribution. No external legal audit is required or planned.

This decision does not assert legal clearance. Maintainers must update the evidence and consciously accept, condition or block the release whenever a review trigger changes.
