# SightAdapt maintainer release-risk decision — <version> — <date>

> Replace every placeholder before committing this record. This is an internal project decision, not legal advice or evidence of external clearance.

## Decision summary

| Field | Value |
|---|---|
| Decision | `<approved / approved with conditions / blocked>` |
| Decision date | `<YYYY-MM-DD>` |
| Decision owner | `<maintainer or publisher>` |
| Product version | `<version>` |
| Source commit/tag | `<commit and tag>` |
| Final artifact name | `<filename>` |
| Final artifact SHA-256 | `<64-character hash>` |
| Compliance report | `<path or release reference>` |
| Review-by date or trigger | `<date or condition>` |

## Scope

### Territories and channels

`<public GitHub distribution / installer / named store / direct distribution / other>`

### Revenue and support model

`<free / paid / sponsorship / support / other>`

### Users and customers

`<individual / enterprise / public sector / healthcare excluded / other>`

### Product and feature scope

`<exact edition, platform, rendering/capture features and exclusions>`

## Evidence reviewed

- [ ] Exact final artifact and checksum
- [ ] Source commit/tag and release metadata
- [ ] SPDX SBOM, dependency summary and license report
- [ ] SightAdapt license and third-party notices
- [ ] Microsoft .NET redistribution record, exact notices and package metadata
- [ ] Third-party names, affiliation and DRM notice
- [ ] Privacy/support-data policy and actual data flows
- [ ] DCO, contribution provenance and exception records
- [ ] Trademark/name/logo risk record and brand materials
- [ ] Intended-purpose policy and product claims
- [ ] Patent-risk feature map and public-source findings, where applicable
- [ ] README, website, screenshots, release/store/support materials
- [ ] Installer/store package and final compliance report, where applicable

Additional or excluded materials:

`<list>`

## Risk decisions

| Area | Evidence and limitation | Treatment |
|---|---|---|
| Open-source/dependency licensing | `<summary>` | `<accepted / mitigated / blocked>` |
| Microsoft redistribution | `<summary>` | `<accepted / mitigated / blocked>` |
| Trademark/name/logo | `<summary>` | `<accepted / rename / monitor / blocked>` |
| Privacy/data handling | `<summary>` | `<accepted / mitigated / blocked>` |
| Intended purpose and medical claims | `<summary>` | `<non-medical scope maintained / blocked>` |
| Patent risk | `<summary>` | `<accepted / mitigated / feature removed / blocked>` |
| Consumer/commercial terms | `<summary>` | `<accepted / not offered / blocked>` |
| Other | `<summary>` | `<treatment>` |

## Conditions before release

1. `<condition and evidence owner>`
2. `<condition and evidence owner>`

State `None` only when the maintainer decision has no conditions.

## Known and accepted limitations

- `<risk or uncertainty>`
- `<risk or uncertainty>`

This record must not state that SightAdapt is legally cleared, patent-free, non-infringing, trademark-cleared, MDR-approved or professionally audited.

## Final decision

`<Concise statement identifying the approved scope, conditions and excluded claims or activities.>`

## Re-review triggers

Repeat the maintainer review when:

- the final artifact differs from the recorded checksum;
- territories, channels, revenue model, customer types or publisher change;
- marketing, intended purpose, medical claims or compatibility claims materially change;
- dependencies, runtime, licenses, notices, SBOM or packaging change;
- privacy/data flows, external services or support-data handling change;
- relevant rendering/capture/automation mechanisms change;
- warranty, liability, support or indemnity statements change;
- a regulator/store rule, legal claim, assertion or third-party objection changes the risk;
- the stated review date or condition is reached.

Additional triggers:

`<list>`
