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