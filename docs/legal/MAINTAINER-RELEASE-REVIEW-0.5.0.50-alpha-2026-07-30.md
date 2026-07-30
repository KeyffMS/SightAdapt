# SightAdapt maintainer release-risk decision — 0.5.0.50-alpha — 2026-07-30

## Decision summary

| Field | Value |
|---|---|
| Decision | Approved for current free/open-source alpha scope |
| Decision date | 2026-07-30 |
| Decision owner | KeyffMS / aiteracja.pl |
| Product version | `0.5.0.50-alpha` |
| Source commit | `e925624408c7a403e00c53141ceaf6fd7cda6d2d` plus governance update from #138 |
| Validated workflow | GitHub Actions run `30525924363` |
| Workflow artifact | `SightAdapt-0.5.0.50-alpha-win-x64-30525924363` |
| GitHub artifact digest | `sha256:c0a1e3ca48423852ab347f69ed18ec76155ef0e9b8705a5ba698b33d53e5aee0` |
| Review trigger | Any material scope or artifact change listed below |

## Approved scope

- free and open-source development;
- source distribution through GitHub;
- verified alpha binary artifacts through the maintained GitHub Actions workflow;
- individual users, developers and evaluators;
- best-effort repository support;
- general-purpose accessibility and display personalization;
- no paid SLA, enterprise warranty, indemnity, medical purpose or legal-clearance claim.

## Evidence reviewed

- release metadata and pinned SDK/runtime configuration;
- successful build/test/publish workflow;
- exact-version .NET notices and redistribution record;
- required legal-document manifest and final package validation;
- SBOM, dependency summary and license-policy report;
- privacy/support-data policy;
- DCO and contribution provenance policy;
- internal trademark-risk record;
- non-medical intended-purpose policy;
- patent-risk feature map;
- third-party names, no-endorsement and DRM notice;
- README and release wording rules.

## Risk decisions

| Area | Decision |
|---|---|
| Microsoft .NET redistribution | Accepted for the recorded configuration; stale configuration is blocked by CI |
| Dependency licensing | Accepted subject to SBOM/license-policy checks; full transitive coverage remains tracked separately |
| Trademark/name/logo | Medium-high uncertainty accepted; no clearance/exclusivity claim; monitor and rename on concrete trigger |
| Privacy | Current local-processing/no-telemetry model accepted; any new data flow requires maintainer review |
| Intended purpose | Permanent non-medical/general-purpose scope maintained |
| Patent risk | No clearance claim; current free alpha accepted; commercial scope requires an internal public-source review |
| Consumer/commercial terms | No paid offer, SLA, warranty or indemnity approved by this record |
| Third-party compatibility | Neutral factual wording only; no endorsement or DRM-circumvention claims |

## Known limitations

- internal review is not legal advice or professional audit;
- no claim is made that the name, product or mechanisms are legally cleared or non-infringing;
- trademark uncertainty around similar `SightAid` uses is accepted;
- patent claims have not been exhaustively searched or interpreted;
- SBOM/transitive dependency completeness and multi-format packaging remain tracked in their own issues;
- external store, paid, enterprise or medical distribution is outside this decision.

## Final decision

The maintainer approves SightAdapt `0.5.0.50-alpha` for the current free/open-source GitHub scope, subject to the maintained CI/package controls and the limitations above. This record does not approve legal-clearance claims, medical positioning, patent warranties, paid SLAs, enterprise indemnities or materially different distribution channels.

## Re-review triggers

Repeat the maintainer review when:

- version, final artifact bytes or packaging format change;
- SDK/runtime/RID/TFM, dependencies, notices or SBOM composition change;
- paid distribution, sponsorship obligations, subscriptions or enterprise commitments are introduced;
- a store, installer or mirror workflow is added;
- product name, logo or compatibility claims materially change;
- telemetry, updates, accounts, uploads, cloud services or support-data flows are introduced;
- medical/clinical claims or regulated workflows are proposed;
- rendering, capture, automation or platform mechanisms materially change;
- a regulator, platform, patent owner or trademark owner raises a concrete objection.
