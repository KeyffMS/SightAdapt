using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ApplicationAssignmentManagementTests
{
    [TestMethod]
    public void AddOrEnableCreatesDefaultSoftInvertAssignment()
    {
        var settings = new SightAdaptSettings();
        var identity = CreateIdentity("Reader");

        var result = ApplicationAssignmentService.AddOrEnable(settings, identity);

        Assert.IsTrue(result.WasCreated);
        Assert.IsTrue(result.IsEnabled);
        Assert.AreEqual(VisualProfilePolicy.NewAssignmentProfileId, result.Assignment.VisualProfileId);
        Assert.AreEqual(1, settings.Assignments.Count);
    }

    [TestMethod]
    public void AssignVisualProfileRequiresExistingProfile()
    {
        var settings = new SightAdaptSettings();
        var assignment = ApplicationAssignmentService
            .AddOrEnable(settings, CreateIdentity("Reader"))
            .Assignment;
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");

        ApplicationAssignmentService.AssignVisualProfile(
            settings,
            assignment,
            custom.Id);

        Assert.AreEqual(custom.Id, assignment.VisualProfileId);
        Assert.ThrowsException<SettingsValidationException>(() =>
            ApplicationAssignmentService.AssignVisualProfile(
                settings,
                assignment,
                "missing-profile"));
    }

    [TestMethod]
    public void TogglePreservesValidCustomProfile()
    {
        var settings = new SightAdaptSettings();
        var identity = CreateIdentity("Reader");
        var assignment = ApplicationAssignmentService
            .AddOrEnable(settings, identity)
            .Assignment;
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");
        ApplicationAssignmentService.AssignVisualProfile(settings, assignment, custom.Id);

        var disabled = ApplicationAssignmentService.Toggle(settings, identity);
        var enabled = ApplicationAssignmentService.Toggle(settings, identity);

        Assert.IsFalse(disabled.IsEnabled);
        Assert.IsTrue(enabled.IsEnabled);
        Assert.AreEqual(custom.Id, enabled.Assignment.VisualProfileId);
    }

    [TestMethod]
    public void DetachedAssignmentCannotBeMutatedOrRemoved()
    {
        var settings = new SightAdaptSettings();
        var detached = new ApplicationAssignment();

        Assert.ThrowsException<SettingsValidationException>(() =>
            ApplicationAssignmentService.SetEnabled(settings, detached, false));
        Assert.ThrowsException<SettingsValidationException>(() =>
            ApplicationAssignmentService.Remove(settings, detached));
    }

    [TestMethod]
    public void ReassignVisualProfileUpdatesAllMatchingAssignments()
    {
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");
        var first = ApplicationAssignmentService
            .AddOrEnable(settings, CreateIdentity("Reader"))
            .Assignment;
        var second = ApplicationAssignmentService
            .AddOrEnable(settings, CreateIdentity("Notes"))
            .Assignment;
        ApplicationAssignmentService.AssignVisualProfile(settings, first, custom.Id);
        ApplicationAssignmentService.AssignVisualProfile(settings, second, custom.Id);

        var changed = ApplicationAssignmentService.ReassignVisualProfile(
            settings,
            custom.Id,
            VisualProfileCatalog.DefaultSoftInvertId);

        Assert.AreEqual(2, changed);
        Assert.IsTrue(settings.Assignments.All(
            assignment => assignment.VisualProfileId == VisualProfileCatalog.DefaultSoftInvertId));
    }

    private static ApplicationIdentity CreateIdentity(string name)
    {
        return new ApplicationIdentity(
            name,
            $"{name}.exe",
            Path.Combine("C:\\Apps", $"{name}.exe"));
    }
}
