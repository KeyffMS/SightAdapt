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

function Get-NormalizedTextSha256([string]$Path) {
    $text = Get-Content -LiteralPath $Path -Raw
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($normalized)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString(
            $sha256.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Read-ArchiveText(
    [System.IO.Compression.ZipArchiveEntry]$Entry,
    [string]$DisplayName) {
    $stream = $Entry.Open()
    try {
        $reader = [System.IO.StreamReader]::new(
            $stream,
            [System.Text.UTF8Encoding]::new($false, $true),
            $true)
        try {
            return $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    catch {
        $failures.Add("Required text file '$DisplayName' is not readable UTF-8: $($_.Exception.Message)")
        return $null
    }
    finally {
        $stream.Dispose()
    }
}

function Get-ArchiveEntrySha256(
    [System.IO.Compression.ZipArchiveEntry]$Entry) {
    $stream = $Entry.Open()
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString(
            $sha256.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
        $stream.Dispose()
    }
}

function Get-HeaderValue([string]$Text, [string]$Name) {
    $pattern = '(?m)^' + [regex]::Escape($Name) + ':\s*(?<value>[^\r\n]+?)\s*$'
    $match = [regex]::Match($Text, $pattern)
    if (-not $match.Success) {
        return $null
    }
    return $match.Groups['value'].Value.Trim()
}

$reviewPath = Join-Path $root 'release\dotnet-redistribution-review.json'
try {
    $review = Get-Content -LiteralPath $reviewPath -Raw | ConvertFrom-Json
    if ([int]$review.schemaVersion -ne 2) {
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
            $failures.Add("Redistribution review has $($expected.Key)='$actual'; expected '$($expected.Value)'.")
        }
    }

    $templatePath = Join-Path $root ([string]$review.noticeTemplate.path)
    if (-not [System.IO.File]::Exists($templatePath)) {
        $failures.Add("Reviewed redistribution template is missing: $($review.noticeTemplate.path)")
    }
    elseif ((Get-NormalizedTextSha256 $templatePath) -ne
            ([string]$review.noticeTemplate.sha256).ToLowerInvariant()) {
        $failures.Add('The redistribution notice template differs from the reviewed SHA-256.')
    }

    $decisionStatus = [string]$review.maintainerDecision.status
    $decisionOwner = [string]$review.maintainerDecision.decisionOwner
    $decisionIssue = [int]$review.maintainerDecision.decisionIssue
    if (@('accepted-for-current-distribution', 'accepted-with-conditions', 'blocked') -notcontains $decisionStatus) {
        $failures.Add("Unsupported maintainer decision '$decisionStatus'.")
    }
    if ([string]::IsNullOrWhiteSpace($decisionOwner)) {
        $failures.Add('The redistribution maintainer decision has no owner.')
    }
    if ($decisionIssue -le 0) {
        $failures.Add('The redistribution maintainer decision has no valid issue number.')
    }
    if ($decisionStatus -eq 'blocked') {
        $failures.Add('The redistribution maintainer decision blocks packaging.')
    }
    if ($decisionStatus -eq 'accepted-with-conditions' -and
        @($review.maintainerDecision.conditions).Count -eq 0) {
        $failures.Add('A conditional redistribution decision has no conditions.')
    }
}
catch {
    $failures.Add("The .NET redistribution review record is invalid: $($_.Exception.Message)")
    $review = $null
}

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
            $failures.Add("Archive contains duplicate case-insensitive path '$normalizedName'.")
            continue
        }
        $entries[$key] = $entry
    }

    foreach ($requiredFile in $requiredFiles) {
        $normalizedRequired = $requiredFile.Replace('\', '/').TrimStart('/')
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
        if ($normalizedRequired -match '\.(txt|md|json)$') {
            $text = Read-ArchiveText $entry $normalizedRequired
            if ($null -ne $text) {
                $textEntries[$key] = $text
                if ([string]::IsNullOrWhiteSpace($text)) {
                    $failures.Add("Required text file '$normalizedRequired' contains no readable text.")
                }
            }
        }
    }

    $metadataKey = 'dotnet-notice-metadata.json'
    if ($textEntries.ContainsKey($metadataKey)) {
        try {
            $noticeMetadata = $textEntries[$metadataKey] | ConvertFrom-Json
            if ([int]$noticeMetadata.schemaVersion -ne 2) {
                $failures.Add("DOTNET-NOTICE-METADATA.json uses unsupported schema '$($noticeMetadata.schemaVersion)'.")
            }
            foreach ($expected in $expectedMetadata.GetEnumerator()) {
                $actual = [string]$noticeMetadata.($expected.Key)
                if ($actual -ne [string]$expected.Value) {
                    $failures.Add("DOTNET-NOTICE-METADATA.json has $($expected.Key)='$actual'; expected '$($expected.Value)'.")
                }
            }

            $requiredPacks = @(
                "Microsoft.NETCore.App.Runtime.$($expectedMetadata.runtimeIdentifier)/$($expectedMetadata.runtimeVersion)",
                "Microsoft.WindowsDesktop.App.Runtime.$($expectedMetadata.runtimeIdentifier)/$($expectedMetadata.runtimeVersion)")
            foreach ($runtimePack in $requiredPacks) {
                if (@($noticeMetadata.runtimePackages) -notcontains $runtimePack) {
                    $failures.Add("DOTNET-NOTICE-METADATA.json does not map runtime pack '$runtimePack'.")
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

            $coverage = $noticeMetadata.componentCoverage
            $components = @($coverage.components)
            $runtimePacks = @($coverage.runtimePacks)
            if ([string]::IsNullOrWhiteSpace([string]$coverage.method)) {
                $failures.Add('Component coverage does not identify its inventory method.')
            }
            if ([string]$coverage.bundleManifestSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
                $failures.Add('Component coverage does not contain a valid FilesToBundle manifest SHA-256.')
            }
            if ([int]$coverage.unmappedExternalComponentCount -ne 0) {
                $failures.Add("Component coverage reports $($coverage.unmappedExternalComponentCount) unmapped external components.")
            }
            if ($components.Count -ne [int]$coverage.runtimeComponentCount) {
                $failures.Add('Component coverage runtime-component count does not match the component list.')
            }
            $embeddedComponents = @($components | Where-Object {
                [string]$_.disposition -eq 'embedded'
            })
            $looseComponents = @($components | Where-Object {
                [string]$_.disposition -eq 'loose'
            })
            if ($embeddedComponents.Count -ne [int]$coverage.embeddedRuntimeComponentCount -or
                $looseComponents.Count -ne [int]$coverage.looseRuntimeComponentCount) {
                $failures.Add('Component coverage disposition counts do not match the component list.')
            }
            if ($components.Count -le 0 -or $embeddedComponents.Count -le 0) {
                $failures.Add('Component coverage does not contain the embedded runtime inventory.')
            }

            $mapping = $coverage.noticeMapping
            if ([string]$mapping.id -ne 'exact-release-dotnet-bundle' -or
                [string]$mapping.licenseFile -ne 'DOTNET-LICENSE-NOTICE.txt' -or
                [string]$mapping.thirdPartyNoticesFile -ne 'THIRD-PARTY-NOTICES.txt') {
                $failures.Add('Component coverage does not use the reviewed exact-release notice mapping.')
            }
            if ([string]$mapping.licenseSha256 -ne
                ([string]$noticeMetadata.source.importedLicenseSha256).ToLowerInvariant() -or
                [string]$mapping.thirdPartyNoticesSha256 -ne
                ([string]$noticeMetadata.source.importedThirdPartyNoticesSha256).ToLowerInvariant()) {
                $failures.Add('Component notice-mapping hashes do not match the imported official notice material.')
            }

            $componentKeys = [System.Collections.Generic.HashSet[string]]::new(
                [StringComparer]::OrdinalIgnoreCase)
            foreach ($component in $components) {
                $disposition = [string]$component.disposition
                $outputPath = ([string]$component.outputPath).Replace('\', '/').TrimStart('/')
                $package = [string]$component.package
                $assetPath = ([string]$component.packageAssetPath).Replace('\', '/')
                $sha256 = [string]$component.sha256
                if (@('embedded', 'loose') -notcontains $disposition -or
                    [string]::IsNullOrWhiteSpace($outputPath) -or
                    [string]::IsNullOrWhiteSpace($assetPath) -or
                    $sha256 -notmatch '^[0-9A-Fa-f]{64}$' -or
                    [string]$component.noticeMapping -ne 'exact-release-dotnet-bundle') {
                    $failures.Add("Invalid runtime component mapping for '$outputPath'.")
                    continue
                }
                if (@($noticeMetadata.runtimePackages) -notcontains $package) {
                    $failures.Add("Runtime component '$outputPath' references unknown package '$package'.")
                }
                $componentKey = "$disposition|$outputPath|$package|$assetPath"
                if (-not $componentKeys.Add($componentKey)) {
                    $failures.Add("Duplicate runtime component mapping '$componentKey'.")
                }

                if ($disposition -eq 'loose') {
                    $archiveKey = $outputPath.ToLowerInvariant()
                    if (-not $entries.ContainsKey($archiveKey)) {
                        $failures.Add("Mapped loose runtime component '$outputPath' is missing from the archive.")
                    }
                    else {
                        $archiveSha256 = Get-ArchiveEntrySha256 $entries[$archiveKey]
                        if ($archiveSha256 -ne $sha256.ToLowerInvariant()) {
                            $failures.Add("Mapped loose runtime component '$outputPath' has a SHA-256 mismatch.")
                        }
                    }
                }
            }

            foreach ($requiredPack in $requiredPacks) {
                if (@($components | Where-Object {
                    [string]$_.package -eq $requiredPack
                }).Count -le 0) {
                    $failures.Add("Required runtime pack '$requiredPack' has no mapped published component.")
                }
                $packEvidence = @($runtimePacks | Where-Object {
                    [string]$_.package -eq $requiredPack
                }) | Select-Object -First 1
                if ($null -eq $packEvidence -or
                    [string]$packEvidence.packageSha512 -notmatch '^[0-9A-Fa-f]{128}$') {
                    $failures.Add("Required runtime pack '$requiredPack' lacks exact NuGet SHA-512 evidence.")
                }
            }

            $mappedLoosePaths = @($looseComponents | ForEach-Object {
                ([string]$_.outputPath).Replace('\', '/').TrimStart('/').ToLowerInvariant()
            })
            foreach ($entryPair in $entries.GetEnumerator()) {
                $path = [string]$entryPair.Key
                $extension = [System.IO.Path]::GetExtension($path).ToLowerInvariant()
                if ($extension -in @('.dll', '.so', '.dylib', '.exe') -and
                    $path -ne 'sightadapt.exe' -and
                    $mappedLoosePaths -notcontains $path) {
                    $failures.Add("Archive binary '$path' has no runtime component notice mapping.")
                }
            }
        }
        catch {
            $failures.Add("DOTNET-NOTICE-METADATA.json is invalid: $($_.Exception.Message)")
        }
    }

    $noticeKey = 'third-party-notices.txt'
    if ($textEntries.ContainsKey($noticeKey)) {
        $noticeText = [string]$textEntries[$noticeKey]
        if (-not $noticeText.StartsWith('SIGHTADAPT EXACT-VERSION THIRD-PARTY NOTICES', [StringComparison]::Ordinal)) {
            $failures.Add('THIRD-PARTY-NOTICES.txt is not an exact-version generated notice.')
        }
        if (-not $noticeText.Contains(".NET runtime and Windows Desktop Runtime: $($expectedMetadata.runtimeVersion)", [StringComparison]::Ordinal)) {
            $failures.Add('THIRD-PARTY-NOTICES.txt does not identify the pinned runtime version.')
        }
        if (-not $noticeText.Contains('Component coverage evidence: DOTNET-NOTICE-METADATA.json', [StringComparison]::Ordinal)) {
            $failures.Add('THIRD-PARTY-NOTICES.txt does not identify the component coverage evidence.')
        }
    }

    $redistributionKey = 'microsoft-dotnet-redistribution.txt'
    if ($textEntries.ContainsKey($redistributionKey) -and $null -ne $review) {
        $redistributionText = [string]$textEntries[$redistributionKey]
        if ($redistributionText -match '\{\{[A-Z0-9_]+\}\}') {
            $failures.Add('MICROSOFT-DOTNET-REDISTRIBUTION.txt contains an unresolved template marker.')
        }
        $expectedHeaders = [ordered]@{
            'Product version' = $expectedMetadata.productVersion
            'SDK version' = $expectedMetadata.sdkVersion
            'Runtime version' = $expectedMetadata.runtimeVersion
            'Windows Desktop Runtime version' = $expectedMetadata.runtimeVersion
            'Target framework' = $targetFramework
            'Runtime identifier' = $expectedMetadata.runtimeIdentifier
            'Publish mode' = $expectedMetadata.publishMode
            'Maintainer review date' = [string]$review.reviewedAt
            'Maintainer decision' = [string]$review.maintainerDecision.status
            'Decision owner' = [string]$review.maintainerDecision.decisionOwner
            'Decision issue' = "#$([int]$review.maintainerDecision.decisionIssue)"
        }
        foreach ($expectedHeader in $expectedHeaders.GetEnumerator()) {
            $actual = Get-HeaderValue $redistributionText ([string]$expectedHeader.Key)
            if ([string]::IsNullOrWhiteSpace($actual)) {
                $failures.Add("MICROSOFT-DOTNET-REDISTRIBUTION.txt is missing header '$($expectedHeader.Key)'.")
            }
            elseif ($actual -ne [string]$expectedHeader.Value) {
                $failures.Add("MICROSOFT-DOTNET-REDISTRIBUTION.txt has $($expectedHeader.Key)='$actual'; expected '$($expectedHeader.Value)'.")
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
    "Release package verified: {0} required files and exact runtime component notice mappings are consistent with the pinned .NET release in {1}." -f
    $requiredFiles.Count,
    $resolvedArchive)
