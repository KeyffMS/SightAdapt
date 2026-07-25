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
            profile.Id == VisualProfile.DefaultNoneId);

        Assert.AreEqual(VisualProfileDefaults.NoneName, none.Name);
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
        var profile = VisualProfile.CreateDefaultNone();
        var transform = VisualTransformCatalog.Default.GetRequired(
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
    public void ExplicitMenuProfileOverridesNoneApplicationProfile()
    {
        var settings = new SightAdaptSettings();
        var assignment = ApplicationProfileManagementService
            .AddOrEnable(
                settings,
                new ApplicationIdentity(
                    "Reader",
                    "reader.exe",
                    @"C:\Apps\reader.exe"))
            .Profile;
        ApplicationProfileManagementService.AssignVisualProfile(
            settings,
            assignment,
            VisualProfile.DefaultNoneId);
        ApplicationProfileManagementService.AssignMenuVisualProfile(
            settings,
            assignment,
            VisualProfile.DefaultInvertId);

        var primary = ProfileResolver.ResolveVisualProfile(
            settings,
            assignment);
        var menu = ProfileResolver.ResolveMenuVisualProfile(
            settings,
            assignment);

        Assert.AreEqual(VisualProfile.DefaultNoneId, primary.Id);
        Assert.AreEqual(VisualProfile.DefaultInvertId, menu.Id);
    }

    [TestMethod]
    public void ExistingSchemaFiveSettingsGainNoneProfileOnce()
    {
        var settings = new SightAdaptSettings
        {
            SchemaVersion = 5,
            VisualProfiles =
            [
                VisualProfile.CreateDefaultInvert(),
                VisualProfile.CreateDefaultSoftInvert(),
            ],
        };

        Assert.IsTrue(SettingsStore.Normalize(settings));
        Assert.IsTrue(settings.VisualProfiles.Any(profile =>
            profile.Id == VisualProfile.DefaultNoneId));
        Assert.IsFalse(SettingsStore.Normalize(settings));
    }
}
