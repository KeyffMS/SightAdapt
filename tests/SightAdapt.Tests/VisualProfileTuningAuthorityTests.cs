using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileTuningAuthorityTests
{
    [TestMethod]
    public void UpdateTuningClampsValuesBeforePersisting()
    {
        var settings = new SightAdaptSettings();
        var profile = VisualProfileManagementService.Create(settings, "Reader");
        var values = profile.CreateWorkingCopy();
        values.OutputBlack = -2.0f;
        values.OutputWhite = 4.0f;
        values.Brightness = float.NaN;
        values.Contrast = 8.0f;
        values.Saturation = -3.0f;
        values.HueShiftDegrees = 900.0f;

        VisualProfileManagementService.UpdateTuning(settings, profile, values);

        Assert.AreEqual(0.0f, profile.OutputBlack);
        Assert.AreEqual(1.0f, profile.OutputWhite);
        Assert.AreEqual(0.0f, profile.Brightness);
        Assert.AreEqual(2.0f, profile.Contrast);
        Assert.AreEqual(0.0f, profile.Saturation);
        Assert.AreEqual(180.0f, profile.HueShiftDegrees);
    }

    [TestMethod]
    public void UpdateTuningRejectsDetachedAndExactInvertProfiles()
    {
        var settings = new SightAdaptSettings();
        var detached = VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultSoftInvertId);
        var exact = settings.VisualProfiles.Single(
            profile => profile.Id == VisualProfileCatalog.DefaultInvertId);

        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.UpdateTuning(
                settings,
                detached,
                detached.CreateWorkingCopy()));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.UpdateTuning(
                settings,
                exact,
                exact.CreateWorkingCopy()));
    }


    [TestMethod]
    public void SecondEditableTransformUsesItsOwnNormalizationAndReset()
    {
        var canonical = new VisualProfileTuning(
            0.12f,
            0.88f,
            0.10f,
            1.10f,
            0.90f,
            15.0f);
        var normalized = new VisualProfileTuning(
            0.20f,
            0.80f,
            0.25f,
            1.50f,
            0.75f,
            30.0f);
        var definition = new VisualProfileDefinition(
            "test-editable-profile",
            TestEditableTransform.TransformId,
            "Test editable",
            supportsTuning: true,
            new TestEditableTransform(),
            canonical,
            _ => normalized);
        var catalog = new VisualProfileCatalog([definition]);
        var profile = new VisualProfile
        {
            Id = "user-test-editable",
            Name = "Test profile",
            TransformId = TestEditableTransform.TransformId,
        };
        var settings = new SightAdaptSettings
        {
            VisualProfiles = [profile],
        };
        var values = profile.CreateWorkingCopy();
        values.OutputBlack = 0.49f;
        values.OutputWhite = 0.51f;
        values.Brightness = -0.5f;
        values.Contrast = 2.0f;
        values.Saturation = 2.0f;
        values.HueShiftDegrees = -180.0f;

        VisualProfileManagementService.UpdateTuning(
            settings,
            profile,
            values,
            catalog);

        AssertTuning(profile, normalized);

        StaTest.Run(() =>
        {
            using var editor =
                new VisualProfileEditorForm(
                    profile,
                    catalog);
            editor.WorkingProfile.Brightness = -0.4f;

            editor.ResetToCanonicalTuning();

            AssertTuning(
                editor.WorkingProfile,
                canonical);
        });
    }

    private static void AssertTuning(
        VisualProfile profile,
        VisualProfileTuning expected)
    {
        Assert.AreEqual(expected.OutputBlack, profile.OutputBlack);
        Assert.AreEqual(expected.OutputWhite, profile.OutputWhite);
        Assert.AreEqual(expected.Brightness, profile.Brightness);
        Assert.AreEqual(expected.Contrast, profile.Contrast);
        Assert.AreEqual(expected.Saturation, profile.Saturation);
        Assert.AreEqual(
            expected.HueShiftDegrees,
            profile.HueShiftDegrees);
    }

    private sealed class TestEditableTransform :
        IVisualTransform
    {
        public const string TransformId = "test-editable";

        public string Id => TransformId;

        public MagColorEffect CreateColorEffect(
            VisualProfile profile)
        {
            ArgumentNullException.ThrowIfNull(profile);
            return MagColorEffect.Invert;
        }
    }

    [TestMethod]
    public void WorkingCopyDoesNotMutatePersistedProfile()
    {
        var source = VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultSoftInvertId);
        var working = source.CreateWorkingCopy();

        working.Brightness = 0.25f;

        Assert.AreEqual(0.0f, source.Brightness);
        Assert.AreEqual(0.25f, working.Brightness);
    }
}
