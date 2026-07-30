# Developer Certificate of Origin policy

SightAdapt uses the **Developer Certificate of Origin, Version 1.1 (DCO 1.1)** rather than a contributor license agreement.

The authoritative DCO text is published at:

https://developercertificate.org/

By adding a `Signed-off-by` trailer to a commit, a contributor certifies that at least one of the DCO provenance paths applies: the contributor created the work and may submit it, the work is based on appropriately licensed material that may be submitted, or the contribution was passed through by another person who made the same certification. The contributor also acknowledges that the contribution and sign-off form part of the public project history.

For SightAdapt, the sign-off additionally confirms that the contributor has the right to submit the complete contribution for distribution under the repository's MIT License and any clearly identified separate license that already governs included third-party material.

## Required commit trailer

Every human-authored commit in a pull request must contain:

```text
Signed-off-by: Full Name <email@example.com>
```

The name and email must identify the commit author or committer making the certification. A GitHub no-reply address may be used. A trailer for an unrelated identity does not satisfy the check.

Add the trailer automatically with:

```bash
git commit --signoff
```

To sign an existing commit:

```bash
git commit --amend --signoff
```

A contributor who rewrites several commits must add a valid trailer to each commit, not only the final commit.

## Covered contributions

The policy applies to:

- application code and build scripts;
- tests and test data;
- documentation and translations;
- icons, screenshots, diagrams, audio, video and other creative assets;
- configuration, templates, generated files and datasets;
- AI-assisted material retained in the contribution.

## Employer or client work

A person contributing work created within employment, a contract or another organization must confirm that they are authorized to submit it. Where an employer or client owns the relevant rights, the contributor must obtain the necessary permission before signing off. A maintainer may request a non-confidential confirmation of authority and may record the existence of supporting permission in the exceptions register.

## Third-party and generated material

Pull requests must disclose copied code, adapted snippets, external templates, generated assets, datasets and other third-party material. The disclosure must identify the source, applicable license and modifications. Do not submit material with unknown, incompatible, non-redistributable or unverifiable terms.

AI tools do not provide provenance or permission. A contributor using AI assistance remains responsible for:

- reviewing every retained output;
- verifying that it is correct and appropriate for the project;
- checking for copied, restricted or confidential material;
- documenting material AI assistance in the pull request;
- ensuring that the contributor can make the DCO certification for the final contribution.

## Bot exceptions

Bot exceptions are disabled by default. Text such as `[bot]` in commit author metadata does not create an exception.

An unsigned automated commit may be exempt only when the pull-request actor and commit metadata match one exact trusted record in `.github/dco-bot-allowlist.json`. A record contains the GitHub login, numeric account ID, account type and permitted author/committer emails. Adding a bot requires a focused provenance review and update to `docs/legal/CONTRIBUTION-EXCEPTIONS.md`.

Any other exception requires a maintainer decision recorded in `docs/legal/CONTRIBUTION-EXCEPTIONS.md`. Supporting permissions that contain confidential information are retained outside the public repository; the public record identifies the scope, reviewer, date and decision without publishing privileged or personal material.

## Enforcement

`.github/workflows/dco.yml` runs repository-owned policy fixtures and checks every pull-request commit using `tools/verify-dco.sh`. The check requires an identity-matching sign-off or an exact trusted-bot allowlist match.

The `main` branch must require the `Verify commit sign-offs` status and the build/test status before merge, require pull requests, and block direct/force pushes and deletion as documented in `docs/legal/REPOSITORY-PROTECTION.md`.

A pull request must not be merged while the DCO check is failing or a required provenance disclosure is unresolved.
