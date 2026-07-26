using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class MenuVisualProfileTests
{
    [TestMethod]
    public void UnsetMenuProfileInheritsApplicationAssignment()
    {
        var settings = new SightAdaptSettings();
        var assignment = AddAssignment(settings);
        var primary = VisualProfileManagementService.Create(
            settings,
            "Reader primary");
        ApplicationAssignmentService
            .AssignVisualProfile(
                settings,
                assignment,
                primary.Id);

        var resolved =
            ProfileResolver.ResolveMenuVisualProfile(
                settings,
                assignment);

        Assert.AreSame(primary, resolved);
    }

    [TestMethod]
    public void ExplicitMenuProfileOverridesApplicationAssignment()
    {
        var settings = new SightAdaptSettings();
        var assignment = AddAssignment(settings);
        var primary = VisualProfileManagementService.Create(
            settings,
            "Reader primary");
        var menu = VisualProfileManagementService.Create(
            settings,
            "Reader menus");
        ApplicationAssignmentService
            .AssignVisualProfile(
                settings,
                assignment,
                primary.Id);
        ApplicationAssignmentService
            .AssignMenuVisualProfile(
                settings,
                assignment,
                menu.Id);

        var resolved =
            ProfileResolver.ResolveMenuVisualProfile(
                settings,
                assignment);

        Assert.AreSame(menu, resolved);
    }

    [TestMethod]
    public void MenuProfileCanReturnToInheritance()
    {
        var settings = new SightAdaptSettings();
        var assignment = AddAssignment(settings);
        var menu = VisualProfileManagementService.Create(
            settings,
            "Reader menus");
        ApplicationAssignmentService
            .AssignMenuVisualProfile(
                settings,
                assignment,
                menu.Id);

        ApplicationAssignmentService
            .AssignMenuVisualProfile(
                settings,
                assignment,
                null);

        Assert.IsNull(assignment.MenuVisualProfileId);
    }

    [TestMethod]
    public void MissingMenuProfileCannotBeAssigned()
    {
        var settings = new SightAdaptSettings();
        var assignment = AddAssignment(settings);

        Assert.ThrowsException<
            SettingsValidationException>(() =>
                ApplicationAssignmentService
                    .AssignMenuVisualProfile(
                        settings,
                        assignment,
                        "missing-profile"));
    }

    [TestMethod]
    public void ReassignmentUpdatesPrimaryAndMenuReferenceOnce()
    {
        var settings = new SightAdaptSettings();
        var assignment = AddAssignment(settings);
        var custom = VisualProfileManagementService.Create(
            settings,
            "Reader shared");
        ApplicationAssignmentService
            .AssignVisualProfile(
                settings,
                assignment,
                custom.Id);
        ApplicationAssignmentService
            .AssignMenuVisualProfile(
                settings,
                assignment,
                custom.Id);

        var changed =
            ApplicationAssignmentService
                .ReassignVisualProfile(
                    settings,
                    custom.Id,
                    VisualProfileCatalog.DefaultSoftInvertId);

        Assert.AreEqual(1, changed);
        Assert.AreEqual(
            VisualProfileCatalog.DefaultSoftInvertId,
            assignment.VisualProfileId);
        Assert.AreEqual(
            VisualProfileCatalog.DefaultSoftInvertId,
            assignment.MenuVisualProfileId);
    }

    [TestMethod]
    public void MissingMenuReferenceRepairsToInheritance()
    {
        var settings = new SightAdaptSettings
        {
            Assignments =
            [
                new ApplicationAssignment
                {
                    DisplayName = "Reader",
                    ExecutableName = "reader.exe",
                    ExecutablePath =
                        "C:\\Apps\\reader.exe",
                    VisualProfileId =
                        VisualProfileCatalog.DefaultSoftInvertId,
                    MenuVisualProfileId =
                        "missing-profile",
                },
            ],
        };

        var changed = SettingsStore.Normalize(settings);

        Assert.IsTrue(changed);
        Assert.IsNull(
            settings.Assignments[0]
                .MenuVisualProfileId);
    }

    [TestMethod]
    public void ExplicitMenuProfileRoundTripsThroughSettingsStore()
    {
        using var temporaryDirectory =
            new TemporaryDirectory();
        var settingsPath = Path.Combine(
            temporaryDirectory.Path,
            "settings.json");
        var store = new SettingsStore(settingsPath);
        var settings = new SightAdaptSettings();
        var assignment = AddAssignment(settings);
        var menu = VisualProfileManagementService.Create(
            settings,
            "Reader menus");
        ApplicationAssignmentService
            .AssignMenuVisualProfile(
                settings,
                assignment,
                menu.Id);

        store.Save(settings);
        var json = File.ReadAllText(settingsPath);
        var reloaded = store.Load();

        StringAssert.Contains(
            json,
            $"\"menuVisualProfileId\": \"{menu.Id}\"");
        Assert.AreEqual(
            menu.Id,
            reloaded.Assignments[0]
                .MenuVisualProfileId);
    }

    [TestMethod]
    public void MenuSelectorDataErrorsUseSelectorRecoveryPolicy()
    {
        Assert.IsTrue(
            ApplicationAssignmentsGrid
                .IsExpectedSelectorDataError(
                    new ArgumentException(),
                    DataGridViewDataErrorContexts.Formatting,
                    ApplicationAssignmentsGrid
                        .MenuVisualProfileColumnName));
    }

    [TestMethod]
    public void SchemaFourSettingsGainMenuProfileInheritance()
    {
        using var temporaryDirectory =
            new TemporaryDirectory();
        var settingsPath = Path.Combine(
            temporaryDirectory.Path,
            "settings.json");
        File.WriteAllText(
            settingsPath,
            """
            {
              "schemaVersion": 4,
              "applications": [
                {
                  "displayName": "Reader",
                  "executableName": "reader.exe",
                  "executablePath": "C:\\Apps\\reader.exe",
                  "visualProfileId": "default-soft-invert"
                }
              ]
            }
            """);

        var store = new SettingsStore(settingsPath);
        var settings = store.Load();

        Assert.IsTrue(store.SettingsWereMigrated);
        Assert.AreEqual(
            SightAdaptSettings.CurrentSchemaVersion,
            settings.SchemaVersion);
        Assert.IsNull(
            settings.Assignments[0]
                .MenuVisualProfileId);
    }

    [TestMethod]
    public void SelectorPolicyRoundTripsInheritance()
    {
        Assert.AreEqual(
            ApplicationMenuProfilePolicy
                .InheritSelectorId,
            ApplicationMenuProfilePolicy
                .ToSelectorId(null));
        Assert.IsNull(
            ApplicationMenuProfilePolicy
                .FromSelectorId(
                    ApplicationMenuProfilePolicy
                        .InheritSelectorId));
        Assert.AreEqual(
            "user-menu",
            ApplicationMenuProfilePolicy
                .FromSelectorId(" user-menu "));
    }

    private static ApplicationAssignment AddAssignment(
        SightAdaptSettings settings)
    {
        return ApplicationAssignmentService
            .AddOrEnable(
                settings,
                new ApplicationIdentity(
                    "Reader",
                    "reader.exe",
                    "C:\\Apps\\reader.exe"))
            .Assignment;
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "SightAdapt.Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, true);
            }
        }
    }
}
