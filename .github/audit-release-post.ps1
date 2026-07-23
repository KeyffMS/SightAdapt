Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'src/SightAdapt/ApplicationProfilesGrid.cs'
$content = (Get-Content -Raw $path).Replace("`r`n", "`n")
$old = @'
    internal static string CreateDataErrorDiagnostic(
        Exception exception,
        DataGridViewDataErrorContexts context,
        int rowIndex,
        int columnIndex,
        string? columnName,
        string? executablePath,
        bool recovered)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return
            $"SightAdapt grid data error; recovered={recovered}; " +
            $"row={rowIndex}; column={columnIndex}; " +
            $"columnName={columnName ?? "<unknown>"}; " +
            $"executablePath={executablePath ?? "<unknown>"}; " +
            $"context={context}; exception={exception}";
    }
'@
$new = @'
    internal static string CreateDataErrorDiagnostic(
        Exception? exception,
        DataGridViewDataErrorContexts context,
        int rowIndex,
        int columnIndex,
        string? columnName,
        string? executablePath,
        bool recovered)
    {
        return
            $"SightAdapt grid data error; recovered={recovered}; " +
            $"row={rowIndex}; column={columnIndex}; " +
            $"columnName={columnName ?? "<unknown>"}; " +
            $"executablePath={executablePath ?? "<unknown>"}; " +
            $"context={context}; " +
            $"exception={exception?.ToString() ?? "<none>"}";
    }
'@
$count = [regex]::Matches(
    $content,
    [regex]::Escape($old.Replace("`r`n", "`n"))).Count
if ($count -ne 1) {
    throw "Expected one DataError diagnostic block, found $count."
}
$content = $content.Replace(
    $old.Replace("`r`n", "`n"),
    $new.Replace("`r`n", "`n"))
$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($path, $content, $utf8)
