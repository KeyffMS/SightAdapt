# Contribution provenance exceptions

This register records non-confidential decisions for contributions that cannot follow the ordinary DCO path or require additional permission evidence.

Confidential employer permissions, contracts, legal advice and personal information must not be committed to this repository. Store them privately and record only the minimum public decision metadata below.

## Required record fields

- date;
- pull request or commit;
- contribution scope;
- contributor or automation identity;
- reason the ordinary DCO path was unavailable;
- license and provenance reviewed;
- supporting evidence location or custodian, without publishing confidential contents;
- maintainer reviewer;
- decision and conditions.

## Bot exceptions

There is no standing exception based on an author name or email ending with `[bot]`.

An unsigned automated commit may be exempt only when:

- the pull-request actor login, numeric GitHub ID and actor type match one exact record in `.github/dco-bot-allowlist.json`;
- the commit author and committer emails match the same record;
- the generated source and changed dependency or file are clear from the pull request;
- a human maintainer reviews the resulting diff, license and provenance before merge;
- the contribution does not introduce unknown or incompatible material;
- the allowlist addition or exception decision is reviewed in a focused pull request.

The allowlist is empty by default. A bot exception does not exempt the pull request from dependency, license, build or maintainer review.

## Emergency repository-protection exceptions

A temporary bypass of protected-branch settings requires a record containing the reason, exact duration, affected commit or pull request, reviewer, supporting evidence and restoration confirmation. A standing administrator bypass is not permitted by policy.

## Recorded exceptions

None as of 2026-07-30.
