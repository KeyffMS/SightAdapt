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

Replace-Exact 'tests/SightAdapt.Tests/ProfilePreviewCacheTests.cs' `
    'using Microsoft.VisualStudio.TestTools.UnitTesting;' `
    "using System.Drawing;`nusing System.Windows.Forms;`nusing Microsoft.VisualStudio.TestTools.UnitTesting;"

Replace-Exact 'src/SightAdapt/ProductInfo.cs' @'
    public static string VersionLabel { get; } =
        Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ??
        Assembly.GetName().Version?.ToString() ??
        string.Empty;
'@ @'
    private static readonly string FullVersion =
        GetVersion();

    public static string VersionLabel { get; } =
        CreateVersionLabel(FullVersion);
'@

Replace-Exact 'src/SightAdapt/ProductInfo.cs' @'
    private static string GetAttribute<TAttribute>(
'@ @'
    internal static string CreateVersionLabel(
        string? version)
    {
        var normalized =
            (version ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "Unknown";
        }

        var metadataIndex = normalized.IndexOf('+');
        var display = metadataIndex >= 0
            ? normalized[..metadataIndex]
            : normalized;
        return string.IsNullOrWhiteSpace(display)
            ? "Unknown"
            : display.Trim();
    }

    private static string GetVersion()
    {
        var informational = Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informational))
        {
            return informational.Trim();
        }

        var fileVersion = Assembly
            .GetCustomAttribute<AssemblyFileVersionAttribute>()
            ?.Version;
        if (!string.IsNullOrWhiteSpace(fileVersion))
        {
            return fileVersion.Trim();
        }

        return Assembly.GetName().Version?.ToString() ??
            "Unknown";
    }

    private static string GetAttribute<TAttribute>(
'@

Write-NewFile 'tests/SightAdapt.Tests/ProductInfoVersionTests.cs' @'
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProductInfoVersionTests
{
    [DataTestMethod]
    [DataRow("0.5.0.22+abcdef", "0.5.0.22")]
    [DataRow("0.5.0.22", "0.5.0.22")]
    [DataRow(" 0.5.0.22+abcdef ", "0.5.0.22")]
    [DataRow("", "Unknown")]
    public void DisplayVersionOmitsBuildMetadata(
        string input,
        string expected)
    {
        Assert.AreEqual(
            expected,
            ProductInfo.CreateVersionLabel(input));
    }

    [TestMethod]
    public void RuntimeVersionLabelIsVisibleAndCompact()
    {
        Assert.IsFalse(
            string.IsNullOrWhiteSpace(
                ProductInfo.VersionLabel));
        Assert.IsFalse(
            ProductInfo.VersionLabel.Contains(
                '+',
                StringComparison.Ordinal));
    }
}
'@
