# Formal legal review and release gate

## Current status

| Field | Current position |
|---|---|
| Status date | 2026-07-29 |
| Responsible publisher/support party | KeyffMS / aiteracja.pl |
| Current approved stage | Free, open-source alpha development and GitHub distribution |
| Production-ready claim | Not approved |
| Paid licensing or sales | Not approved |
| Major-store production distribution | Not approved |
| Enterprise legal warranties/indemnities | Not approved |
| Formal counsel sign-off | Not obtained |

Repository policies, notices and technical compliance controls are preparation materials. They are not a privileged legal opinion, professional trademark clearance, patent FTO opinion, GDPR assessment or medical-device classification decision.

SightAdapt must not be described as completely legally cleared, patent-cleared, non-infringing, MDR-compliant or production-approved until qualified professionals complete the reviews applicable to the planned activity.

## Responsible publisher and support party

The current public publisher and person/organization responsible for SightAdapt-controlled release and support decisions is:

**KeyffMS / aiteracja.pl**

The public contact route is the repository owner and issue tracker at `https://github.com/KeyffMS/SightAdapt`. Sensitive information must use the private-contact procedure in `PRIVACY.md` rather than a public issue.

Before a commercial launch, counsel must confirm whether a separate legal entity, registered business identity, address, tax status, consumer-contract party or dedicated support contact is required for the planned territories and channels.

## Current operating model

| Dimension | Current alpha position |
|---|---|
| Territories | Public internet availability through GitHub; no territory-specific commercial launch approved |
| Distribution channels | Source repository and verified GitHub alpha artifacts |
| Revenue model | Free/open-source; no approved paid license, subscription, sale or advertising model |
| Customer/user types | Individual users, developers and evaluators; no approved enterprise, healthcare or regulated-customer offer |
| Support model | Best-effort repository support; no SLA or paid support obligation |
| Store distribution | No approved production listing in a major store |
| Warranties | Repository MIT warranty disclaimer and separate third-party/Microsoft terms; no commercial warranty |

Counsel sign-off must define the exact launch territories, channels, commercial model, contracting party, customer groups and support obligations. A sign-off for one scope does not automatically cover another.

## Counsel review package

Provide counsel with immutable or versioned copies of the exact materials for the proposed launch:

### Product and release bytes

- exact final binary artifact and installer/store container, if applicable;
- archive/container SHA-256 and retained compliance report;
- source commit, release tag, product version, SDK/runtime/RID and publish mode;
- `SBOM.spdx.json`, `LICENSE-REPORT.json` and `DEPENDENCIES.md`;
- `LICENSE.txt`, exact `THIRD-PARTY-NOTICES.txt`, `DOTNET-LICENSE-NOTICE.txt`, `DOTNET-NOTICE-METADATA.json` and `MICROSOFT-DOTNET-REDISTRIBUTION.txt`;
- `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt` and `PRIVACY.md`.

### Governance and provenance

- `DCO.md`, `CONTRIBUTING.md`, pull-request template and DCO workflow;
- contribution provenance/history review and exception register;
- relevant permissions or provenance evidence stored privately;
- security policy and current support-data process.

### Brand, claims and marketing

- current README, website pages, metadata and screenshots;
- release notes, store listing, directory text, advertising/social materials and support descriptions;
- `docs/BRAND.md` and preliminary trademark-clearance record;
- intended-purpose/MDR policy and any proposed condition-specific or clinical wording;
- third-party compatibility, affiliation and DRM policy.

### Commercial and contractual scope

- planned legal entity/contracting party;
- territories, channels, price/revenue model and taxes;
- customer groups and enterprise/consumer status;
- proposed EULA, terms of sale/use, support/SLA, refund and warranty language;
- requested indemnities, non-infringement statements or insurance requirements;
- data processors/services, hosting, telemetry or support vendors.

## Required review topics

Qualified counsel must determine or confirm, for the exact planned launch:

1. whether the Microsoft .NET redistribution analysis and package implementation are acceptable;
2. whether third-party dependencies, notices, SBOM and license policy are adequate;
3. whether the SightAdapt name/logo trademark decision is adequate for the territories/channels, and whether professional clearance or registration is required;
4. whether the current general-purpose intended-purpose/MDR position remains correct for the product and marketing;
5. whether a patent FTO opinion is required and whether the patent gate has been satisfied;
6. whether contributor provenance/DCO and existing history are adequate;
7. whether privacy, support-data handling, retention and external services satisfy the intended operating model;
8. whether end-user warranty, liability, support, consumer, export and third-party language is appropriate;
9. whether store, accessibility, advertising, product-safety or other regulatory obligations apply;
10. which unresolved risks may be accepted, which require conditions and which block release.

Different specialists may be required for trademark, patent, privacy/data protection or medical-device questions. A general review must not be represented as covering a specialty it expressly excludes.

## Current public issue treatment

| Area | Repository preparation | Professional status |
|---|---|---|
| .NET redistribution | Exact-version notices, redistribution analysis and package notice implemented | Counsel confirmation required before production/paid release |
| Dependency licensing | SPDX SBOM, policy report, notices and final package gate implemented | Ambiguous/custom terms require professional judgment |
| Trademark | Preliminary public-source knockout record completed | Professional clearance optional for current stage; required as counsel determines before major commercialization/registration |
| Intended purpose/MDR | General-purpose non-medical positioning documented | Qualified assessment required before medical targeting/claims |
| Patents/FTO | Technical map and commercialization gate documented | No FTO opinion; mandatory before activities defined by the patent gate |
| Privacy | Local-processing/support-data policy documented | Counsel/DPO review required for intended commercial model, territories and new data flows |
| Contributions | DCO and provenance process implemented | Counsel may require additional historical evidence or CLA for a future model |
| End-user terms | Package notices and MIT disclaimer exist | Commercial EULA/consumer/support/warranty terms not approved |

## Privileged advice and evidence

Do not commit privileged legal advice, claim charts, attorney communications, confidential licenses, personal identity documents, contracts or sensitive permission records to the public repository.

The responsible publisher must maintain a private review file containing:

- engagement scope and counsel identity;
- exact materials supplied;
- privileged advice and working papers;
- supporting permissions/contracts;
- accepted-risk approvals;
- evidence that release conditions were satisfied.

The public repository contains only the minimum non-confidential decision record.

## Public sign-off record

After counsel review, copy `LEGAL-SIGNOFF-TEMPLATE.md` to a dated file such as:

```text
docs/legal/LEGAL-SIGNOFF-<version>-<YYYY-MM-DD>.md
```

The public record must contain:

- reviewer role/qualification, without disclosing privileged communications;
- review date;
- exact product version, commit, tag and artifact checksum;
- territories, channels, revenue model and customer types covered;
- materials and specialist areas reviewed;
- final decision: approved, approved with conditions, redesign/review required or not approved;
- mandatory pre-release conditions and unresolved non-confidential risks;
- expiration/re-review date and invalidation triggers.

Do not mark a release approved until every mandatory condition is evidenced privately and reflected in the public status.

## Invalidation and renewed-review triggers

A sign-off is invalidated or requires confirmation when any material part of its scope changes, including:

- product version or final artifact after the reviewed checksum;
- legal entity, publisher, support provider or contracting party;
- territory, store/channel, revenue model or customer group;
- EULA, warranty, liability, support, refund or indemnity terms;
- product name, logo or significant marketing/compatibility claim;
- medical/intended-purpose positioning or condition-specific feature;
- rendering, capture, automation or platform mechanism relevant to patent analysis;
- dependency, runtime, license, notice or SBOM composition;
- telemetry, update checks, cloud service, account, upload or support-data flow;
- contribution/provenance model;
- applicable law, regulator/store rule, claim/assertion or third-party objection;
- counsel's stated expiration or limited scope.

Minor changes may be covered only when counsel's written scope expressly permits them.

## Release decision

Current free/open-source alpha work may continue under the documented limitations. Production-ready claims, paid distribution, commercial licensing, major-store production publication, enterprise warranties/indemnities and complete legal-clearance claims remain blocked until a valid professional sign-off record exists for the exact launch.
