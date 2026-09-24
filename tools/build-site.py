#!/usr/bin/env python3
import argparse
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SITE = ROOT / "site"
BRAND = ROOT / "assets" / "brand"

def normalize_base(value: str) -> str:
    value = value.strip()
    if not value.startswith("/"):
        value = "/" + value
    if not value.endswith("/"):
        value += "/"
    return value

def main() -> int:
    parser = argparse.ArgumentParser(description="Build the SightAdapt static product site.")
    parser.add_argument("--base-path", default="/SightAdapt/", help="Deployment path, e.g. /SightAdapt/ or /sightadapt/.")
    parser.add_argument("--canonical-root", default="https://aiteracja.pl/sightadapt/")
    parser.add_argument("--output", default=str(ROOT / "artifacts" / "site"))
    args = parser.parse_args()

    base = normalize_base(args.base_path)
    canonical = args.canonical_root.rstrip("/") + "/"
    output = Path(args.output).resolve()

    if output.exists():
        shutil.rmtree(output)
    output.mkdir(parents=True)

    release = json.loads((SITE / "release.json").read_text(encoding="utf-8"))
    size_mib = release["archiveSizeBytes"] / (1024 * 1024)

    replacements = {
        "{{BASE}}": base,
        "{{CANONICAL}}": canonical,
        "{{VERSION}}": release["version"],
        "{{RELEASE_DATE}}": release["releaseDate"],
        "{{ARCHIVE_SIZE}}": f'{release["archiveSizeBytes"]:,} bytes ({size_mib:.1f} MiB)',
        "{{SHA256}}": release["sha256"],
        "{{RELEASE_URL}}": release["releaseUrl"],
        "{{TAG_URL}}": release["tagUrl"],
        "{{DOWNLOAD_URL}}": release["downloadUrl"],
        "{{CHECKSUM_URL}}": release["checksumUrl"],
        "{{SBOM_URL}}": release["sbomUrl"],
        "{{LEGAL_BUNDLE_URL}}": release["complianceUrl"],
    }

    for source in SITE.rglob("*"):
        if source.is_dir() or source.name in {"README.md", "release.json"}:
            continue
        relative = source.relative_to(SITE)
        target = output / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        if source.suffix.lower() in {".html", ".css", ".js", ".svg", ".txt"}:
            text = source.read_text(encoding="utf-8")
            for key, value in replacements.items():
                text = text.replace(key, value)
            target.write_text(text, encoding="utf-8", newline="\n")
        else:
            shutil.copy2(source, target)

    brand_target = output / "assets" / "brand"
    brand_target.mkdir(parents=True, exist_ok=True)
    for name in ("sightadapt-mark.svg", "sightadapt-lockup-dark.svg", "sightadapt-lockup-light.svg"):
        shutil.copy2(BRAND / name, brand_target / name)

    (output / ".nojekyll").write_text("", encoding="utf-8")
    print(f"Built SightAdapt site at {output}")
    print(f"Base path: {base}")
    print(f"Canonical root: {canonical}")
    return 0

if __name__ == "__main__":
    raise SystemExit(main())
