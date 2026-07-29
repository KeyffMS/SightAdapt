# SightAdapt privacy notice and support-data policy

Last updated: 2026-07-29

This notice describes the data behavior of the SightAdapt Windows application distributed from this repository and the handling of information voluntarily submitted for support.

## Summary

SightAdapt processes application, process and window information locally to apply visual settings selected by the user. The current application does not include telemetry, analytics, advertising, account registration, cloud synchronization, remote crash reporting, automatic diagnostic upload or an automatic update check.

SightAdapt does not intentionally transmit captured screen content, saved application assignments, executable paths, settings or visual-profile data to the publisher or another SightAdapt service.

The only current network-related action controlled by SightAdapt is opening a website or repository link in the user's default browser after the user activates that link. The browser and destination service then operate under their own privacy terms.

## Data processed locally

To identify the active application and render the configured overlay, SightAdapt may read or derive local Windows information including:

- active and visible top-level window handles;
- process identifiers and GUI thread identifiers;
- application display names;
- executable file names and full executable paths;
- executable file-description metadata where Windows makes it available;
- window position, size, monitor, visibility and foreground state;
- saved application assignments and enabled states;
- selected application and popup-menu visual profiles;
- overlay-scope settings;
- user-defined profile names and adjustment values;
- local diagnostic messages generated when an operation fails.

A full executable path can contain a person's user-account name, organization name, project name or another identifying value. This information is used locally for matching saved application assignments.

SightAdapt uses a separate Windows Magnification API overlay. It does not intentionally modify a target application's files or process memory.

## Local storage

SightAdapt stores its settings at:

```text
%LOCALAPPDATA%\SightAdapt\settings.json
```

The file can contain application display names, executable names and paths, enabled states, selected visual-profile identifiers, overlay scopes and user-defined visual-profile parameters.

### Inspect and export

`settings.json` is a human-readable JSON file. A user can inspect it with a text editor. To export or back it up, close SightAdapt or avoid changing settings while copying the file to another location.

### Delete and reset

To delete locally stored SightAdapt configuration:

1. exit SightAdapt;
2. delete `%LOCALAPPDATA%\SightAdapt\settings.json`;
3. start SightAdapt again if required.

The application recreates default settings when no settings file exists. Deleting the file does not delete information separately submitted through GitHub, email or another support service.

## Screen content

SightAdapt uses Windows display and Magnification APIs to render corrected content in an overlay. The current application does not intentionally:

- save screenshots;
- record the screen;
- build a screen-content history;
- perform image recognition or content analysis;
- transmit screen content over a network.

Protected or DRM-controlled content may remain unavailable or unfilterable.

## Telemetry, diagnostics and network behavior

The current release has:

- no telemetry or product analytics;
- no advertising or tracking SDK;
- no remote crash-reporting service;
- no automatic log or settings upload;
- no account or cloud synchronization;
- no automatic update check;
- no background connection to a SightAdapt server.

Runtime warnings and errors are handled locally. The application does not automatically submit them to the publisher.

## Information submitted for support

Information is transferred to the project owner only when a person voluntarily includes it in a GitHub issue, discussion, security report, direct message or another support communication.

Executable paths, screenshots, videos, settings files, logs and crash details can reveal personal, confidential or security-sensitive information. Before submitting support material:

- replace user names, organization names, project names and private directory names with neutral placeholders;
- remove unrelated windows, notifications, browser tabs, documents and account details from screenshots or recordings;
- do not publish passwords, tokens, private keys, license keys or confidential customer information;
- share the smallest excerpt needed to reproduce the problem;
- review `settings.json` before attaching it and remove unrelated application assignments and paths;
- use a private channel for security-sensitive or personal information instead of a public issue.

A public issue should contain a minimal reproduction and redacted technical details. When private communication is needed and no dedicated private support address is published, contact the repository owner through GitHub and request a private communication channel before sending the material.

## Controller and contact for submitted support data

When support material is received directly by the project publisher, the responsible controller is:

**KeyffMS / aiteracja.pl**

Current contact route:

- GitHub repository owner and issue tracker: `https://github.com/KeyffMS/SightAdapt`;
- for non-public material, contact the repository owner through GitHub and request a private communication channel before sending data.

GitHub and any other service used to submit information operate as separate service providers or controllers under their own terms and privacy notices.

## Retention and deletion of support material

Public GitHub issues, comments and attachments remain subject to GitHub's platform controls and repository history. Contributors should not submit personal or confidential material publicly when deletion from forks, notifications, caches or archives cannot be guaranteed.

Privately submitted support material is handled under the following project policy:

- access is limited to the people needed to investigate the report;
- material is used only for support, security investigation, compliance evidence or resolving the reported problem;
- unnecessary copies should not be created;
- material is deleted when it is no longer needed and normally no later than 90 days after the related support matter is closed;
- a longer period may be used when required for an active security investigation, legal obligation, dispute or explicitly agreed follow-up;
- a deletion request can be made through the same private contact route used to submit the material;
- deletion applies to copies controlled by the project owner where deletion is technically and legally possible; it cannot guarantee deletion by an independent platform, recipient, fork, backup or archive.

The project does not use privately submitted support data for advertising or unrelated profiling.

## Operating-system and runtime components

SightAdapt depends on Microsoft Windows and redistributes Microsoft .NET runtime components. Those components are governed by their applicable licenses and privacy terms. See `DOTNET-LICENSE-NOTICE.txt`, `THIRD-PARTY-NOTICES.txt`, `MICROSOFT-DOTNET-REDISTRIBUTION.txt`, `DEPENDENCIES.md` and the SBOM in the binary package.

## Privacy review requirement

A pull request must receive a privacy review and update this notice before introducing or materially changing any of the following:

- telemetry or analytics;
- remote crash reporting;
- automatic diagnostic, settings, screenshot or log upload;
- an update service or automatic network check;
- cloud synchronization, accounts or authentication;
- remote configuration or feature flags;
- collection of new identifiers or persistent usage history;
- a new external service, tracking SDK or network endpoint;
- a new support-data collection or retention practice.

The review must identify data categories, purpose, destinations, user control, retention, security, notices and any consent or legal-basis requirements before the feature is merged or distributed.

## Consistency of product claims

README, release notes, store descriptions, website copy and application-facing descriptions must not claim broader data collection or stronger privacy guarantees than the implemented behavior. Statements such as "no telemetry" apply only while the application remains consistent with this notice.

## Changes to this notice

Material changes to local processing, network behavior or support-data handling must update this file before release. The package copy of `PRIVACY.md` is readable without starting SightAdapt.
