# Configuration grid ownership refactor

## Purpose

This refactor follows the confirmed issue #9 correction. It changes structure without changing the accepted user-visible behavior.

## Ownership

### `SettingsCoordinator`

Owns the transaction boundary for settings:

- creates a working copy;
- applies a domain mutation;
- persists the copy;
- replaces committed settings;
- publishes one synchronous `Changed` event.

It has no knowledge of WinForms controls, grid editing, dialogs, timers, or popup state.

### `ModernVisualProfileEditingControl`

Owns the `IDataGridViewEditingControl` contract:

- exposes the selected display name as the formatted value;
- marks the current cell dirty;
- does not write `DataGridViewCell.Value` directly;
- does not force grid refresh or edit completion.

### `ApplicationProfilesGrid`

Owns application-grid presentation and mechanics:

- creates and configures columns;
- binds application and visual-profile data;
- stores only the executable path in `DataGridViewRow.Tag`;
- preserves selection by executable path;
- renders the enabled status;
- presents the empty state;
- emits typed value-change and selection events;
- updates or restores only the affected row or cell.

The component does not know about `SettingsCoordinator` or persistence.

### `ConfigurationForm`

Owns use-case orchestration:

- translates grid events into domain-service operations;
- invokes `SettingsCoordinator.Commit`;
- suppresses only its own refresh during a grid-originated transaction;
- asks the grid to update the committed row or restore a failed cell;
- resolves selected applications from `SettingsCoordinator.Current`;
- owns application and profile dialogs.

## Single source of truth

`SettingsCoordinator.Current` remains the model source of truth. The grid stores stable executable-path keys, never mutable `ApplicationProfile` instances.

## Regression coverage

The integration tests verify that:

- profile selection commits without rebuilding the active grid;
- row tags remain executable-path keys;
- leaving the edited cell releases edit mode;
- external committed settings changes rebind the grid;
- architecture tests enforce the ownership boundaries above.
