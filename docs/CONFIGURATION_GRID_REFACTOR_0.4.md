# Configuration grid ownership — 0.4

## Status

The issue #9 correction and the subsequent grid-ownership refactor are complete and manually accepted. This document describes the current boundary used by both visual-profile and overlay-scope editing.

## Root problem resolved

The former selector path wrote directly to the current `DataGridView` cell and triggered a settings refresh while the grid was still committing the edit. Rebuilding rows inside that stack produced the reported `InvalidOperationException`.

The final design keeps settings events synchronous and removes the UI reentrancy at its actual boundary. It does not use delayed global dispatch, modal-owner timers, `Application.Idle`, reflection, or selector-specific lifecycle guards.

## Ownership

### `SettingsCoordinator`

Owns the committed settings transaction:

- creates a working copy;
- applies a domain mutation;
- normalizes and persists the copy;
- replaces committed settings;
- publishes one synchronous `Changed` event after successful persistence.

It has no knowledge of WinForms controls, grid editing, dialogs, timers, or popup state.

### `ModernSelectorEditingControl`

Owns the generic `IDataGridViewEditingControl` contract used by the profile and overlay-scope selectors:

- receives a stable set of identifier/display-name options;
- exposes the selected display name as the formatted value;
- marks the current cell dirty through `NotifyCurrentCellDirty(true)`;
- does not write `DataGridViewCell.Value` directly;
- does not force `CommitEdit`, `EndEdit`, grid refresh, or settings dispatch;
- supports mouse, keyboard, focus, and accessible descriptions.

### `ApplicationProfilesGrid`

Owns application-table presentation and mechanics:

- creates and configures all columns;
- binds applications, visual-profile options, and overlay-scope options;
- stores only the executable path in `DataGridViewRow.Tag`;
- preserves selection by executable path;
- renders the enabled-state indicator;
- presents the empty state;
- emits typed events for enabled, visual-profile, and overlay-scope changes;
- updates the affected row after a successful commit;
- restores only the affected cell after a failed commit.

The component does not know about `SettingsCoordinator`, persistence, domain dialogs, or message boxes.

### `ConfigurationForm`

Owns use-case orchestration:

- resolves the edited assignment from `SettingsCoordinator.Current` using the executable-path key;
- translates typed grid events into `ApplicationProfileManagementService` operations;
- invokes `SettingsCoordinator.Commit`;
- suppresses only its own synchronous full refresh during a grid-originated transaction;
- asks the grid to update the committed row or restore the failed cell;
- owns application, profile, validation, and confirmation dialogs.

Other settings observers continue to receive the synchronous committed-settings event.

## Transaction flow

```text
selector input
    |
    v
DataGridView commits formatted value
    |
    v
ApplicationProfilesGrid emits typed edit event
    |
    v
ConfigurationForm resolves current assignment
    |
    v
SettingsCoordinator.Commit(domain mutation)
    |
    +-- failure --> restore affected cell
    |
    `-- success --> update affected row
```

The active grid is not cleared or rebound inside its own cell-commit stack.

## Single source of truth

`SettingsCoordinator.Current` remains the committed model source of truth.

Grid rows contain stable executable-path keys rather than mutable `ApplicationProfile` references. After every successful mutation, the form resolves the committed assignment again before updating the row.

Selector items contain identifiers and display names, not copies of domain objects used as alternate authorities.

## Regression coverage

The integration and architecture tests verify that:

- visual-profile selection commits without rebuilding the active grid;
- overlay-scope selection changes only the selected application;
- row tags remain executable-path strings;
- leaving an edited cell releases edit mode;
- external committed settings changes rebind the grid;
- a failed commit restores only the edited value;
- the grid has no dependency on `SettingsCoordinator` or persistence;
- the editing control does not directly mutate cell values;
- global timer, idle-dispatch, reflection, and modal-owner workarounds are absent.