Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

function Write-NewFile([string]$Path, [string]$Content) {
    if (Test-Path $Path) {
        throw "File '$Path' already exists."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

Replace-Exact 'src/SightAdapt/ProfileResolver.cs' @'
    public static ApplicationProfile? FindEnabledAssignment(
'@ @'
    public static ApplicationProfile? FindAssignmentByExecutablePath(
        SightAdaptSettings settings,
        string? executablePath)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        var normalizedPath = executablePath.Trim();
        return settings.Applications?.FirstOrDefault(profile =>
            profile is not null && string.Equals(
                profile.ExecutablePath,
                normalizedPath,
                StringComparison.OrdinalIgnoreCase));
    }

    public static ApplicationProfile RequireAssignmentByExecutablePath(
        SightAdaptSettings settings,
        string executablePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        return FindAssignmentByExecutablePath(
                settings,
                executablePath) ??
            throw new InvalidOperationException(
                "The selected application assignment no longer exists.");
    }

    public static ApplicationProfile? FindEnabledAssignment(
'@

Replace-Exact 'src/SightAdapt/ProfileResolver.cs' @'
    public static VisualProfile ResolveVisualProfile(
'@ @'
    public static VisualProfile RequireVisualProfile(
        SightAdaptSettings settings,
        string profileId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(profileId);
        return FindVisualProfile(settings, profileId) ??
            throw new InvalidOperationException(
                "The selected visual profile no longer exists.");
    }

    public static string ResolveVisualProfileName(
        SightAdaptSettings settings,
        string? profileId,
        string fallback)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);

        var name = FindVisualProfile(settings, profileId)?.Name;
        return string.IsNullOrWhiteSpace(name)
            ? fallback
            : name;
    }

    public static VisualProfile ResolveVisualProfile(
'@

Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' @'
        return Settings.Applications.FirstOrDefault(profile =>
            string.Equals(
                profile.ExecutablePath,
                executablePath,
                StringComparison.OrdinalIgnoreCase));
'@ @'
        return ProfileResolver.FindAssignmentByExecutablePath(
            Settings,
            executablePath);
'@

Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' `
    'FindAssignment(Settings, executablePath)' `
    'ProfileResolver.RequireAssignmentByExecutablePath(Settings, executablePath)'
Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' `
    'FindAssignment(settings, executablePath)' `
    'ProfileResolver.RequireAssignmentByExecutablePath(settings, executablePath)' `
    1
Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' `
    'FindAssignment(settings, path)' `
    'ProfileResolver.RequireAssignmentByExecutablePath(settings, path)'

Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' @'
                ProfileResolver.FindVisualProfile(settings, profileId) ??
                    throw new InvalidOperationException("The selected visual profile no longer exists."),
'@ @'
                ProfileResolver.RequireVisualProfile(
                    settings,
                    profileId),
'@

Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' @'

    private static ApplicationProfile FindAssignment(SightAdaptSettings settings, string executablePath)
    {
        return settings.Applications.FirstOrDefault(profile => string.Equals(
                profile.ExecutablePath,
                executablePath,
                StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidOperationException("The selected application assignment no longer exists.");
    }
'@ "`n"

Replace-Exact 'src/SightAdapt/VisualProfileManagerForm.cs' `
    'FindProfile(settings,' `
    'ProfileResolver.RequireVisualProfile(settings,' `
    4
Replace-Exact 'src/SightAdapt/VisualProfileManagerForm.cs' @'

    private static VisualProfile FindProfile(SightAdaptSettings settings, string profileId)
    {
        return ProfileResolver.FindVisualProfile(settings, profileId) ??
            throw new InvalidOperationException("The selected visual profile no longer exists.");
    }
'@ "`n"

Replace-Exact 'src/SightAdapt/SightAdaptContext.cs' @'
        var profileName = ResolveProfileName(
            state.VisualProfileId);
'@ @'
        var profileName = ProfileResolver.ResolveVisualProfileName(
            Settings,
            state.VisualProfileId,
            "Visual correction");
'@
Replace-Exact 'src/SightAdapt/SightAdaptContext.cs' @'

    private string ResolveProfileName(
        string? profileId)
    {
        return Settings.VisualProfiles
            .FirstOrDefault(profile => string.Equals(
                profile.Id,
                profileId,
                StringComparison.OrdinalIgnoreCase))
            ?.Name ?? "Visual correction";
    }
'@ "`n"

Write-NewFile 'tests/SightAdapt.Tests/ProfileResolverLookupTests.cs' @'
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
        Assert.ThrowsException<InvalidOperationException>(() =>
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
        Assert.ThrowsException<InvalidOperationException>(() =>
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
'@
