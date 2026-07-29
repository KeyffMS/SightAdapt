# SightAdapt™ 0.5.0.50 Alpha

SightAdapt is a free, open-source Windows application for per-application visual accessibility and color correction.

SightAdapt applies configurable color correction to selected application windows through a separate input-transparent overlay. It does not modify another application's files or process memory.

- Product website: <https://aiteracja.pl/sightadapt/>
- Source repository: <https://github.com/KeyffMS/SightAdapt>
- Publisher: `KeyffMS / aiteracja.pl`
- License: `MIT License`

## Current version

```text
Product version: 0.5.0.50-alpha
File version:    0.5.0.50
Milestone:       0.5.0.50 Alpha
Settings schema: 5
Runtime:         .NET 8, Windows x64
```

The product and file versions are read from the executable assembly metadata.

## Functionality

SightAdapt currently provides:

- persistent per-application visual-profile assignments;
- optional distinct visual profiles for native Win32 popup menus, with inheritance from the application profile;
- automatic activation for enabled application assignments;
- built-in `None` (no correction), fixed `Exact invert`, and editable `Soft invert` profiles;
- independent user-defined Soft Invert profiles;
- output-black, output-white, brightness, contrast, saturation, and hue controls;
- grayscale and hue-spectrum profile previews;
- create, duplicate, rename, edit, assign, and delete operations for user-defined profiles;
- per-application overlay scope: client area, full window, current screen, or all screens;
- foreground-window detection every 75 ms with duplicate suppression;
- a bounded runtime application-identity cache;
- reuse and retargeting of one persistent application overlay, plus transient native-menu overlays;
- a short transition grace period that reduces visible white flashes;
- two global shortcuts;
- a notification-area menu and configuration panel;
- immediate explicit and emergency overlay shutdown;
- schema-versioned JSON settings with migration and atomic persistence;
- automated build, test, architecture, migration, Windows publish and final-archive legal-bundle validation.

New application assignments use Soft Invert, inherit that profile for native popup menus, and use client-area scope by default. Selecting `None` for the application and an explicit menu profile leaves the application unchanged while correcting only its native popup menus.

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

The publish directory contains `SightAdapt.exe` and the legal-document bundle required for binary distribution:

```text
artifacts\win-x64\SightAdapt.exe
artifacts\win-x64\LICENSE.txt
artifacts\win-x64\THIRD-PARTY-NOTICES.txt
artifacts\win-x64\DOTNET-LICENSE-NOTICE.txt
artifacts\win-x64\DEPENDENCIES.md
artifacts\win-x64\PRIVACY.md
```

Create and validate the final archive according to [the binary packaging standard](docs/PACKAGING.md).

## Documentation

- [Product identity and brand usage](docs/BRAND.md)
- [Release naming and attribution](docs/RELEASING.md)
- [Binary packaging standard](docs/PACKAGING.md)
- [Complete functionality](docs/FEATURES.md)
- [Current architecture](docs/ARCHITECTURE.md)
- [Build and package a standalone EXE](docs/BUILD.md)
- [Privacy notice](PRIVACY.md)
- [Dependency inventory](DEPENDENCIES.md)
- [Third-party notices](THIRD-PARTY-NOTICES.txt)
- [Microsoft .NET redistribution notice](DOTNET-LICENSE-NOTICE.txt)
- [Documentation index](docs/README.md)
- [Security policy](SECURITY.md)
- [Contribution guide](CONTRIBUTING.md)

## Current limitations

- only one foreground application session is corrected at a time;
- background or fully obscured windows are not persistently filtered by the current Magnification API backend;
- separate menu profiles apply only to native Win32 `#32768` popup menus; WPF, WinUI, Chromium/Electron, Qt, and other custom-rendered menus are not separately corrected;
- profile import and export are not implemented;
- palette analysis, dominant-color extraction, targeted color rules, and LUT import are not implemented;
- protected or DRM-controlled content may not be capturable;
- elevated applications may require SightAdapt to run at a compatible integrity level;
- remote-desktop sessions and some graphics drivers may not support the magnifier control correctly;
- endurance and broad compatibility testing are not complete.

## Product identity and license

The product identity is always written as `SightAdapt`. The first or most prominent public use may be written as `SightAdapt™`; technical identifiers such as `SightAdapt.exe`, namespaces, URLs, tags, and package identifiers remain plain `SightAdapt`. The registered-mark symbol `®` must not be used unless a valid registration is obtained.

SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.

SightAdapt is licensed under the MIT License. See [LICENSE](LICENSE). Binary distributions include the same terms as `LICENSE.txt` together with the privacy, dependency and third-party notice files.
