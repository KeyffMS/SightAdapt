# Contributing to SightAdapt

SightAdapt is in an alpha stage. Contributions should prioritize user safety, predictable behavior, and Windows 10/11 compatibility.

## Developer Certificate of Origin

SightAdapt uses the [Developer Certificate of Origin 1.1 workflow](DCO.md). Every human-authored commit in a pull request must contain a valid sign-off:

```text
Signed-off-by: Full Name <email@example.com>
```

Create signed-off commits with:

```bash
git commit --signoff
```

The sign-off certifies that the contributor created the work or is authorized to submit it, and that it may be distributed under the repository's MIT License subject to clearly identified compatible third-party terms. The DCO workflow must pass before merge.

## Product identity

Public naming and attribution must follow [the SightAdapt product identity standard](docs/BRAND.md).

- Spell the product identity only as `SightAdapt`.
- Use `SightAdapt™` for the first or most prominent public occurrence where appropriate.
- Keep executable, assembly, namespace, package, URL, tag, and file identifiers as plain `SightAdapt`.
- Do not use `®` unless a valid registration is obtained and the brand standard is updated.
- Use `KeyffMS / aiteracja.pl` as the public publisher wording.
- Use `https://aiteracja.pl/sightadapt/` as the canonical public product URL.
- Update the brand standard first when proposing a product-name, publisher, description, URL, or primary-logo change.

## Development setup

- Windows 10 or Windows 11
- Visual Studio with .NET desktop development, or the .NET 8 SDK
- x64 target

Build the application with:

```powershell
dotnet build src/SightAdapt/SightAdapt.csproj -c Release
```

Create a self-contained executable with the steps in [docs/BUILD.md](docs/BUILD.md).

## Contribution provenance

The pull request template requires disclosure of:

- copied or adapted code, snippets, documentation, templates, media, data or generated assets;
- the source, license and modifications for third-party material;
- employer, client or organization authorization where applicable;
- material AI assistance and the human provenance review performed;
- any requested exception or private permission record.

Do not add code, tests, documentation, icons, screenshots, data or other creative material whose origin or license is unknown, incompatible or not redistributable. AI-assisted output must be reviewed by the human contributor, checked for restricted/confidential material and covered by the contributor's DCO certification.

The complete policy, employer process, historical review and exception handling are documented in [Contribution provenance](docs/legal/CONTRIBUTION-PROVENANCE.md) and [Contribution exceptions](docs/legal/CONTRIBUTION-EXCEPTIONS.md).

## Pull requests

- keep changes focused;
- complete the provenance and licensing checklist in the pull request template;
- sign off every human-authored commit;
- describe Windows versions and DPI configurations tested when application behavior changes;
- do not add DLL injection or kernel drivers;
- do not add telemetry, analytics, remote crash reporting, automatic diagnostic upload, update checks, cloud synchronization or another network/data-collection feature without completing the privacy review defined in [PRIVACY.md](PRIVACY.md);
- update `PRIVACY.md` in the same pull request when local processing, network behavior, support-data handling or retention changes;
- preserve the emergency overlay shutdown path;
- document known limitations;
- include manual test steps for visual or input-related changes;
- keep public naming consistent with [docs/BRAND.md](docs/BRAND.md).

## Bug reports

Include:

- Windows version;
- display scaling and monitor arrangement;
- target application and framework, when known;
- whether the target application was elevated;
- exact steps to reproduce;
- whether the emergency tray command still worked.

Before attaching paths, settings, logs, screenshots or recordings, follow the redaction and private-reporting guidance in [PRIVACY.md](PRIVACY.md). Do not publish passwords, tokens, confidential documents, personal paths or screenshots containing unrelated private information. For sensitive material, contact the repository owner through GitHub and request a private communication channel first.
