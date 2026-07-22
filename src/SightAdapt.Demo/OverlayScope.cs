namespace SightAdapt.Demo;

internal enum OverlayScope
{
    ClientArea = 0,
    Window = 1,
    Screen = 2,
    AllScreens = 3,
}

internal static class OverlayScopePolicy
{
    public const OverlayScope Default = OverlayScope.ClientArea;

    private static readonly OverlayScope[] SupportedScopes =
    [
        OverlayScope.ClientArea,
        OverlayScope.Window,
        OverlayScope.Screen,
        OverlayScope.AllScreens,
    ];

    public static IReadOnlyList<OverlayScope> All => SupportedScopes;

    public static bool IsSupported(OverlayScope scope)
    {
        return scope is
            OverlayScope.ClientArea or
            OverlayScope.Window or
            OverlayScope.Screen or
            OverlayScope.AllScreens;
    }

    public static string ToId(OverlayScope scope)
    {
        return scope switch
        {
            OverlayScope.ClientArea => "client-area",
            OverlayScope.Window => "window",
            OverlayScope.Screen => "screen",
            OverlayScope.AllScreens => "all-screens",
            _ => throw new ArgumentOutOfRangeException(
                nameof(scope),
                scope,
                "The overlay scope is not supported."),
        };
    }

    public static bool TryParseId(string? value, out OverlayScope scope)
    {
        var normalized = (value ?? string.Empty)
            .Trim()
            .ToLowerInvariant();

        scope = normalized switch
        {
            "client-area" or "client" => OverlayScope.ClientArea,
            "window" or "full-window" => OverlayScope.Window,
            "screen" or "current-screen" => OverlayScope.Screen,
            "all-screens" or "virtual-screen" => OverlayScope.AllScreens,
            _ => Default,
        };

        return normalized is
            "client-area" or "client" or
            "window" or "full-window" or
            "screen" or "current-screen" or
            "all-screens" or "virtual-screen";
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
        return scope switch
        {
            OverlayScope.ClientArea => "Client area",
            OverlayScope.Window => "Full window",
            OverlayScope.Screen => "Current screen",
            OverlayScope.AllScreens => "All screens",
            _ => throw new ArgumentOutOfRangeException(
                nameof(scope),
                scope,
                "The overlay scope is not supported."),
        };
    }
}
