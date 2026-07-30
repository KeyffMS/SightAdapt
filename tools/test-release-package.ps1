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

[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\SightAdapt\SightAdapt.csproj')
$projectGroup = @($project.Project.PropertyGroup) | Select-Object -First 1
$targetFramework = [string]$projectGroup.TargetFramework

$failures = [System.Collections.Generic.List[string]]::new()
$entries = @{}
$textEntries = @{}

$reviewPath = Join-Path $root 'release\dotnet-redistribution-review.json'
try {
    $review = Get-Content -LiteralPath $reviewPath -Raw | ConvertFrom-Json
    if ([int]$review.schemaVersion -ne 1) {
        $failures.Add("Unsupported .NET redistribution review schema '$($review.schemaVersion)'.")
    }
    if ([string]$review.reviewedAt -notmatch '^\d{4}-\d{2}-\d{2}$') {
        $failures.Add('The .NET redistribution review date must use YYYY-MM-DD.')
    }

    $reviewExpected = @{
        sdkVersion = $expectedMetadata.sdkVersion
        runtimeVersion = $expectedMetadata.runtimeVersion
        targetFramework = $targetFramework
        runtimeIdentifier = $expectedMetadata.runtimeIdentifier
        publishMode = $expectedMetadata.publishMode
    }
    foreach ($expected in $reviewExpected.GetEnumerator()) {
        $actual = [string]$review.reviewedConfiguration.($expected.Key)
        if ($actual -ne [string]$expected.Value) {
            $failures.Add(
                "Redistribution review has $($expected.Key)='$actual'; expected '$($expected.Value)'.")
        }
    }

    $templateRelativePath = ([string]$review.noticeTemplate.path).Replace(
        '/',
        [System.IO.Path]::DirectorySeparatorChar)
    $templatePath = Join-Path $root $templateRelativePath
    if (-not [System.IO.File]::Exists($templatePath)) {
        $failures.Add("Reviewed redistribution template is missing: $($review.noticeTemplate.path)")
    }
    else {
        $templateHash = (Get-FileHash -LiteralPath $templatePath -Algorithm SHA256).Hash.ToLowerInvariant()
        if ($templateHash -ne ([string]$review.noticeTemplate.sha256).ToLowerInvariant()) {
            $failures.Add('The redistribution notice template differs from the reviewed SHA-256.')
        }
    }

    $professionalStatus = [string]$review.professionalReview.status
    $professionalIssue = [int]$review.professionalReview.trackingIssue
    $professionalRecord = [string]$review.professionalReview.publicRecord
    if (@('not-obtained', 'approved-with-conditions', 'approved') -notcontains $professionalStatus) {
        $failures.Add("Unsupported professional-review status '$professionalStatus'.")
    }
    if ($professionalIssue -ne 93) {
        $failures.Add('The professional review is not linked to Issue #93.')
    }
    if ($professionalStatus -eq 'not-obtained' -and
        -not [string]::IsNullOrWhiteSpace($professionalRecord)) {
        $failures.Add('Status not-obtained must not identify a professional approval record.')
    }
    if ($professionalStatus -ne 'not-obtained' -and
        ([string]::IsNullOrWhiteSpace($professionalRecord) -or
         -not [System.IO.File]::Exists((Join-Path $root $professionalRecord)))) {
        $failures.Add("Professional-review status '$professionalStatus' lacks an existing public record.")
    }
}
catch {
    $failures.Add("The .NET redistribution review record is invalid: $($_.Exception.Message)")
    $review = $null
}

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

function Get-HeaderValue(
    [string]$Text,
    [string]$Name) {
    $pattern = '(?m)^' + [regex]::Escape($Name) + ':\s*(?<value>[^\r\n]+?)\s*$'
    $match = [regex]::Match($Text, $pattern)
    if (-not $match.Success) {
        return $null
    }
    return $match.Groups['value'].Value.Trim()
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

    $redistributionKey = 'microsoft-dotnet-redistribution.txt'
    if ($textEntries.ContainsKey($redistributionKey) -and $null -ne $review) {
        $redistributionText = [string]$textEntries[$redistributionKey]
        if ($redistributionText -match '\{\{[A-Z0-9_]+\}\}') {
            $failures.Add('MICROSOFT-DOTNET-REDISTRIBUTION.txt contains an unresolved template marker.')
        }

        $professionalRecord = [string]$review.professionalReview.publicRecord
        $expectedHeaders = [ordered]@{
            'Product version' = $expectedMetadata.productVersion
            'SDK version' = $expectedMetadata.sdkVersion
            'Runtime version' = $expectedMetadata.runtimeVersion
            'Windows Desktop Runtime version' = $expectedMetadata.runtimeVersion
            'Target framework' = $targetFramework
            'Runtime identifier' = $expectedMetadata.runtimeIdentifier
            'Publish mode' = $expectedMetadata.publishMode
            'Maintainer review date' = [string]$review.reviewedAt
            'Professional review status' = [string]$review.professionalReview.status
            'Professional review tracking issue' = "#$([int]$review.professionalReview.trackingIssue)"
            'Professional review public record' = if ([string]::IsNullOrWhiteSpace($professionalRecord)) {
                'none'
            }
            else {
                $professionalRecord
            }
        }
        foreach ($expectedHeader in $expectedHeaders.GetEnumerator()) {
            $actual = Get-HeaderValue $redistributionText ([string]$expectedHeader.Key)
            if ([string]::IsNullOrWhiteSpace($actual)) {
                $failures.Add(
                    "MICROSOFT-DOTNET-REDISTRIBUTION.txt is missing header '$($expectedHeader.Key)'.")
            }
            elseif ($actual -ne [string]$expectedHeader.Value) {
                $failures.Add(
                    "MICROSOFT-DOTNET-REDISTRIBUTION.txt has $($expectedHeader.Key)='$actual'; expected '$($expectedHeader.Value)'.")
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
    "Release package verified: {0} required files are present, readable and consistent with the pinned .NET release in {1}." -f
    $requiredFiles.Count,
    $resolvedArchive)
