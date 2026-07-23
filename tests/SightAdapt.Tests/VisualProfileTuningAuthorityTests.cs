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
        var detached = VisualProfile.CreateDefaultSoftInvert();
        var exact = settings.VisualProfiles.Single(
            profile => profile.Id == VisualProfile.DefaultInvertId);

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
    public void WorkingCopyDoesNotMutatePersistedProfile()
    {
        var source = VisualProfile.CreateDefaultSoftInvert();
        var working = source.CreateWorkingCopy();

        working.Brightness = 0.25f;

        Assert.AreEqual(0.0f, source.Brightness);
        Assert.AreEqual(0.25f, working.Brightness);
    }
}
