# Repository protection evidence template

Use this template in Issue #87 after activating the `main` ruleset. Record only non-secret settings and public pull-request links.

## Configuration evidence

- Review date: `YYYY-MM-DD`
- Maintainer: `KeyffMS`
- Repository: `KeyffMS/SightAdapt`
- Ruleset name: `Protected main — DCO and build`
- Enforcement status: `Active`
- Target branch: `main`
- Pull request required: `Yes`
- Required approvals: `0` unless another maintainer is available
- Required status checks:
  - `Verify commit sign-offs`
  - `build`
- Require current/up-to-date head before merge: `Yes`
- Conversation resolution required: `<Yes/No and rationale>`
- Direct pushes blocked: `Yes`
- Force pushes blocked: `Yes`
- Branch deletion blocked: `Yes`
- Bypass actors: `None`
- Evidence screenshot or non-secret export: `<location>`

## Blocking test

- Test pull request: `<URL>`
- Unsigned commit SHA: `<SHA>`
- DCO check result: `Failure`
- Merge control result: `Merge blocked`
- Evidence location: `<screenshot or public check URL>`
- Corrective action: `Commit amended/recreated with a matching Signed-off-by trailer`
- Passing DCO run: `<URL>`
- Final disposition: `<merged or closed>`

## Maintainer statement

> I verified that `main` requires a pull request and the current successful `Verify commit sign-offs` and `build` checks, blocks direct and force pushes and branch deletion, and has no standing bypass actor. I also verified through the linked test pull request that an unsigned commit cannot be merged.

Do not include tokens, private administration URLs, confidential permissions or personal data in the Issue record.
