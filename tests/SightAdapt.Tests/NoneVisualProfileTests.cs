using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class NoneVisualProfileTests
{
    [TestMethod]
    public void DefaultSettingsExposeProtectedNoneProfile()
    {
        var settings = new SightAdaptSettings();
        var none = settings.VisualProfiles.Single(profile =>
            profile.Id == VisualProfileCatalog.DefaultNoneId);

        Assert.AreEqual(
            VisualProfileCatalog.Default
                .GetRequiredBuiltInDefinition(
                    VisualProfileCatalog.DefaultNoneId)
                .DisplayName,
            none.Name);
        Assert.AreEqual(NoneVisualTransform.TransformId, none.TransformId);
        Assert.IsFalse(none.SupportsTuning);
        Assert.IsTrue(VisualProfileManagementService.IsBuiltIn(none));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Rename(
                settings,
                none,
                "Changed"));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Delete(settings, none));
    }

    [TestMethod]
    public void NoneTransformProducesIdentityColorEffect()
    {
        var profile = VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultNoneId);
        var transform = VisualProfileCatalog.Default.GetRequiredTransform(
            profile.TransformId);

        var effect = transform.CreateColorEffect(profile);

        Assert.AreEqual(1.0f, effect.M00);
        Assert.AreEqual(1.0f, effect.M11);
        Assert.AreEqual(1.0f, effect.M22);
        Assert.AreEqual(1.0f, effect.M33);
        Assert.AreEqual(1.0f, effect.M44);
        Assert.AreEqual(0.0f, effect.M01);
        Assert.AreEqual(0.0f, effect.M02);
        Assert.AreEqual(0.0f, effect.M10);
        Assert.AreEqual(0.0f, effect.M12);
        Assert.AreEqual(0.0f, effect.M20);
        Assert.AreEqual(0.0f, effect.M21);
        Assert.AreEqual(0.0f, effect.M40);
        Assert.AreEqual(0.0f, effect.M41);
        Assert.AreEqual(0.0f, effect.M42);
    }

    [TestMethod]
    public void ExplicitMenuProfileOverridesNoneApplicationAssignment()
    {
        var settings = new SightAdaptSettings();
        var assignment = ApplicationAssignmentService
            .AddOrEnable(
                settings,
                new ApplicationIdentity(
                    "Reader",
                    "reader.exe",
                    @"C:\Apps\reader.exe"))
            .Assignment;
        ApplicationAssignmentService.AssignVisualProfile(
            settings,
            assignment,
            VisualProfileCatalog.DefaultNoneId);
        ApplicationAssignmentService.AssignMenuVisualProfile(
            settings,
            assignment,
            VisualProfileCatalog.DefaultInvertId);

        var primary = ProfileResolver.ResolveVisualProfile(
            settings,
            assignment);
        var menu = ProfileResolver.ResolveMenuVisualProfile(
            settings,
            assignment);

        Assert.AreEqual(VisualProfileCatalog.DefaultNoneId, primary.Id);
        Assert.AreEqual(VisualProfileCatalog.DefaultInvertId, menu.Id);
    }

    [TestMethod]
    public void ExistingSchemaFiveSettingsGainNoneProfileOnce()
    {
        var settings = new SightAdaptSettings
        {
            SchemaVersion = 5,
            VisualProfiles =
            [
                VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultInvertId),
                VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultSoftInvertId),
            ],
        };

        Assert.IsTrue(SettingsStore.Normalize(settings));
        Assert.IsTrue(settings.VisualProfiles.Any(profile =>
            profile.Id == VisualProfileCatalog.DefaultNoneId));
        Assert.IsFalse(SettingsStore.Normalize(settings));
    }
}
