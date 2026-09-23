# SightAdapt GitHub Release checklist

This checklist is the maintainer gate for publishing a GitHub Release. The release workflow also enforces the machine-verifiable subset.

## Before requesting publication

- [ ] The release commit is on `main`.
- [ ] The product version, artifact name and settings schema in `Directory.Build.props` are final.
- [ ] The complete build/test workflow for the release commit passed.
- [ ] Windows manual smoke tests passed for startup, tray controls, both global shortcuts, automatic assignment, profile editing, overlay scopes, native popup-menu behavior and emergency disable.
- [ ] Known limitations and migration/upgrade notes were reviewed.
- [ ] The final package legal bundle, dependency report, SBOM and privacy notice are current.
- [ ] The repository setting **Enable release immutability** is enabled. GitHub documents that this protects the release tag and assets only for releases published after the setting is enabled.
- [ ] `release/release-request.json` contains the exact product version, tag and notes path.
- [ ] `manualWindowsSmokeAccepted` is set to `true` only after the manual test evidence above exists.
- [ ] `immutableReleasesConfirmed` is set to `true` only after the repository setting has been verified by an administrator.
- [ ] `publish` is changed to `true` in the final release-request commit.

## Automated publication gate

The GitHub Release workflow must:

1. consume only a successful `Build and test SightAdapt` run for the exact request commit;
2. require the request version to equal the canonical product version;
3. require the tag to be exactly `v<productVersion>`;
4. refuse publication unless both manual and immutable-release confirmations are true;
5. refuse an existing tag that points at a different commit;
6. reconstruct the release package from the verified CI ZIP and run `tools/new-verified-release-package.ps1` with the `github-release` channel;
7. publish the verified ZIP, schema-3 compliance report, SHA-256 manifest, SBOM and legal/privacy files;
8. publish the release as a prerelease when requested;
9. verify the resulting release metadata after publication.

## Tag and withdrawal policy

Published release tags are immutable. Do not move, recreate or reuse a published version tag.

If a release must be withdrawn for security or safety reasons, preserve the historical version record, mark the release/documentation as withdrawn where the platform permits, stop promoting its download, and publish a replacement version with a new tag. Do not silently replace bytes under an existing version.

SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.
