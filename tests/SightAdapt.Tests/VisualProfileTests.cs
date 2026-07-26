using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileTests
{
    private const float MatrixTolerance =
        0.0001f;

    [TestMethod]
    public void InvertTransformCreatesExpectedMatrix()
    {
        var effect =
            new InvertVisualTransform()
                .CreateColorEffect(
                    VisualProfileCatalog.Default.CreateBuiltInProfile(
                    VisualProfileCatalog.DefaultInvertId));

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
        Assert.AreEqual(
            "Exact invert",
            VisualProfileCatalog.Default.CreateBuiltInProfile(
                    VisualProfileCatalog.DefaultInvertId)
                .ToString());
        Assert.AreEqual(
            "Soft invert",
            VisualProfileCatalog.Default.CreateBuiltInProfile(
                    VisualProfileCatalog.DefaultSoftInvertId)
                .ToString());
    }

    [TestMethod]
    public void ProfileComboColumnKeepsStableUnboundOptions()
    {
        using var column =
            new StableModernSelectorComboBoxColumn();
        var profiles = new[]
        {
            VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultInvertId),
            VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultSoftInvertId),
        };

        column.SetProfiles(profiles);
        column.SetProfiles(profiles.ToList());

        Assert.IsNull(column.DataSource);
        Assert.AreEqual(
            nameof(ModernSelectorOption.Name),
            column.DisplayMember);
        Assert.AreEqual(
            nameof(ModernSelectorOption.Id),
            column.ValueMember);
        Assert.AreEqual(2, column.Items.Count);
        Assert.AreEqual(
            "Exact invert",
            column.Items[0].ToString());
        Assert.AreEqual(
            "Soft invert",
            column.Items[1].ToString());
    }

    [TestMethod]
    public void DefaultSoftInvertLimitsOutput()
    {
        var effect =
            new SoftInvertVisualTransform()
                .CreateColorEffect(
                    VisualProfileCatalog.Default.CreateBuiltInProfile(
                    VisualProfileCatalog.DefaultSoftInvertId));

        Assert.AreEqual(
            -0.84f,
            effect.M00,
            MatrixTolerance);
        Assert.AreEqual(
            -0.84f,
            effect.M11,
            MatrixTolerance);
        Assert.AreEqual(
            -0.84f,
            effect.M22,
            MatrixTolerance);
        Assert.AreEqual(
            0.92f,
            effect.M40,
            MatrixTolerance);
        Assert.AreEqual(
            0.92f,
            effect.M41,
            MatrixTolerance);
        Assert.AreEqual(
            0.92f,
            effect.M42,
            MatrixTolerance);
    }

    [TestMethod]
    public void SoftInvertComposesContrastAndBrightness()
    {
        var profile =
            VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultSoftInvertId);
        profile.OutputBlack = 0.0f;
        profile.OutputWhite = 1.0f;
        profile.Contrast = 1.5f;
        profile.Brightness = 0.1f;

        var effect =
            new SoftInvertVisualTransform()
                .CreateColorEffect(profile);

        Assert.AreEqual(
            -1.5f,
            effect.M00,
            MatrixTolerance);
        Assert.AreEqual(
            1.35f,
            effect.M40,
            MatrixTolerance);
    }

    [TestMethod]
    public void CatalogIsCapabilitySourceOfTruth()
    {
        Assert.IsTrue(
            VisualProfileCatalog.Default.IsSupportedTransform(
                InvertVisualTransform.TransformId));
        Assert.IsTrue(
            VisualProfileCatalog.Default.IsSupportedTransform(
                SoftInvertVisualTransform.TransformId));
        Assert.IsFalse(
            VisualProfileCatalog.Default.IsSupportedTransform(
                "missing"));
        Assert.IsFalse(
            VisualProfileCatalog.Default.SupportsTuning(
                InvertVisualTransform.TransformId));
        Assert.IsTrue(
            VisualProfileCatalog.Default.SupportsTuning(
                SoftInvertVisualTransform.TransformId));
        Assert.AreEqual(
            "Soft invert",
            VisualProfileCatalog.Default.GetTransformDisplayName(
                SoftInvertVisualTransform.TransformId));
    }

    [TestMethod]
    public void CatalogContainsSoftInvertTransform()
    {
        var transform =
            VisualProfileCatalog.Default
                .GetRequiredTransform(
                    SoftInvertVisualTransform.TransformId);

        Assert.IsInstanceOfType<
            SoftInvertVisualTransform>(transform);
    }

    [TestMethod]
    public void CatalogRejectsUnknownTransform()
    {
        Assert.ThrowsException<
            InvalidOperationException>(
            () => VisualProfileCatalog.Default
                .GetRequiredTransform("missing"));
    }

    [TestMethod]
    public void ResolverMatchesPathCaseInsensitively()
    {
        var settings =
            CreateSettingsWithAssignment(
                enabled: true);
        var identity = new ApplicationIdentity(
            "Reader",
            "reader.exe",
            "c:\\apps\\reader.exe");

        Assert.IsNotNull(
            ProfileResolver.FindEnabledAssignment(
                settings,
                identity));
    }

    [TestMethod]
    public void DisabledAssignmentDoesNotMatchAutomaticResolver()
    {
        var settings =
            CreateSettingsWithAssignment(
                enabled: false);

        Assert.IsNull(
            ProfileResolver.FindEnabledAssignment(
                settings,
                CreateIdentity()));
    }

    [TestMethod]
    public void DisabledAssignmentRemainsAvailableLocally()
    {
        var settings =
            CreateSettingsWithAssignment(
                enabled: false);

        var assignment =
            ProfileResolver.FindAssignment(
                settings,
                CreateIdentity());

        Assert.IsNotNull(assignment);
        Assert.AreEqual(
            VisualProfileCatalog.DefaultSoftInvertId,
            assignment.VisualProfileId);
    }

    [TestMethod]
    public void MissingProfileFallsBackToDefaultInvert()
    {
        var profile =
            ProfileResolver.ResolveVisualProfile(
                new SightAdaptSettings(),
                new ApplicationAssignment
                {
                    VisualProfileId =
                        "missing-profile",
                });

        Assert.AreEqual(
            VisualProfilePolicy
                .MissingReferenceFallbackProfileId,
            profile.Id);
    }

    [TestMethod]
    public void ProfileToggleCreatesEnabledAssignment()
    {
        var settings =
            new SightAdaptSettings();

        var result =
            ApplicationAssignmentService
                .Toggle(
                    settings,
                    CreateIdentity());

        Assert.IsTrue(result.WasCreated);
        Assert.IsTrue(result.IsEnabled);
        Assert.AreEqual(
            VisualProfilePolicy
                .NewAssignmentProfileId,
            result.Assignment.VisualProfileId);
    }

    [TestMethod]
    public void ProfileTogglePreservesCustomProfile()
    {
        var customProfile =
            VisualProfileCatalog.Default.CreateBuiltInProfile(VisualProfileCatalog.DefaultSoftInvertId);
        customProfile.Id = "custom-profile";
        customProfile.Name = "Custom profile";
        var settings =
            CreateSettingsWithAssignment(
                enabled: true);
        settings.VisualProfiles.Add(
            customProfile);
        settings.Assignments[0]
            .VisualProfileId =
            customProfile.Id;
        var identity = new ApplicationIdentity(
            "Reader updated",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");

        var disabled =
            ApplicationAssignmentService
                .Toggle(settings, identity);
        var enabled =
            ApplicationAssignmentService
                .Toggle(settings, identity);

        Assert.IsFalse(disabled.IsEnabled);
        Assert.IsTrue(enabled.IsEnabled);
        Assert.AreEqual(
            customProfile.Id,
            enabled.Assignment.VisualProfileId);
        Assert.AreEqual(
            "Reader updated",
            enabled.Assignment.DisplayName);
    }

    [TestMethod]
    public void ProfileToggleRepairsMissingReference()
    {
        var settings =
            CreateSettingsWithAssignment(
                enabled: true);
        settings.Assignments[0]
            .VisualProfileId =
            "missing-profile";

        var result =
            ApplicationAssignmentService
                .Toggle(
                    settings,
                    CreateIdentity());

        Assert.IsFalse(result.IsEnabled);
        Assert.AreEqual(
            VisualProfilePolicy
                .MissingReferenceFallbackProfileId,
            result.Assignment.VisualProfileId);
    }

    private static SightAdaptSettings
        CreateSettingsWithAssignment(bool enabled)
    {
        return new SightAdaptSettings
        {
            Assignments =
            [
                new ApplicationAssignment
                {
                    DisplayName = "Reader",
                    ExecutableName =
                        "Reader.exe",
                    ExecutablePath =
                        "C:\\Apps\\Reader.exe",
                    Enabled = enabled,
                    VisualProfileId =
                        VisualProfileCatalog
                            .DefaultSoftInvertId,
                },
            ],
        };
    }

    private static ApplicationIdentity
        CreateIdentity()
    {
        return new ApplicationIdentity(
            "Reader",
            "Reader.exe",
            "C:\\Apps\\Reader.exe");
    }
}
