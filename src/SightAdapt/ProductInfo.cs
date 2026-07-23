using System.Reflection;

namespace SightAdapt;

internal static class ProductInfo
{
    private static readonly Assembly Assembly =
        typeof(ProductInfo).Assembly;

    public static string ProductName { get; } =
        GetAttribute<AssemblyProductAttribute>(
            attribute => attribute.Product,
            "SightAdapt");

    private static readonly string FullVersion =
        GetVersion();

    public static string VersionLabel { get; } =
        CreateVersionLabel(FullVersion);

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
