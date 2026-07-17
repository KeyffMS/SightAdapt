# Security policy

## Reporting a vulnerability

Please do not disclose security-sensitive issues in a public GitHub issue.

Until a private reporting channel is configured, contact the repository owner through GitHub and request a private communication channel.

## Demo security boundaries

The current demo:

- does not inject code into other processes;
- does not install a driver;
- does not save captured screen content;
- does not transmit captured screen content;
- runs with the current user's privileges;
- provides an emergency shortcut and tray command to remove the overlay.

The demo has not yet completed a formal security review and must not be treated as production-ready assistive software.
