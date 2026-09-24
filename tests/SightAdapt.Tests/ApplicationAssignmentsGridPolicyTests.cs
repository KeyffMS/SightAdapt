using System.Drawing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ApplicationAssignmentsGridPolicyTests
{
    private const string PathValue =
        @"C:\Apps\reader.exe";

    [TestMethod]
    public void CellChangeMapperCreatesTypedChangesWithoutWinFormsEvents()
    {
        var enabled =
            ApplicationAssignmentCellChangeMapper.Map(
                PathValue,
                ApplicationAssignmentsGridColumns
                    .EnabledColumnName,
                false);
        var visual =
            ApplicationAssignmentCellChangeMapper.Map(
                PathValue,
                ApplicationAssignmentsGridColumns
                    .VisualProfileColumnName,
                "profile-id");
        var menu =
            ApplicationAssignmentCellChangeMapper.Map(
                PathValue,
                ApplicationAssignmentsGridColumns
                    .MenuVisualProfileColumnName,
                ApplicationMenuProfilePolicy
                    .InheritSelectorId);
        var scope =
            ApplicationAssignmentCellChangeMapper.Map(
                PathValue,
                ApplicationAssignmentsGridColumns
                    .OverlayScopeColumnName,
                "screen");

        Assert.AreEqual(
            new ApplicationAssignmentChange.Enabled(
                PathValue,
                false),
            enabled);
        Assert.AreEqual(
            new ApplicationAssignmentChange.VisualProfile(
                PathValue,
                "profile-id"),
            visual);
        Assert.AreEqual(
            new ApplicationAssignmentChange.MenuVisualProfile(
                PathValue,
                null),
            menu);
        Assert.AreEqual(
            new ApplicationAssignmentChange.OverlayScope(
                PathValue,
                OverlayScope.Screen),
            scope);
    }

    [TestMethod]
    public void CellChangeMapperIgnoresReadOnlyColumn()
    {
        Assert.IsNull(
            ApplicationAssignmentCellChangeMapper.Map(
                PathValue,
                ApplicationAssignmentsGridColumns
                    .ApplicationColumnName,
                "Reader"));
    }

    [TestMethod]
    public void EnabledIndicatorGeometryIsCenteredAndStable()
    {
        var cell = new Rectangle(
            x: 10,
            y: 20,
            width: 92,
            height: 40);

        var indicator =
            ApplicationAssignmentEnabledCellRenderer
                .GetIndicatorBounds(cell);

        Assert.AreEqual(15, indicator.Width);
        Assert.AreEqual(15, indicator.Height);
        Assert.AreEqual(
            cell.Left +
                (cell.Width - indicator.Width) / 2,
            indicator.Left);
        Assert.AreEqual(
            cell.Top +
                (cell.Height - indicator.Height) / 2,
            indicator.Top);
    }

    [TestMethod]
    public void ColumnPolicyCreatesExpectedSelectorColumns()
    {
        StaTest.Run(() =>
        {
            using var grid = new DataGridView();
            ApplicationAssignmentsGridColumns.AddTo(grid);

            Assert.AreEqual(7, grid.Columns.Count);
            Assert.IsInstanceOfType<
                StableModernSelectorComboBoxColumn>(
                    grid.Columns[
                        ApplicationAssignmentsGridColumns
                            .VisualProfileColumnName]);
            Assert.IsInstanceOfType<
                StableModernSelectorComboBoxColumn>(
                    grid.Columns[
                        ApplicationAssignmentsGridColumns
                            .MenuVisualProfileColumnName]);
            Assert.IsInstanceOfType<
                StableModernSelectorComboBoxColumn>(
                    grid.Columns[
                        ApplicationAssignmentsGridColumns
                            .OverlayScopeColumnName]);
        });
    }
}
