using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ApplicationAssignmentPresentationTests
{
    [TestMethod]
    public void RowMapperCreatesTheOnlyGridRepresentation()
    {
        var assignment = new ApplicationAssignment
        {
            DisplayName = "Reader",
            ExecutableName = "reader.exe",
            ExecutablePath = @"C:\Apps\reader.exe",
            Enabled = false,
            VisualProfileId = VisualProfileCatalog.DefaultNoneId,
            MenuVisualProfileId = VisualProfileCatalog.DefaultInvertId,
            OverlayScopeId = OverlayScopePolicy.ToId(
                OverlayScope.Screen),
        };

        var row = ApplicationAssignmentRowMapper.Map(assignment);

        Assert.AreEqual(assignment.ExecutablePath, row.ExecutablePath);
        Assert.IsFalse(row.Enabled);
        Assert.AreEqual(assignment.DisplayName, row.DisplayName);
        Assert.AreEqual(assignment.VisualProfileId, row.VisualProfileId);
        Assert.AreEqual(
            assignment.MenuVisualProfileId,
            row.MenuVisualProfileSelectorId);
        Assert.AreEqual("screen", row.OverlayScopeId);
        Assert.AreEqual(assignment.ExecutableName, row.ExecutableName);
    }

    [TestMethod]
    public void InheritedMenuProfileMapsToSelectorSentinel()
    {
        var assignment = new ApplicationAssignment
        {
            ExecutablePath = @"C:\Apps\reader.exe",
            ExecutableName = "reader.exe",
            MenuVisualProfileId = null,
        };

        var row = ApplicationAssignmentRowMapper.Map(assignment);

        Assert.AreEqual(
            ApplicationMenuProfilePolicy.InheritSelectorId,
            row.MenuVisualProfileSelectorId);
    }

    [TestMethod]
    public void AssignmentChangesCarryOneStableApplicationKey()
    {
        const string path = @"C:\Apps\reader.exe";
        ApplicationAssignmentChange[] changes =
        [
            new ApplicationAssignmentChange.Enabled(path, true),
            new ApplicationAssignmentChange.VisualProfile(
                path,
                VisualProfileCatalog.DefaultInvertId),
            new ApplicationAssignmentChange.MenuVisualProfile(path, null),
            new ApplicationAssignmentChange.OverlayScope(
                path,
                OverlayScope.Window),
        ];

        Assert.IsTrue(changes.All(change => change.ExecutablePath == path));
    }
}
