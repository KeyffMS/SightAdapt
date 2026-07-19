using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class ApplicationProfileManagementTests
{
    [TestMethod]
    public void AddOrEnableCreatesDefaultSoftInvertAssignment()
    {
        var settings = new SightAdaptSettings();
        var identity = CreateIdentity("Reader");

        var result = ApplicationProfileManagementService.AddOrEnable(settings, identity);

        Assert.IsTrue(result.WasCreated);
        Assert.IsTrue(result.IsEnabled);
        Assert.AreEqual(VisualProfilePolicy.NewAssignmentProfileId, result.Profile.VisualProfileId);
        Assert.AreEqual(1, settings.Applications.Count);
    }

    [TestMethod]
    public void AssignVisualProfileRequiresExistingProfile()
    {
        var settings = new SightAdaptSettings();
        var assignment = ApplicationProfileManagementService
            .AddOrEnable(settings, CreateIdentity("Reader"))
            .Profile;
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");

        ApplicationProfileManagementService.AssignVisualProfile(
            settings,
            assignment,
            custom.Id);

        Assert.AreEqual(custom.Id, assignment.VisualProfileId);
        Assert.ThrowsException<InvalidOperationException>(() =>
            ApplicationProfileManagementService.AssignVisualProfile(
                settings,
                assignment,
                "missing-profile"));
    }

    [TestMethod]
    public void TogglePreservesValidCustomProfile()
    {
        var settings = new SightAdaptSettings();
        var identity = CreateIdentity("Reader");
        var assignment = ApplicationProfileManagementService
            .AddOrEnable(settings, identity)
            .Profile;
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");
        ApplicationProfileManagementService.AssignVisualProfile(settings, assignment, custom.Id);

        var disabled = ApplicationProfileManagementService.Toggle(settings, identity);
        var enabled = ApplicationProfileManagementService.Toggle(settings, identity);

        Assert.IsFalse(disabled.IsEnabled);
        Assert.IsTrue(enabled.IsEnabled);
        Assert.AreEqual(custom.Id, enabled.Profile.VisualProfileId);
    }

    [TestMethod]
    public void DetachedAssignmentCannotBeMutatedOrRemoved()
    {
        var settings = new SightAdaptSettings();
        var detached = new ApplicationProfile();

        Assert.ThrowsException<InvalidOperationException>(() =>
            ApplicationProfileManagementService.SetEnabled(settings, detached, false));
        Assert.ThrowsException<InvalidOperationException>(() =>
            ApplicationProfileManagementService.Remove(settings, detached));
    }

    [TestMethod]
    public void ReassignVisualProfileUpdatesAllMatchingAssignments()
    {
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");
        var first = ApplicationProfileManagementService
            .AddOrEnable(settings, CreateIdentity("Reader"))
            .Profile;
        var second = ApplicationProfileManagementService
            .AddOrEnable(settings, CreateIdentity("Notes"))
            .Profile;
        ApplicationProfileManagementService.AssignVisualProfile(settings, first, custom.Id);
        ApplicationProfileManagementService.AssignVisualProfile(settings, second, custom.Id);

        var changed = ApplicationProfileManagementService.ReassignVisualProfile(
            settings,
            custom.Id,
            VisualProfile.DefaultSoftInvertId);

        Assert.AreEqual(2, changed);
        Assert.IsTrue(settings.Applications.All(
            assignment => assignment.VisualProfileId == VisualProfile.DefaultSoftInvertId));
    }

    private static ApplicationIdentity CreateIdentity(string name)
    {
        return new ApplicationIdentity(
            name,
            $"{name}.exe",
            Path.Combine("C:\\Apps", $"{name}.exe"));
    }
}
