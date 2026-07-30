# Microsoft .NET redistribution analysis

## Status

| Field | Value |
|---|---|
| Maintainer review record | `release/dotnet-redistribution-review.json` |
| Reviewed configuration | Exact SDK, runtime, target framework, runtime identifier and publish mode recorded in the review record |
| Reviewed notice wording | `release/MICROSOFT-DOTNET-REDISTRIBUTION.template.txt`, protected by the SHA-256 in the review record |
| Package notice | Generated as `MICROSOFT-DOTNET-REDISTRIBUTION.txt` from the reviewed template and canonical release metadata |
| Decision owner | KeyffMS / aiteracja.pl |
| External legal audit | Not planned |

This document records the project's internal technical and business-risk decision. It is not legal advice and does not claim legal clearance or review by outside counsel.

The machine-readable review record is the source of truth for the exact technical configuration covered by the maintainer decision. A change to the SDK, runtime, target framework, runtime identifier, publish mode or notice template fails release validation until the record is deliberately updated.

## Authoritative sources

The review uses official Microsoft sources:

1. [.NET license information](https://github.com/dotnet/core/blob/main/license-information.md), describing the licensing model for .NET distributions and self-contained applications;
2. [.NET Library License Terms](https://dotnet.microsoft.com/en-us/dotnet_library_license.htm);
3. the exact release metadata URL recorded in `Directory.Build.props`;
4. the hash-verified official SDK archive identified in `DOTNET-NOTICE-METADATA.json`;
5. the actual `project.assets.json` produced by restore.

The generated files preserve the authoritative text and evidence. This analysis paraphrases operational requirements and does not replace them.

## Microsoft-origin component inventory

### Included in the self-contained binary

| Component family | Release identity authority | Included purpose | Project treatment |
|---|---|---|---|
| .NET application host and host policy | `Microsoft.NETCore.App.Runtime` from the restore graph | Starts and hosts the application | Object code included only as part of SightAdapt |
| .NET managed runtime and base libraries | Exact runtime-pack version from restore | CLR, GC, framework libraries and native support | Microsoft terms and imported notices preserved |
| Windows Desktop / Windows Forms runtime | Exact `Microsoft.WindowsDesktop.App.Runtime` from restore | Windows Forms UI/runtime | Microsoft terms and imported notices preserved |
| Runtime third-party components | Official `ThirdPartyNotices.txt` for the release train | Native and managed dependencies incorporated into Microsoft runtime packs | Separate notices preserved in `THIRD-PARTY-NOTICES.txt` |

The precise restore identities are recorded in `DOTNET-NOTICE-METADATA.json`. Single-file embedding does not change the separate licensing status of incorporated components.

A restore dependency is retained for auditability, but classified as shipped only when the final package and SBOM identify it as present or embedded in the documented single-file container.

### Inputs not redistributed as product components

| Component | Role | Distribution status |
|---|---|---|
| Pinned .NET SDK | Restore, compile, test and publish; source of matching legal text | Build infrastructure only |
| `Microsoft.Windows.SDK.NET.Ref` | Compile-time Windows API metadata | Build reference only |
| Test SDK/adapter/framework | Automated testing | Test-only |
| GitHub Actions | CI infrastructure | Not shipped |
| Windows system APIs/DLLs | Operating-system services | Supplied by Windows, not copied as SightAdapt redistributables |

## Maintainer redistribution controls

For the reviewed configuration:

1. Microsoft components are distributed in object-code form only as part of SightAdapt, not as a standalone .NET distribution.
2. The package preserves Microsoft's license text, exact-version notices and machine-readable source/checksum evidence.
3. SightAdapt's MIT License applies only to SightAdapt project code.
4. Recipients receive `MICROSOFT-DOTNET-REDISTRIBUTION.txt` with the project's redistribution conditions and source references.
5. Downstream packages must preserve the legal bundle and keep Microsoft components within an intact application distribution.
6. The project does not imply Microsoft sponsorship, certification or endorsement.
7. The project does not impose reciprocal/source-disclosure terms on Microsoft distributable code.
8. Notices must not be removed and Microsoft components must not be redistributed standalone.
9. Applicable export-control and non-waivable consumer rights remain relevant.
10. Microsoft warranty/support/remedy provisions remain separate from SightAdapt's MIT terms.

## Package implementation

Every binary package includes:

| File | Function |
|---|---|
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated maintainer-reviewed redistribution notice |
| `DOTNET-LICENSE-NOTICE.txt` | Exact Microsoft license text from the hash-verified SDK archive |
| `THIRD-PARTY-NOTICES.txt` | Exact Microsoft/.NET third-party notices |
| `DOTNET-NOTICE-METADATA.json` | Exact versions, runtime packs, sources and checksums |
| `DEPENDENCIES.md` | Human-readable dependency inventory |

`tools/generate-dotnet-redistribution-notice.ps1` validates the maintainer review record and template checksum, confirms exact-version .NET metadata and renders the package notice. `tools/test-release-package.ps1` independently validates its headers inside the final ZIP.

## Review triggers

Repeat the maintainer review when any of the following changes:

- .NET SDK, runtime or Windows Desktop Runtime version;
- target framework or minimum Windows target;
- runtime identifier or architecture;
- framework-dependent/self-contained or single-file settings;
- trimming, ReadyToRun, Native AOT or extraction settings;
- runtime-pack inventory or Microsoft redistributables;
- product name, Microsoft compatibility claims or distribution channels;
- Microsoft licensing, privacy, export or support terms;
- reviewed notice wording.

The early metadata check, generator and final-package validator reject configuration mismatches. The negative package test proves that stale notice wording cannot pass.

## Decision

The maintainers accept the documented redistribution approach for the recorded configuration and current distribution. No external legal audit is required or planned.

This decision does not assert legal clearance. Maintainers must update the evidence and consciously accept, condition or block the release whenever a review trigger changes.
