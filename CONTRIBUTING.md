# Contributing to SightAdapt

SightAdapt is in an alpha stage. Contributions should prioritize user safety, predictable behavior, and Windows 10/11 compatibility.

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

## Pull requests

- keep changes focused;
- describe Windows versions and DPI configurations tested;
- do not add DLL injection, kernel drivers, or screen-content telemetry;
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

Do not attach screenshots containing private information.
