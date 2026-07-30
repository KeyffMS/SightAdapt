[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory,

    [string]$AssetsPath,

    [string]$BundleManifestPath,

    [string]$PolicyPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($AssetsPath)) {
    $AssetsPath = Join-Path $root 'src\SightAdapt\obj\project.assets.json'
}
if ([string]::IsNullOrWhiteSpace($BundleManifestPath)) {
    $BundleManifestPath = Join-Path $root 'artifacts\dotnet-files-to-bundle.tsv'
}
if ([string]::IsNullOrWhiteSpace($PolicyPath)) {
    $PolicyPath = Join-Path $root 'release\dependency-policy.json'
}

function Get-FileSha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-FileSha512([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA512).Hash.ToLowerInvariant()
}

function Get-TextSha256([string]$Text) {
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($Text)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString(
            $sha256.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

function Convert-Base64Sha512ToHex([string]$Value) {
    try {
        $bytes = [Convert]::FromBase64String($Value.Trim())
    }
    catch {
        throw "Invalid NuGet SHA-512 value: $($_.Exception.Message)"
    }
    if ($bytes.Length -ne 64) {
        throw "NuGet SHA-512 value has $($bytes.Length) bytes instead of 64."
    }
    return [System.BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
}

function Get-NormalizedRelativePath(
    [string]$BasePath,
    [string]$Path) {
    return [System.IO.Path]::GetRelativePath(
        [System.IO.Path]::GetFullPath($BasePath),
        [System.IO.Path]::GetFullPath($Path)).Replace('\', '/')
}

function Test-PathUnderRoot(
    [string]$Path,
    [string]$CandidateRoot) {
    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($CandidateRoot).TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    return $fullPath.StartsWith(
        $fullRoot,
        [StringComparison]::OrdinalIgnoreCase)
}

function Get-OutputPath(
    [string]$SourcePath,
    [string]$RelativePath,
    [string]$BundleRelativePath) {
    foreach ($candidate in @($RelativePath, $BundleRelativePath)) {
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            return $candidate.Replace('\', '/').TrimStart('/')
        }
    }
    return [System.IO.Path]::GetFileName($SourcePath)
}

function Get-AssetKind([string]$PackageAssetPath) {
    $normalized = $PackageAssetPath.Replace('\', '/').ToLowerInvariant()
    $extension = [System.IO.Path]::GetExtension($normalized)
    if ($normalized -match '(^|/)native/' -or
        $extension -in @('.exe', '.so', '.dylib')) {
        return 'native'
    }
    if ($extension -eq '.dll') {
        return 'managed-or-native-dll'
    }
    return 'runtime-content'
}

$resolvedPublish = (Resolve-Path -LiteralPath $PublishDirectory).Path
$resolvedAssets = (Resolve-Path -LiteralPath $AssetsPath).Path
$resolvedBundleManifest = (Resolve-Path -LiteralPath $BundleManifestPath).Path
$resolvedPolicy = (Resolve-Path -LiteralPath $PolicyPath).Path
$metadataPath = Join-Path $resolvedPublish 'DOTNET-NOTICE-METADATA.json'
$thirdPartyNoticesPath = Join-Path $resolvedPublish 'THIRD-PARTY-NOTICES.txt'
if (-not [System.IO.File]::Exists($metadataPath) -or
    -not [System.IO.File]::Exists($thirdPartyNoticesPath)) {
    throw 'Generate exact-version .NET notices before component coverage.'
}

$metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
if ([int]$metadata.schemaVersion -ne 1) {
    throw "Expected base .NET notice metadata schema 1; found '$($metadata.schemaVersion)'."
}
$runtimeVersion = [string]$metadata.runtimeVersion
$rid = [string]$metadata.runtimeIdentifier
$assets = Get-Content -LiteralPath $resolvedAssets -Raw | ConvertFrom-Json
$policy = Get-Content -LiteralPath $resolvedPolicy -Raw | ConvertFrom-Json
$allowedLicenses = @($policy.allowedLicenseExpressions)

$packageFolders = @(
    $assets.packageFolders.PSObject.Properties |
        ForEach-Object { [string]$_.Name }
)
if ($packageFolders.Count -eq 0) {
    throw 'The restore graph does not record NuGet package folders.'
}

function Get-PolicyComponent([string]$PackageId) {
    $property = @($policy.components.PSObject.Properties | Where-Object {
        [string]$_.Name -ieq $PackageId
    }) | Select-Object -First 1
    if ($null -eq $property) {
        return $null
    }
    return $property.Value
}

function Resolve-PackageLocation(
    [string]$PackageId,
    [string]$PackageVersion) {
    foreach ($folder in $packageFolders) {
        $candidate = Join-Path (
            Join-Path $folder ($PackageId.ToLowerInvariant())) $PackageVersion
        if ([System.IO.Directory]::Exists($candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    return $null
}

$packageRecords = @{}
function Get-OrCreatePackageRecord(
    [string]$PackageId,
    [string]$PackageVersion,
    [string]$PackageRoot) {
    $key = "$($PackageId.ToLowerInvariant())/$($PackageVersion.ToLowerInvariant())"
    if ($packageRecords.ContainsKey($key)) {
        return $packageRecords[$key]
    }

    $policyComponent = Get-PolicyComponent $PackageId
    if ($null -eq $policyComponent) {
        throw "Package '$PackageId/$PackageVersion' is present in the published bundle but absent from release/dependency-policy.json."
    }
    $expectedVersion = [string]$policyComponent.expectedVersion
    $expectedVersionSource = [string]$policyComponent.expectedVersionSource
    if ($expectedVersionSource -eq 'runtime') {
        $expectedVersion = $runtimeVersion
    }
    if (-not [string]::IsNullOrWhiteSpace($expectedVersion) -and
        $expectedVersion -ne $PackageVersion) {
        throw "Package '$PackageId' version '$PackageVersion' does not match reviewed version '$expectedVersion'."
    }
    $policyLicense = [string]$policyComponent.license
    if ($allowedLicenses -notcontains $policyLicense) {
        throw "Package '$PackageId/$PackageVersion' uses unapproved policy license '$policyLicense'."
    }

    $nuspec = Get-ChildItem -LiteralPath $PackageRoot -File -Filter '*.nuspec' |
        Select-Object -First 1
    if ($null -eq $nuspec) {
        throw "Package '$PackageId/$PackageVersion' has no extracted nuspec."
    }
    [xml]$nuspecXml = Get-Content -LiteralPath $nuspec.FullName
    $metadataNode = $nuspecXml.SelectSingleNode(
        "//*[local-name()='metadata']")
    $canonicalId = [string]$metadataNode.SelectSingleNode(
        "*[local-name()='id']").InnerText
    $canonicalVersion = [string]$metadataNode.SelectSingleNode(
        "*[local-name()='version']").InnerText
    if ($canonicalId -ine $PackageId -or $canonicalVersion -ne $PackageVersion) {
        throw "Package cache path '$PackageId/$PackageVersion' does not match nuspec '$canonicalId/$canonicalVersion'."
    }

    $licenseNode = $metadataNode.SelectSingleNode("*[local-name()='license']")
    $licenseUrlNode = $metadataNode.SelectSingleNode("*[local-name()='licenseUrl']")
    $licenseType = $null
    $licenseValue = $null
    $licenseFileSha256 = $null
    $licenseFileText = $null
    if ($null -ne $licenseNode) {
        $licenseType = [string]$licenseNode.type
        $licenseValue = ([string]$licenseNode.InnerText).Trim()
        if ($licenseType -ieq 'file') {
            $licenseFilePath = Join-Path $PackageRoot $licenseValue
            if (-not [System.IO.File]::Exists($licenseFilePath)) {
                $licenseFile = Get-ChildItem -LiteralPath $PackageRoot -File -Recurse |
                    Where-Object {
                        $_.FullName.EndsWith(
                            $licenseValue.Replace('/', [System.IO.Path]::DirectorySeparatorChar),
                            [StringComparison]::OrdinalIgnoreCase)
                    } |
                    Select-Object -First 1
                if ($null -ne $licenseFile) {
                    $licenseFilePath = $licenseFile.FullName
                }
            }
            if (-not [System.IO.File]::Exists($licenseFilePath)) {
                throw "Package '$PackageId/$PackageVersion' references missing license file '$licenseValue'."
            }
            $licenseFileText = Get-Content -LiteralPath $licenseFilePath -Raw
            $licenseFileSha256 = Get-FileSha256 $licenseFilePath
        }
    }
    elseif ($null -ne $licenseUrlNode) {
        $licenseType = 'url'
        $licenseValue = ([string]$licenseUrlNode.InnerText).Trim()
    }
    else {
        throw "Package '$PackageId/$PackageVersion' has no nuspec license metadata."
    }

    $shaFile = Get-ChildItem -LiteralPath $PackageRoot -File -Filter '*.nupkg.sha512' |
        Select-Object -First 1
    if ($null -eq $shaFile) {
        throw "Package '$PackageId/$PackageVersion' has no NuGet SHA-512 evidence."
    }
    $packageSha512 = Convert-Base64Sha512ToHex (
        Get-Content -LiteralPath $shaFile.FullName -Raw)
    $nupkg = Get-ChildItem -LiteralPath $PackageRoot -File -Filter '*.nupkg' |
        Select-Object -First 1
    if ($null -ne $nupkg -and (Get-FileSha512 $nupkg.FullName) -ne $packageSha512) {
        throw "Package '$PackageId/$PackageVersion' nupkg does not match its SHA-512 evidence."
    }

    $repositoryNode = $metadataNode.SelectSingleNode("*[local-name()='repository']")
    $repositoryUrl = if ($null -ne $repositoryNode) {
        [string]$repositoryNode.url
    }
    else {
        [string]$policyComponent.source
    }
    $repositoryCommit = if ($null -ne $repositoryNode) {
        [string]$repositoryNode.commit
    }
    else {
        $null
    }

    $runtimeIdentity = "$canonicalId/$canonicalVersion"
    $isDotNetRuntimePack = @($metadata.runtimePackages) -contains $runtimeIdentity
    $mappingId = if ($isDotNetRuntimePack) {
        'exact-release-dotnet-bundle'
    }
    else {
        "package-license:$runtimeIdentity"
    }

    $record = [pscustomobject]@{
        key = $key
        id = $canonicalId
        version = $canonicalVersion
        identity = $runtimeIdentity
        root = $PackageRoot
        policy = $policyComponent
        policyLicense = $policyLicense
        packageSha512 = $packageSha512
        packageUrl = "https://api.nuget.org/v3-flatcontainer/$($canonicalId.ToLowerInvariant())/$canonicalVersion/$($canonicalId.ToLowerInvariant()).$canonicalVersion.nupkg"
        repositoryUrl = $repositoryUrl
        repositoryCommit = $repositoryCommit
        licenseType = $licenseType
        licenseValue = $licenseValue
        licenseFileSha256 = $licenseFileSha256
        licenseFileText = $licenseFileText
        mappingId = $mappingId
        isDotNetRuntimePack = $isDotNetRuntimePack
    }
    $packageRecords[$key] = $record
    return $record
}

function Resolve-PackageForSource([string]$SourcePath) {
    foreach ($folder in $packageFolders) {
        if (-not (Test-PathUnderRoot $SourcePath $folder)) {
            continue
        }
        $relative = Get-NormalizedRelativePath $folder $SourcePath
        $segments = $relative -split '/'
        if ($segments.Count -lt 3) {
            throw "Cannot identify package and version for '$SourcePath'."
        }
        $packageId = $segments[0]
        $packageVersion = $segments[1]
        $packageRoot = Resolve-PackageLocation $packageId $packageVersion
        if ([string]::IsNullOrWhiteSpace($packageRoot)) {
            throw "Cannot resolve package root for '$packageId/$packageVersion'."
        }
        return Get-OrCreatePackageRecord $packageId $packageVersion $packageRoot
    }
    return $null
}

foreach ($runtimeIdentity in @($metadata.runtimePackages)) {
    $parts = ([string]$runtimeIdentity) -split '/', 2
    if ($parts.Count -ne 2) {
        throw "Invalid runtime package identity '$runtimeIdentity'."
    }
    $runtimeRoot = Resolve-PackageLocation $parts[0] $parts[1]
    if ([string]::IsNullOrWhiteSpace($runtimeRoot)) {
        throw "Cannot resolve restored runtime package '$runtimeIdentity'."
    }
    Get-OrCreatePackageRecord $parts[0] $parts[1] $runtimeRoot | Out-Null
}

$bundleLines = @(
    Get-Content -LiteralPath $resolvedBundleManifest |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
if ($bundleLines.Count -eq 0) {
    throw 'The MSBuild FilesToBundle manifest is empty.'
}

$components = [System.Collections.Generic.List[object]]::new()
$applicationBundleEntryCount = 0
foreach ($line in $bundleLines) {
    $parts = $line -split '\|', 3
    $sourcePath = $parts[0].Trim()
    $relativePath = if ($parts.Count -gt 1) { $parts[1].Trim() } else { '' }
    $bundleRelativePath = if ($parts.Count -gt 2) { $parts[2].Trim() } else { '' }
    if ([string]::IsNullOrWhiteSpace($sourcePath) -or
        -not [System.IO.File]::Exists($sourcePath)) {
        throw "The FilesToBundle manifest references an unavailable source file: '$sourcePath'."
    }

    $package = Resolve-PackageForSource $sourcePath
    if ($null -eq $package) {
        $applicationBundleEntryCount++
        continue
    }
    if (-not [bool]$package.policy.shipped) {
        throw "Package '$($package.identity)' contributes to the published bundle but policy marks it as not shipped."
    }
    $assetPath = Get-NormalizedRelativePath $package.root $sourcePath
    $components.Add([ordered]@{
        disposition = 'embedded'
        outputPath = Get-OutputPath $sourcePath $relativePath $bundleRelativePath
        package = $package.identity
        packageAssetPath = $assetPath
        assetKind = Get-AssetKind $assetPath
        sha256 = Get-FileSha256 $sourcePath
        noticeMapping = $package.mappingId
    })
}

$assetCandidatesByName = @{}
foreach ($package in $packageRecords.Values) {
    foreach ($asset in Get-ChildItem -LiteralPath $package.root -File -Recurse) {
        $extension = $asset.Extension.ToLowerInvariant()
        if ($extension -notin @('.dll', '.exe', '.so', '.dylib')) {
            continue
        }
        $key = $asset.Name.ToLowerInvariant()
        if (-not $assetCandidatesByName.ContainsKey($key)) {
            $assetCandidatesByName[$key] = [System.Collections.Generic.List[object]]::new()
        }
        $assetCandidatesByName[$key].Add([pscustomobject]@{
            package = $package
            file = $asset
        })
    }
}

$looseBinaryFiles = @(
    Get-ChildItem -LiteralPath $resolvedPublish -File -Recurse |
        Where-Object {
            $_.Extension.ToLowerInvariant() -in @('.dll', '.exe', '.so', '.dylib') -and
            $_.Name -ne 'SightAdapt.exe'
        }
)
foreach ($looseFile in $looseBinaryFiles) {
    $key = $looseFile.Name.ToLowerInvariant()
    $looseSha256 = Get-FileSha256 $looseFile.FullName
    $matches = if ($assetCandidatesByName.ContainsKey($key)) {
        @($assetCandidatesByName[$key] | Where-Object {
            (Get-FileSha256 $_.file.FullName) -eq $looseSha256
        })
    }
    else {
        @()
    }
    if ($matches.Count -eq 0) {
        throw "Loose binary '$($looseFile.Name)' does not match any reviewed package asset."
    }
    if ($matches.Count -gt 1) {
        $identities = @($matches | ForEach-Object {
            "$($_.package.identity):$(Get-NormalizedRelativePath $_.package.root $_.file.FullName)"
        })
        throw "Loose binary '$($looseFile.Name)' has ambiguous package matches:`n$($identities -join "`n")"
    }
    $match = $matches[0]
    if (-not [bool]$match.package.policy.shipped) {
        throw "Loose binary '$($looseFile.Name)' maps to package '$($match.package.identity)' marked as not shipped."
    }
    $assetPath = Get-NormalizedRelativePath $match.package.root $match.file.FullName
    $components.Add([ordered]@{
        disposition = 'loose'
        outputPath = Get-NormalizedRelativePath $resolvedPublish $looseFile.FullName
        package = $match.package.identity
        packageAssetPath = $assetPath
        assetKind = Get-AssetKind $assetPath
        sha256 = $looseSha256
        noticeMapping = $match.package.mappingId
    })
}

$componentArray = @($components | Sort-Object disposition, outputPath, package)
if ($componentArray.Count -eq 0) {
    throw 'No package components were mapped from the published application.'
}
$requiredRuntimePacks = @(
    "Microsoft.NETCore.App.Runtime.$rid/$runtimeVersion",
    "Microsoft.WindowsDesktop.App.Runtime.$rid/$runtimeVersion"
)
foreach ($requiredPack in $requiredRuntimePacks) {
    if (@($componentArray | Where-Object {
        [string]$_.package -eq $requiredPack
    }).Count -eq 0) {
        throw "Required runtime pack '$requiredPack' has no mapped published component."
    }
}

$noticeMappings = [System.Collections.Generic.List[object]]::new()
$noticeMappings.Add([ordered]@{
    id = 'exact-release-dotnet-bundle'
    kind = 'official-dotnet-release-bundle'
    licenseFile = 'DOTNET-LICENSE-NOTICE.txt'
    licenseSha256 = ([string]$metadata.source.importedLicenseSha256).ToLowerInvariant()
    thirdPartyNoticesFile = 'THIRD-PARTY-NOTICES.txt'
    thirdPartyNoticesSha256 = ([string]$metadata.source.importedThirdPartyNoticesSha256).ToLowerInvariant()
})

$additionalSections = [System.Collections.Generic.List[string]]::new()
$publishedPackageEvidence = [System.Collections.Generic.List[object]]::new()
foreach ($package in ($packageRecords.Values | Sort-Object identity)) {
    $packageComponents = @($componentArray | Where-Object {
        [string]$_.package -eq $package.identity
    })
    if ($packageComponents.Count -eq 0) {
        continue
    }

    $publishedPackageEvidence.Add([ordered]@{
        package = $package.identity
        packageUrl = $package.packageUrl
        packageSha512 = $package.packageSha512
        repositoryUrl = $package.repositoryUrl
        repositoryCommit = $package.repositoryCommit
        policyLicense = $package.policyLicense
        publishedComponentCount = $packageComponents.Count
    })

    if ($package.isDotNetRuntimePack) {
        continue
    }

    $licenseFileLine = if ([string]::IsNullOrWhiteSpace($package.licenseFileSha256)) {
        'NuGet license file SHA-256: none'
    }
    else {
        "NuGet license file SHA-256: $($package.licenseFileSha256)"
    }
    $section = @"

================================================================================
SIGHTADAPT EXACT PACKAGE NOTICE

Package: $($package.identity)
Package URL: $($package.packageUrl)
Package SHA-512: $($package.packageSha512)
Policy license: $($package.policyLicense)
NuGet license type: $($package.licenseType)
NuGet license value: $($package.licenseValue)
$licenseFileLine
Repository: $($package.repositoryUrl)
Repository commit: $($package.repositoryCommit)
Mapped components: $($packageComponents.Count)

This exact package contributes components embedded in or shipped with SightAdapt.
The package identity, package hash, component paths and component hashes are
recorded in DOTNET-NOTICE-METADATA.json.
"@
    if (-not [string]::IsNullOrWhiteSpace($package.licenseFileText)) {
        $section += @"

Exact package license file follows without substantive modification:

--------------------------------------------------------------------------------
$($package.licenseFileText)
"@
    }
    $sectionSha256 = Get-TextSha256 $section
    $additionalSections.Add($section)
    $noticeMappings.Add([ordered]@{
        id = $package.mappingId
        kind = 'exact-nuget-package-license'
        package = $package.identity
        packageSha512 = $package.packageSha512
        policyLicense = $package.policyLicense
        nuspecLicenseType = $package.licenseType
        nuspecLicenseValue = $package.licenseValue
        licenseFileSha256 = $package.licenseFileSha256
        noticeFile = 'THIRD-PARTY-NOTICES.txt'
        noticeSectionSha256 = $sectionSha256
    })
}

if ($additionalSections.Count -gt 0) {
    Add-Content -LiteralPath $thirdPartyNoticesPath `
        -Value ($additionalSections -join '') `
        -Encoding utf8
}

$embeddedCount = @($componentArray | Where-Object {
    [string]$_.disposition -eq 'embedded'
}).Count
$looseCount = @($componentArray | Where-Object {
    [string]$_.disposition -eq 'loose'
}).Count
$metadata.schemaVersion = 2
$metadata | Add-Member -NotePropertyName componentCoverage -NotePropertyValue ([ordered]@{
    method = 'MSBuild PrepareForBundle/FilesToBundle plus SHA-256 matching of loose binaries to exact restored package assets'
    bundleManifestSha256 = Get-FileSha256 $resolvedBundleManifest
    bundleEntryCount = $bundleLines.Count
    applicationBundleEntryCount = $applicationBundleEntryCount
    runtimeComponentCount = $componentArray.Count
    embeddedRuntimeComponentCount = $embeddedCount
    looseRuntimeComponentCount = $looseCount
    unmappedExternalComponentCount = 0
    noticeMappings = @($noticeMappings)
    packages = @($publishedPackageEvidence)
    components = $componentArray
}) -Force

[System.IO.File]::WriteAllText(
    $metadataPath,
    ($metadata | ConvertTo-Json -Depth 14) + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Host (
    "Generated exact notice coverage for {0} published package components ({1} embedded, {2} loose) across {3} packages." -f
    $componentArray.Count,
    $embeddedCount,
    $looseCount,
    $publishedPackageEvidence.Count)
