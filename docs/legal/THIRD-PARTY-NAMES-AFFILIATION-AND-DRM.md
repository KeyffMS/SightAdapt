# Third-party names, affiliation and protected-content policy

## Approved notice

The package source of truth is `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt`. It must be included in every binary distribution and preserved in store, installer and mirror packaging.

Approved reusable summary:

> Third-party product names and trademarks are the property of their respective owners and are used only to identify applications selected or configured by the user. SightAdapt is not affiliated with, sponsored by or endorsed by Microsoft or those owners unless an explicit written relationship is identified. SightAdapt does not circumvent DRM or other access controls, and protected content may remain unavailable or unfilterable.

## Identification-only use

SightAdapt stores or displays an application name, executable name, path or Windows file description only to help the user identify and configure a local application assignment. That use does not claim ownership of the product name or create an official integration.

Do not use a third-party name more prominently than reasonably needed to describe compatibility or identify a user-selected target. Do not incorporate a third-party mark into the SightAdapt product name, package identifier, release title or primary branding.

## Affiliation and endorsement

Unless a current written agreement expressly states otherwise, product-controlled materials must say or clearly remain consistent with the following:

- SightAdapt is independently published by KeyffMS / aiteracja.pl;
- Microsoft does not publish, sponsor, certify or endorse SightAdapt;
- owners of user-selected applications do not publish, sponsor, certify or endorse SightAdapt;
- technical compatibility does not imply a partnership, official integration, support obligation or warranty from the third party.

Any actual relationship must be described narrowly according to its written scope and review date.

## Compatibility wording

Preferred neutral wording:

- `works with user-selected Windows applications`;
- `compatible with supported application windows`;
- `tested with <product/version>`;
- `uses Windows Magnification API`;
- `applies an overlay to an application selected by the user`.

Avoid wording that implies a relationship or guarantee, including:

- `official integration`;
- `partnered with`;
- `approved by`;
- `certified by`;
- `supported by <third party>`;
- `built for <third-party product>` when no authorization exists;
- third-party warranty or endorsement claims.

A factual compatibility statement should identify the tested version or limitations when relevant and should use `works with` or `tested with`, not partnership language.

## Logos, screenshots and trade dress

Do not use third-party logos, branded icons, product artwork, store badges, screenshots dominated by third-party trade dress or other brand assets in SightAdapt-controlled materials without permission or another documented lawful basis.

A screenshot used to explain SightAdapt should:

- show only what is necessary to demonstrate SightAdapt behavior;
- avoid implying joint branding or endorsement;
- follow the privacy/redaction policy;
- identify third-party software only when necessary;
- preserve any required attribution and avoid misleading alteration.

Locally displayed file icons or metadata remain the third party's material and are not reusable SightAdapt brand assets.

## DRM and access controls

SightAdapt is a display overlay. It is not intended or designed to:

- decrypt or unlock content;
- evade subscription, licensing or authentication checks;
- bypass DRM or protected-content restrictions;
- defeat capture prevention or operating-system security decisions;
- provide access that the user or operating system does not already have.

Protected surfaces may render blank, remain unchanged or be unavailable to Windows magnification/capture facilities. Documentation must state this as a compatibility limitation, not as a defect to be bypassed.

Do not accept feature requests or support instructions whose purpose is to circumvent an access control. Security-sensitive requests should follow `SECURITY.md` and the privacy policy.

## About-window and current materials review — 2026-07-29

The current About window contains SightAdapt's own product name and icon, version, publisher, website, repository and license. It does not display a third-party application name, logo or endorsement statement. No wording change to the About window is required for this issue.

Current repository screenshots/brand assets are SightAdapt-controlled assets and do not intentionally incorporate third-party logos. Any future screenshot, store listing or promotional asset must be reviewed against this policy.

The wording was reviewed together with the preliminary trademark-clearance record in `docs/legal/TRADEMARK-CLEARANCE-2026-07-27.md`. That record does not create third-party endorsement or permission to use another party's marks.

## Required surfaces

The approved summary or a direct link to the full notice must be present in:

- repository README and legal documentation;
- binary package legal bundle;
- GitHub release descriptions when third-party compatibility is discussed;
- store and directory descriptions;
- installer legal/readme surfaces where available;
- compatibility pages or support articles using third-party names.

## Change control

Review this policy when:

- a partnership, certification or endorsement is proposed;
- a third-party logo or screenshot is proposed;
- a compatibility claim names a specific product;
- protected-content behavior changes;
- a DRM/access-control request is received;
- the trademark-clearance record changes;
- a third party objects to the use of its name or mark.
