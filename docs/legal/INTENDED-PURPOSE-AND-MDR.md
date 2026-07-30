# Intended purpose and medical-device claims policy

## Decision record

| Field | Reviewed value |
|---|---|
| Review date | 2026-07-30 |
| Product version | `0.5.0.50-alpha` |
| Publisher | KeyffMS / aiteracja.pl |
| Current positioning | General-purpose accessibility and display-personalization software |
| Medical purpose intended | No |
| External classification audit | Not planned |
| Decision owner | KeyffMS / aiteracja.pl |

This is an internal product-scope and claims decision. It is not a legal opinion, medical-device approval or professional classification assessment.

## Project scope

SightAdapt is general-purpose Windows accessibility and personalization software for people who choose to alter the visual presentation of selected application windows.

Intended users are ordinary Windows users. Use is self-directed in personal, educational or workplace computing environments on supported Windows computers.

The software applies user-selected display transformations through a separate overlay and stores per-application preferences. It does not intentionally modify another application's files or process memory.

SightAdapt is not intended to:

- diagnose, prevent, monitor, predict, prognose, treat or alleviate a disease;
- diagnose, monitor, treat, alleviate or medically compensate for an injury or disability;
- measure visual function or determine a clinical state;
- recommend treatment, therapy, medication or clinical action;
- replace an examination, diagnosis, prescription or professional advice;
- provide patient-specific clinical decision support;
- form part of a medical device or clinical workflow.

The maintained project scope is therefore **general-purpose accessibility/display-personalization software, not software intended for a medical purpose**.

## Approved non-medical statement

> SightAdapt is general-purpose accessibility and display-personalization software. It is not intended to diagnose, treat, prevent, monitor or clinically alleviate any disease, injury or disability, and it is not a substitute for professional medical or eye-care advice.

The statement must not contradict actual features, instructions or marketing.

## Claims policy

### Permitted descriptions

Product-controlled materials may accurately describe:

- per-application visual accessibility;
- display or color correction;
- user-selected brightness, contrast, saturation, hue and inversion settings;
- personalization of application-window presentation;
- saved profiles and assignments;
- technical behavior, privacy and limitations.

### Prohibited claims and features

Do not claim or imply that SightAdapt:

- diagnoses or detects a named condition;
- prevents, treats, cures, monitors or clinically alleviates a condition;
- restores, improves or corrects vision clinically;
- compensates medically for visual impairment or disability;
- has clinically proven effectiveness or therapeutic benefit;
- is prescribed, recommended or endorsed by healthcare authorities;
- is suitable for a patient, treatment plan, therapy or clinical workflow;
- replaces professional examination or treatment.

Testimonials, screenshots, metadata, store categories and support answers follow the same rule.

## Terminology rules

| Term | Rule |
|---|---|
| `correction` | Use only for display, color or visual-presentation correction; never clinical vision correction. |
| `accessibility` | Permitted for general-purpose access and personalization. |
| `assistive` | Use only in a general accessibility sense; do not describe SightAdapt as a medical device. |
| `compensation` | Do not use for a disease, injury or disability. |
| `patient` | Do not use for current users. Use `user`. |
| `therapy`, `treatment`, `clinical` | Prohibited as product-purpose or effectiveness claims. |
| named diagnoses or disorders | Do not target them or claim outcomes for them. |

## Materials reviewed

The maintainer review covers repository README and documentation, brand/release wording, implemented feature descriptions, About text, privacy/support materials, screenshots and product-controlled website/store copy when published.

Before publishing or materially updating an external website or store listing, compare the rendered text, screenshots, metadata and category selection against this policy and record the maintainer review.

## Change policy

The following proposals are outside the current SightAdapt scope and must not be merged or marketed under the existing policy:

- targeting a named disease, disorder, impairment, injury or disability;
- patient, clinician, prescription, therapy or treatment workflows;
- medical or clinical effectiveness claims;
- measurements, scores or recommendations about visual function or health;
- patient-specific analysis or clinical decision support;
- integration with a medical device or regulated clinical system;
- use of medical data to determine an output;
- medical-device categories, reimbursement claims or healthcare procurement positioning;
- claims of medically compensating for a disability.

A maintainer may propose a separate future regulated-product project, but it is not part of SightAdapt unless this policy and the complete product plan are explicitly replaced. No external audit is required by the current project plan because medical positioning is excluded rather than pursued.

## Release gate

Do not publish medical, therapeutic or clinical claims. A release passes this policy only when the maintained general-purpose, non-medical scope remains intact.
