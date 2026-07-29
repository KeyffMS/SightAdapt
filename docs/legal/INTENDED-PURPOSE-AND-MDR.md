# Intended purpose and medical-device claims policy

## Decision record

| Field | Reviewed value |
|---|---|
| Review date | 2026-07-29 |
| Product version | `0.5.0.50-alpha` |
| Publisher | KeyffMS / aiteracja.pl |
| Current positioning | General-purpose accessibility and display-personalization software |
| Medical purpose intended | No |
| Formal professional classification opinion | Not obtained; required before introducing medical claims or targeting diagnosed conditions |

This document controls the current product positioning. It is a maintainer regulatory-risk decision, not a legal opinion or a professional medical-device classification assessment.

## Regulatory basis considered

Regulation (EU) 2017/745 distinguishes software specifically intended for a medical purpose from general-purpose software. Its definition of intended purpose includes the manufacturer's labels, instructions, promotional/sales materials and statements. The current review also considered European Commission guidance `MDCG 2019-11 rev.1`, published in June 2025, on qualification and classification of software.

Authoritative sources:

- https://eur-lex.europa.eu/legal-content/EN/TXT/?uri=CELEX:32017R0745
- https://health.ec.europa.eu/medical-devices-sector/new-regulations/guidance-mdcg-endorsed-documents-and-other-guidance_en

## Approved intended purpose

SightAdapt is general-purpose Windows accessibility and personalization software for people who choose to alter the visual presentation of selected application windows.

Intended users are ordinary Windows users, including users who prefer different brightness, contrast, saturation, hue or color-inversion presentation. Use is self-directed in personal, educational or workplace computing environments on supported Windows computers.

The software applies user-selected display transformations through a separate overlay. It stores per-application preferences and does not intentionally modify another application's files or process memory.

SightAdapt is not intended to:

- diagnose, prevent, monitor, predict, prognose, treat or alleviate a disease;
- diagnose, monitor, treat, alleviate or medically compensate for an injury or disability;
- measure visual function or determine a clinical state;
- recommend treatment, therapy, medication or clinical action;
- replace an examination, diagnosis, prescription or advice from a qualified professional;
- provide patient-specific clinical decision support;
- be used as part of a medical device or clinical workflow without a new assessment.

The current product is therefore positioned as **general-purpose accessibility/display-personalization software, not software intended for a medical purpose**.

## Approved non-medical statement

Use this statement where a purpose clarification is appropriate:

> SightAdapt is general-purpose accessibility and display-personalization software. It is not intended to diagnose, treat, prevent, monitor or clinically alleviate any disease, injury or disability, and it is not a substitute for professional medical or eye-care advice.

The statement must not be used to contradict features, instructions or marketing that actually communicate a medical purpose.

## Claims policy

### Permitted descriptions

Product-controlled materials may accurately describe:

- per-application visual accessibility;
- display or color correction;
- user-selected brightness, contrast, saturation, hue and inversion settings;
- personalization of application-window presentation;
- saved profiles and assignments;
- technical behavior, privacy and limitations.

### Prohibited without renewed assessment and evidence

Do not claim or imply that SightAdapt:

- diagnoses or detects a named condition;
- prevents, treats, cures, monitors or clinically alleviates a condition;
- restores, improves or corrects vision clinically;
- compensates medically for visual impairment or disability;
- has clinically proven effectiveness, sensitivity, specificity or therapeutic benefit;
- is prescribed, recommended or endorsed by healthcare professionals or authorities;
- is suitable for a patient, treatment plan, therapy or clinical workflow;
- can replace professional examination, treatment or assistive-device assessment.

Testimonials, screenshots, search metadata, store categories and support answers must follow the same rule. A third party's statement must not be adopted or amplified by the project when it would create a medical claim.

## Terminology rules

| Term | Rule |
|---|---|
| `correction` | Use only for display, color or visual-presentation correction; never claim clinical vision correction. |
| `accessibility` | Permitted for general-purpose access and personalization. |
| `assistive` | Use cautiously and only in a general accessibility sense; do not describe SightAdapt as an assistive medical device. |
| `compensation` | Do not use for injury, disability or a medical condition without renewed classification review. |
| `patient` | Do not use for current users. Use `user`. |
| `therapy`, `treatment`, `clinical` | Prohibited as product-purpose or effectiveness claims for the current release. |
| named diagnoses or disorders | Do not target or claim outcomes for them without professional assessment and the required evidence. |

## Materials reviewed

The 2026-07-29 review covered:

- repository `README.md` and documentation index;
- canonical descriptions and website rules in `docs/BRAND.md`;
- release/store-description rules in `docs/RELEASING.md`;
- implemented feature descriptions in `docs/FEATURES.md`;
- assembly description and About-dialog wording sourced through `ProductInfo.Tagline`;
- current About dialog fields (product identity, version, publisher, links and license);
- privacy, contribution and support materials;
- supplied brand screenshots/assets and current repository release materials.

No product-controlled repository wording was identified that claims diagnosis, treatment, therapy or clinical effectiveness. The canonical product website URL is controlled by the same brand description policy. Before publishing or materially updating the external website or a store listing, the responsible publisher must manually compare the rendered text, screenshots, metadata and category selection against this document because external content may change outside the repository review.

## Review triggers

Obtain a new written classification assessment before merging, marketing or distributing a change that introduces any of the following:

- targeting a named disease, disorder, impairment, injury or disability;
- patient, clinician, prescription, therapy or treatment workflows;
- medical or clinical effectiveness claims;
- measurements, tests, scores or recommendations about visual function or health;
- patient-specific analysis or clinical decision support;
- integration with a medical device, electronic health record or regulated clinical system;
- use of medical data to determine an output;
- marketing in a medical-device category, reimbursement pathway or healthcare procurement process;
- claims of medically compensating for a disability;
- a feature or marketing change that a qualified regulatory professional considers potentially medical.

Before such a change, obtain a written assessment from a qualified EU medical-device regulatory professional and resolve any resulting MDR obligations. Record the reviewed version, materials, territories, professional role, date and non-confidential decision. Privileged or confidential advice remains outside the public repository.

## Release gate

Do not market SightAdapt for a named medical condition or claim medical, therapeutic or clinical outcomes until the professional classification assessment and any resulting regulatory obligations are complete. Current free distribution may use only the approved general-purpose positioning.
