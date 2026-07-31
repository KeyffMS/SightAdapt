# Public materials, third-party names and asset review

## Purpose

This process controls SightAdapt materials that mention third-party products, display third-party names or assets, make compatibility statements, or discuss protected content and digital-rights management (DRM).

The authoritative notice is `THIRD-PARTY-NAMES-AND-DRM-NOTICE.txt`. `release/public-materials.json` records the exact maintained surfaces and their reviewed Git blob identities. Internal review is a maintainer risk control, not legal advice, trademark clearance or permission from a third party.

## Maintained versus planned surfaces

A surface is `maintained` only when it is listed in the registry with:

- an exact source or evidence path;
- an immutable Git blob SHA;
- a review date and reviewer;
- the third-party names and compatibility claims it contains;
- every third-party asset and its documented use basis, or an explicit empty asset list;
- an `approved` review result.

A future website page, GitHub Release, store listing, directory entry, mirror, social profile, announcement, screenshot or video remains `planned` until its implementation adds exact evidence and passes `tools/verify-public-materials.ps1`.

## Compatibility statements

Use narrow factual wording such as:

- `works with user-selected Windows applications`;
- `tested with <product/version>`;
- `compatible with supported application windows`;
- `uses Windows Magnification API`;
- `applies an overlay to an application selected by the user`.

A named statement must identify the tested version when practical and state relevant limits. Do not use `official integration`, `partner`, `approved by`, `certified by`, `endorsed by`, `supported by` or similar wording unless a current written relationship authorizes that exact statement.

## Screenshots, logos and other assets

Prefer SightAdapt-controlled interfaces, project-owned sample content and the canonical SightAdapt brand assets. A material must not include a third-party logo, branded icon, screenshot dominated by third-party trade dress, certification badge or promotional asset unless the registry records:

- the owner or source;
- the exact asset path and Git blob SHA or archived evidence;
- the use basis or permission record;
- the approved purpose and surface;
- the review date and reviewer.

Locally displayed application icons and file metadata are identification data, not SightAdapt brand assets. Do not reuse them in project-controlled marketing without a separate recorded review.

## About window

The current About window was reviewed as an application-owned surface. It contains SightAdapt's own product identity, version, publisher, website, repository and license. It does not contain a named third-party application, third-party logo, partnership statement or endorsement claim. A change to About content or assets requires a registry update and review.

## Protected content and DRM

SightAdapt is not intended to decrypt, unlock, evade, bypass or interfere with DRM, authentication, licensing, encryption, capture prevention or another access control. Protected content may remain blank, unavailable, unchanged or unfilterable. Do not publish troubleshooting or compatibility material that describes bypassing those restrictions.

## Publication workflow

Before publishing or materially updating a controlled surface:

1. add or update its registry entry;
2. record every named third party, compatibility statement and asset;
3. archive the final text or asset in the repository where practical;
4. record the exact Git blob SHA or other immutable evidence;
5. run `tools/verify-public-materials.ps1` and its negative tests;
6. obtain the maintainer review recorded in the registry;
7. publish only the reviewed bytes or text;
8. record the public URL and publication date when the channel becomes active.

A public account requiring private credentials is managed interactively, but its final public text and assets must still be represented by repository evidence.

## Objection or correction procedure

When a rights holder or platform objects to a name, mark, screenshot, compatibility statement or asset:

1. preserve the objection privately when it contains personal or confidential information;
2. archive or hash the affected public material and record its URL and date;
3. pause new publication or promotion of the disputed material;
4. create or update a GitHub Issue with a non-confidential summary;
5. remove, neutralize or replace the material when continued use is not supported by the recorded basis;
6. update the registry, notice and dependent surfaces;
7. rerun the public-material and release-package gates;
8. record the maintainer decision and any conditions before republication.

Do not send automated legal notices or claim that the internal review determines trademark rights.

## Re-review triggers

Repeat review when:

- a new public surface is activated;
- a named product/version compatibility claim is added or changed;
- a third-party logo, icon, screenshot, badge or trade dress is proposed;
- an endorsement, partnership or certification is proposed;
- protected-content behavior changes;
- the SightAdapt trademark-risk record changes;
- a third party objects;
- a source path or pinned Git blob SHA changes.
