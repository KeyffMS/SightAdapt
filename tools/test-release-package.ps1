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

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

$resolvedArchive = (Resolve-Path -LiteralPath $ArchivePath).Path
$resolvedManifest = (Resolve-Path -LiteralPath $ManifestPath).Path
$root = Split-Path -Parent (Split-Path -Parent $resolvedManifest)

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

[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup
$expectedMetadata = @{
    productVersion = [string]$group.SightAdaptProductVersion
    sdkVersion = [string]$group.SightAdaptDotNetSdkVersion
    runtimeVersion = [string]$group.SightAdaptDotNetRuntimeVersion
    runtimeIdentifier = [string]$group.SightAdaptRuntimeIdentifier
    publishMode = [string]$group.SightAdaptPublishMode
}

$failures = [System.Collections.Generic.List[string]]::new()
$entries = @{}
$textEntries = @{}
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedArchive)

function Read-ArchiveText(
    [System.IO.Compression.ZipArchiveEntry]$Entry,
    [string]$DisplayName) {
    $stream = $Entry.Open()
    try {
        $utf8 = [System.Text.UTF8Encoding]::new($false, $true)
        $reader = [System.IO.StreamReader]::new($stream, $utf8, $true)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    catch {
        $failures.Add(
            "Required text file '$DisplayName' is not readable UTF-8: $($_.Exception.Message)")
        return $null
    }
    finally {
        $stream.Dispose()
    }
}

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
            $normalizedRequired.EndsWith('.md', [StringComparison]::OrdinalIgnoreCase) -or
            $normalizedRequired.EndsWith('.json', [StringComparison]::OrdinalIgnoreCase)) {
            $text = Read-ArchiveText $entry $normalizedRequired
            if ($null -eq $text) {
                continue
            }
            $textEntries[$key] = $text
            if ([string]::IsNullOrWhiteSpace($text)) {
                $failures.Add(
                    "Required text file '$normalizedRequired' contains no readable text.")
            }
        }
    }

    $metadataKey = 'dotnet-notice-metadata.json'
    if ($textEntries.ContainsKey($metadataKey)) {
        try {
            $noticeMetadata = $textEntries[$metadataKey] | ConvertFrom-Json
            foreach ($expected in $expectedMetadata.GetEnumerator()) {
                $actual = [string]$noticeMetadata.($expected.Key)
                if ($actual -ne [string]$expected.Value) {
                    $failures.Add(
                        "DOTNET-NOTICE-METADATA.json has $($expected.Key)='$actual'; expected '$($expected.Value)'.")
                }
            }

            $requiredRuntimePacks = @(
                "Microsoft.NETCore.App.Runtime.$($expectedMetadata.runtimeIdentifier)/$($expectedMetadata.runtimeVersion)",
                "Microsoft.WindowsDesktop.App.Runtime.$($expectedMetadata.runtimeIdentifier)/$($expectedMetadata.runtimeVersion)"
            )
            foreach ($runtimePack in $requiredRuntimePacks) {
                if (@($noticeMetadata.runtimePackages) -notcontains $runtimePack) {
                    $failures.Add(
                        "DOTNET-NOTICE-METADATA.json does not map runtime pack '$runtimePack'.")
                }
            }

            if ([string]::IsNullOrWhiteSpace([string]$noticeMetadata.source.packageUrl)) {
                $failures.Add('DOTNET-NOTICE-METADATA.json does not record the official source package URL.')
            }
            if ([string]$noticeMetadata.source.packageSha512 -notmatch '^[0-9A-Fa-f]{128}$') {
                $failures.Add('DOTNET-NOTICE-METADATA.json does not contain a valid package SHA-512.')
            }
            if ([string]$noticeMetadata.source.importedLicenseSha256 -notmatch '^[0-9A-Fa-f]{64}$' -or
                [string]$noticeMetadata.source.importedThirdPartyNoticesSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
                $failures.Add('DOTNET-NOTICE-METADATA.json does not contain valid imported-file SHA-256 values.')
            }
        }
        catch {
            $failures.Add(
                "DOTNET-NOTICE-METADATA.json is invalid: $($_.Exception.Message)")
        }
    }

    $noticeKey = 'third-party-notices.txt'
    if ($textEntries.ContainsKey($noticeKey)) {
        $noticeText = [string]$textEntries[$noticeKey]
        if (-not $noticeText.StartsWith(
            'SIGHTADAPT EXACT-VERSION THIRD-PARTY NOTICES',
            [StringComparison]::Ordinal)) {
            $failures.Add('THIRD-PARTY-NOTICES.txt is not an exact-version generated notice.')
        }
        if (-not $noticeText.Contains(
            ".NET runtime and Windows Desktop Runtime: $($expectedMetadata.runtimeVersion)",
            [StringComparison]::Ordinal)) {
            $failures.Add('THIRD-PARTY-NOTICES.txt does not identify the pinned runtime version.')
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
    "Release package verified: {0} required files are present, readable and consistent with the pinned .NET release in {1}." -f
    $requiredFiles.Count,
    $resolvedArchive)
