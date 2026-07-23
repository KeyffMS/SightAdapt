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