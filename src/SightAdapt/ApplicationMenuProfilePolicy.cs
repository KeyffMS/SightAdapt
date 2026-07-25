namespace SightAdapt;

internal static class ApplicationMenuProfilePolicy
{
    public const string InheritSelectorId =
        "inherit-from-application";

    public const string InheritDisplayName =
        "Same as application";

    public static string ToSelectorId(
        string? menuVisualProfileId)
    {
        return string.IsNullOrWhiteSpace(
                menuVisualProfileId)
            ? InheritSelectorId
            : menuVisualProfileId.Trim();
    }

    public static string? FromSelectorId(
        string? selectorId)
    {
        var normalized =
            (selectorId ?? string.Empty).Trim();

        return string.IsNullOrWhiteSpace(normalized) ||
               IsReservedProfileId(normalized)
            ? null
            : normalized;
    }

    public static bool IsReservedProfileId(
        string? profileId)
    {
        return string.Equals(
            profileId,
            InheritSelectorId,
            StringComparison.OrdinalIgnoreCase);
    }
}
