# Final package compliance gate

## Maintained distribution scope

SightAdapt currently maintains one binary format: a self-contained Windows x64 portable ZIP.

The format has two maintained producer contexts recorded in `release/distribution-channels.json`:

- `github-actions-artifact` — the ZIP uploaded by `.github/workflows/build.yml`;
- `local-portable-zip` — a maintainer-created ZIP produced through the same entry point.

GitHub Releases, installers, store packages and official mirrors are planned or inactive channels. They are not treated as maintained distributions until their implementation changes the registry and invokes the same reusable gate. GitHub Release publication remains tracked by #98.

## Reusable entry point

All maintained packages are created with:

```powershell
.\tools\new-verified-release-package.ps1 `
    -DirectoryPath <staged-directory> `
    -ArchivePath <archive-path> `
    -ReportPath <compliance-report-path> `
    -DistributionChannel <registered-channel>
```

The entry point creates the ZIP and invokes `tools/verify-final-package.ps1`. Packaging code must not replace this sequence with an unverified `Compress-Archive` call.

## Authoritative inputs

- `release/distribution-channels.json` — maintained and planned channels;
- `release/required-files.txt` — canonical package manifest;
- `Directory.Build.props` and `SightAdapt.csproj` — product and publish identity;
- `release/dotnet-redistribution-review.json` — maintainer redistribution decision;
- `DOTNET-NOTICE-METADATA.json` — exact component and notice evidence;
- `LICENSE-REPORT.json` and `SBOM.spdx.json` — complete dependency and file inventory;
- the final staged directory and ZIP;
- Git commit/ref, optional release tag and workflow/run identity.

## Validation layers

The final gate consumes the existing focused validators rather than duplicating their domain logic:

1. `verify-release-compliance.ps1` validates the legal bundle, exact metadata, SBOM, licenses and staged/archive path sets;
2. `verify-dotnet-component-coverage.ps1` validates every embedded and loose package component;
3. `verify-final-package.ps1` validates distribution-channel state, provenance and SHA-256 equality for every staged/archive file.

The final report uses schema 3 and records:

- distribution channel and format;
- source commit SHA and repository HEAD SHA;
- source ref and release tag, when applicable;
- workflow name, run ID and run attempt;
- archive name, size and SHA-256;
- one staged/archive SHA-256 comparison record per file;
- base compliance, component coverage and hash-comparison results;
- package/SBOM counts and failures.

A report is publishable only when `result` is `pass`.

## Provenance rules

- The source commit must be a full 40-character Git SHA and match the checked-out repository HEAD.
- A tag value is valid only with the matching `refs/tags/<tag>` source ref.
- Channels marked `requiresTag` cannot publish an untagged package.
- Channels marked `requiresWorkflowRun` require GitHub workflow/run provenance.
- A channel listed only under `plannedChannels` is rejected by the gate.

## Negative validation

CI proves rejection of:

- an incomplete package;
- stale redistribution metadata;
- an unmapped runtime binary;
- an unknown-license transitive dependency;
- a ZIP whose file bytes differ from the staged directory;
- a source commit that does not match repository HEAD;
- inconsistent tag/ref provenance;
- the planned but inactive `github-release` channel.

## Future channels

A new installer, store package, GitHub Release or mirror implementation must, in the same pull request:

1. define the final installed or unpacked staging tree;
2. activate its channel in `release/distribution-channels.json`;
3. invoke `new-verified-release-package.ps1` or an equivalent adapter that ends in `verify-final-package.ps1`;
4. retain the verified package and matching schema-3 report;
5. add channel-specific negative tests where the container differs from portable ZIP.

Official mirrors must publish byte-identical verified artifacts and the matching report.
