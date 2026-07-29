[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ArchivePath,

    [ValidateNotNullOrEmpty()]
    [string]$ManifestPath =
        (Join-Path $PSScriptRoot '..\release\required-files.txt')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedArchive = (Resolve-Path -LiteralPath $ArchivePath).Path
$resolvedManifest = (Resolve-Path -LiteralPath $ManifestPath).Path

$requiredFiles = @(
    Get-Content -LiteralPath $resolvedManifest |
        ForEach-Object { $_.Trim() } |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_) -and
            -not $_.StartsWith('#', [StringComparison]::Ordinal)
        }
)

if ($requiredFiles.Count -eq 0) {
    throw "The release manifest is empty: $resolvedManifest"
}

$failures = [System.Collections.Generic.List[string]]::new()
$entries = @{}
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedArchive)

try {
    foreach ($entry in $archive.Entries) {
        $normalizedName = $entry.FullName.Replace('\', '/').TrimStart('/')
        if ([string]::IsNullOrWhiteSpace($normalizedName) -or
            $normalizedName.EndsWith('/', [StringComparison]::Ordinal)) {
            continue
        }

        $key = $normalizedName.ToLowerInvariant()
        if ($entries.ContainsKey($key)) {
            $failures.Add(
                "Archive contains duplicate case-insensitive path '$normalizedName'.")
            continue
        }

        $entries[$key] = $entry
    }

    foreach ($requiredFile in $requiredFiles) {
        $normalizedRequired =
            $requiredFile.Replace('\', '/').TrimStart('/')
        $key = $normalizedRequired.ToLowerInvariant()

        if (-not $entries.ContainsKey($key)) {
            $failures.Add("Missing required file '$normalizedRequired'.")
            continue
        }

        $entry = $entries[$key]
        if ($entry.Length -le 0) {
            $failures.Add("Required file '$normalizedRequired' is empty.")
            continue
        }

        if ($normalizedRequired.EndsWith('.txt', [StringComparison]::OrdinalIgnoreCase) -or
            $normalizedRequired.EndsWith('.md', [StringComparison]::OrdinalIgnoreCase)) {
            $stream = $entry.Open()
            try {
                $utf8 = [System.Text.UTF8Encoding]::new($false, $true)
                $reader = [System.IO.StreamReader]::new($stream, $utf8, $true)
                try {
                    $text = $reader.ReadToEnd()
                }
                finally {
                    $reader.Dispose()
                }
            }
            catch {
                $failures.Add(
                    "Required text file '$normalizedRequired' is not readable UTF-8: $($_.Exception.Message)")
                continue
            }
            finally {
                $stream.Dispose()
            }

            if ([string]::IsNullOrWhiteSpace($text)) {
                $failures.Add(
                    "Required text file '$normalizedRequired' contains no readable text.")
            }
        }
    }
}
finally {
    $archive.Dispose()
}

if ($failures.Count -gt 0) {
    $details = $failures | ForEach-Object { " - $_" }
    throw "Release package validation failed:`n$($details -join "`n")"
}

Write-Host (
    "Release package verified: {0} required files are present, non-empty and readable in {1}." -f
    $requiredFiles.Count,
    $resolvedArchive)
