using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileTests
{
    [TestMethod]
    public void InvertTransformCreatesExpectedMatrix()
    {
        var transform = new InvertVisualTransform();

        var effect = transform.CreateColorEffect();

        Assert.AreEqual(-1.0f, effect.M00);
        Assert.AreEqual(-1.0f, effect.M11);
        Assert.AreEqual(-1.0f, effect.M22);
        Assert.AreEqual(1.0f, effect.M33);
        Assert.AreEqual(1.0f, effect.M40);
        Assert.AreEqual(1.0f, effect.M41);
        Assert.AreEqual(1.0f, effect.M42);
        Assert.AreEqual(1.0f, effect.M44);
    }

    [TestMethod]
    public void ResolverMatchesExecutablePathCaseInsensitively()
    {
        var settings = new SightAdaptSettings
        {
            Applications =
            [
                new ApplicationProfile
                {
                    DisplayName = "Reader",
                    ExecutableName = "Reader.exe",
                    ExecutablePath = "C:\\Apps\\Reader.exe",
                    Enabled = true,
                },
            ],
        };
        var identity = new ApplicationIdentity(
            "Reader",
            "reader.exe",
            "c:\\apps\\reader.exe");

        var assignment = ProfileResolver.FindEnabledAssignment(settings, identity);

        Assert.IsNotNull(assignment);
    }

    [TestMethod]
    public void DisabledAssignmentDoesNotMatch()
    {
        var settings = new SightAdaptSettings
        {
            Applications =
            [
                new ApplicationProfile
                {
                    DisplayName = "Reader",
                    ExecutableName = "Reader.exe",
                    ExecutablePath = "C:\\Apps\\Reader.exe",
                    Enabled = false,
                },
            ],
        };
        var identity = new ApplicationIdentity(
            "Reader",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var assignment = ProfileResolver.FindEnabledAssignment(settings, identity);

        Assert.IsNull(assignment);
    }

    [TestMethod]
    public void MissingProfileFallsBackToDefaultInvert()
    {
        var settings = new SightAdaptSettings();
        var assignment = new ApplicationProfile
        {
            VisualProfileId = "missing-profile",
        };

        var profile = ProfileResolver.ResolveVisualProfile(settings, assignment);

        Assert.AreEqual(VisualProfile.DefaultInvertId, profile.Id);
        Assert.AreEqual(InvertVisualTransform.TransformId, profile.TransformId);
    }

    [TestMethod]
    public void CatalogRejectsUnknownTransform()
    {
        var catalog = new VisualTransformCatalog();

        Assert.ThrowsException<InvalidOperationException>(
            () => catalog.GetRequired("missing"));
    }

    [TestMethod]
    public void ProfileToggleCreatesEnabledAssignment()
    {
        var settings = new SightAdaptSettings();
        var identity = new ApplicationIdentity(
            "Reader",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var result = ApplicationProfileToggleService.Toggle(settings, identity);

        Assert.IsTrue(result.WasCreated);
        Assert.IsTrue(result.IsEnabled);
        Assert.AreEqual(1, settings.Applications.Count);
        Assert.AreEqual(VisualProfile.DefaultInvertId, result.Profile.VisualProfileId);
    }

    [TestMethod]
    public void ProfileToggleDisablesAndReenablesExistingAssignment()
    {
        var settings = new SightAdaptSettings
        {
            Applications =
            [
                new ApplicationProfile
                {
                    DisplayName = "Reader",
                    ExecutableName = "Reader.exe",
                    ExecutablePath = "C:\\Apps\\Reader.exe",
                    Enabled = true,
                    VisualProfileId = "custom-profile",
                },
            ],
        };
        var identity = new ApplicationIdentity(
            "Reader updated",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var disabled = ApplicationProfileToggleService.Toggle(settings, identity);
        var enabled = ApplicationProfileToggleService.Toggle(settings, identity);

        Assert.IsFalse(disabled.WasCreated);
        Assert.IsFalse(disabled.IsEnabled);
        Assert.IsTrue(enabled.IsEnabled);
        Assert.AreEqual("custom-profile", enabled.Profile.VisualProfileId);
        Assert.AreEqual("Reader updated", enabled.Profile.DisplayName);
        Assert.AreEqual(1, settings.Applications.Count);
    }
}
