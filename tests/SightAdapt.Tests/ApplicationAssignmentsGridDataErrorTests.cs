using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ApplicationAssignmentsGridDataErrorTests
{
    [DataTestMethod]
    [DataRow(
        ApplicationAssignmentsGrid.VisualProfileColumnName)]
    [DataRow(
        ApplicationAssignmentsGrid.OverlayScopeColumnName)]
    public void SelectorPresentationArgumentErrorIsRecoverable(
        string columnName)
    {
        Assert.IsTrue(
            ApplicationAssignmentsGrid.IsExpectedSelectorDataError(
                new ArgumentException("Transient selector value"),
                DataGridViewDataErrorContexts.Formatting |
                    DataGridViewDataErrorContexts.Display,
                columnName));
    }

    [TestMethod]
    public void CommitErrorIsNotClassifiedAsPresentationRace()
    {
        Assert.IsFalse(
            ApplicationAssignmentsGrid.IsExpectedSelectorDataError(
                new ArgumentException("Invalid committed value"),
                DataGridViewDataErrorContexts.Commit,
                ApplicationAssignmentsGrid.VisualProfileColumnName));
    }

    [TestMethod]
    public void InvalidOperationIsNeverBlanketSuppressed()
    {
        Assert.IsFalse(
            ApplicationAssignmentsGrid.IsExpectedSelectorDataError(
                new InvalidOperationException("Broken selector"),
                DataGridViewDataErrorContexts.Formatting,
                ApplicationAssignmentsGrid.VisualProfileColumnName));
    }

    [TestMethod]
    public void NonSelectorArgumentErrorIsNotSuppressed()
    {
        Assert.IsFalse(
            ApplicationAssignmentsGrid.IsExpectedSelectorDataError(
                new ArgumentException("Invalid enabled value"),
                DataGridViewDataErrorContexts.Formatting,
                "Enabled"));
    }

    [TestMethod]
    public void DiagnosticContainsRowColumnPathAndContext()
    {
        var diagnostic =
            ApplicationAssignmentsGrid.CreateDataErrorDiagnostic(
                new ArgumentException("Transient selector value"),
                DataGridViewDataErrorContexts.Display,
                rowIndex: 4,
                columnIndex: 2,
                ApplicationAssignmentsGrid.VisualProfileColumnName,
                @"C:\Apps\Reader.exe",
                recovered: true);

        StringAssert.Contains(diagnostic, "recovered=True");
        StringAssert.Contains(diagnostic, "row=4");
        StringAssert.Contains(diagnostic, "column=2");
        StringAssert.Contains(
            diagnostic,
            ApplicationAssignmentsGrid.VisualProfileColumnName);
        StringAssert.Contains(
            diagnostic,
            @"C:\Apps\Reader.exe");
        StringAssert.Contains(diagnostic, "Display");
    }
}