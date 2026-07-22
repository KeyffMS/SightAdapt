using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SightAdapt.Demo;

internal static class VisualProfileEditingLifecycleGuard
{
    private static readonly FieldInfo DropDownField =
        typeof(ModernVisualProfileEditingControl).GetField(
            "_dropDown",
            BindingFlags.Instance | BindingFlags.NonPublic) ??
        throw new MissingFieldException(
            typeof(ModernVisualProfileEditingControl).FullName,
            "_dropDown");

    private static readonly HashSet<DataGridView> PendingGrids = [];

    [ModuleInitializer]
    internal static void Initialize()
    {
        Application.Idle += ApplicationIdle;
    }

    private static void ApplicationIdle(object? sender, EventArgs eventArgs)
    {
        foreach (Form form in Application.OpenForms)
        {
            InspectControlTree(form);
        }
    }

    private static void InspectControlTree(Control control)
    {
        if (control is DataGridView grid &&
            grid.EditingControl is ModernVisualProfileEditingControl editor &&
            editor.EditingControlValueChanged)
        {
            QueueEditCompletion(grid, editor);
        }

        foreach (Control child in control.Controls)
        {
            InspectControlTree(child);
        }
    }

    private static void QueueEditCompletion(
        DataGridView grid,
        ModernVisualProfileEditingControl editor)
    {
        if (grid.IsDisposed ||
            grid.Disposing ||
            !grid.IsHandleCreated ||
            !PendingGrids.Add(grid))
        {
            return;
        }

        try
        {
            grid.BeginInvoke((Action)(() =>
            {
                PendingGrids.Remove(grid);
                if (grid.IsDisposed ||
                    grid.Disposing ||
                    !ReferenceEquals(grid.EditingControl, editor) ||
                    !editor.EditingControlValueChanged)
                {
                    return;
                }

                if (DropDownField.GetValue(editor) is ToolStripDropDown dropDown &&
                    dropDown.Visible)
                {
                    dropDown.Close(ToolStripDropDownCloseReason.ItemClicked);
                }

                grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
                if (!grid.EndEdit())
                {
                    Debug.WriteLine(
                        "SightAdapt could not release the visual-profile selector after a committed value change.");
                    return;
                }

                grid.Focus();
            }));
        }
        catch (InvalidOperationException exception) when (
            grid.IsDisposed ||
            grid.Disposing ||
            !grid.IsHandleCreated)
        {
            PendingGrids.Remove(grid);
            Debug.WriteLine(
                $"SightAdapt skipped selector lifecycle completion for a disposed grid: {exception}");
        }
    }
}
