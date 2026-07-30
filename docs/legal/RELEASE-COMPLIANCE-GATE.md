# Release compliance gate

## Purpose

The release compliance gate treats the license, privacy, dependency, notice and SBOM bundle as a release invariant. It runs after publication and before artifact upload. It does not change application behavior.

## Authoritative inputs

- `release/required-files.txt` — package manifest;
- `Directory.Build.props` — product, artifact, SDK, runtime and RID metadata;
- `SightAdapt.csproj` — target framework and publish settings;
- `release/dotnet-redistribution-review.json` — reviewed configuration, template checksum and maintainer decision;
- `DOTNET-NOTICE-METADATA.json` — exact Microsoft source/checksum/runtime evidence;
- `MICROSOFT-DOTNET-REDISTRIBUTION.txt` — generated maintainer-reviewed package notice;
- `LICENSE-REPORT.json` — dependency-license policy result;
- `SBOM.spdx.json` — component and shipped-file inventory;
- final staged directory and ZIP.

## Checks

`verify-release-compliance.ps1` verifies:

- all manifest files exist, are non-empty and readable;
- required text has no unresolved placeholders;
- archive naming and build metadata are consistent;
- every staged file is in the ZIP and no unexpected file was added;
- exact-version .NET metadata matches product/SDK/runtime/RID/publish inputs;
- redistribution configuration and template SHA-256 remain reviewed;
- the generated redistribution notice identifies the exact artifact and maintainer decision;
- a `blocked` maintainer decision stops packaging;
- the license report is `pass` and matches the build;
- the SPDX SBOM describes the release and every shipped file;
- SightAdapt is identified as the single-file container for embedded runtime components.

The report contains the build identity, archive SHA-256, redistribution review date, maintainer decision/owner/issue, notice SHA-256, manifest, file lists and failures.

No field represents an external audit or legal clearance.

## Negative package tests

`test-release-compliance-negative.ps1` proves that:

1. an incomplete package is rejected;
2. a complete package with a deliberately changed redistribution runtime header is rejected as stale.

## Workflow order

1. verify canonical metadata and maintainer review record;
2. restore, build and test;
3. publish the self-contained directory;
4. generate exact-version .NET notices;
5. generate the maintainer-reviewed redistribution notice;
6. generate SBOM, dependency summary and license report;
7. reject incomplete/stale packages;
8. create the final ZIP;
9. run the compliance gate;
10. upload the verified ZIP and report together.

No upload runs after a failed gate.

## Other distribution workflows

GitHub Releases, installers, store packages, portable packages and mirrors must use the same staged directory and manifest, validate final contents, retain a checksum/report and stop before publication on failure.

## Maintenance

Update the canonical manifest first when package requirements change. A change to the reviewed .NET configuration, maintainer decision or notice wording must update `release/dotnet-redistribution-review.json`. Update generators, documentation and packaging workflows in the same pull request.
