# Issue #9 — application-grid transaction boundary

## Root cause

The visual-profile editing control violated two parts of the `IDataGridViewEditingControl` contract:

1. it wrote directly to `DataGridView.CurrentCell.Value`, which raised `CellValueChanged` inside the selector's own input stack;
2. it exposed the profile identifier as `EditingControlFormattedValue`, although `DataGridViewComboBoxCell` expects the display text and converts that text to `ValueMember` itself.

The direct cell write was therefore compensating for the incorrect formatted value. Removing only that write made profile selection appear to do nothing; keeping it caused the re-entrant grid refresh.

`ConfigurationForm` committed settings after `CellValueChanged`. Its settings notification rebuilt the same grid with `Rows.Clear()` while the cell was still committing, causing the original exception.

Later attempts moved knowledge about WinForms controls, modal dialogs, grid editing, timers, and popup lifetime into `SettingsCoordinator`. Those attempts crossed architectural boundaries and produced secondary failures in `Manage profiles`, `Browse for .exe`, and the selector itself.

## Correct ownership

- `SettingsCoordinator` owns transactional persistence and publishes one synchronous settings event after a successful save.
- `ModernVisualProfileEditingControl` returns the selected profile name as its formatted value. `DataGridViewComboBoxCell` resolves that display text to the profile identifier through `DisplayMember` and `ValueMember`.
- The editing control reports a dirty value through `EditingControlValueChanged` and `NotifyCurrentCellDirty(true)`. It does not mutate a grid cell directly and does not force `EndEdit`.
- `ConfigurationForm` owns the application grid and its refresh boundary.
- During a grid-originated settings commit, `ConfigurationForm` ignores only its own settings notification because the edited cell already contains the committed value.
- After success, the form replaces the row's stale model reference with the committed assignment and updates the dependent actions without rebuilding the table.
- Other observers still receive the committed settings synchronously.
- Failed commits restore only the edited cell, without clearing the grid inside the active edit stack.

## Removed workarounds

The correction removes:

- UI and `DataGridView` inspection from `SettingsCoordinator`;
- modal-owner timers and delayed observer dispatch;
- automatic `CommitEdit` / `EndEdit` forcing from the selector;
- the global `Application.Idle` selector guard;
- reflection-based popup manipulation.

This restores KISS, DRY, Single Point of Authority, and Single Point of Truth boundaries for the settings transaction and the configuration grid.
