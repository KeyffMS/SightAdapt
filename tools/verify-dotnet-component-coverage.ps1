[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ArchivePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

$resolvedArchive = (Resolve-Path -LiteralPath $ArchivePath).Path
$failures = [System.Collections.Generic.List[string]]::new()
$entries = @{}
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedArchive)

function Read-EntryText([System.IO.Compression.ZipArchiveEntry]$Entry) {
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
    finally {
        $stream.Dispose()
    }
}

function Get-EntrySha256([System.IO.Compression.ZipArchiveEntry]$Entry) {
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

try {
    foreach ($entry in $archive.Entries) {
        $path = $entry.FullName.Replace('\', '/').TrimStart('/')
        if ([string]::IsNullOrWhiteSpace($path) -or
            $path.EndsWith('/', [StringComparison]::Ordinal)) {
            continue
        }
        $key = $path.ToLowerInvariant()
        if ($entries.ContainsKey($key)) {
            $failures.Add("Archive contains duplicate path '$path'.")
            continue
        }
        $entries[$key] = $entry
    }

    foreach ($required in @(
        'dotnet-notice-metadata.json',
        'third-party-notices.txt',
        'dotnet-license-notice.txt',
        'sightadapt.exe')) {
        if (-not $entries.ContainsKey($required)) {
            $failures.Add("Component coverage validation is missing '$required'.")
        }
    }
    if ($failures.Count -gt 0) {
        throw 'Required component coverage inputs are unavailable.'
    }

    $metadataText = Read-EntryText $entries['dotnet-notice-metadata.json']
    $noticeText = Read-EntryText $entries['third-party-notices.txt']
    $metadata = $metadataText | ConvertFrom-Json
    if ([int]$metadata.schemaVersion -ne 2) {
        $failures.Add("Component coverage requires metadata schema 2, found '$($metadata.schemaVersion)'.")
    }
    if ($metadataText -match '(?i)[A-Z]:\\|/home/|/Users/|runneradmin|github\\workspace') {
        $failures.Add('Component metadata exposes an absolute build-machine path.')
    }

    $coverage = $metadata.componentCoverage
    $components = @($coverage.components)
    $packages = @($coverage.packages)
    $mappings = @($coverage.noticeMappings)
    if ([string]::IsNullOrWhiteSpace([string]$coverage.method)) {
        $failures.Add('Component coverage does not identify its inventory method.')
    }
    if ([string]$coverage.bundleManifestSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
        $failures.Add('Component coverage lacks a valid FilesToBundle manifest SHA-256.')
    }
    if ([int]$coverage.unmappedExternalComponentCount -ne 0) {
        $failures.Add("Component coverage reports $($coverage.unmappedExternalComponentCount) unmapped components.")
    }
    if ($components.Count -ne [int]$coverage.runtimeComponentCount) {
        $failures.Add('Component count does not match the detailed inventory.')
    }

    $embedded = @($components | Where-Object {
        [string]$_.disposition -eq 'embedded'
    })
    $loose = @($components | Where-Object {
        [string]$_.disposition -eq 'loose'
    })
    if ($embedded.Count -ne [int]$coverage.embeddedRuntimeComponentCount -or
        $loose.Count -ne [int]$coverage.looseRuntimeComponentCount) {
        $failures.Add('Embedded/loose component totals do not match the inventory.')
    }
    if ($embedded.Count -le 0) {
        $failures.Add('No embedded package components were recorded for SightAdapt.exe.')
    }

    $mappingById = @{}
    foreach ($mapping in $mappings) {
        $id = [string]$mapping.id
        if ([string]::IsNullOrWhiteSpace($id) -or $mappingById.ContainsKey($id)) {
            $failures.Add("Invalid or duplicate notice mapping '$id'.")
            continue
        }
        $mappingById[$id] = $mapping
        if ([string]$mapping.kind -eq 'official-dotnet-release-bundle') {
            if ([string]$mapping.licenseFile -ne 'DOTNET-LICENSE-NOTICE.txt' -or
                [string]$mapping.thirdPartyNoticesFile -ne 'THIRD-PARTY-NOTICES.txt' -or
                [string]$mapping.licenseSha256 -ne
                    ([string]$metadata.source.importedLicenseSha256).ToLowerInvariant() -or
                [string]$mapping.thirdPartyNoticesSha256 -ne
                    ([string]$metadata.source.importedThirdPartyNoticesSha256).ToLowerInvariant()) {
                $failures.Add('The official .NET notice mapping does not match imported exact-release evidence.')
            }
        }
        elseif ([string]$mapping.kind -eq 'exact-nuget-package-license') {
            if ([string]$mapping.packageSha512 -notmatch '^[0-9A-Fa-f]{128}$' -or
                [string]::IsNullOrWhiteSpace([string]$mapping.policyLicense) -or
                [string]::IsNullOrWhiteSpace([string]$mapping.nuspecLicenseType) -or
                [string]::IsNullOrWhiteSpace([string]$mapping.nuspecLicenseValue) -or
                [string]$mapping.noticeSectionSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
                $failures.Add("Package notice mapping '$id' has incomplete exact-license evidence.")
            }
            $package = [string]$mapping.package
            if (-not $noticeText.Contains("Package: $package", [StringComparison]::Ordinal) -or
                -not $noticeText.Contains("Package SHA-512: $($mapping.packageSha512)", [StringComparison]::Ordinal) -or
                -not $noticeText.Contains("Policy license: $($mapping.policyLicense)", [StringComparison]::Ordinal)) {
                $failures.Add("THIRD-PARTY-NOTICES.txt lacks the exact package section for '$package'.")
            }
        }
        else {
            $failures.Add("Unsupported notice mapping kind '$($mapping.kind)'.")
        }
    }

    $packageByIdentity = @{}
    foreach ($package in $packages) {
        $identity = [string]$package.package
        if ([string]::IsNullOrWhiteSpace($identity) -or
            $packageByIdentity.ContainsKey($identity)) {
            $failures.Add("Invalid or duplicate package evidence '$identity'.")
            continue
        }
        $packageByIdentity[$identity] = $package
        if ([string]$package.packageSha512 -notmatch '^[0-9A-Fa-f]{128}$' -or
            [int]$package.publishedComponentCount -le 0 -or
            [string]::IsNullOrWhiteSpace([string]$package.policyLicense)) {
            $failures.Add("Published package evidence '$identity' is incomplete.")
        }
    }

    $componentKeys = [System.Collections.Generic.HashSet[string]]::new(
        [StringComparer]::OrdinalIgnoreCase)
    foreach ($component in $components) {
        $disposition = [string]$component.disposition
        $outputPath = ([string]$component.outputPath).Replace('\', '/').TrimStart('/')
        $package = [string]$component.package
        $assetPath = ([string]$component.packageAssetPath).Replace('\', '/')
        $sha256 = [string]$component.sha256
        $mappingId = [string]$component.noticeMapping
        if (@('embedded', 'loose') -notcontains $disposition -or
            [string]::IsNullOrWhiteSpace($outputPath) -or
            [string]::IsNullOrWhiteSpace($package) -or
            [string]::IsNullOrWhiteSpace($assetPath) -or
            $sha256 -notmatch '^[0-9A-Fa-f]{64}$') {
            $failures.Add("Invalid component mapping for '$outputPath'.")
            continue
        }
        if (-not $packageByIdentity.ContainsKey($package)) {
            $failures.Add("Component '$outputPath' references package '$package' without package evidence.")
        }
        if (-not $mappingById.ContainsKey($mappingId)) {
            $failures.Add("Component '$outputPath' references missing notice mapping '$mappingId'.")
        }
        $key = "$disposition|$outputPath|$package|$assetPath"
        if (-not $componentKeys.Add($key)) {
            $failures.Add("Duplicate component mapping '$key'.")
        }
        if ($disposition -eq 'loose') {
            $archiveKey = $outputPath.ToLowerInvariant()
            if (-not $entries.ContainsKey($archiveKey)) {
                $failures.Add("Mapped loose binary '$outputPath' is absent from the ZIP.")
            }
            elseif ((Get-EntrySha256 $entries[$archiveKey]) -ne $sha256.ToLowerInvariant()) {
                $failures.Add("Mapped loose binary '$outputPath' has a SHA-256 mismatch.")
            }
        }
    }

    foreach ($requiredPack in @(
        "Microsoft.NETCore.App.Runtime.$($metadata.runtimeIdentifier)/$($metadata.runtimeVersion)",
        "Microsoft.WindowsDesktop.App.Runtime.$($metadata.runtimeIdentifier)/$($metadata.runtimeVersion)")) {
        if (@($components | Where-Object {
            [string]$_.package -eq $requiredPack
        }).Count -eq 0) {
            $failures.Add("Required runtime pack '$requiredPack' has no mapped published component.")
        }
    }

    $mappedLoosePaths = @($loose | ForEach-Object {
        ([string]$_.outputPath).Replace('\', '/').TrimStart('/').ToLowerInvariant()
    })
    foreach ($entryPair in $entries.GetEnumerator()) {
        $path = [string]$entryPair.Key
        $extension = [System.IO.Path]::GetExtension($path).ToLowerInvariant()
        if ($extension -in @('.dll', '.exe', '.so', '.dylib') -and
            $path -ne 'sightadapt.exe' -and
            $mappedLoosePaths -notcontains $path) {
            $failures.Add("Archive binary '$path' has no component notice mapping.")
        }
    }
}
catch {
    if ($failures.Count -eq 0) {
        $failures.Add("Component coverage validation failed: $($_.Exception.Message)")
    }
}
finally {
    $archive.Dispose()
}

if ($failures.Count -gt 0) {
    $details = $failures | ForEach-Object { " - $_" }
    throw "Exact .NET component coverage validation failed:`n$($details -join "`n")"
}

Write-Host "Exact .NET component coverage verified for $resolvedArchive."
