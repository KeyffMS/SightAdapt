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
