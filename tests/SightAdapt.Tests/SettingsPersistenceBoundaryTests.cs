using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class SettingsPersistenceBoundaryTests
{
    [TestMethod]
    public void DomainModelsDoNotExposeSerializationOrLegacyMembers()
    {
        Assert.IsNull(typeof(ApplicationAssignment)
            .GetProperty("LegacyEffect"));
        Assert.IsNull(typeof(ApplicationAssignment)
            .GetProperty("Effect"));

        foreach (var type in new[]
                 {
                     typeof(SightAdaptSettings),
                     typeof(ApplicationAssignment),
                     typeof(VisualProfile),
                 })
        {
            Assert.IsFalse(type.GetProperties().Any(
                property => property
                    .GetCustomAttributes(inherit: true)
                    .Any(attribute =>
                        attribute is JsonPropertyNameAttribute or
                        JsonIgnoreAttribute)));
        }
    }

    [TestMethod]
    public void LegacyEffectIsHandledOnlyDuringMaterialization()
    {
        var persisted = new PersistedSightAdaptSettings
        {
            SchemaVersion = SightAdaptSettings.CurrentSchemaVersion,
            Applications =
            [
                new PersistedApplicationAssignment
                {
                    DisplayName = "Reader",
                    ExecutableName = "reader.exe",
                    ExecutablePath = @"C:\Apps\reader.exe",
                    LegacyEffect = "invert",
                },
            ],
        };

        var result = PersistedSettingsMapper.ToDomain(persisted);

        Assert.IsTrue(result.WasMigrated);
        Assert.AreEqual(
            VisualProfileCatalog.DefaultInvertId,
            result.Settings.Assignments.Single()
                .VisualProfileId);
    }

    [TestMethod]
    public void CurrentSerializationDoesNotEmitLegacyEffect()
    {
        var settings = new SightAdaptSettings
        {
            Assignments =
            [
                new ApplicationAssignment
                {
                    DisplayName = "Reader",
                    ExecutableName = "reader.exe",
                    ExecutablePath = @"C:\Apps\reader.exe",
                },
            ],
        };
        var persisted = PersistedSettingsMapper.FromDomain(
            settings);
        var json = JsonSerializer.Serialize(
            persisted,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy =
                    JsonNamingPolicy.CamelCase,
            });

        Assert.IsFalse(json.Contains(
            "\"effect\"",
            StringComparison.OrdinalIgnoreCase));
    }
}
