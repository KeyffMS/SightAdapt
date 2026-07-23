Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

function Write-NewFile([string]$Path, [string]$Content) {
    if (Test-Path $Path) {
        throw "File '$Path' already exists."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

Replace-Exact 'src/SightAdapt/ApplicationProfilesGrid.cs' @'
    private const string VisualProfileColumnName = "VisualProfile";
    private const string OverlayScopeColumnName = "OverlayScope";
'@ @'
    internal const string VisualProfileColumnName = "VisualProfile";
    internal const string OverlayScopeColumnName = "OverlayScope";

    private const DataGridViewDataErrorContexts
        RecoverableSelectorContexts =
            DataGridViewDataErrorContexts.Formatting |
            DataGridViewDataErrorContexts.Display |
            DataGridViewDataErrorContexts.PreferredSize |
            DataGridViewDataErrorContexts.InitialValueRestoration;
'@

Replace-Exact 'src/SightAdapt/ApplicationProfilesGrid.cs' @'
    private static void GridDataError(
        object? sender,
        DataGridViewDataErrorEventArgs eventArgs)
    {
        if (eventArgs.Exception is ArgumentException or InvalidOperationException)
        {
            Debug.WriteLine(
                $"SightAdapt ignored an expected grid binding race: {eventArgs.Exception}");
            eventArgs.ThrowException = false;
        }
    }
'@ @'
    private static void GridDataError(
        object? sender,
        DataGridViewDataErrorEventArgs eventArgs)
    {
        var grid = sender as DataGridView;
        var columnName = GetColumnName(
            grid,
            eventArgs.ColumnIndex);
        var executablePath = GetExecutablePath(
            grid,
            eventArgs.RowIndex);
        var recovered = IsExpectedSelectorDataError(
            eventArgs.Exception,
            eventArgs.Context,
            columnName);

        Debug.WriteLine(CreateDataErrorDiagnostic(
            eventArgs.Exception,
            eventArgs.Context,
            eventArgs.RowIndex,
            eventArgs.ColumnIndex,
            columnName,
            executablePath,
            recovered));
        eventArgs.ThrowException = !recovered;
    }

    internal static bool IsExpectedSelectorDataError(
        Exception? exception,
        DataGridViewDataErrorContexts context,
        string? columnName)
    {
        if (exception is not ArgumentException ||
            !IsSelectorColumn(columnName))
        {
            return false;
        }

        var recoverableContext =
            context & RecoverableSelectorContexts;
        var unexpectedContext =
            context & ~RecoverableSelectorContexts;
        return recoverableContext != 0 &&
            unexpectedContext == 0;
    }

    internal static string CreateDataErrorDiagnostic(
        Exception exception,
        DataGridViewDataErrorContexts context,
        int rowIndex,
        int columnIndex,
        string? columnName,
        string? executablePath,
        bool recovered)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return
            $"SightAdapt grid data error; recovered={recovered}; " +
            $"row={rowIndex}; column={columnIndex}; " +
            $"columnName={columnName ?? "<unknown>"}; " +
            $"executablePath={executablePath ?? "<unknown>"}; " +
            $"context={context}; exception={exception}";
    }

    private static bool IsSelectorColumn(
        string? columnName)
    {
        return string.Equals(
                columnName,
                VisualProfileColumnName,
                StringComparison.Ordinal) ||
            string.Equals(
                columnName,
                OverlayScopeColumnName,
                StringComparison.Ordinal);
    }

    private static string? GetColumnName(
        DataGridView? grid,
        int columnIndex)
    {
        return grid is not null &&
            columnIndex >= 0 &&
            columnIndex < grid.Columns.Count
                ? grid.Columns[columnIndex].Name
                : null;
    }

    private static string? GetExecutablePath(
        DataGridView? grid,
        int rowIndex)
    {
        return grid is not null &&
            rowIndex >= 0 &&
            rowIndex < grid.Rows.Count &&
            grid.Rows[rowIndex].Tag is string path
                ? path
                : null;
    }
'@

Write-NewFile 'tests/SightAdapt.Tests/ApplicationProfilesGridDataErrorTests.cs' @'
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ApplicationProfilesGridDataErrorTests
{
    [DataTestMethod]
    [DataRow(
        ApplicationProfilesGrid.VisualProfileColumnName)]
    [DataRow(
        ApplicationProfilesGrid.OverlayScopeColumnName)]
    public void SelectorPresentationArgumentErrorIsRecoverable(
        string columnName)
    {
        Assert.IsTrue(
            ApplicationProfilesGrid.IsExpectedSelectorDataError(
                new ArgumentException("Transient selector value"),
                DataGridViewDataErrorContexts.Formatting |
                    DataGridViewDataErrorContexts.Display,
                columnName));
    }

    [TestMethod]
    public void CommitErrorIsNotClassifiedAsPresentationRace()
    {
        Assert.IsFalse(
            ApplicationProfilesGrid.IsExpectedSelectorDataError(
                new ArgumentException("Invalid committed value"),
                DataGridViewDataErrorContexts.Commit,
                ApplicationProfilesGrid.VisualProfileColumnName));
    }

    [TestMethod]
    public void InvalidOperationIsNeverBlanketSuppressed()
    {
        Assert.IsFalse(
            ApplicationProfilesGrid.IsExpectedSelectorDataError(
                new InvalidOperationException("Broken selector"),
                DataGridViewDataErrorContexts.Formatting,
                ApplicationProfilesGrid.VisualProfileColumnName));
    }

    [TestMethod]
    public void NonSelectorArgumentErrorIsNotSuppressed()
    {
        Assert.IsFalse(
            ApplicationProfilesGrid.IsExpectedSelectorDataError(
                new ArgumentException("Invalid enabled value"),
                DataGridViewDataErrorContexts.Formatting,
                "Enabled"));
    }

    [TestMethod]
    public void DiagnosticContainsRowColumnPathAndContext()
    {
        var diagnostic =
            ApplicationProfilesGrid.CreateDataErrorDiagnostic(
                new ArgumentException("Transient selector value"),
                DataGridViewDataErrorContexts.Display,
                rowIndex: 4,
                columnIndex: 2,
                ApplicationProfilesGrid.VisualProfileColumnName,
                @"C:\Apps\Reader.exe",
                recovered: true);

        StringAssert.Contains(diagnostic, "recovered=True");
        StringAssert.Contains(diagnostic, "row=4");
        StringAssert.Contains(diagnostic, "column=2");
        StringAssert.Contains(
            diagnostic,
            ApplicationProfilesGrid.VisualProfileColumnName);
        StringAssert.Contains(
            diagnostic,
            @"C:\Apps\Reader.exe");
        StringAssert.Contains(diagnostic, "Display");
    }
}
'@
