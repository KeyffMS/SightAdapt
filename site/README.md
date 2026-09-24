# SightAdapt™ static product site

This directory contains the dependency-free static implementation of the SightAdapt product site defined by issue #124 and implemented under #99.

## Hosting model

- canonical public product root: `https://aiteracja.pl/sightadapt/`
- GitHub Pages origin/fallback: `https://keyffms.github.io/SightAdapt/`
- optional convenience address: `https://sightadapt.aiteracja.pl/` (permanent redirect only; not a second canonical site)
- official binary authority: GitHub Releases

The site intentionally uses relative asset and navigation paths so the same tree can be served below both `/SightAdapt/` and `/sightadapt/`.

## Architecture

The site is plain HTML and CSS:

- no JavaScript framework;
- no build-time package manager;
- no cookies;
- no analytics;
- no tracking pixels;
- no remote font requests;
- no unsolicited third-party embeds.

Canonical repository SVG assets are copied into `site/assets/` so the public pages do not depend on runtime requests to GitHub raw content.

## Pages

- `/` — product overview
- `/features/`
- `/download/`
- `/docs/`
- `/how-it-works/`
- `/faq/`
- `/releases/`
- `/privacy/`
- `/legal/`
- `/security/`
- `/support/`

## Local preview

From the repository root, run any local static HTTP server. With Python 3:

```powershell
python -m http.server 8000 --directory .\site
```

Then open:

```text
http://localhost:8000/
```

The site does not require Python to build or deploy; Python is only one convenient local preview server.

## Validation

Run:

```powershell
.\tools\test-site.ps1
```

The validator checks the required page inventory, document structure, canonical URLs, accessibility basics, local link/assets, required trademark/footer wording and the absence of remote active content.

## Deployment

`.github/workflows/pages.yml` deploys the exact `site/` tree only after a successful `Build and test SightAdapt` run for a push to `main`. Manual dispatch is also available for maintainers.

Repository Settings → Pages must use GitHub Actions as the Pages source. The repository `main` branch must also be protected according to issue #87 before the protected-default-branch acceptance criterion can be considered complete.

SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.
