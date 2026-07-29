# SBOM and dependency-license review

## Scope

Every SightAdapt release candidate produces a machine-readable SPDX 2.3 software bill of materials and a machine-readable license-policy report. These files describe the published application container, exact .NET runtime packs, application/test/build dependencies and every file placed in the final publish directory.

This process is supply-chain and release documentation. It does not change SightAdapt application behavior.

## Authoritative inputs

`tools/generate-sbom.ps1` reads:

- `Directory.Build.props` for the product, SDK, runtime and RID versions;
- `src/SightAdapt/obj/project.assets.json` for the actual application restore graph;
- `DOTNET-NOTICE-METADATA.json` for exact runtime-pack and Microsoft source-package evidence;
- application and test project files for direct NuGet references;
- `.github/workflows/build.yml` for GitHub Actions build dependencies;
- `release/dependency-policy.json` for reviewed suppliers, licenses, versions, scopes and allow/deny/review decisions;
- the final publish directory for file names and SHA-256 checksums.

A component not present in the policy is unresolved and fails the release step. A component version different from the reviewed version also fails. Denied or unreviewed license expressions fail before archive creation.

## Generated files

The generator writes these files at the package root:

| File | Purpose |
|---|---|
| `SBOM.spdx.json` | SPDX 2.3 document containing packages, scopes, suppliers, versions, declared/concluded licenses, source references, file inventory, relationships and checksums |
| `LICENSE-REPORT.json` | License-policy result with allowed, denied and review lists plus the evaluated component inventory and failures |
| `DEPENDENCIES.md` | Human-readable summary generated from the same evaluated component list |

The SBOM intentionally treats `SightAdapt.exe` as the single-file publication container. Runtime packs embedded by self-contained single-file publication are represented as package components and related to the SightAdapt package. Every separately shipped file is represented with its SHA-256 checksum. The SBOM excludes only itself from its file list because a file cannot contain a stable checksum of its own final bytes.

## License policy

`release/dependency-policy.json` is the review source of truth. It defines:

- allowed license expressions;
- explicitly denied expressions;
- expressions requiring review;
- expected component versions or version sources;
- supplier and source information;
- whether a component is shipped, restore-only, test-only or build-only.

Custom Microsoft `LicenseRef` expressions point to the exact Microsoft license, redistribution and third-party notice files included in the same package. They do not relicense Microsoft components under MIT.

## Release behavior

The maintained workflow runs SBOM generation after self-contained publication and exact-version .NET notice generation, but before ZIP creation. A failed component or license review prevents archive publication. `release/required-files.txt` requires the SBOM and report in every final distribution, so GitHub artifacts, manual ZIPs, installers, store packages, portable packages and mirrors must preserve them.

The final ZIP retained for a release therefore contains the exact SBOM, license report and human-readable dependency inventory associated with the shipped bytes.

## Maintenance

Update the dependency policy when any package, runtime pack, SDK, GitHub Action, supplier, license or version changes. Do not add an unknown component with an assumed license. Review the authoritative package/repository terms, record the result in the policy and preserve any required notices.

The SBOM is evidence of composition. It does not replace legal review of ambiguous, custom or conflicting terms.
