using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileTests
{
    private const float MatrixTolerance = 0.0001f;

    [TestMethod]
    public void InvertTransformCreatesExpectedMatrix()
    {
        var transform = new InvertVisualTransform();

        var effect = transform.CreateColorEffect(VisualProfile.CreateDefaultInvert());

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
    public void BuiltInProfilesExposeReadableDisplayNames()
    {
        var exactInvert = VisualProfile.CreateDefaultInvert();
        var softInvert = VisualProfile.CreateDefaultSoftInvert();

        Assert.AreEqual("Exact invert", exactInvert.ToString());
        Assert.AreEqual("Soft invert", softInvert.ToString());
    }

    [TestMethod]
    public void ProfileComboColumnKeepsStableUnboundOptionsAcrossRefreshes()
    {
        using var column = new DataGridViewComboBoxColumn();
        var profiles = new[]
        {
            VisualProfile.CreateDefaultInvert(),
            VisualProfile.CreateDefaultSoftInvert(),
        };

        column.DataSource = profiles;
        column.DataSource = null;
        column.DataSource = profiles.ToList();

        Assert.IsNull(((System.Windows.Forms.DataGridViewComboBoxColumn)column).DataSource);
        Assert.AreEqual(nameof(VisualProfileOption.Name), column.DisplayMember);
        Assert.AreEqual(nameof(VisualProfileOption.Id), column.ValueMember);
        Assert.AreEqual(2, column.Items.Count);
        Assert.AreEqual("Exact invert", column.Items[0].ToString());
        Assert.AreEqual("Soft invert", column.Items[1].ToString());
    }

    [TestMethod]
    public void DefaultSoftInvertLimitsBlackAndWhiteOutput()
    {
        var profile = VisualProfile.CreateDefaultSoftInvert();
        var transform = new SoftInvertVisualTransform();

        var effect = transform.CreateColorEffect(profile);

        Assert.AreEqual(-0.84f, effect.M00, MatrixTolerance);
        Assert.AreEqual(-0.84f, effect.M11, MatrixTolerance);
        Assert.AreEqual(-0.84f, effect.M22, MatrixTolerance);
        Assert.AreEqual(0.92f, effect.M40, MatrixTolerance);
        Assert.AreEqual(0.92f, effect.M41, MatrixTolerance);
        Assert.AreEqual(0.92f, effect.M42, MatrixTolerance);
        Assert.AreEqual(1.0f, effect.M33, MatrixTolerance);
        Assert.AreEqual(1.0f, effect.M44, MatrixTolerance);
    }

    [TestMethod]
    public void SoftInvertComposesContrastAndBrightness()
    {
        var profile = VisualProfile.CreateDefaultSoftInvert();
        profile.OutputBlack = 0.0f;
        profile.OutputWhite = 1.0f;
        profile.Contrast = 1.5f;
        profile.Brightness = 0.1f;
        var transform = new SoftInvertVisualTransform();

        var effect = transform.CreateColorEffect(profile);

        Assert.AreEqual(-1.5f, effect.M00, MatrixTolerance);
        Assert.AreEqual(-1.5f, effect.M11, MatrixTolerance);
        Assert.AreEqual(-1.5f, effect.M22, MatrixTolerance);
        Assert.AreEqual(1.35f, effect.M40, MatrixTolerance);
        Assert.AreEqual(1.35f, effect.M41, MatrixTolerance);
        Assert.AreEqual(1.35f, effect.M42, MatrixTolerance);
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
    public void DisabledAssignmentDoesNotMatchAutomaticResolver()
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
    public void DisabledAssignmentRemainsAvailableForLocalProfileSelection()
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
                    VisualProfileId = VisualProfile.DefaultSoftInvertId,
                },
            ],
        };
        var identity = new ApplicationIdentity(
            "Reader",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var assignment = ProfileResolver.FindAssignment(settings, identity);

        Assert.IsNotNull(assignment);
        Assert.AreEqual(VisualProfile.DefaultSoftInvertId, assignment.VisualProfileId);
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

        Assert.AreEqual(
            VisualProfilePolicy.MissingReferenceFallbackProfileId,
            profile.Id);
        Assert.AreEqual(InvertVisualTransform.TransformId, profile.TransformId);
    }

    [TestMethod]
    public void CatalogContainsSoftInvertTransform()
    {
        var catalog = new VisualTransformCatalog();

        var transform = catalog.GetRequired(SoftInvertVisualTransform.TransformId);

        Assert.IsInstanceOfType<SoftInvertVisualTransform>(transform);
    }

    [TestMethod]
    public void CatalogRejectsUnknownTransform()
    {
        var catalog = new VisualTransformCatalog();

        Assert.ThrowsException<InvalidOperationException>(
            () => catalog.GetRequired("missing"));
    }

    [TestMethod]
    public void ProfileToggleCreatesEnabledSoftInvertAssignment()
    {
        var settings = new SightAdaptSettings();
        var identity = new ApplicationIdentity(
            "Reader",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var result = ApplicationProfileManagementService.Toggle(settings, identity);

        Assert.IsTrue(result.WasCreated);
        Assert.IsTrue(result.IsEnabled);
        Assert.AreEqual(1, settings.Applications.Count);
        Assert.AreEqual(
            VisualProfilePolicy.NewAssignmentProfileId,
            result.Profile.VisualProfileId);
    }

    [TestMethod]
    public void ProfileToggleDisablesAndReenablesExistingAssignment()
    {
        var customProfile = VisualProfile.CreateDefaultSoftInvert();
        customProfile.Id = "custom-profile";
        customProfile.Name = "Custom profile";
        var settings = new SightAdaptSettings
        {
            VisualProfiles =
            [
                VisualProfile.CreateDefaultInvert(),
                VisualProfile.CreateDefaultSoftInvert(),
                customProfile,
            ],
            Applications =
            [
                new ApplicationProfile
                {
                    DisplayName = "Reader",
                    ExecutableName = "Reader.exe",
                    ExecutablePath = "C:\\Apps\\Reader.exe",
                    Enabled = true,
                    VisualProfileId = customProfile.Id,
                },
            ],
        };
        var identity = new ApplicationIdentity(
            "Reader updated",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var disabled = ApplicationProfileManagementService.Toggle(settings, identity);
        var enabled = ApplicationProfileManagementService.Toggle(settings, identity);

        Assert.IsFalse(disabled.WasCreated);
        Assert.IsFalse(disabled.IsEnabled);
        Assert.IsTrue(enabled.IsEnabled);
        Assert.AreEqual(customProfile.Id, enabled.Profile.VisualProfileId);
        Assert.AreEqual("Reader updated", enabled.Profile.DisplayName);
        Assert.AreEqual(1, settings.Applications.Count);
    }

    [TestMethod]
    public void ProfileToggleRepairsMissingProfileReference()
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
                    VisualProfileId = "missing-profile",
                },
            ],
        };
        var identity = new ApplicationIdentity(
            "Reader",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var result = ApplicationProfileManagementService.Toggle(settings, identity);

        Assert.IsFalse(result.IsEnabled);
        Assert.AreEqual(
            VisualProfilePolicy.MissingReferenceFallbackProfileId,
            result.Profile.VisualProfileId);
    }
}
