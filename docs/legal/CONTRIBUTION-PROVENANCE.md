# Contribution provenance and historical review

## Policy decision

SightAdapt adopts the Developer Certificate of Origin 1.1 workflow documented in [`DCO.md`](../../DCO.md). The project does not require a separate contributor license agreement at this stage.

A valid sign-off confirms that the contributor has the right to submit the contribution and permits its distribution under the repository's MIT License, subject to any clearly identified compatible third-party terms.

## Provenance requirements

Every pull request must identify:

- whether the work is entirely original to the contributor;
- copied or adapted code, snippets, documentation, templates or assets;
- source URLs and licenses for third-party material;
- employer, client or organization ownership where relevant;
- material AI assistance used to create retained content;
- permissions or exceptions required for anything not covered by the normal DCO path.

Maintainers must reject material whose origin, ownership or license cannot be established. This applies equally to code, tests, documentation, translations, icons, screenshots, diagrams, datasets and generated assets.

## Employer and organizational contributions

A contributor acting for an employer, client or other rights holder must have authority to submit the work. The pull request should state the organization and authorization basis without exposing confidential terms. When additional evidence is required, it is reviewed privately and only a non-confidential decision record is added to the exceptions register.

## AI-assisted contributions

AI assistance does not transfer responsibility to the tool provider. The human contributor must review the complete output, verify provenance, remove confidential or restricted material and make the DCO certification personally. A pull request must disclose material AI assistance and describe the human review performed.

## Historical review — 2026-07-29

The repository pull-request history available through PR #130 was reviewed for independently attributed contributions. The reviewed PR records identify `KeyffMS` as the author. No merged pull request authored by an independent non-owner contributor was identified in that history.

A review of the available commit history did not identify a separately attributed non-owner contribution requiring retroactive permission evidence. This is a repository-history review, not a forensic authorship opinion. If later evidence identifies an external contribution, maintainers must review its provenance and record the outcome in `CONTRIBUTION-EXCEPTIONS.md`.

Existing third-party dependencies, Microsoft runtime content and brand/source assets are governed separately by the dependency, notice and asset records in the repository; they are not treated as contributor-owned merely because they appear in repository or release files.

## Maintainer process

Before merging a contribution, maintainers confirm:

1. the DCO check passes for every non-exempt commit;
2. the pull request contains the required provenance disclosures;
3. third-party terms are known and compatible;
4. any employer/client authority is credible;
5. AI-assisted material has documented human review;
6. any exception has a public decision record and private evidence where necessary.

Repository or branch settings should require the DCO workflow status before merge. Direct pushes to protected branches should be restricted to maintainers and used only under the same provenance obligations.
