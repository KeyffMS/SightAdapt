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

Write-ExistingFile 'src/SightAdapt/OverlayScope.cs' 'private static readonly OverlayScope[] SupportedScopes' @'
namespace SightAdapt;

internal enum OverlayScope
{
    ClientArea = 0,
    Window = 1,
    Screen = 2,
    AllScreens = 3,
}

internal sealed record OverlayScopeDefinition
{
    public OverlayScopeDefinition(
        OverlayScope scope,
        string id,
        string displayName,
        params string[] aliases)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException(
                "A canonical overlay-scope identifier is required.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException(
                "An overlay-scope display name is required.",
                nameof(displayName));
        }

        Scope = scope;
        Id = id;
        DisplayName = displayName;
        Identifiers =
        [
            id,
            .. aliases
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Select(alias => alias.Trim()),
        ];
    }

    public OverlayScope Scope { get; }

    public string Id { get; }

    public string DisplayName { get; }

    public IReadOnlyList<string> Identifiers { get; }
}

internal static class OverlayScopePolicy
{
    public const OverlayScope Default = OverlayScope.ClientArea;

    private static readonly OverlayScopeDefinition[] CanonicalDefinitions =
    [
        new(
            OverlayScope.ClientArea,
            "client-area",
            "Client area",
            "client"),
        new(
            OverlayScope.Window,
            "window",
            "Full window",
            "full-window"),
        new(
            OverlayScope.Screen,
            "screen",
            "Current screen",
            "current-screen"),
        new(
            OverlayScope.AllScreens,
            "all-screens",
            "All screens",
            "virtual-screen"),
    ];

    private static readonly IReadOnlyDictionary<
        OverlayScope,
        OverlayScopeDefinition> DefinitionsByScope =
        CanonicalDefinitions.ToDictionary(
            definition => definition.Scope);

    private static readonly IReadOnlyDictionary<
        string,
        OverlayScopeDefinition> DefinitionsByIdentifier =
        CanonicalDefinitions
            .SelectMany(definition =>
                definition.Identifiers.Select(identifier =>
                    new KeyValuePair<string, OverlayScopeDefinition>(
                        identifier,
                        definition)))
            .ToDictionary(
                pair => pair.Key,
                pair => pair.Value,
                StringComparer.OrdinalIgnoreCase);

    private static readonly IReadOnlyList<OverlayScope> SupportedScopes =
        CanonicalDefinitions
            .Select(definition => definition.Scope)
            .ToArray();

    internal static IReadOnlyList<OverlayScopeDefinition> Definitions =>
        CanonicalDefinitions;

    public static IReadOnlyList<OverlayScope> All =>
        SupportedScopes;

    public static bool IsSupported(OverlayScope scope)
    {
        return DefinitionsByScope.ContainsKey(scope);
    }

    public static string ToId(OverlayScope scope)
    {
        return GetRequiredDefinition(scope).Id;
    }

    public static bool TryParseId(
        string? value,
        out OverlayScope scope)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (DefinitionsByIdentifier.TryGetValue(
                normalized,
                out var definition))
        {
            scope = definition.Scope;
            return true;
        }

        scope = Default;
        return false;
    }

    public static OverlayScope ParseRequired(string value)
    {
        if (TryParseId(value, out var scope))
        {
            return scope;
        }

        throw new ArgumentException(
            $"The overlay scope '{value}' is not supported.",
            nameof(value));
    }

    public static string GetDisplayName(OverlayScope scope)
    {
        return GetRequiredDefinition(scope).DisplayName;
    }

    private static OverlayScopeDefinition GetRequiredDefinition(
        OverlayScope scope)
    {
        return DefinitionsByScope.TryGetValue(
                scope,
                out var definition)
            ? definition
            : throw new ArgumentOutOfRangeException(
                nameof(scope),
                scope,
                "The overlay scope is not supported.");
    }
}
'@

Replace-Exact 'tests/SightAdapt.Tests/OverlayScopeTests.cs' @'
    [TestMethod]
    public void ScopeIdentifiersRoundTrip()
    {
        foreach (var scope in OverlayScopePolicy.All)
        {
            var id = OverlayScopePolicy.ToId(scope);
            Assert.IsTrue(OverlayScopePolicy.TryParseId(id, out var parsed));
            Assert.AreEqual(scope, parsed);
        }
    }
'@ @'
    [TestMethod]
    public void ScopeMetadataRoundTripsEveryIdentifier()
    {
        CollectionAssert.AreEqual(
            OverlayScopePolicy.Definitions
                .Select(definition => definition.Scope)
                .ToArray(),
            OverlayScopePolicy.All.ToArray());

        foreach (var definition in OverlayScopePolicy.Definitions)
        {
            Assert.IsTrue(
                OverlayScopePolicy.IsSupported(definition.Scope));
            Assert.AreEqual(
                definition.Id,
                OverlayScopePolicy.ToId(definition.Scope));
            Assert.AreEqual(
                definition.DisplayName,
                OverlayScopePolicy.GetDisplayName(definition.Scope));

            foreach (var identifier in definition.Identifiers)
            {
                Assert.IsTrue(
                    OverlayScopePolicy.TryParseId(
                        $"  {identifier.ToUpperInvariant()}  ",
                        out var parsed));
                Assert.AreEqual(definition.Scope, parsed);
            }
        }
    }

    [TestMethod]
    public void UnknownScopeIdentifierIsRejectedConsistently()
    {
        Assert.IsFalse(
            OverlayScopePolicy.TryParseId(
                "unknown-scope",
                out var parsed));
        Assert.AreEqual(OverlayScopePolicy.Default, parsed);
        Assert.ThrowsException<ArgumentException>(() =>
            OverlayScopePolicy.ParseRequired("unknown-scope"));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            OverlayScopePolicy.ToId((OverlayScope)999));
        Assert.ThrowsException<ArgumentOutOfRangeException>(() =>
            OverlayScopePolicy.GetDisplayName((OverlayScope)999));
    }
'@

Replace-Exact 'docs/ARCHITECTURE.md' `
    '| Scope identifiers, default, and display names | `OverlayScopePolicy` |' `
    '| Scope enum values, canonical identifiers, aliases, default, and display names | `OverlayScopePolicy` definition table |'
