# SBOM and dependency-license review

## Scope

Every SightAdapt release candidate produces a machine-readable SPDX 2.3 software bill of materials, a machine-readable license-evidence report and a human-readable dependency summary. The inventory covers the published application, exact embedded/runtime packages, every application package, the complete test graph, SDK/build inputs and GitHub Actions used by the maintained workflow.

This process is supply-chain and release documentation. It does not change SightAdapt application behavior and does not claim that an internal license decision is external legal advice.

## Authoritative inventory inputs

`tools/generate-sbom.ps1` reads:

- `src/SightAdapt/obj/project.assets.json`, including every package in `libraries` and every package/dependency edge in `targets`;
- `tests/SightAdapt.Tests/obj/project.assets.json`, including direct and transitive test packages and their graph edges;
- `DOTNET-NOTICE-METADATA.json` for exact packages and component hashes proven to be embedded in `SightAdapt.exe` or shipped as loose binaries;
- `.github/workflows/build.yml` for maintained build actions;
- `Directory.Build.props` for product, SDK, runtime and RID identity;
- `release/dependency-policy.json` for license allow/deny/review decisions, version constraints and explicit custom-license overrides;
- the final publish directory for file names and SHA-256 checksums.

The policy is an override and decision layer, not the component inventory. A new direct or transitive NuGet package is discovered from restore data even when it has no policy entry.

## NuGet license evidence

For every discovered NuGet package the generator locates the exact restored package and records:

- exact package identity and NuGet SHA-512;
- SHA-256 of the exact `.nuspec` file;
- declared license expression, license-file path or license URL from `.nuspec`;
- SHA-256 of a packaged license file when one is declared;
- package repository URL and commit where supplied;
- package authors/supplier evidence.

An SPDX expression present in exact package metadata is evaluated directly against the allow/deny/review lists. A policy override is required only for package-specific or custom terms that cannot be represented by the package's expression alone. Missing package cache data, missing `.nuspec`, unknown licenses, denied licenses and review-required licenses fail CI.

Exact published Microsoft components continue to use the component-level evidence produced by #83. The runtime packs and `Microsoft.Windows.SDK.NET.Ref` retain exact package SHA-512, component SHA-256 and package-specific notice mappings.

## Generated files

| File | Purpose |
|---|---|
| `SBOM.spdx.json` | SPDX 2.3 document containing every package, complete dependency edges, scopes, versions, suppliers, licenses, purls, package checksums and packaged-file SHA-256 values |
| `LICENSE-REPORT.json` | Schema-2 inventory and policy result containing direct/transitive classification, evidence hashes, application/test graph counts and failures |
| `DEPENDENCIES.md` | Human-readable summary generated from the same authoritative component list |

The SBOM excludes only its own final bytes from the file inventory. `SightAdapt.exe` is the documented single-file container for embedded components; separately shipped files remain individually hashed.

## SPDX relationship rules

The document describes the SightAdapt package. Relationships are intentionally scope-aware:

- packages proven to be shipped are represented as `SightAdapt DEPENDS_ON <package>`;
- test-only packages are represented as `<package> TEST_DEPENDENCY_OF SightAdapt`;
- SDK, Actions, restore-only and application-build packages are represented as `<package> BUILD_DEPENDENCY_OF SightAdapt`;
- package-to-package dependency edges from each restore target use `DEPENDS_ON` and retain the originating application or test graph in the relationship comment;
- SightAdapt `CONTAINS` every separately packaged file.

Build and test tools are therefore not presented as runtime dependencies of the delivered application.

## License policy

`release/dependency-policy.json` schema 2 defines:

- permissive expressions approved by maintainers;
- explicitly denied expressions;
- expressions requiring explicit review;
- custom `LicenseRef` records included in the SPDX document;
- version and supplier overrides for selected components;
- package-specific conclusions where exact package metadata uses a file, URL or custom terms.

A package absent from `components` is allowed only when its exact metadata resolves to an allowed license. Absence from the policy never removes it from the inventory.

## Negative test

`tools/test-sbom-license-negative.ps1` adds a deliberately transitive package to a copy of the real test restore graph. The fixture has valid package/nuspec checksums but no license metadata. CI must reject it specifically as an unresolved transitive license.

## Release behavior

The workflow restores both projects, retains both `project.assets.json` files in diagnostics, generates the real SBOM/license report, runs the unknown-transitive negative test and stops before archive creation on any failure. The final package contains the exact `SBOM.spdx.json`, `LICENSE-REPORT.json` and `DEPENDENCIES.md` associated with its bytes.

## Maintenance

When dependencies change, do not add a handwritten inventory entry. Restore both projects and let the generator discover the graph. Update the policy only when a version constraint, supplier decision, custom license conclusion or allow/deny/review rule changes. Preserve additional notice text when a newly discovered package requires it.
