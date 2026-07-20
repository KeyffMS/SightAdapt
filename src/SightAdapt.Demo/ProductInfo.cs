using System.Reflection;

namespace SightAdapt.Demo;

internal static class ProductInfo
{
    private static readonly Assembly Assembly =
        typeof(ProductInfo).Assembly;

    public static string ProductName { get; } =
        GetAttribute<AssemblyProductAttribute>(
            attribute => attribute.Product,
            "SightAdapt");

    public static string VersionLabel { get; } =
        Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ??
        Assembly.GetName().Version?.ToString() ??
        string.Empty;

    public static string MilestoneLabel { get; } =
        GetMetadata("Milestone", VersionLabel);

    public static string DisplayName { get; } =
        string.IsNullOrWhiteSpace(MilestoneLabel)
            ? ProductName
            : $"{ProductName} · {MilestoneLabel}";

    public static string WindowTitle { get; } =
        $"{DisplayName} · Application and color profiles";

    public static string Tagline { get; } =
        GetAttribute<AssemblyDescriptionAttribute>(
            attribute => attribute.Description,
            "Automatic visual accessibility for Windows applications");

    public static string License { get; } =
        GetMetadata("License", "MIT License");

    public static string Author { get; } =
        GetAttribute<AssemblyCompanyAttribute>(
            attribute => attribute.Company,
            string.Empty);

    public static string RepositoryUrl { get; } =
        GetMetadata("RepositoryUrl", string.Empty);

    public static string RepositoryDisplay { get; } =
        CreateRepositoryDisplay(RepositoryUrl);

    private static string GetAttribute<TAttribute>(
        Func<TAttribute, string?> selector,
        string fallback)
        where TAttribute : Attribute
    {
        return Assembly.GetCustomAttribute<TAttribute>() is { } attribute &&
            selector(attribute) is { Length: > 0 } value
                ? value
                : fallback;
    }

    private static string GetMetadata(
        string key,
        string fallback)
    {
        return Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => string.Equals(
                attribute.Key,
                key,
                StringComparison.Ordinal))
            ?.Value ?? fallback;
    }

    private static string CreateRepositoryDisplay(string repositoryUrl)
    {
        if (!Uri.TryCreate(
                repositoryUrl,
                UriKind.Absolute,
                out var uri))
        {
            return repositoryUrl;
        }

        return uri.Host + uri.AbsolutePath.TrimEnd('/');
    }
}
