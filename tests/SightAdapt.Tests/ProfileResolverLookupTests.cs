using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProfileResolverLookupTests
{
    [TestMethod]
    public void AssignmentPathLookupIsTrimmedAndCaseInsensitive()
    {
        var settings = CreateSettings();
        var assignment =
            settings.Applications.Single();

        Assert.AreSame(
            assignment,
            ProfileResolver.FindAssignmentByExecutablePath(
                settings,
                " c:\\apps\\reader.exe "));
        Assert.AreSame(
            assignment,
            ProfileResolver.RequireAssignmentByExecutablePath(
                settings,
                "C:\\APPS\\READER.EXE"));
    }

    [TestMethod]
    public void RequiredAssignmentReportsMissingSelection()
    {
        var settings = CreateSettings();

        Assert.IsNull(
            ProfileResolver.FindAssignmentByExecutablePath(
                settings,
                "C:\\Apps\\Missing.exe"));
        Assert.ThrowsException<SettingsValidationException>(() =>
            ProfileResolver.RequireAssignmentByExecutablePath(
                settings,
                "C:\\Apps\\Missing.exe"));
    }

    [TestMethod]
    public void RequiredVisualProfileUsesCanonicalCaseInsensitiveLookup()
    {
        var settings = CreateSettings();
        var profile = settings.VisualProfiles.Single(candidate =>
            candidate.Id == VisualProfile.DefaultSoftInvertId);

        Assert.AreSame(
            profile,
            ProfileResolver.RequireVisualProfile(
                settings,
                " DEFAULT-SOFT-INVERT "));
    }

    [TestMethod]
    public void RequiredVisualProfileReportsMissingSelection()
    {
        Assert.ThrowsException<SettingsValidationException>(() =>
            ProfileResolver.RequireVisualProfile(
                CreateSettings(),
                "missing-profile"));
    }

    [TestMethod]
    public void ProfileNameResolutionUsesSharedFallback()
    {
        var settings = CreateSettings();

        Assert.AreEqual(
            "Soft invert",
            ProfileResolver.ResolveVisualProfileName(
                settings,
                "DEFAULT-SOFT-INVERT",
                "Visual correction"));
        Assert.AreEqual(
            "Visual correction",
            ProfileResolver.ResolveVisualProfileName(
                settings,
                "missing-profile",
                "Visual correction"));
    }

    private static SightAdaptSettings CreateSettings()
    {
        var settings = new SightAdaptSettings();
        settings.Applications.Add(new ApplicationProfile
        {
            DisplayName = "Reader",
            ExecutableName = "Reader.exe",
            ExecutablePath = @"C:\Apps\Reader.exe",
            VisualProfileId =
                VisualProfile.DefaultSoftInvertId,
        });
        return settings;
    }
}
