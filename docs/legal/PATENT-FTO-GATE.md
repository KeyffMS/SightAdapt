# Patent freedom-to-operate commercialization gate

## Current decision

| Field | Current position |
|---|---|
| Decision date | 2026-07-29 |
| Product version reviewed | `0.5.0.50-alpha` |
| Current distribution model | Free, open-source source code and alpha binary distribution |
| Paid licensing | Not approved |
| Enterprise warranty or indemnity | Not approved |
| Investor or customer non-infringement representation | Not approved |
| Large-scale commercial deployment | Not approved |
| Professional patent FTO opinion | Not obtained |

No statement in this repository represents that SightAdapt is patent-free, cleared for commercial use or non-infringing. Open-source licensing, original implementation and use of documented Windows APIs do not establish patent freedom to operate.

The current project has no approved commercial activity, customer segment, revenue model or launch territory requiring an immediate professional FTO project. This issue is therefore closed as not planned for the present free/open-source stage, with a mandatory re-opening gate before commercialization.

## Technical feature map for future counsel

A future FTO review must examine at least the following implemented mechanisms and their combinations:

| Feature area | SightAdapt behavior to map against patent claims |
|---|---|
| Per-window visual transformation | User-selected color matrices applied to the visual presentation of selected application windows without modifying the target process |
| Windows magnification color effects | Use of Windows Magnification API controls and color-effect matrices as the rendering mechanism |
| Automatic foreground correction | Foreground-window tracking, process/application identity matching and automatic activation of saved assignments |
| Overlay rendering | Separate layered, input-transparent and non-activating overlay windows aligned with target geometry |
| Popup-menu-specific correction | Detection of native `#32768` popup menus and separate transient overlays/profile selection |
| Source exclusion/filtering | Exclusion of SightAdapt overlay windows from magnifier sources to avoid recursive capture |
| Recursive-capture prevention | Maintaining active overlay/filter lists and removing transient resources when targets disappear |
| Scope selection | Client area, full window, current monitor and virtual-desktop targeting |
| Transition handling | Reuse/retargeting of an overlay and a short grace period during foreground switching |
| Safety shutdown | Immediate removal of overlays and resource cleanup on explicit or fault paths |

The detailed implementation authorities are documented in `docs/ARCHITECTURE.md` and `docs/FEATURES.md`. Counsel should receive the exact commercial release source and binary, not only this summary.

## Commercialization scope that must be defined before review

Before commissioning the FTO review, the publisher must record:

- paid or otherwise commercial activities planned;
- countries/territories where making, using, offering, selling, importing or supplying will occur;
- customer types and regulated/enterprise contexts;
- distribution channels and whether binaries, installers, services or licenses are supplied;
- warranties, indemnities or non-infringement representations requested;
- exact product version and technical mechanisms included;
- expected scale, revenue model and launch date.

A global public repository is not a substitute for defining the territories relevant to a commercial opinion.

## Required professional work

A qualified patent attorney or patent professional must, for the defined scope:

1. develop claim-oriented search concepts and classifications;
2. search relevant patent families for the feature areas above;
3. review active claims, owners, priority/filing dates, expiration, legal status and territorial coverage;
4. map potentially relevant independent and dependent claims to the exact product behavior;
5. assess claim construction, prosecution history and relevant limitations where necessary;
6. identify design-around, licensing, invalidity or scope-reduction options for material risks;
7. issue a written FTO opinion or equivalent risk decision appropriate to the planned territories and launch.

Search results or keyword hits alone are not an FTO opinion. Privileged legal analysis, claim charts and attorney communications must be stored outside the public repository.

## Repository-safe decision record

After professional review, add only a non-confidential record containing:

- reviewer role and professional qualification;
- review date;
- product version/commit and binary reviewed;
- territories and commercial activities covered;
- high-level scope of feature families reviewed;
- decision: proceed, proceed with conditions, redesign/license required or do not proceed;
- unresolved public conditions and expiration/re-review date.

Do not publish privileged claim charts, legal strategy or confidential licensing discussions.

## Design-around planning categories

If counsel identifies a material risk, potential engineering decisions may include:

- removing or narrowing automatic activation;
- limiting correction scope or supported window types;
- disabling separate popup-menu handling;
- changing the rendering/capture architecture;
- replacing persistent overlay/filter behavior with a different mechanism;
- making a feature opt-in or non-commercial;
- excluding a territory or customer use;
- obtaining a license.

These are planning categories, not conclusions that any particular design avoids a claim.

## Re-review triggers

Reopen the patent/FTO gate before:

- paid licensing, subscriptions or paid binary distribution;
- enterprise warranties, indemnities or contractual non-infringement language;
- investor or partner representations about patent clearance;
- major commercial marketing or large-scale deployment;
- adding a new rendering, capture, automation, AI-analysis or platform mechanism;
- materially changing foreground tracking, overlay filtering, menu correction or recursive-capture prevention;
- expanding commercial activity to a new territory;
- receiving a patent assertion, licensing request or credible conflict notice.

## Mandatory language control

Do not state that SightAdapt is patent-free, patent-cleared, safe from infringement or covered by an FTO opinion unless a qualified professional has provided a written opinion covering the exact statement, product version, activity and territory.

## Gate outcome

Free/open-source development and alpha distribution may continue without representing patent clearance. Commercialization remains prohibited until this record is reopened and the required professional review is completed for the planned launch.
