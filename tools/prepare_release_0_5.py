from __future__ import annotations

import re
import shutil
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def run(*args: str) -> None:
    subprocess.run(args, cwd=ROOT, check=True)


def write(path: str, content: str) -> None:
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content.rstrip() + "\n", encoding="utf-8")


def remove(path: Path) -> None:
    if path.is_dir():
        shutil.rmtree(path)
    elif path.exists():
        path.unlink()


# Rename the executable project and remove the development-only product name.
old_project = ROOT / "src" / "SightAdapt.Demo"
new_project = ROOT / "src" / "SightAdapt"
if old_project.exists():
    run("git", "mv", str(old_project.relative_to(ROOT)), str(new_project.relative_to(ROOT)))

old_csproj = new_project / "SightAdapt.Demo.csproj"
new_csproj = new_project / "SightAdapt.csproj"
if old_csproj.exists():
    run("git", "mv", str(old_csproj.relative_to(ROOT)), str(new_csproj.relative_to(ROOT)))

# Remove versioned, historical, planning, and development-stage documentation.
for root_document in ("DEMO.md", "LIGHT.md", "HARD.md"):
    remove(ROOT / root_document)

for markdown in (ROOT / "docs").glob("*.md"):
    remove(markdown)
remove(ROOT / "docs" / "assets")

# Replace development-only identifiers in all remaining tracked text files.
text_suffixes = {
    ".cs",
    ".csproj",
    ".props",
    ".targets",
    ".yml",
    ".yaml",
    ".md",
    ".xml",
    ".manifest",
    ".json",
    ".txt",
    ".ps1",
    ".sh",
}

tracked = subprocess.run(
    ["git", "ls-files"],
    cwd=ROOT,
    check=True,
    capture_output=True,
    text=True,
).stdout.splitlines()

for relative in tracked:
    path = ROOT / relative
    if not path.exists() or path.is_dir():
        continue
    if path.suffix.lower() not in text_suffixes and path.name not in {".gitignore"}:
        continue
    try:
        content = path.read_text(encoding="utf-8")
    except UnicodeDecodeError:
        continue
    updated = content.replace("SightAdapt.Demo.Tests", "SightAdapt.Tests")
    updated = updated.replace("SightAdapt.Demo", "SightAdapt")
    if updated != content:
        path.write_text(updated, encoding="utf-8")

# Canonical 0.5 Alpha product metadata.
project = new_csproj
content = project.read_text(encoding="utf-8")
replacements = {
    "AssemblyName": "SightAdapt",
    "RootNamespace": "SightAdapt",
    "Version": "0.5.0-alpha.1",
    "AssemblyVersion": "0.5.0.0",
    "FileVersion": "0.5.0.0",
    "InformationalVersion": "0.5.0-alpha.1",
}
for element, value in replacements.items():
    content, count = re.subn(
        rf"<{element}>.*?</{element}>",
        f"<{element}>{value}</{element}>",
        content,
        count=1,
    )
    if count != 1:
        raise RuntimeError(f"Expected one <{element}> element")
content, count = re.subn(
    r'<AssemblyMetadata Include="Milestone" Value="[^"]*"\s*/>',
    '<AssemblyMetadata Include="Milestone" Value="Alpha 0.5" />',
    content,
    count=1,
)
if count != 1:
    raise RuntimeError("Expected one Milestone assembly metadata entry")
project.write_text(content, encoding="utf-8")

write(
    ".github/workflows/build.yml",
    """
name: Build and test SightAdapt

on:
  push:
    branches: [main, "release/**"]
  pull_request:
    branches: [main]

permissions:
  contents: read

jobs:
  build:
    runs-on: windows-latest

    steps:
      - name: Check out repository
        uses: actions/checkout@v4

      - name: Set up .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: 8.0.x

      - name: Restore application
        run: dotnet restore src/SightAdapt/SightAdapt.csproj

      - name: Restore tests
        run: dotnet restore tests/SightAdapt.Tests/SightAdapt.Tests.csproj

      - name: Build
        shell: pwsh
        run: |
          $output = & dotnet build src/SightAdapt/SightAdapt.csproj --configuration Release --no-restore 2>&1
          $exitCode = $LASTEXITCODE
          $output | Set-Content -Path build-output.txt -Encoding utf8
          $output | ForEach-Object { Write-Host $_ }
          exit $exitCode

      - name: Test
        shell: pwsh
        run: |
          $output = & dotnet test tests/SightAdapt.Tests/SightAdapt.Tests.csproj --configuration Release --no-restore 2>&1
          $exitCode = $LASTEXITCODE
          $output | Set-Content -Path test-output.txt -Encoding utf8
          $output | ForEach-Object { Write-Host $_ }
          exit $exitCode

      - name: Upload build diagnostics
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: SightAdapt-build-diagnostics
          path: |
            build-output.txt
            test-output.txt
          if-no-files-found: warn
          retention-days: 7

      - name: Publish self-contained alpha
        run: >-
          dotnet publish src/SightAdapt/SightAdapt.csproj
          --configuration Release
          --runtime win-x64
          --self-contained true
          -p:PublishSingleFile=true
          --output artifacts/win-x64

      - name: Upload Windows alpha
        uses: actions/upload-artifact@v4
        with:
          name: SightAdapt-0.5-Alpha-win-x64
          path: artifacts/win-x64/
          if-no-files-found: error
""",
)

write(
    "README.md",
    """
# SightAdapt 0.5 Alpha

SightAdapt is an open-source visual-accessibility application for Windows 10 and Windows 11. It applies configurable visual correction to selected application windows through a separate input-transparent overlay. It does not modify another application's files or process memory.

## Current version

```text
Product version: 0.5.0-alpha.1+<commit>
File version:    0.5.0.0
Milestone:       Alpha 0.5
Settings schema: 4
Runtime:         .NET 8, Windows x64
```

The commit suffix in `ProductVersion` identifies the exact source revision used to build the executable.

## Functionality

SightAdapt currently provides:

- persistent per-application visual-profile assignments;
- automatic activation for enabled application assignments;
- fixed `Exact invert` and editable `Soft invert` profiles;
- independent user-defined Soft Invert profiles;
- output-black, output-white, brightness, contrast, saturation, and hue controls;
- grayscale and hue-spectrum profile previews;
- create, duplicate, rename, edit, assign, and delete operations for user-defined profiles;
- per-application overlay scope: client area, full window, current screen, or all screens;
- foreground-window detection every 75 ms with duplicate suppression;
- a bounded runtime application-identity cache;
- reuse and retargeting of one overlay instance during normal application switching;
- a short transition grace period that reduces visible white flashes;
- two global shortcuts;
- a notification-area menu and configuration panel;
- immediate explicit and emergency overlay shutdown;
- schema-versioned JSON settings with migration and atomic persistence;
- automated build, test, architecture, migration, and Windows publish validation.

New application assignments use Soft Invert and client-area scope by default.

## Keyboard controls

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Locally enable or disable visual correction for the active window without changing saved settings |
| `Ctrl+Alt+Shift+I` | Add, disable, or re-enable the active application's persistent automatic assignment |

Emergency shutdown is available from the notification-area menu.

## Build a standalone EXE

The application can be published as a self-contained single-file executable. It does not need to be started with `dotnet run`, and the target computer does not need a separately installed .NET runtime.

Detailed instructions: [docs/BUILD.md](docs/BUILD.md).

From the repository root:

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj

dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    --output .\artifacts\win-x64
```

The executable is created at:

```text
artifacts\win-x64\SightAdapt.exe
```

## Documentation

- [Complete functionality](docs/FEATURES.md)
- [Current architecture](docs/ARCHITECTURE.md)
- [Build a standalone EXE](docs/BUILD.md)
- [Documentation index](docs/README.md)
- [Security policy](SECURITY.md)
- [Contribution guide](CONTRIBUTING.md)

## Current limitations

- only one foreground target is corrected at a time;
- background or fully obscured windows are not persistently filtered by the current Magnification API backend;
- profile import and export are not implemented;
- palette analysis, dominant-color extraction, targeted color rules, and LUT import are not implemented;
- protected or DRM-controlled content may not be capturable;
- elevated applications may require SightAdapt to run at a compatible integrity level;
- remote-desktop sessions and some graphics drivers may not support the magnifier control correctly;
- endurance and broad compatibility testing are not complete.

## License

SightAdapt is licensed under the MIT License. See [LICENSE](LICENSE).
""",
)

write(
    "docs/README.md",
    """
# SightAdapt documentation

The documentation describes only the current SightAdapt 0.5 Alpha implementation.

- [Complete functionality](FEATURES.md) — user-visible behavior, profiles, application assignments, overlay scopes, switching, settings, safety, and limitations.
- [Current architecture](ARCHITECTURE.md) — authorities, sources of truth, settings transaction, foreground switching, overlay lifecycle, geometry, UI boundaries, and failure behavior.
- [Build a standalone EXE](BUILD.md) — step-by-step local self-contained Windows x64 publication.
- [Security policy](../SECURITY.md)
- [Contribution guide](../CONTRIBUTING.md)
- [License](../LICENSE)
""",
)

write(
    "docs/FEATURES.md",
    """
# SightAdapt 0.5 Alpha functionality

## Application operation

SightAdapt runs in the Windows notification area and enforces one process per user session. It tracks the active supported top-level window and applies the saved assignment when automatic mode and that assignment are enabled.

Only one foreground target is corrected at a time. The overlay is separate from the target process, never intentionally receives input, and does not modify target files or memory.

## Application assignments

Every assignment stores:

- display name;
- executable name;
- executable path;
- enabled state;
- visual-profile identifier;
- overlay-scope identifier.

Assignments are matched primarily by executable path without regard to letter case. A disabled assignment remains available for the local shortcut but does not activate automatically.

New assignments use:

- visual profile: `Soft invert`;
- overlay scope: `Client area`.

## Visual profiles

### Exact invert

`Exact invert` is a fixed built-in profile. It cannot be edited, renamed, or deleted.

### Soft invert

The built-in `Soft invert` profile is editable and shared by every assignment that references it.

Default values:

```text
Output black: 8%
Output white: 92%
Brightness:   0%
Contrast:     100%
Saturation:   100%
Hue shift:    0°
```

The current matrix pipeline applies:

```text
soft inversion and output limits
→ saturation
→ hue rotation
→ contrast
→ brightness
```

All operations are composed into one Magnification API color-effect matrix.

### User-defined profiles

Users can create a profile from Soft Invert defaults or duplicate an editable profile. User-defined profiles have stable identifiers, independent tuning values, and unique case-insensitive names.

Supported operations:

- create;
- duplicate;
- rename;
- edit;
- assign;
- delete.

Deleting a user-defined profile reassigns affected applications to built-in Soft Invert before removing the profile. Built-in profiles are protected.

## Overlay scope per application

| UI choice | Persisted ID | Result |
|---|---|---|
| Client area | `client-area` | Application content without title bar and frame; default |
| Full window | `window` | Complete visible application window |
| Current screen | `screen` | Complete monitor containing the target |
| All screens | `all-screens` | Complete Windows virtual desktop |

Changing one assignment does not modify another assignment's scope. Missing or invalid persisted scope values recover to `client-area`.

## Foreground switching

The foreground tracker polls every 75 ms by default and publishes only a changed supported top-level handle. Application identity is cached in a bounded 64-entry least-recently-used process cache. The cache contains derived runtime data only; saved assignments remain authoritative.

During normal switching, SightAdapt reuses one existing overlay instance and retargets it with the new window handle, profile, scope, and geometry. The last rendered frame may remain visible for at most 125 ms while the new target is resolved. Explicit disable and emergency shutdown bypass this grace period.

## Keyboard and tray controls

| Shortcut | Action |
|---|---|
| `Ctrl+Alt+I` | Local correction toggle without changing saved settings |
| `Ctrl+Alt+Shift+I` | Add, disable, or re-enable the persistent assignment for the active application |

The notification-area menu provides:

- local correction toggle;
- persistent assignment toggle;
- automatic-mode switch;
- application and profile configuration;
- About dialog;
- emergency shutdown;
- application exit.

## Settings

Settings are stored at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

Schema `4` contains automatic mode, application assignments, overlay scopes, and visual profiles. Changes use a copy, mutate, normalize, save, and publish transaction. Failed domain operations or failed writes do not replace the committed in-memory state.

Older valid assignments are preserved where possible. Legacy `effect: "invert"` values migrate to built-in Exact Invert.

## Safety behavior

- overlay windows are layered, input-transparent, non-activating tool windows;
- emergency shutdown removes the overlay before attempting settings persistence;
- renderer fault and explicit emergency shutdown are separate runtime states;
- failed persistence cannot publish candidate settings;
- destroyed targets close the overlay;
- minimized, hidden, or unavailable targets hide it;
- application exit and disposal release native overlay resources;
- no DLL injection or kernel driver is used.

## Limitations

- only one foreground target is corrected at a time;
- the current Magnification API backend cannot provide a stable persistent filter for obscured background windows;
- minimized targets are not continuously rendered;
- profile import and export are not implemented;
- palette analysis, targeted per-color correction, and LUT import are not implemented;
- DRM, protected surfaces, elevated targets, remote sessions, and some graphics drivers may limit capture;
- endurance and broad compatibility testing remain incomplete.
""",
)

write(
    "docs/ARCHITECTURE.md",
    """
# SightAdapt current architecture

## Product flow

```text
Foreground window
      ↓
ForegroundWindowTracker
(detect and deduplicate)
      ↓
ApplicationDiscovery
(process path and bounded cache)
      ↓
ProfileResolver
(committed assignment)
      ↓
SightAdaptContext
(use-case orchestration)
      ↓
OverlayController
(create once, retarget, disable)
      ↓
MagnifierOverlay
(target, geometry, rendering)
```

## Settings transaction

```text
SettingsCoordinator.Current
      ↓
CreateWorkingCopy
      ↓
Domain-service mutation
      ↓
SettingsStore.Save
(normalize and atomic replacement)
      ↓
Current.ReplaceWith
      ↓
one synchronous Changed event
```

A failed mutation or failed write does not replace committed settings and does not publish a settings change.

## Authorities

| Concern | Authority |
|---|---|
| Settings transaction | `SettingsCoordinator` |
| Migration, normalization, recovery, and reference repair | `SettingsStore.Normalize` |
| Application assignment mutations and overlay scope | `ApplicationProfileManagementService` |
| Visual-profile lifecycle and tuning | `VisualProfileManagementService` |
| Automatic-mode mutation | `AutomaticModeManagementService` |
| Runtime mode, target, profile, suppression, and message | `ApplicationStateController` |
| Foreground detection and duplicate suppression | `ForegroundWindowTracker` |
| Runtime identity resolution | `ApplicationDiscovery` |
| Bounded process identity cache | `ApplicationIdentityCache` |
| Overlay geometry | `OverlayBoundsResolver` |
| Overlay resource lifetime and retargeting | `OverlayController` |
| Native target, rendering, geometry refresh, and transition grace | `MagnifierOverlay` |
| Notification-area presentation | `TrayPresenter` |
| Application-table presentation and edit mechanics | `ApplicationProfilesGrid` |
| Configuration use cases and dialogs | `ConfigurationForm` |
| Selector editing contract | `ModernSelectorEditingControl` |

## Sources of truth

| Data or rule | Source of truth |
|---|---|
| Persisted automatic mode, applications, assignments, scopes, and profiles | `SightAdaptSettings` committed through `SettingsCoordinator.Current` |
| Runtime mode, target, active profile, suppression, and message | `ApplicationStateController.Current` |
| Actual overlay resource and target | `OverlayController` and active `MagnifierOverlay` |
| Per-application overlay scope | `ApplicationProfile.OverlayScopeId` |
| Scope identifiers, default, and display names | `OverlayScopePolicy` |
| Profile IDs, fallback, user-ID, and name rules | `VisualProfilePolicy` |
| Canonical profile values | `VisualProfileDefaults` |
| Supported transforms and tuning capability | `VisualTransformCatalog` |
| Parameter ranges | `VisualProfileLimits` |
| Product name, version, milestone, repository, author, and license | project and assembly metadata exposed through `ProductInfo` |

`ApplicationIdentityCache` is an optimization, not a product source of truth.

## Foreground and overlay lifecycle

The foreground tracker polls every 75 ms and publishes only a changed supported top-level handle. When an enabled assignment exists, the context resolves its profile and scope and activates correction.

- without an active overlay, `OverlayController` creates one `MagnifierOverlay`;
- with an active overlay, it retargets the same instance;
- without an enabled assignment, an automatically active overlay is disabled;
- local disable, emergency shutdown, exit, and disposal remove it immediately.

A rendered frame may remain visible for at most 125 ms during target transition. This is a rendering grace period, not a second runtime state.

## Geometry

`OverlayBoundsResolver` is the only authority for:

- client-area bounds converted to screen coordinates;
- full visible window bounds;
- containing monitor bounds;
- Windows virtual-screen bounds.

The current backend uses the same rectangle for the magnifier source and overlay destination.

## Configuration grid boundary

`ApplicationProfilesGrid` owns columns, rows, selectors, status painting, selection, empty state, stable executable-path keys, typed value-change events, row updates, and failed-cell restoration. It does not know about persistence or dialogs.

`ConfigurationForm` resolves current committed assignments and translates typed grid events into domain-service mutations wrapped by `SettingsCoordinator.Commit`. It suppresses only its own synchronous full refresh during a grid-originated commit.

`ModernSelectorEditingControl` exposes display text as its formatted value and marks the cell dirty. It does not write directly to a grid cell, force edit completion, or control settings dispatch.

## Safety and intentional constraints

- overlay windows do not accept input or activate themselves;
- emergency shutdown disables rendering before settings I/O;
- fault and emergency are distinct states;
- no dependency-injection container, event bus, repository layer, global selector guard, delayed settings workaround, or reflection-based popup control is used;
- no DLL injection, kernel driver, or target-process memory modification is used;
- the Magnification API backend intentionally corrects only the active foreground target.
""",
)

write(
    "docs/BUILD.md",
    """
# Build SightAdapt as a standalone EXE

These steps create a self-contained Windows x64 executable. The published application is started directly as `SightAdapt.exe`; `dotnet run` is not required.

## 1. Install prerequisites

Use a 64-bit Windows 10 or Windows 11 computer and install one of:

- the .NET 8 SDK; or
- Visual Studio with the **.NET desktop development** workload.

Verify the SDK:

```powershell
dotnet --version
```

The displayed version should begin with `8.`.

## 2. Clone the repository

```powershell
git clone https://github.com/KeyffMS/SightAdapt.git
cd SightAdapt
```

## 3. Restore dependencies

```powershell
dotnet restore .\src\SightAdapt\SightAdapt.csproj
dotnet restore .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj
```

## 4. Run the tests

```powershell
dotnet test .\tests\SightAdapt.Tests\SightAdapt.Tests.csproj `
    --configuration Release `
    --no-restore
```

All tests must pass before publication.

## 5. Publish a self-contained single-file executable

```powershell
dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    --output .\artifacts\win-x64
```

## 6. Start the executable

```powershell
.\artifacts\win-x64\SightAdapt.exe
```

The application appears in the Windows notification area.

## 7. Verify the built version

While SightAdapt is running:

```powershell
$process = Get-Process SightAdapt

(Get-Item $process.Path).VersionInfo |
    Format-List ProductVersion, FileVersion
```

Expected version prefix:

```text
ProductVersion: 0.5.0-alpha.1+<commit>
FileVersion:    0.5.0.0
```

The commit suffix identifies the exact source revision.

## Output directory

The publication directory contains `SightAdapt.exe` and any files required by the selected .NET publication mode:

```text
artifacts\win-x64\
```

To rebuild from a clean state:

```powershell
Remove-Item .\artifacts\win-x64 -Recurse -Force -ErrorAction SilentlyContinue

dotnet publish .\src\SightAdapt\SightAdapt.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    --output .\artifacts\win-x64
```
""",
)

# All current documentation must be free of development branch labels and product-stage naming.
run("git", "add", "-A")

for forbidden in ("SightAdapt.Demo", "SightAdapt.Demo.Tests", "agent/"):
    result = subprocess.run(
        ["git", "grep", "-n", "-F", forbidden, "--", ":(exclude)tools/prepare_release_0_5.py"],
        cwd=ROOT,
        capture_output=True,
        text=True,
    )
    if result.returncode == 0:
        raise RuntimeError(f"Forbidden text remains: {forbidden}\n{result.stdout}")
    if result.returncode not in (0, 1):
        raise RuntimeError(result.stderr)

if any((ROOT / "docs").glob("*ROADMAP*")):
    raise RuntimeError("Roadmap documentation remains")
