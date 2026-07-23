Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Write-ExistingFile(
    [string]$Path,
    [string]$ExpectedMarker,
    [string]$Content) {
    if (-not (Test-Path $Path)) {
        throw "File '$Path' does not exist."
    }

    $current = Normalize-Newlines (Get-Content -Raw $Path)
    if (-not $current.Contains((Normalize-Newlines $ExpectedMarker))) {
        throw "Expected marker was not found in '$Path'."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
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

Write-ExistingFile 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' `
    'public sealed class ArchitectureComplianceTests' @'
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ArchitectureComplianceTests
{
    [TestMethod]
    public void ApplicationAssignmentWritesStayInAuthorities()
    {
        // Intentional exhaustive source scan: a finite runtime scenario cannot
        // prove that no other top-level production file writes these collections.
        AssertPatternRestrictedTo(
            @"(?m)^(?!\s*string\?\s+VisualProfileId\s*=)\s*.*\bVisualProfileId\s*=",
            "ApplicationProfile.cs",
            "ApplicationProfileManagementService.cs",
            "SettingsStore.cs",
            "OverlayController.cs");
        AssertPatternRestrictedTo(
            @"\.Applications\.(Add|Remove)\(",
            "ApplicationProfileManagementService.cs",
            "SettingsStore.cs");
    }

    [TestMethod]
    public void AutomaticModeWritesStayInAuthority()
    {
        // Intentional exhaustive source scan for the single mutation authority.
        AssertPatternRestrictedTo(
            @"\.AutomaticMode\s*=",
            "AutomaticModeManagementService.cs");
    }

    [TestMethod]
    public void VisualProfileCollectionWritesStayInLifecycleAuthority()
    {
        // Intentional exhaustive source scan for collection ownership.
        AssertPatternRestrictedTo(
            @"\.VisualProfiles\.(Add|Remove)\(",
            "VisualProfileManagementService.cs");
    }

    [TestMethod]
    public void UiAndRuntimeDoNotPersistSettingsDirectly()
    {
        // This is a dependency-boundary check rather than an implementation-
        // spelling check: these components must use SettingsCoordinator.
        foreach (var fileName in new[]
                 {
                     "ConfigurationForm.cs",
                     "VisualProfileManagerForm.cs",
                     "SightAdaptContext.cs",
                     "RuntimeCoordinator.cs",
                 })
        {
            var source = ReadSource(fileName);
            Assert.IsFalse(
                source.Contains(
                    "SettingsStore",
                    StringComparison.Ordinal),
                $"{fileName} must not own settings persistence.");
        }
    }

    [TestMethod]
    public void ExpectedFailuresAreNotSilentlySwallowed()
    {
        // Intentional exhaustive scan: empty catch blocks have no observable
        // behavior and therefore cannot be covered reliably by runtime tests.
        var violations = Directory
            .EnumerateFiles(
                SourceDirectory,
                "*.cs",
                SearchOption.TopDirectoryOnly)
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                @"catch\s*(?:\([^)]*\))?\s*\{\s*\}",
                RegexOptions.CultureInvariant |
                RegexOptions.Singleline))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            $"Empty catch blocks found in: {string.Join(", ", violations)}");
    }

    [TestMethod]
    public void RemovedLegacyMutationServiceDoesNotReturn()
    {
        Assert.IsFalse(
            File.Exists(Path.Combine(
                SourceDirectory,
                "ApplicationProfileToggleService.cs")));
    }

    private static void AssertPatternRestrictedTo(
        string pattern,
        params string[] allowedFiles)
    {
        var allowed = allowedFiles.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        var violations = Directory
            .EnumerateFiles(
                SourceDirectory,
                "*.cs",
                SearchOption.TopDirectoryOnly)
            .Where(path => !allowed.Contains(
                Path.GetFileName(path)))
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                pattern,
                RegexOptions.CultureInvariant))
            .Select(Path.GetFileName)
            .OrderBy(name => name)
            .ToArray();

        Assert.AreEqual(
            0,
            violations.Length,
            $"Restricted mutation pattern '{pattern}' found in: " +
            string.Join(", ", violations));
    }

    private static string ReadSource(string fileName)
    {
        return File.ReadAllText(
            Path.Combine(SourceDirectory, fileName));
    }

    private static string SourceDirectory =>
        Path.Combine(
            RepositoryRoot,
            "src",
            "SightAdapt");

    private static string RepositoryRoot
    {
        get
        {
            var directory =
                new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(
                        directory.FullName,
                        "src",
                        "SightAdapt")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "The SightAdapt repository root could not be located.");
        }
    }
}
'@

$auditPath = 'tests/SightAdapt.Tests/ArchitectureAuditRegressionTests.cs'
if (-not (Test-Path $auditPath)) {
    throw "File '$auditPath' does not exist."
}
Remove-Item $auditPath

Write-NewFile 'tests/SightAdapt.Tests/ProductMetadataBehaviorTests.cs' @'
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProductMetadataBehaviorTests
{
    [TestMethod]
    public void AssemblyMetadataProducesCompleteProductInformation()
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.ProductName));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.VersionLabel));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.MilestoneLabel));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.Author));
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(ProductInfo.License));
        Assert.IsFalse(
            ProductInfo.VersionLabel.Contains(
                '+',
                StringComparison.Ordinal));
    }

    [TestMethod]
    public void RepositoryMetadataIsAnAbsoluteWebAddress()
    {
        Assert.IsTrue(Uri.TryCreate(
            ProductInfo.RepositoryUrl,
            UriKind.Absolute,
            out var repository));
        Assert.IsTrue(
            repository.Scheme is "http" or "https");
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(
                ProductInfo.RepositoryDisplay));
    }
}
'@

$architecturePath = 'docs/ARCHITECTURE.md'
$architecture = Normalize-Newlines (
    Get-Content -Raw $architecturePath)
$heading = '## Architecture test strategy'
if ($architecture.Contains($heading)) {
    throw "Architecture test strategy already exists."
}
$architecture += @'

## Architecture test strategy

Architecture checks are behavior-first. Transaction publication, failed persistence, emergency ordering, runtime state transitions, transform catalog consistency, overlay-scope recovery, grid commits, menu roles, preview caching, and profile-manager refresh behavior are exercised through executable tests.

Source inspection is retained only for exhaustive negative rules that cannot be proven by a finite runtime scenario:

- collection and property writes must remain inside their mutation authorities;
- UI and runtime components must not instantiate the persistence store;
- empty catch blocks are forbidden;
- removed legacy mutation services must not return.

These focused scans intentionally avoid asserting field names, statement ordering, formatting, RGB literals, or the exact internal spelling of valid implementations.
'@
[System.IO.File]::WriteAllText(
    $architecturePath,
    $architecture,
    $Utf8NoBom)
