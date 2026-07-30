# Repository protection and DCO enforcement

## Required protected-branch state

The `main` branch must be governed by an active GitHub repository ruleset or equivalent branch protection with all of these properties:

- changes enter `main` through a pull request;
- direct pushes are blocked;
- force pushes and branch deletion are blocked;
- the `Verify commit sign-offs` status check is required before merge;
- the main build/test status check is required before merge;
- required checks must pass on the current pull-request head;
- no standing administrator or maintainer bypass is enabled;
- emergency exceptions are temporary, documented in `CONTRIBUTION-EXCEPTIONS.md` and removed immediately after use.

The project has one maintainer, so a separate approving reviewer is not required by this policy. The pull-request requirement and required automated checks remain mandatory.

## Repository-enforced checks

`.github/workflows/dco.yml` runs the repository-owned DCO verifier. It:

1. tests the DCO policy against signed, unsigned, mismatched and spoofed-bot fixtures;
2. checks every pull-request commit;
3. requires a `Signed-off-by` trailer matching the commit author or committer identity;
4. permits an unsigned bot commit only when the pull-request actor login, numeric GitHub ID, actor type and commit emails match one exact record in `.github/dco-bot-allowlist.json`;
5. treats `[bot]` text in an author name or email as ordinary, untrusted commit metadata.

The bot allowlist is empty by default. Adding an entry requires a focused pull request that identifies the automation owner, generated source, expected account identity and provenance controls.

## Merge process

Before merging, a maintainer verifies:

- `Verify commit sign-offs` is successful;
- the build/test workflow is successful;
- the pull-request provenance checklist is complete;
- third-party and AI-assisted material is disclosed;
- any exception is recorded with supporting evidence.

A merge performed while a required check is failing is a policy violation even if GitHub settings temporarily allow it.

## Protection evidence

After configuring or changing repository protection, record non-secret evidence in Issue #87 or its successor:

- date and maintainer;
- protected branch or ruleset name;
- enforcement status;
- required status-check names;
- pull-request requirement;
- bypass actors, expected to be none;
- force-push and deletion settings;
- screenshot or exported API response location;
- a blocked test attempt or other evidence that an unsigned commit cannot merge.

Do not commit access tokens, private administration URLs or confidential account information.

## Review triggers

Review this policy when:

- workflow or job names change;
- a new bot or GitHub App needs an exception;
- repository ownership or maintainer roles change;
- merge methods or protected branches change;
- GitHub changes ruleset or branch-protection behavior;
- an unsigned, direct-push or bypass incident occurs.
