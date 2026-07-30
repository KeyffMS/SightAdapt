# Microsoft .NET redistribution analysis

## Status

| Field | Value |
|---|---|
| Maintainer review record | `release/dotnet-redistribution-review.json` |
| Reviewed configuration | Exact SDK, runtime, target framework, runtime identifier and publish mode recorded in the review record |
| Reviewed notice wording | `release/MICROSOFT-DOTNET-REDISTRIBUTION.template.txt`, protected by the SHA-256 in the review record |
| Package notice | Generated as `MICROSOFT-DOTNET-REDISTRIBUTION.txt` from the reviewed template and canonical release metadata |
| Professional legal review | Status and public record are controlled by the review record and Issue #93 |

This document records the project's technical redistribution analysis. It is not legal advice and is not evidence of review by qualified legal counsel.

The machine-readable review record is the source of truth for the exact technical configuration covered by the maintainer review. A change to the SDK, runtime, target framework, runtime identifier or publish mode fails release validation until that record is deliberately updated.

## Authoritative sources

The review uses these official Microsoft sources:

1. [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md), which describes the licensing model for .NET product distributions and runtime packs and explains that self-contained applications embed runtime components;
2. [.NET Library License Terms](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm), which contains the applicable distribution rights, requirements, restrictions, warranty terms and regional qualifications;
3. the exact release metadata URL recorded in `Directory.Build.props`, which maps the reviewed runtime, Windows Desktop Runtime and SDK release train;
4. the hash-verified official SDK archive identified in each package's `DOTNET-NOTICE-METADATA.json`, which supplies the exact imported Microsoft license and third-party notice text;
5. the actual `project.assets.json` produced by restore, which identifies the runtime packs selected for the release build.

The generated files in the final package preserve the authoritative text and evidence. This analysis paraphrases operational requirements and does not replace those files.

## Microsoft-origin component inventory

### Included in the self-contained binary

The Microsoft-origin runtime content is selected by the pinned SDK through the runtime-pack identities recorded in `DOTNET-NOTICE-METADATA.json`:

| Component family | Release identity authority | Included purpose | Distribution position |
|---|---|---|---|
| .NET application host and host policy | `Microsoft.NETCore.App.Runtime` entry from the exact restore graph | Starts and hosts the self-contained managed application | Distributed in object-code form only as part of SightAdapt under the .NET Library License |
| .NET managed runtime and base libraries | Exact `Microsoft.NETCore.App.Runtime` version from the restore graph | CLR, garbage collector, framework libraries and native runtime support | Distributed in object-code form only as part of SightAdapt under the .NET Library License and imported third-party notices |
| Windows Desktop / Windows Forms runtime | Exact `Microsoft.WindowsDesktop.App.Runtime` version from the restore graph | Windows Forms UI and Windows desktop framework support | Distributed in object-code form only as part of SightAdapt under the .NET Library License and imported third-party notices |
| Runtime third-party components | Official `ThirdPartyNotices.txt` from the reviewed SDK/runtime release train | Native and managed dependencies incorporated into Microsoft runtime packs | Governed by the separate licenses and notices preserved in `THIRD-PARTY-NOTICES.txt` |

The precise restore identities are recorded in `DOTNET-NOTICE-METADATA.json`. The single-file publisher may embed managed assemblies and native components inside `SightAdapt.exe`; embedding does not change their separate licensing status.

A framework download dependency recorded during restore is preserved for auditability, but a component is classified as shipped only when the final package and SBOM identify it as present or embedded in the documented single-file container.

### Microsoft-origin dependencies not redistributed as product components

| Component | Role | Distribution status |
|---|---|---|
| Pinned .NET SDK | Restore, compile, test and publish; source of matching legal-text bundle | Build infrastructure, not shipped as the SightAdapt application |
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
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated operational end-user and downstream-distributor notice for Microsoft components |
| `DOTNET-LICENSE-NOTICE.txt` | Exact Microsoft license text imported from the hash-verified official SDK archive |
| `THIRD-PARTY-NOTICES.txt` | Exact Microsoft/.NET third-party notices imported from the same archive |
| `DOTNET-NOTICE-METADATA.json` | Exact versions, runtime packs, source URLs and checksums |
| `DEPENDENCIES.md` | Human-readable shipped/build/platform dependency inventory |

`tools/generate-dotnet-redistribution-notice.ps1` validates the review record and reviewed template checksum, confirms that exact-version .NET metadata already exists, and renders the package notice from canonical release values. `tools/test-release-package.ps1` independently checks the generated notice headers inside the final ZIP.

## Review triggers

Repeat the maintainer analysis and update `release/dotnet-redistribution-review.json` when any of the following changes:

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

Changing the reviewed notice wording also requires updating the template SHA-256 in the review record. Product-version changes are rendered automatically from `Directory.Build.props`, but the generated package notice must still match the exact artifact.

The early release-metadata check, notice generator and final-package validator all reject a reviewed-configuration mismatch. The negative package check also mutates the runtime version in a generated notice and proves that stale wording cannot pass the release workflow.

## Professional review gate

Repository automation proves configuration and package consistency; it does not replace professional legal advice. Before an official production release, paid distribution, commercial support offering or bundled redistribution by a commercial partner, qualified legal counsel must review:

- this analysis and the machine-readable review record;
- the reviewed notice template and generated package notice;
- the exact generated package legal bundle;
- the intended distribution channels and end-user terms;
- any regional consumer, export or commercial-law implications.

The current professional-review status and any non-confidential decision record are stored in `release/dotnet-redistribution-review.json` and governed by Issue #93. A `not-obtained` status keeps production and paid distribution blocked; no repository document should be interpreted as claiming that the external sign-off has occurred.
