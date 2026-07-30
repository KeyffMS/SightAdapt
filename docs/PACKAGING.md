# SightAdapt binary packaging standard

This document defines the minimum contents and verification rules for every binary distribution of SightAdapt.

## Canonical required files

The machine-readable source of truth is `release/required-files.txt`.

| File | Purpose |
|---|---|
| `SightAdapt.exe` | Application executable and single-file container |
| `LICENSE.txt` | MIT License for SightAdapt project code |
| `THIRD-PARTY-NOTICES.txt` | Exact .NET notices and package-specific notice sections |
| `DOTNET-LICENSE-NOTICE.txt` | Exact Microsoft .NET license text and source metadata |
| `DOTNET-NOTICE-METADATA.json` | Exact package, component, notice and checksum evidence |
| `MICROSOFT-DOTNET-REDISTRIBUTION.txt` | Generated maintainer-reviewed redistribution notice |
| `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` | Mark ownership, no-endorsement and DRM/access-control boundaries |
| `DEPENDENCIES.md` | Human-readable dependency inventory generated from complete restore graphs |
| `SBOM.spdx.json` | SPDX 2.3 package graph and shipped-file inventory |
| `LICENSE-REPORT.json` | Schema-2 dependency-license evidence and policy result |
| `PRIVACY.md` | Application privacy and local-data notice |

A package is incomplete if a required file is missing, unreadable, stale, inconsistent with pinned inputs, contains a binary without exact component notice evidence or has an unresolved dependency/license result.

## Publish sequence

1. `SightAdapt.csproj` copies the static legal baseline and captures SDK `FilesToBundle` inputs.
2. `generate-dotnet-notices.ps1` imports exact official .NET legal text.
3. `generate-dotnet-component-coverage.ps1` maps all embedded and loose package components and adds package-specific notices.
4. `generate-dotnet-redistribution-notice.ps1` renders the reviewed redistribution summary.
5. `generate-sbom.ps1` traverses complete application/test restore graphs, collects package license evidence and generates `DEPENDENCIES.md`, `LICENSE-REPORT.json` and `SBOM.spdx.json`.
6. Negative tests prove rejection of incomplete, stale, unmapped and unknown-license inputs.

## Exact component coverage

Schema-2 `DOTNET-NOTICE-METADATA.json` records:

- SHA-256 of the temporary `FilesToBundle` manifest;
- exact package identities and NuGet SHA-512 values;
- repository and license metadata;
- notice mappings for exact .NET release text and package-specific terms;
- one sanitized record per embedded or loose component;
- package-relative asset path and component SHA-256;
- zero unmapped external components.

The reviewed alpha contains 452 mapped package components across:

- `Microsoft.NETCore.App.Runtime.win-x64/8.0.29`;
- `Microsoft.WindowsDesktop.App.Runtime.win-x64/8.0.29`;
- `Microsoft.Windows.SDK.NET.Ref/10.0.19041.56`.

`Microsoft.Windows.SDK.NET.Ref` is classified as `shipped-embedded`, because `Microsoft.Windows.SDK.NET.dll` and `WinRT.Runtime.dll` are present in `FilesToBundle`.

## Complete dependency and license inventory

The SBOM generator uses both application and test `project.assets.json` files. Every NuGet package in `libraries`, every package dependency edge in `targets`, every framework download dependency, each package contributing published components, the pinned SDK and each maintained GitHub Action is inventoried.

For NuGet packages the generated evidence retains exact package SHA-512, `.nuspec` SHA-256, declared license metadata, packaged-license SHA-256 where applicable, repository information and direct/transitive scope. `release/dependency-policy.json` supplies allow/deny/review decisions and selected custom-license overrides; it does not define which packages exist.

SPDX root relationships distinguish scopes:

- shipped packages: `SightAdapt DEPENDS_ON package`;
- test-only packages: `package TEST_DEPENDENCY_OF SightAdapt`;
- SDK, Actions, restore-only and application-build packages: `package BUILD_DEPENDENCY_OF SightAdapt`.

Package-to-package restore edges remain `DEPENDS_ON` and identify the originating graph.

## Final package validation

```powershell
$archive = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64.zip'
$report = '.\artifacts\SightAdapt-0.5.0.50-alpha-win-x64-compliance.json'

Compress-Archive `
    -Path '.\artifacts\win-x64\*' `
    -DestinationPath $archive `
    -CompressionLevel Optimal

.\tools\verify-release-compliance.ps1 `
    -DirectoryPath '.\artifacts\win-x64' `
    -ArchivePath $archive `
    -ReportPath $report

.\tools\verify-dotnet-component-coverage.ps1 `
    -ArchivePath $archive
```

The general compliance gate validates package, metadata, license-report and SBOM invariants. The focused component validator independently verifies package evidence, notice mappings, exact loose-binary hashes and absence of unmapped binaries.

## Negative checks

```powershell
.\tools\test-sbom-license-negative.ps1 `
    -PublishDirectory '.\artifacts\win-x64'

.\tools\test-release-compliance-negative.ps1 `
    -PublishDirectory '.\artifacts\win-x64'
```

The workflow proves rejection of:

- a transitive package with valid package metadata but no declared/resolved license;
- an incomplete package;
- stale redistribution metadata;
- a package containing a real runtime DLL after its component mapping is deliberately removed.

## Distribution formats

The same legal/compliance bundle is required for Actions artifacts, manual ZIPs, installers, store packages, portable builds and mirrors. Each maintained format must validate its final installed or unpacked file set. Mirrors publish the same verified bytes and report.

## Release checklist

1. verify canonical release metadata and maintainer decision;
2. restore application and test projects;
3. build, test, publish and capture `FilesToBundle`;
4. import exact official .NET notices;
5. generate exact package/component coverage;
6. generate the redistribution summary;
7. generate complete SBOM, license report and dependency inventory;
8. resolve every unknown, denied, review-required or evidence failure;
9. prove unknown-transitive, incomplete, stale and unmapped inputs are rejected;
10. create and run both validators against the final package;
11. retain the package and compliance report;
12. publish identical verified bytes to official mirrors.

Do not publish when any notice, package hash, component mapping, restore graph, SBOM, license, metadata or maintainer-decision check fails.
