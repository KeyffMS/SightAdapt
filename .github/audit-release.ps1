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

Replace-Exact 'src/SightAdapt/VisualTransforms.cs' @'
    private readonly IReadOnlyDictionary<
        string,
        VisualTransformDefinition> _definitions;

    public VisualTransformCatalog(
        IEnumerable<VisualTransformDefinition>? definitions = null)
    {
        var available =
            definitions?.ToArray() ?? CanonicalDefinitions;

        _definitions = available.ToDictionary(
            definition => definition.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    public VisualTransformCatalog(
        IEnumerable<IVisualTransform> transforms)
        : this(transforms?.Select(CreateDefinition) ??
               throw new ArgumentNullException(nameof(transforms)))
    {
    }
'@ @'
    private VisualTransformCatalog()
    {
    }
'@

Replace-Exact 'src/SightAdapt/VisualTransforms.cs' `
    '    public static bool IsSupported(string? transformId)' `
    '    public bool IsSupported(string? transformId)'
Replace-Exact 'src/SightAdapt/VisualTransforms.cs' `
    '    public static bool SupportsTuning(string? transformId)' `
    '    public bool SupportsTuning(string? transformId)'
Replace-Exact 'src/SightAdapt/VisualTransforms.cs' `
    '    public static string GetDisplayName(string? transformId)' `
    '    public string GetDisplayName(string? transformId)'
Replace-Exact 'src/SightAdapt/VisualTransforms.cs' `
    'TryGetCanonicalDefinition' `
    'TryGetDefinition' `
    4
Replace-Exact 'src/SightAdapt/VisualTransforms.cs' `
    '_definitions.TryGetValue' `
    'DefinitionsById.TryGetValue'
Replace-Exact 'src/SightAdapt/VisualTransforms.cs' @'

    private static VisualTransformDefinition CreateDefinition(
        IVisualTransform transform)
    {
        ArgumentNullException.ThrowIfNull(transform);

        return new VisualTransformDefinition(
            transform.Id,
            GetDisplayName(transform.Id),
            SupportsTuning(transform.Id),
            transform);
    }
'@ "`n"

Replace-Exact 'src/SightAdapt/ApplicationProfile.cs' `
    'VisualTransformCatalog.SupportsTuning(TransformId)' `
    'VisualTransformCatalog.Default.SupportsTuning(TransformId)'
Replace-Exact 'src/SightAdapt/VisualProfilePolicy.cs' `
    'VisualTransformCatalog.IsSupported(' `
    'VisualTransformCatalog.Default.IsSupported('
Replace-Exact 'src/SightAdapt/VisualProfileManagerForm.cs' `
    'VisualTransformCatalog.GetDisplayName(profile.TransformId)' `
    'VisualTransformCatalog.Default.GetDisplayName(profile.TransformId)'
Replace-Exact 'tests/SightAdapt.Tests/VisualProfileTests.cs' `
    'VisualTransformCatalog.IsSupported(' `
    'VisualTransformCatalog.Default.IsSupported(' `
    3
Replace-Exact 'tests/SightAdapt.Tests/VisualProfileTests.cs' `
    'VisualTransformCatalog.SupportsTuning(' `
    'VisualTransformCatalog.Default.SupportsTuning(' `
    2
Replace-Exact 'tests/SightAdapt.Tests/VisualProfileTests.cs' `
    'VisualTransformCatalog.GetDisplayName(' `
    'VisualTransformCatalog.Default.GetDisplayName('
Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' `
    'StringAssert.Contains(model, "VisualTransformCatalog.SupportsTuning");' `
    'StringAssert.Contains(model, "VisualTransformCatalog.Default.SupportsTuning");'
Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' `
    'StringAssert.Contains(policy, "VisualTransformCatalog.IsSupported");' `
    'StringAssert.Contains(policy, "VisualTransformCatalog.Default.IsSupported");'

Write-NewFile 'tests/SightAdapt.Tests/VisualTransformCatalogTests.cs' @'
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualTransformCatalogTests
{
    [TestMethod]
    public void BuiltInMetadataAndResolutionUseTheSameRegistry()
    {
        var catalog = VisualTransformCatalog.Default;

        Assert.IsTrue(catalog.IsSupported(InvertVisualTransform.TransformId));
        Assert.IsFalse(catalog.SupportsTuning(InvertVisualTransform.TransformId));
        Assert.AreEqual(
            VisualProfileDefaults.ExactInvertName,
            catalog.GetDisplayName(InvertVisualTransform.TransformId));
        Assert.AreEqual(
            InvertVisualTransform.TransformId,
            catalog.GetRequired(InvertVisualTransform.TransformId).Id);

        Assert.IsTrue(catalog.IsSupported(SoftInvertVisualTransform.TransformId));
        Assert.IsTrue(catalog.SupportsTuning(SoftInvertVisualTransform.TransformId));
        Assert.AreEqual(
            VisualProfileDefaults.SoftInvertName,
            catalog.GetDisplayName(SoftInvertVisualTransform.TransformId));
        Assert.AreEqual(
            SoftInvertVisualTransform.TransformId,
            catalog.GetRequired(SoftInvertVisualTransform.TransformId).Id);
    }

    [TestMethod]
    public void UnknownTransformIsRejectedConsistently()
    {
        var catalog = VisualTransformCatalog.Default;

        Assert.IsFalse(catalog.IsSupported("custom-transform"));
        Assert.IsFalse(catalog.SupportsTuning("custom-transform"));
        Assert.AreEqual(
            "custom-transform",
            catalog.GetDisplayName(" custom-transform "));
        Assert.ThrowsException<InvalidOperationException>(() =>
            catalog.GetRequired("custom-transform"));
    }
}
'@
