# SightAdapt website source

The public product site is a dependency-free static site whose content follows issue #124.

- Canonical production URL: `https://aiteracja.pl/sightadapt/`
- GitHub Pages fallback: `https://keyffms.github.io/SightAdapt/`
- GitHub Releases remain the authoritative binary source.

## Build for GitHub Pages

```powershell
python .\tools\build-site.py --base-path /SightAdapt/ --output .\artifacts\site
```

## Build for the canonical Aiteracja path

```powershell
python .\tools\build-site.py --base-path /sightadapt/ --output .\artifacts\site
```

## Validate

```powershell
python .\tools\verify-site.py .\artifacts\site --base-path /SightAdapt/
```

## Preview

```powershell
python -m http.server 8000 --directory .\artifacts\site
```

The build copies the canonical SVG brand assets from `assets/brand/` into the generated site, so the deployed site does not depend on a third-party CDN or raw GitHub asset request.

The initial site intentionally contains no analytics, cookies, remote fonts, advertising, session replay or third-party embeds.
