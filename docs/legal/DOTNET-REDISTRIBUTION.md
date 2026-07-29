# Microsoft .NET redistribution analysis

## Status

| Field | Value |
|---|---|
| Maintainer review date | 2026-07-29 |
| SightAdapt version | `0.5.0.50-alpha` |
| .NET SDK | `8.0.423` |
| .NET Runtime | `8.0.29` |
| Windows Desktop Runtime | `8.0.29` |
| Target framework | `net8.0-windows10.0.19041.0` |
| Runtime identifier | `win-x64` |
| Publish mode | self-contained, single-file |
| Professional legal review | Required before production or paid distribution under Issue #93; not yet claimed by this document |

This document records the project's technical redistribution analysis. It is not legal advice and is not evidence of review by qualified legal counsel.

## Authoritative sources

The review uses these official Microsoft sources:

1. [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md), which states that Windows product distributions and runtime packs use the .NET Library License and that self-contained applications embed parts of the runtime;
2. [.NET Library License Terms](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm), which contains the applicable distribution rights, requirements, restrictions, warranty terms and regional qualifications;
3. the exact [.NET 8 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/8.0/releases.json), which maps runtime `8.0.29`, Windows Desktop Runtime `8.0.29` and SDK `8.0.423`;
4. the hash-verified official SDK archive identified in each package's `DOTNET-NOTICE-METADATA.json`, which supplies the exact imported Microsoft license and third-party notice text;
5. the actual `project.assets.json` produced by restore, which identifies the runtime packs selected for the release build.

The generated files in the final package preserve the authoritative text and evidence. This analysis paraphrases operational requirements and does not replace those files.

## Microsoft-origin component inventory

### Included in the self-contained binary

The current application has no direct third-party NuGet package references. The Microsoft-origin runtime content is selected by the SDK through these exact runtime-pack identities:

| Component family | Exact release identity | Included purpose | Distribution position |
|---|---|---|---|
| .NET application host and host policy | `Microsoft.NETCore.App.Runtime.win-x64/8.0.29` and associated host assets | Starts and hosts the self-contained managed application | Distributed in object-code form only as part of SightAdapt under the .NET Library License |
| .NET managed runtime and base libraries | `Microsoft.NETCore.App.Runtime.win-x64/8.0.29` | CLR, garbage collector, framework libraries and native runtime support | Distributed in object-code form only as part of SightAdapt under the .NET Library License and imported third-party notices |
| Windows Desktop / Windows Forms runtime | `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29` | Windows Forms UI and Windows desktop framework support | Distributed in object-code form only as part of SightAdapt under the .NET Library License and imported third-party notices |
| Runtime third-party components | Components covered by the official `ThirdPartyNotices.txt` for the `.NET 8.0.29 / SDK 8.0.423` release train | Native and managed dependencies incorporated into the Microsoft runtime packs | Governed by the separate licenses and notices preserved in `THIRD-PARTY-NOTICES.txt` |

The precise restore identities are recorded in `DOTNET-NOTICE-METADATA.json`. The single-file publisher may embed managed assemblies and native components inside `SightAdapt.exe`; embedding does not change their separate licensing status.

The SDK may record `Microsoft.AspNetCore.App.Runtime.win-x64/8.0.29` as a framework download dependency. SightAdapt does not use ASP.NET Core APIs. That restore record is preserved for auditability, but a component is classified as shipped only when the final package/SBOM identifies it as present.

### Microsoft-origin dependencies not redistributed as product components

| Component | Role | Distribution status |
|---|---|---|
| .NET SDK `8.0.423` | Restore, compile, test and publish; source of matching legal-text bundle | Build infrastructure, not shipped as the SightAdapt application |
| `Microsoft.Windows.SDK.NET.Ref` reference pack | Compile-time Windows API metadata | Reference/build input, not shipped as an independent runtime component |
| `Microsoft.NET.Test.Sdk`, MSTest adapter/framework | Automated test infrastructure | Test-only dependencies, not shipped |
| GitHub Actions maintained by GitHub/Microsoft | CI checkout, SDK setup and artifact upload | CI infrastructure, not shipped in the application |
| Windows operating-system APIs and system DLLs | Magnification, window management, shell, monitor, input and process services | Supplied by Windows; referenced at runtime but not copied into the package as SightAdapt redistributables |

## Applicable terms and redistribution conditions

Microsoft's Windows .NET product distributions and runtime packs use the .NET Library License. For the reviewed configuration, the project adopts these controls:

1. Microsoft components are distributed in object-code form only as part of SightAdapt, never as a standalone .NET distribution.
2. The package preserves Microsoft's license text, the exact-version third-party notices and machine-readable source/checksum evidence.
3. The SightAdapt MIT License is clearly limited to SightAdapt project code. It does not purport to relicense Microsoft components.
4. Recipients and downstream distributors receive `MICROSOFT-DOTNET-REDISTRIBUTION.txt`, containing conditions intended to protect Microsoft distributable code at least as much as the applicable Microsoft agreement.
5. Downstream redistribution must preserve the complete legal bundle and keep Microsoft components within an intact application distribution.
6. The project does not use Microsoft trademarks in the application name and does not imply Microsoft sponsorship, certification or endorsement.
7. The project does not modify Microsoft distributable source code or subject Microsoft code to a reciprocal or source-disclosure license.
8. Recipients may not remove notices, bypass technical limitations, redistribute the Microsoft components standalone or use them unlawfully.
9. Distributors are responsible for claims arising from their own application distribution except where a claim is based solely on unmodified Microsoft distributable code, reflecting the distributor responsibility in Microsoft's terms.
10. Applicable export-control obligations and non-waivable local consumer rights remain in force.
11. Microsoft components are provided under Microsoft's as-is, support, warranty and remedy provisions, subject to mandatory regional law.

## Package implementation

Every SightAdapt binary package must include these separate files at its root:

| File | Function |
|---|---|
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Operational end-user and downstream-distributor notice for Microsoft components |
| `DOTNET-LICENSE-NOTICE.txt` | Exact Microsoft license text imported from the hash-verified official SDK archive |
| `THIRD-PARTY-NOTICES.txt` | Exact Microsoft/.NET third-party notices imported from the same archive |
| `DOTNET-NOTICE-METADATA.json` | Exact versions, runtime packs, source URLs and checksums |
| `DEPENDENCIES.md` | Human-readable shipped/build/platform dependency inventory |

The files are readable without executing SightAdapt. `release/required-files.txt` and `tools/test-release-package.ps1` enforce their presence and consistency in the final ZIP.

## Review triggers

Repeat this analysis and update the package notice when any of the following changes:

- .NET SDK version;
- .NET Runtime or Windows Desktop Runtime version;
- target framework or minimum Windows target;
- runtime identifier or architecture;
- framework-dependent versus self-contained publication;
- single-file, trimming, ReadyToRun, Native AOT or extraction settings;
- restored runtime-pack inventory;
- any new Microsoft package, SDK, native binary or redistributable;
- product name, Microsoft compatibility claims or distribution channels;
- Microsoft licensing, privacy, export or support terms.

The verifier intentionally embeds the reviewed versions and configuration in the package notice. A version or publish-setting change therefore fails CI until the redistribution notice and analysis are reviewed and updated.

## Professional review gate

This implementation satisfies the repository's engineering and documentation controls, but it does not replace professional legal advice. Before an official production release, paid distribution, commercial support offering or bundled redistribution by a commercial partner, qualified legal counsel must review:

- this analysis;
- `MICROSOFT-DOTNET-REDISTRIBUTION.txt`;
- the exact generated package legal bundle;
- the intended distribution channels and end-user terms;
- any regional consumer, export or commercial-law implications.

That external sign-off is the explicit release gate tracked by Issue #93. No statement in this repository should be interpreted as claiming that the sign-off has already occurred.
