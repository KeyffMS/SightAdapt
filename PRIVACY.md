# SightAdapt privacy notice

Last updated: 2026-07-29

This notice describes the data behavior of the SightAdapt Windows application distributed from this repository.

## Summary

SightAdapt processes application and window information locally to apply the visual profile selected by the user. The current application does not include analytics, advertising, account registration, cloud synchronization, remote crash reporting or automatic telemetry.

SightAdapt does not intentionally transmit captured screen content, saved assignments or visual-profile settings to the publisher or another service.

## Data processed locally

To select and render the configured correction, SightAdapt processes local Windows information such as:

- the active top-level window and its handle;
- the target process identifier;
- application display name;
- executable name and executable path;
- window position, size, monitor and visibility state;
- saved visual profiles, application assignments and overlay-scope settings.

This processing occurs on the user's computer. SightAdapt uses a separate Windows Magnification API overlay and does not intentionally modify the target application's files or process memory.

## Local storage

SightAdapt stores settings in:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

The file can contain application display names, executable names and paths, enabled states, selected visual-profile identifiers, overlay scopes and user-defined visual-profile parameters.

Settings remain on the computer until they are changed in SightAdapt or the settings file is deleted by the user. Deleting the file resets locally stored configuration on the next start.

## Screen content

SightAdapt uses Windows display and Magnification APIs to render corrected content in an overlay. The application does not intentionally save screenshots, record the screen, build a screen-content history or send screen content over a network.

## Network access and external links

SightAdapt does not automatically contact a SightAdapt server. The About window contains user-activated links to the product website and source repository. Selecting one of those links opens the system's default browser, whose privacy behavior is governed by the browser and destination site.

## Diagnostics

Runtime warnings and errors are handled locally. The current application does not automatically upload logs or crash reports. Information manually included by a user in a GitHub issue or another support request is provided voluntarily and is subject to the privacy terms of the service used to submit it.

## Operating-system and runtime components

SightAdapt depends on Microsoft Windows and redistributes Microsoft .NET runtime components. Those components are governed by their applicable licenses and privacy terms. See `DOTNET-LICENSE-NOTICE.txt`, `THIRD-PARTY-NOTICES.txt` and `DEPENDENCIES.md` in the binary package.

## Changes to this notice

A change that introduces telemetry, network services, cloud synchronization, remote diagnostics or another material data flow must update this notice before that feature is distributed.

## Contact and source review

The source code and issue tracker are available at:

https://github.com/KeyffMS/SightAdapt
