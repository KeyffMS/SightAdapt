# Patent-risk review for commercialization

## Current decision

| Field | Current position |
|---|---|
| Decision date | 2026-07-30 |
| Product version reviewed | `0.5.0.50-alpha` |
| Current distribution model | Free, open-source source code and alpha binary distribution |
| External patent audit | Not planned |
| Patent-clearance claim | Prohibited |
| Current risk treatment | Internal review, scope control and explicit risk acceptance |

Open-source licensing, original implementation and use of documented Windows APIs do not prove patent freedom to operate. SightAdapt must not be described as patent-free, patent-cleared or non-infringing.

## Technical feature map

Internal patent-risk review must consider at least:

| Feature area | SightAdapt behavior |
|---|---|
| Per-window visual transformation | User-selected color matrices applied to selected application windows without modifying the target process |
| Windows magnification effects | Windows Magnification API controls and color-effect matrices |
| Automatic foreground correction | Foreground tracking, application identity matching and automatic activation |
| Overlay rendering | Layered, input-transparent and non-activating overlay windows |
| Popup-menu correction | Native `#32768` popup detection and transient overlays |
| Source exclusion/filtering | Exclusion of SightAdapt overlays to prevent recursive capture |
| Scope selection | Client area, full window, current monitor and virtual desktop |
| Transition handling | Overlay reuse/retargeting and foreground-switch grace period |
| Safety shutdown | Immediate removal of overlays and cleanup on explicit or fault paths |

The exact source, architecture documentation and release binary are the authoritative technical evidence.

## Internal public-source review

For a material commercial launch or major mechanism change, maintainers should:

1. define planned activities, territories, channels, customer types and representations;
2. search public patent databases and ordinary technical sources using the feature map;
3. record potentially relevant families, owners, dates, status and territories where identifiable;
4. compare public claim language at a high level with implemented behavior;
5. record uncertainty where claim interpretation or legal status cannot be reliably resolved internally;
6. decide whether to accept risk, remove/narrow a feature, exclude a territory/use, seek a license independently or block the launch.

This is a business-risk screen, not a legal FTO opinion.

## Decision record

Record only repository-safe information:

- reviewer/decision owner and date;
- exact version/commit and artifact;
- scope, territories and commercial activity;
- public sources searched;
- relevant feature areas;
- known uncertainties;
- decision: proceed, proceed with conditions, redesign/scope reduction or block;
- re-review triggers.

## Design and scope controls

Possible risk treatments include:

- removing or narrowing automatic activation;
- limiting correction scope or supported window types;
- disabling popup-menu handling;
- changing the rendering/capture architecture;
- making a feature opt-in;
- excluding a territory, channel or customer use;
- declining warranties, indemnities and non-infringement statements;
- obtaining a license through ordinary business negotiation;
- accepting documented residual risk.

These are planning choices, not conclusions that a design avoids any claim.

## Re-review triggers

Repeat the internal patent-risk review before:

- paid licensing, subscriptions or paid binary distribution;
- enterprise warranties, indemnities or non-infringement language;
- major commercial marketing or large-scale deployment;
- adding a new rendering, capture, automation, AI-analysis or platform mechanism;
- materially changing foreground tracking, overlay filtering, menu correction or recursive-capture prevention;
- expanding commercial activity to a new territory;
- receiving a patent assertion, licensing request or credible conflict notice.

## Mandatory language control

Do not state that SightAdapt is patent-free, patent-cleared, safe from infringement or covered by an FTO opinion. Maintainers may state only that they completed an internal public-source patent-risk review and accepted or mitigated the recorded risk for a defined scope.
