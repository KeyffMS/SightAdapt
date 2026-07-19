from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]

def run(*args):
    return subprocess.run(args, cwd=ROOT, check=True, text=True, capture_output=True).stdout.strip()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8")

def write(path, content):
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content, encoding="utf-8", newline="\n")

def replace(path, old, new):
    content = read(path)
    if old not in content:
        raise RuntimeError(f"Missing replacement anchor in {path}: {old[:100]!r}")
    write(path, content.replace(old, new, 1))

run("git", "config", "user.name", "SightAdapt architecture automation")
run("git", "config", "user.email", "actions@users.noreply.github.com")

write("src/SightAdapt.Demo/ApplicationProfileManagementService.cs", """namespace SightAdapt.Demo;

internal sealed record ApplicationProfileToggleResult(
    ApplicationProfile Profile,
    bool WasCreated,
    bool IsEnabled);

internal static class ApplicationProfileManagementService
{
    public static ApplicationProfileToggleResult AddOrEnable(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);
        EnsureCollections(settings);

        var profile = ProfileResolver.FindAssignment(settings, identity);
        var wasCreated = profile is null;

        if (profile is null)
        {
            profile = new ApplicationProfile
            {
                VisualProfileId = VisualProfilePolicy.NewAssignmentProfileId,
            };
            settings.Applications.Add(profile);
        }

        UpdateIdentity(profile, identity);
        profile.Enabled = true;
        profile.LegacyEffect = null;
        EnsureValidProfileReference(settings, profile);

        return new ApplicationProfileToggleResult(profile, wasCreated, profile.Enabled);
    }

    public static ApplicationProfileToggleResult Toggle(
        SightAdaptSettings settings,
        ApplicationIdentity identity)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(identity);
        EnsureCollections(settings);

        var profile = ProfileResolver.FindAssignment(settings, identity);
        var wasCreated = profile is null;

        if (profile is null)
        {
            profile = new ApplicationProfile
            {
                Enabled = true,
                VisualProfileId = VisualProfilePolicy.NewAssignmentProfileId,
            };
            settings.Applications.Add(profile);
        }
        else
        {
            profile.Enabled = !profile.Enabled;
        }

        UpdateIdentity(profile, identity);
        profile.LegacyEffect = null;
        EnsureValidProfileReference(settings, profile);

        return new ApplicationProfileToggleResult(profile, wasCreated, profile.Enabled);
    }

    public static void SetEnabled(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        bool enabled)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureCollections(settings);
        EnsureMember(settings, profile);
        profile.Enabled = enabled;
    }

    public static void AssignVisualProfile(
        SightAdaptSettings settings,
        ApplicationProfile profile,
        string visualProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureCollections(settings);
        EnsureMember(settings, profile);

        var visualProfile = ProfileResolver.FindVisualProfile(settings, visualProfileId)
            ?? throw new InvalidOperationException(
                $"The visual profile '{visualProfileId}' does not exist.");

        profile.VisualProfileId = visualProfile.Id;
        profile.LegacyEffect = null;
    }

    public static void Remove(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        EnsureCollections(settings);
        EnsureMember(settings, profile);
        settings.Applications.Remove(profile);
    }

    public static int ReassignVisualProfile(
        SightAdaptSettings settings,
        string sourceProfileId,
        string targetProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        EnsureCollections(settings);

        var target = ProfileResolver.FindVisualProfile(settings, targetProfileId)
            ?? throw new InvalidOperationException(
                $"The fallback visual profile '{targetProfileId}' does not exist.");

        var assignments = settings.Applications
            .Where(assignment => assignment is not null && string.Equals(
                assignment.VisualProfileId,
                sourceProfileId,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var assignment in assignments)
        {
            assignment.VisualProfileId = target.Id;
            assignment.LegacyEffect = null;
        }

        return assignments.Length;
    }

    public static int CountAssignments(
        SightAdaptSettings settings,
        string visualProfileId)
    {
        ArgumentNullException.ThrowIfNull(settings);
        EnsureCollections(settings);

        return settings.Applications.Count(assignment =>
            assignment is not null && string.Equals(
                assignment.VisualProfileId,
                visualProfileId,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void EnsureCollections(SightAdaptSettings settings)
    {
        settings.Applications ??= [];
        settings.VisualProfiles ??= [];
    }

    private static void EnsureMember(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        if (!settings.Applications.Contains(profile))
        {
            throw new InvalidOperationException(
                "The application assignment is not part of the current settings.");
        }
    }

    private static void EnsureValidProfileReference(
        SightAdaptSettings settings,
        ApplicationProfile profile)
    {
        if (ProfileResolver.FindVisualProfile(settings, profile.VisualProfileId) is null)
        {
            profile.VisualProfileId = VisualProfilePolicy.NewAssignmentProfileId;
        }
    }

    private static void UpdateIdentity(
        ApplicationProfile profile,
        ApplicationIdentity identity)
    {
        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
    }
}
""")

old = ROOT / "src/SightAdapt.Demo/ApplicationProfileToggleService.cs"
if old.exists():
    old.unlink()

replace("src/SightAdapt.Demo/VisualProfileManagementService.cs", """        var assignments = settings.Applications
            .Where(assignment => assignment is not null && string.Equals(
                assignment.VisualProfileId,
                profile.Id,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        foreach (var assignment in assignments)
        {
            assignment.VisualProfileId = fallback.Id;
        }

        settings.VisualProfiles.Remove(profile);
        return assignments.Length;""", """        var reassigned = ApplicationProfileManagementService.ReassignVisualProfile(
            settings,
            profile.Id,
            fallback.Id);

        settings.VisualProfiles.Remove(profile);
        return reassigned;""")

replace("src/SightAdapt.Demo/VisualProfileManagementService.cs", """        return settings.Applications.Count(assignment =>
            assignment is not null && string.Equals(
                assignment.VisualProfileId,
                profile.Id,
                StringComparison.OrdinalIgnoreCase));""", """        return ApplicationProfileManagementService.CountAssignments(
            settings,
            profile.Id);""")

replace("src/SightAdapt.Demo/ConfigurationForm.cs", """        if (columnName == EnabledColumnName)
        {
            profile.Enabled = row.Cells[eventArgs.ColumnIndex].Value is true;
        }
        else if (columnName == VisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string visualProfileId &&
                 _settings.VisualProfiles.Any(candidate => string.Equals(
                     candidate.Id,
                     visualProfileId,
                     StringComparison.OrdinalIgnoreCase)))
        {
            profile.VisualProfileId = visualProfileId;
        }
        else
        {
            return;
        }""", """        if (columnName == EnabledColumnName)
        {
            ApplicationProfileManagementService.SetEnabled(
                _settings,
                profile,
                row.Cells[eventArgs.ColumnIndex].Value is true);
        }
        else if (columnName == VisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string visualProfileId)
        {
            ApplicationProfileManagementService.AssignVisualProfile(
                _settings,
                profile,
                visualProfileId);
        }
        else
        {
            return;
        }""")

replace("src/SightAdapt.Demo/ConfigurationForm.cs", """        var profile = _settings.Applications.FirstOrDefault(candidate => candidate.Matches(identity));
        var added = profile is null;

        if (profile is null)
        {
            profile = new ApplicationProfile
            {
                VisualProfileId = VisualProfile.DefaultSoftInvertId,
            };
            _settings.Applications.Add(profile);
        }

        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
        profile.Enabled = true;
        profile.LegacyEffect = null;
        _settings.AutomaticMode = true;""", """        var result = ApplicationProfileManagementService.AddOrEnable(
            _settings,
            identity);
        var added = result.WasCreated;
        _settings.AutomaticMode = true;""")

replace("src/SightAdapt.Demo/ConfigurationForm.cs", """        _settings.Applications.Remove(profile);
        _settingsChanged();""", """        ApplicationProfileManagementService.Remove(_settings, profile);
        _settingsChanged();""")

replace("src/SightAdapt.Demo/SightAdaptContext.cs",
        "ApplicationProfileToggleService.Toggle(_settings, identity)",
        "ApplicationProfileManagementService.Toggle(_settings, identity)")

write("tests/SightAdapt.Tests/ApplicationProfileManagementTests.cs", """using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class ApplicationProfileManagementTests
{
    [TestMethod]
    public void AddOrEnableCreatesDefaultSoftInvertAssignment()
    {
        var settings = new SightAdaptSettings();
        var identity = CreateIdentity("Reader");

        var result = ApplicationProfileManagementService.AddOrEnable(settings, identity);

        Assert.IsTrue(result.WasCreated);
        Assert.IsTrue(result.IsEnabled);
        Assert.AreEqual(VisualProfilePolicy.NewAssignmentProfileId, result.Profile.VisualProfileId);
        Assert.AreEqual(1, settings.Applications.Count);
    }

    [TestMethod]
    public void AssignVisualProfileRequiresExistingProfile()
    {
        var settings = new SightAdaptSettings();
        var assignment = ApplicationProfileManagementService
            .AddOrEnable(settings, CreateIdentity("Reader"))
            .Profile;
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");

        ApplicationProfileManagementService.AssignVisualProfile(
            settings,
            assignment,
            custom.Id);

        Assert.AreEqual(custom.Id, assignment.VisualProfileId);
        Assert.ThrowsException<InvalidOperationException>(() =>
            ApplicationProfileManagementService.AssignVisualProfile(
                settings,
                assignment,
                "missing-profile"));
    }

    [TestMethod]
    public void TogglePreservesValidCustomProfile()
    {
        var settings = new SightAdaptSettings();
        var identity = CreateIdentity("Reader");
        var assignment = ApplicationProfileManagementService
            .AddOrEnable(settings, identity)
            .Profile;
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");
        ApplicationProfileManagementService.AssignVisualProfile(settings, assignment, custom.Id);

        var disabled = ApplicationProfileManagementService.Toggle(settings, identity);
        var enabled = ApplicationProfileManagementService.Toggle(settings, identity);

        Assert.IsFalse(disabled.IsEnabled);
        Assert.IsTrue(enabled.IsEnabled);
        Assert.AreEqual(custom.Id, enabled.Profile.VisualProfileId);
    }

    [TestMethod]
    public void DetachedAssignmentCannotBeMutatedOrRemoved()
    {
        var settings = new SightAdaptSettings();
        var detached = new ApplicationProfile();

        Assert.ThrowsException<InvalidOperationException>(() =>
            ApplicationProfileManagementService.SetEnabled(settings, detached, false));
        Assert.ThrowsException<InvalidOperationException>(() =>
            ApplicationProfileManagementService.Remove(settings, detached));
    }

    [TestMethod]
    public void ReassignVisualProfileUpdatesAllMatchingAssignments()
    {
        var settings = new SightAdaptSettings();
        var custom = VisualProfileManagementService.Create(settings, "Reader colors");
        var first = ApplicationProfileManagementService
            .AddOrEnable(settings, CreateIdentity("Reader"))
            .Profile;
        var second = ApplicationProfileManagementService
            .AddOrEnable(settings, CreateIdentity("Notes"))
            .Profile;
        ApplicationProfileManagementService.AssignVisualProfile(settings, first, custom.Id);
        ApplicationProfileManagementService.AssignVisualProfile(settings, second, custom.Id);

        var changed = ApplicationProfileManagementService.ReassignVisualProfile(
            settings,
            custom.Id,
            VisualProfile.DefaultSoftInvertId);

        Assert.AreEqual(2, changed);
        Assert.IsTrue(settings.Applications.All(
            assignment => assignment.VisualProfileId == VisualProfile.DefaultSoftInvertId));
    }

    private static ApplicationIdentity CreateIdentity(string name)
    {
        return new ApplicationIdentity(
            name,
            $"{name}.exe",
            $"C:\\Apps\\{name}.exe");
    }
}
""")

Path(__file__).unlink()
run("git", "add", "-A")
run("git", "commit", "-m", "Implement application assignment authority [0.4A.3.001]")
run("git", "push", "origin", "HEAD:agent/alpha-v0.4")
