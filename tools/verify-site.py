#!/usr/bin/env python3
import argparse
import re
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import urlparse

REQUIRED = [
    "", "features", "download", "docs", "how-it-works", "faq",
    "releases", "privacy", "legal", "security", "support"
]
FORBIDDEN = [
    "google-analytics", "googletagmanager", "gtag(", "plausible.io",
    "matomo", "hotjar", "clarity.ms", "segment.com", "facebook.com/tr",
    "<iframe", "fonts.googleapis.com", "use.typekit.net"
]

class Inspector(HTMLParser):
    def __init__(self):
        super().__init__()
        self.titles = 0
        self.h1 = 0
        self.meta_description = 0
        self.canonical = []
        self.images_without_alt = []
        self.links = []
        self.heading_levels = []
        self.lang = None
        self.has_skip = False
        self.has_main = False

    def handle_starttag(self, tag, attrs):
        attrs = dict(attrs)
        if tag == "html":
            self.lang = attrs.get("lang")
        elif tag == "title":
            self.titles += 1
        elif tag == "h1":
            self.h1 += 1
            self.heading_levels.append(1)
        elif tag in {"h2", "h3", "h4", "h5", "h6"}:
            self.heading_levels.append(int(tag[1]))
        elif tag == "meta" and attrs.get("name") == "description" and attrs.get("content", "").strip():
            self.meta_description += 1
        elif tag == "link" and attrs.get("rel") == "canonical":
            self.canonical.append(attrs.get("href", ""))
        elif tag == "img":
            if "alt" not in attrs:
                self.images_without_alt.append(attrs.get("src", "<unknown>"))
        elif tag == "a":
            href = attrs.get("href")
            if href:
                self.links.append(href)
            if attrs.get("class") == "skip" and href == "#main":
                self.has_skip = True
        elif tag == "main" and attrs.get("id") == "main":
            self.has_main = True

def expected_file(root: Path, base: str, href: str):
    clean = href.split("#", 1)[0].split("?", 1)[0]
    if not clean.startswith(base):
        return None
    rel = clean[len(base):]
    if not rel:
        return root / "index.html"
    p = root / rel
    if rel.endswith("/"):
        return p / "index.html"
    return p

def main() -> int:
    parser = argparse.ArgumentParser(description="Validate generated SightAdapt static site.")
    parser.add_argument("site")
    parser.add_argument("--base-path", default="/SightAdapt/")
    parser.add_argument("--canonical-root", default="https://aiteracja.pl/sightadapt/")
    args = parser.parse_args()

    root = Path(args.site).resolve()
    base = args.base_path
    if not base.startswith("/"):
        base = "/" + base
    if not base.endswith("/"):
        base += "/"
    canonical = args.canonical_root.rstrip("/") + "/"

    failures = []

    for name in REQUIRED:
        path = root / name / "index.html" if name else root / "index.html"
        if not path.is_file():
            failures.append(f"Missing required page: {path}")

    for html_file in root.rglob("*.html"):
        text = html_file.read_text(encoding="utf-8")
        lower = text.lower()
        if "{{" in text or "}}" in text:
            failures.append(f"Unresolved template token in {html_file}")
        for marker in FORBIDDEN:
            if marker in lower:
                failures.append(f"Forbidden tracking/embed marker '{marker}' in {html_file}")

        inspector = Inspector()
        inspector.feed(text)

        if inspector.lang != "en":
            failures.append(f"{html_file}: html lang must be 'en'")
        if inspector.titles != 1:
            failures.append(f"{html_file}: expected one title, found {inspector.titles}")
        if inspector.h1 != 1:
            failures.append(f"{html_file}: expected one h1, found {inspector.h1}")
        if inspector.meta_description != 1:
            failures.append(f"{html_file}: expected one meta description")
        if len(inspector.canonical) != 1 or not inspector.canonical[0].startswith(canonical):
            failures.append(f"{html_file}: invalid canonical URL {inspector.canonical}")
        if inspector.images_without_alt:
            failures.append(f"{html_file}: images missing alt: {inspector.images_without_alt}")
        if not inspector.has_skip or not inspector.has_main:
            failures.append(f"{html_file}: missing skip link or main landmark")

        previous = 0
        for level in inspector.heading_levels:
            if previous and level > previous + 1:
                failures.append(f"{html_file}: heading level skips from h{previous} to h{level}")
            previous = level

        for href in inspector.links:
            parsed = urlparse(href)
            if parsed.scheme in {"http", "https", "mailto"}:
                continue
            if href.startswith("#"):
                continue
            target = expected_file(root, base, href)
            if target is not None and not target.exists():
                failures.append(f"{html_file}: broken internal link {href} -> {target}")

    required_assets = [
        root / "assets" / "styles.css",
        root / "assets" / "site.js",
        root / "assets" / "architecture.svg",
        root / "assets" / "brand" / "sightadapt-mark.svg",
        root / "assets" / "brand" / "sightadapt-lockup-dark.svg",
        root / "assets" / "brand" / "sightadapt-lockup-light.svg",
    ]
    for asset in required_assets:
        if not asset.is_file():
            failures.append(f"Missing required asset: {asset}")

    if failures:
        print("SightAdapt site validation failed:")
        for failure in failures:
            print(f"- {failure}")
        return 1

    pages = len(list(root.rglob("*.html")))
    print(f"SightAdapt site validation passed: {pages} HTML pages.")
    print("No analytics, remote fonts or third-party embeds detected.")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
