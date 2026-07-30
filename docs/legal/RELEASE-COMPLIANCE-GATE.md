# Release compliance gate

## Purpose

The release compliance gate treats the legal, privacy, dependency, notice and SBOM bundle as a release invariant. It runs after the final publication directory has been produced and before any artifact is uploaded.

This is a packaging control. It does not test or change SightAdapt application behavior.

## Authoritative inputs

The gate consumes existing authoritative outputs rather than creating independent legal data:

- `release/required-files.txt` — canonical package manifest;
- `Directory.Build.props` — product, artifact, SDK, runtime and RID metadata;
- `src/SightAdapt/SightAdapt.csproj` — target framework and publish settings;
- `release/dotnet-redistribution-review.json` — reviewed .NET redistribution configuration, template checksum and professional-review status;
- `DOTNET-NOTICE-METADATA.json` — exact-version Microsoft source/checksum/runtime evidence;
- `MICROSOFT-DOTNET-REDISTRIBUTION.txt` — generated package notice tied to canonical and reviewed metadata;
- `LICENSE-REPORT.json` — dependency-license review result;
- `SBOM.spdx.json` — SPDX component and shipped-file inventory;
- the final staged publication directory;
- the final ZIP archive.

## Checks

`tools/verify-release-compliance.ps1` verifies:

- every manifest file exists in the staged publication directory;
- required files are non-empty and readable;
- required text does not contain common unresolved template markers;
- the final archive name matches the canonical artifact metadata;
- the existing archive validator accepts the final ZIP;
- every staged file is in the final ZIP and no unexpected file was added;
- exact-version notice metadata matches the product, SDK, runtime and RID;
- the reviewed redistribution configuration still matches SDK, runtime, target framework, RID and publish mode;
- the reviewed redistribution-template SHA-256 still matches;
- the generated redistribution notice headers identify the exact artifact and current professional-review status;
- the license report has a `pass` result and matches the same build metadata;
- the SBOM is SPDX 2.3, describes the correct SightAdapt release and contains every separately shipped file except its own final bytes;
- the SBOM identifies SightAdapt as the single-file packaging container for embedded runtime components.

The gate writes a machine-readable compliance report containing the result, build identity, canonical artifact name, archive SHA-256, redistribution review date/status, generated redistribution-notice SHA-256, manifest, staged/archive file lists and failures.

## Negative package tests

`tools/test-release-compliance-negative.ps1` proves two failure paths:

1. a temporary ZIP containing only a fake `SightAdapt.exe` is rejected as incomplete;
2. a copy of the complete staged package with a deliberately changed redistribution `Runtime version` header is rejected as stale.

The workflow fails if either invalid package is accepted.

## Workflow order

The maintained release sequence is:

1. verify canonical metadata and the reviewed redistribution record;
2. restore, build and run the existing application test suite;
3. publish the final self-contained directory;
4. generate exact-version .NET notices;
5. generate the reviewed Microsoft .NET redistribution notice;
6. generate the SBOM, dependency summary and license report;
7. confirm incomplete and stale-notice packages are rejected;
8. create the final ZIP;
9. run the release compliance gate against the staged directory and ZIP;
10. upload the verified ZIP and compliance report together.

No upload step runs after a failed compliance gate.

## Other distribution workflows

GitHub Releases, installer builds, store packages, portable packages and release mirrors must consume the same staged directory and canonical manifest. Each maintained packaging workflow must:

- run the equivalent directory/package verification after its final files are staged;
- preserve all required documents in the installed or extractable application directory;
- retain a compliance report tied to the exact package bytes;
- stop before publication when verification fails.

A platform-specific installer or store container may use a different outer format, but it must verify the unpacked/installed contents against `release/required-files.txt` and record the final container checksum. A mirror must publish the same verified bytes and report rather than rebuilding or removing files.

## Maintenance

Update the canonical manifest first when a required compliance artifact changes. A change to the reviewed .NET configuration or redistribution wording must update `release/dotnet-redistribution-review.json` through an explicit review. Update the generators, packaging documentation and all distribution workflows in the same pull request. Do not add a separate hard-coded package list to another workflow.
