# Maintainer release-risk review and decision

## Project policy

SightAdapt does not plan or require external legal, trademark, patent, privacy/DPO or medical-device audits. Release decisions are made by the responsible maintainer/publisher using repository evidence, public sources and documented risk acceptance.

Internal review is not equivalent to professional advice. The project must not claim legal clearance, patent freedom to operate, non-infringement, trademark availability, regulatory approval or professional audit.

## Current status

| Field | Current position |
|---|---|
| Status date | 2026-07-30 |
| Responsible publisher/support party | KeyffMS / aiteracja.pl |
| Current approved stage | Free, open-source alpha development and verified GitHub distribution |
| External audit requirement | None planned |
| Release decision owner | KeyffMS / aiteracja.pl |
| Complete legal-clearance claims | Prohibited |
| Patent/trademark/regulatory clearance claims | Prohibited |

## Decision scope

Before materially changing the distribution or business model, the maintainer must record:

- exact product version, source commit/tag and final artifact checksum;
- territories and distribution channels;
- free, paid, sponsorship or support model;
- intended users/customer groups and excluded regulated uses;
- support, warranty, refund or indemnity commitments;
- external services, telemetry, accounts, uploads or support-data flows;
- important technical mechanisms and third-party compatibility claims.

A decision for one scope does not automatically approve another.

## Evidence package

The maintainer review should use immutable or versioned copies of:

### Product and release bytes

- final binary artifact and installer/store container, if applicable;
- archive/container SHA-256 and retained compliance report;
- source commit, tag, product version, SDK/runtime/RID and publish mode;
- `SBOM.spdx.json`, `LICENSE-REPORT.json` and `DEPENDENCIES.md`;
- license, exact .NET notices, redistribution notice, affiliation/DRM notice and `PRIVACY.md`.

### Governance and provenance

- `DCO.md`, `CONTRIBUTING.md`, pull-request template and DCO workflow;
- contribution provenance/history review and exception register;
- permissions or provenance evidence retained by the maintainer;
- security policy and support-data process.

### Brand, claims and marketing

- README, website pages, metadata and screenshots;
- release/store/directory/social/support wording;
- `docs/BRAND.md` and trademark-risk record;
- intended-purpose policy and third-party compatibility policy;
- patent-risk feature map and any public-source findings.

### Commercial scope, where applicable

- publisher/contracting identity;
- territories, channels, pricing/revenue model and customer groups;
- support/SLA, refund, warranty and liability statements;
- requested indemnities or non-infringement statements;
- processors, hosting, telemetry or support vendors.

## Required maintainer decisions

For the exact planned release, record whether:

1. Microsoft .NET redistribution files match the reviewed build configuration;
2. dependencies, notices, SBOM and license policy are complete enough for the release;
3. the current name/logo risk is accepted, monitored or requires a rename;
4. the general-purpose non-medical positioning remains intact;
5. public patent-risk findings require feature removal, scope reduction or accepted risk;
6. contribution provenance and DCO evidence are adequate;
7. privacy and support-data handling match the actual operating model;
8. warranty, support and third-party statements are accurate;
9. store, advertising, accessibility or product-safety rules add conditions;
10. each unresolved risk is accepted, mitigated, excluded or blocking.

## Decision record

Copy `MAINTAINER-RELEASE-REVIEW-TEMPLATE.md` to a dated file such as:

```text
docs/legal/MAINTAINER-RELEASE-REVIEW-<version>-<YYYY-MM-DD>.md
```

The record must identify:

- decision owner and date;
- exact product version, commit/tag and artifact checksum;
- territories, channels, revenue model and users covered;
- evidence reviewed;
- accepted, mitigated, excluded and blocking risks;
- mandatory conditions and re-review triggers;
- final decision: approved, approved with conditions or blocked.

## Re-review triggers

Repeat the internal review when any material part of the scope changes, including:

- product version or artifact bytes;
- publisher, channel, territory, revenue model or customer group;
- warranty, liability, support, refund or indemnity statements;
- product name, logo, marketing or compatibility claims;
- medical/intended-purpose positioning;
- rendering, capture, automation or platform mechanisms;
- dependency, runtime, license, notice or SBOM composition;
- telemetry, cloud service, account, upload or support-data flow;
- contribution/provenance model;
- regulator/store rules, legal assertions or third-party objections.

## Release language

Maintainers may state that a release passed the project's internal technical and risk review. They must not state that it is legally cleared, patent-cleared, non-infringing, trademark-cleared, MDR-approved or professionally audited.
