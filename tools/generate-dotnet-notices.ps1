[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory,

    [string]$AssetsPath,

    [string]$BundleManifestPath,

    [string]$PropsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($PropsPath)) {
    $PropsPath = Join-Path $root 'Directory.Build.props'
}
if ([string]::IsNullOrWhiteSpace($AssetsPath)) {
    $AssetsPath = Join-Path $root 'src\SightAdapt\obj\project.assets.json'
}
if ([string]::IsNullOrWhiteSpace($BundleManifestPath)) {
    $BundleManifestPath = Join-Path $root 'artifacts\dotnet-files-to-bundle.tsv'
}

function Get-FileSha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-FileSha512([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA512).Hash.ToLowerInvariant()
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

function Get-RuntimeAssetKind([string]$PackageAssetPath) {
    $normalized = $PackageAssetPath.Replace('\', '/').ToLowerInvariant()
    $extension = [System.IO.Path]::GetExtension($normalized)
    if ($normalized -match '(^|/)native/' -or
        $extension -in @('.exe', '.so', '.dylib')) {
        return 'native'
    }
    if ($extension -eq '.dll' -and $normalized -match '(^|/)lib/') {
        return 'managed'
    }
    return 'runtime-content'
}

function Get-BundleOutputPath(
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

$resolvedProps = (Resolve-Path -LiteralPath $PropsPath).Path
$resolvedAssets = (Resolve-Path -LiteralPath $AssetsPath).Path
$resolvedBundleManifest = (Resolve-Path -LiteralPath $BundleManifestPath).Path
$resolvedPublish = [System.IO.Path]::GetFullPath($PublishDirectory)
[System.IO.Directory]::CreateDirectory($resolvedPublish) | Out-Null

[xml]$props = Get-Content -LiteralPath $resolvedProps
$group = $props.Project.PropertyGroup
$sdkVersion = [string]$group.SightAdaptDotNetSdkVersion
$runtimeVersion = [string]$group.SightAdaptDotNetRuntimeVersion
$rid = [string]$group.SightAdaptRuntimeIdentifier
$publishMode = [string]$group.SightAdaptPublishMode
$productVersion = [string]$group.SightAdaptProductVersion
$metadataUrl = [string]$group.SightAdaptDotNetReleaseMetadataUrl

$requiredValues = @{
    SightAdaptDotNetSdkVersion = $sdkVersion
    SightAdaptDotNetRuntimeVersion = $runtimeVersion
    SightAdaptRuntimeIdentifier = $rid
    SightAdaptPublishMode = $publishMode
    SightAdaptProductVersion = $productVersion
    SightAdaptDotNetReleaseMetadataUrl = $metadataUrl
}
foreach ($item in $requiredValues.GetEnumerator()) {
    if ([string]::IsNullOrWhiteSpace([string]$item.Value)) {
        throw "Directory.Build.props is missing $($item.Key)."
    }
}

$globalJsonPath = Join-Path $root 'global.json'
$globalJson = Get-Content -LiteralPath $globalJsonPath -Raw | ConvertFrom-Json
if ([string]$globalJson.sdk.version -ne $sdkVersion -or
    [string]$globalJson.sdk.rollForward -ne 'disable') {
    throw 'global.json must pin the exact SightAdapt .NET SDK with rollForward=disable.'
}

$actualSdkVersion = (& dotnet --version).Trim()
if ($LASTEXITCODE -ne 0 -or $actualSdkVersion -ne $sdkVersion) {
    throw "Expected .NET SDK $sdkVersion but dotnet --version returned '$actualSdkVersion'."
}

$assets = Get-Content -LiteralPath $resolvedAssets -Raw | ConvertFrom-Json
$assetsFramework = @(
    $assets.project.frameworks.PSObject.Properties
) | Select-Object -First 1
if ($null -eq $assetsFramework) {
    throw 'The restored assets do not contain project framework metadata.'
}

$downloadDependencies = @($assetsFramework.Value.downloadDependencies)
if ($downloadDependencies.Count -eq 0) {
    throw 'The restored assets do not record framework download dependencies.'
}

$reviewedRuntimePackNames = @(
    "Microsoft.AspNetCore.App.Runtime.$rid",
    "Microsoft.NETCore.App.Runtime.$rid",
    "Microsoft.WindowsDesktop.App.Runtime.$rid"
)
$runtimePackEntries = @(
    $downloadDependencies | Where-Object {
        ([string]$_.name).EndsWith(
            ".Runtime.$rid",
            [StringComparison]::OrdinalIgnoreCase)
    }
)
$runtimePackages = @(
    foreach ($entry in $runtimePackEntries) {
        $name = [string]$entry.name
        if ($reviewedRuntimePackNames -notcontains $name) {
            throw "Unreviewed runtime pack '$name' was selected by restore."
        }

        $versionRange = [string]$entry.version
        $match = [regex]::Match(
            $versionRange,
            '^\[([^,\]]+),\s*\1\]$')
        if (-not $match.Success) {
            throw "Runtime pack '$name' does not use an exact version range: '$versionRange'."
        }

        "$name/$($match.Groups[1].Value)"
    }
)

$requiredRuntimePacks = @(
    "Microsoft.NETCore.App.Runtime.$rid/$runtimeVersion",
    "Microsoft.WindowsDesktop.App.Runtime.$rid/$runtimeVersion"
)
foreach ($requiredPack in $requiredRuntimePacks) {
    if (-not ($runtimePackages -contains $requiredPack)) {
        throw "The restored assets do not contain required runtime pack '$requiredPack'. Restored packs:`n$($runtimePackages -join "`n")"
    }
}

$wrongVersionPacks = @(
    $runtimePackages | Where-Object {
        ($_ -split '/', 2)[1] -ne $runtimeVersion
    }
)
if ($wrongVersionPacks.Count -gt 0) {
    throw "Runtime packs do not match pinned runtime ${runtimeVersion}:`n$($wrongVersionPacks -join "`n")"
}

$packageFolders = @(
    $assets.packageFolders.PSObject.Properties |
        ForEach-Object { [string]$_.Name }
)
if ($packageFolders.Count -eq 0) {
    throw 'The restored assets do not record NuGet package folders.'
}

$runtimePackRecords = @(
    foreach ($runtimePackage in ($runtimePackages | Sort-Object)) {
        $parts = $runtimePackage -split '/', 2
        $packageId = $parts[0]
        $packageVersion = $parts[1]
        $packageRoot = $null
        foreach ($packageFolder in $packageFolders) {
            $candidate = Join-Path (
                Join-Path $packageFolder $packageId.ToLowerInvariant()) $packageVersion
            if ([System.IO.Directory]::Exists($candidate)) {
                $packageRoot = (Resolve-Path -LiteralPath $candidate).Path
                break
            }
        }
        if ([string]::IsNullOrWhiteSpace($packageRoot)) {
            throw "Runtime pack '$runtimePackage' is not available in the restored NuGet package folders."
        }

        $nupkg = Get-ChildItem -LiteralPath $packageRoot -File -Filter '*.nupkg' |
            Select-Object -First 1
        $shaFile = Get-ChildItem -LiteralPath $packageRoot -File -Filter '*.nupkg.sha512' |
            Select-Object -First 1
        if ($null -eq $nupkg -and $null -eq $shaFile) {
            throw "Runtime pack '$runtimePackage' has no NuGet package or SHA-512 evidence."
        }

        $actualPackageSha512 = if ($null -ne $nupkg) {
            Get-FileSha512 $nupkg.FullName
        }
        else {
            $null
        }
        $declaredPackageSha512 = if ($null -ne $shaFile) {
            Convert-Base64Sha512ToHex (
                Get-Content -LiteralPath $shaFile.FullName -Raw)
        }
        else {
            $actualPackageSha512
        }
        if ($null -ne $actualPackageSha512 -and
            $actualPackageSha512 -ne $declaredPackageSha512) {
            throw "Runtime pack '$runtimePackage' NuGet package SHA-512 does not match its restore-cache evidence."
        }

        $repositoryUrl = $null
        $repositoryCommit = $null
        $nuspec = Get-ChildItem -LiteralPath $packageRoot -File -Filter '*.nuspec' |
            Select-Object -First 1
        if ($null -ne $nuspec) {
            [xml]$nuspecXml = Get-Content -LiteralPath $nuspec.FullName
            $repositoryNode = $nuspecXml.SelectSingleNode(
                "//*[local-name()='metadata']/*[local-name()='repository']")
            if ($null -ne $repositoryNode) {
                $repositoryUrl = [string]$repositoryNode.url
                $repositoryCommit = [string]$repositoryNode.commit
            }
        }

        [pscustomobject]@{
            package = $runtimePackage
            id = $packageId
            version = $packageVersion
            root = $packageRoot
            packageSha512 = $declaredPackageSha512
            packageUrl = "https://api.nuget.org/v3-flatcontainer/$($packageId.ToLowerInvariant())/$packageVersion/$($packageId.ToLowerInvariant()).$packageVersion.nupkg"
            repositoryUrl = $repositoryUrl
            repositoryCommit = $repositoryCommit
        }
    }
)

$bundleLines = @(
    Get-Content -LiteralPath $resolvedBundleManifest |
        Where-Object { -not [string]::IsNullOrWhiteSpace($_) }
)
if ($bundleLines.Count -eq 0) {
    throw 'The MSBuild FilesToBundle manifest is empty.'
}
$bundleManifestSha256 = Get-FileSha256 $resolvedBundleManifest
$components = [System.Collections.Generic.List[object]]::new()
$applicationBundleEntryCount = 0
$unmappedExternalComponents = [System.Collections.Generic.List[string]]::new()

foreach ($line in $bundleLines) {
    $parts = $line -split '\|', 3
    $sourcePath = $parts[0].Trim()
    $relativePath = if ($parts.Count -gt 1) { $parts[1].Trim() } else { '' }
    $bundleRelativePath = if ($parts.Count -gt 2) { $parts[2].Trim() } else { '' }
    if ([string]::IsNullOrWhiteSpace($sourcePath) -or
        -not [System.IO.File]::Exists($sourcePath)) {
        throw "The FilesToBundle manifest references an unavailable source file: '$sourcePath'."
    }

    $matchedPacks = @(
        $runtimePackRecords | Where-Object {
            Test-PathUnderRoot $sourcePath $_.root
        }
    )
    if ($matchedPacks.Count -gt 1) {
        throw "Bundled file '$sourcePath' maps to more than one runtime pack."
    }
    if ($matchedPacks.Count -eq 1) {
        $pack = $matchedPacks[0]
        $assetPath = Get-NormalizedRelativePath $pack.root $sourcePath
        $components.Add([ordered]@{
            disposition = 'embedded'
            outputPath = Get-BundleOutputPath `
                $sourcePath $relativePath $bundleRelativePath
            package = $pack.package
            packageAssetPath = $assetPath
            assetKind = Get-RuntimeAssetKind $assetPath
            sha256 = Get-FileSha256 $sourcePath
            noticeMapping = 'exact-release-dotnet-bundle'
        })
        continue
    }

    $fromPackageCache = $false
    foreach ($packageFolder in $packageFolders) {
        if (Test-PathUnderRoot $sourcePath $packageFolder) {
            $fromPackageCache = $true
            break
        }
    }
    if ($fromPackageCache) {
        $unmappedExternalComponents.Add(
            "Bundled package-cache file is not mapped to a reviewed runtime pack: $sourcePath")
    }
    else {
        $applicationBundleEntryCount++
    }
}

$assetCandidatesByName = @{}
foreach ($pack in $runtimePackRecords) {
    $runtimeAssetsRoot = Join-Path $pack.root 'runtimes'
    if (-not [System.IO.Directory]::Exists($runtimeAssetsRoot)) {
        continue
    }
    foreach ($asset in Get-ChildItem -LiteralPath $runtimeAssetsRoot -File -Recurse) {
        $key = $asset.Name.ToLowerInvariant()
        if (-not $assetCandidatesByName.ContainsKey($key)) {
            $assetCandidatesByName[$key] = [System.Collections.Generic.List[object]]::new()
        }
        $assetCandidatesByName[$key].Add([pscustomobject]@{
            pack = $pack
            file = $asset
        })
    }
}

$binaryExtensions = @('.dll', '.so', '.dylib', '.exe')
$looseBinaryFiles = @(
    Get-ChildItem -LiteralPath $resolvedPublish -File -Recurse |
        Where-Object {
            $binaryExtensions -contains $_.Extension.ToLowerInvariant() -and
            $_.Name -ne 'SightAdapt.exe'
        }
)
foreach ($looseFile in $looseBinaryFiles) {
    $key = $looseFile.Name.ToLowerInvariant()
    $looseSha256 = Get-FileSha256 $looseFile.FullName
    $matches = @()
    if ($assetCandidatesByName.ContainsKey($key)) {
        $matches = @(
            $assetCandidatesByName[$key] | Where-Object {
                (Get-FileSha256 $_.file.FullName) -eq $looseSha256
            }
        )
    }
    if ($matches.Count -eq 0) {
        $unmappedExternalComponents.Add(
            "Loose binary '$($looseFile.Name)' does not match an asset in the reviewed runtime packs.")
        continue
    }
    if ($matches.Count -gt 1) {
        $identities = @($matches | ForEach-Object {
            "$($_.pack.package):$(Get-NormalizedRelativePath $_.pack.root $_.file.FullName)"
        })
        throw "Loose binary '$($looseFile.Name)' has ambiguous runtime-pack matches:`n$($identities -join "`n")"
    }

    $match = $matches[0]
    $assetPath = Get-NormalizedRelativePath $match.pack.root $match.file.FullName
    $components.Add([ordered]@{
        disposition = 'loose'
        outputPath = Get-NormalizedRelativePath $resolvedPublish $looseFile.FullName
        package = $match.pack.package
        packageAssetPath = $assetPath
        assetKind = Get-RuntimeAssetKind $assetPath
        sha256 = $looseSha256
        noticeMapping = 'exact-release-dotnet-bundle'
    })
}

if ($unmappedExternalComponents.Count -gt 0) {
    throw "Runtime component notice mapping failed:`n$($unmappedExternalComponents -join "`n")"
}

$componentArray = @($components | Sort-Object disposition, outputPath, package)
if ($componentArray.Count -eq 0) {
    throw 'No runtime components were mapped from FilesToBundle or the final publish directory.'
}
foreach ($requiredPack in $requiredRuntimePacks) {
    if (-not @($componentArray | Where-Object {
        [string]$_.package -eq $requiredPack
    })) {
        throw "Required runtime pack '$requiredPack' has no mapped published component."
    }
}

$releaseMetadata = Invoke-RestMethod -Uri $metadataUrl -Method Get
$release = @($releaseMetadata.releases | Where-Object {
    [string]$_.'release-version' -eq $runtimeVersion
}) | Select-Object -First 1
if ($null -eq $release) {
    throw "The official .NET release metadata does not contain runtime $runtimeVersion."
}
if ([string]$release.sdk.version -ne $sdkVersion) {
    throw "Runtime $runtimeVersion maps to SDK '$($release.sdk.version)', not pinned SDK $sdkVersion."
}
if ([string]$release.windowsdesktop.version -ne $runtimeVersion) {
    throw "Windows Desktop Runtime metadata does not match $runtimeVersion."
}

$legalSourcePackage = @($release.sdk.files | Where-Object {
    [string]$_.rid -eq $rid -and
    [string]$_.name -like '*.zip'
}) | Select-Object -First 1
if ($null -eq $legalSourcePackage) {
    throw "No official .NET SDK ZIP was found for SDK $sdkVersion/$rid."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-dotnet-notices-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$packagePath = Join-Path $tempRoot ([string]$legalSourcePackage.name)

try {
    Invoke-WebRequest -Uri ([string]$legalSourcePackage.url) -OutFile $packagePath
    $actualPackageHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA512).Hash
    $expectedPackageHash = ([string]$legalSourcePackage.hash).ToUpperInvariant()
    if ($actualPackageHash -ne $expectedPackageHash) {
        throw "Official .NET SDK package SHA-512 mismatch. Expected $expectedPackageHash; received $actualPackageHash."
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $archive = [System.IO.Compression.ZipFile]::OpenRead($packagePath)
    try {
        $licenseEntry = @($archive.Entries | Where-Object {
            [System.IO.Path]::GetFileName($_.FullName) -ieq 'LICENSE.txt'
        }) | Select-Object -First 1
        $noticeEntry = @($archive.Entries | Where-Object {
            [System.IO.Path]::GetFileName($_.FullName) -ieq 'ThirdPartyNotices.txt'
        }) | Select-Object -First 1
        if ($null -eq $licenseEntry -or $null -eq $noticeEntry) {
            throw 'The official .NET SDK archive does not contain LICENSE.txt and ThirdPartyNotices.txt.'
        }

        function Read-ZipText([System.IO.Compression.ZipArchiveEntry]$Entry) {
            $stream = $Entry.Open()
            try {
                $reader = [System.IO.StreamReader]::new(
                    $stream,
                    [System.Text.Encoding]::UTF8,
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

        $licenseText = Read-ZipText $licenseEntry
        $noticeText = Read-ZipText $noticeEntry
    }
    finally {
        $archive.Dispose()
    }

    $licenseSourcePath = Join-Path $tempRoot 'LICENSE.txt'
    $noticeSourcePath = Join-Path $tempRoot 'ThirdPartyNotices.txt'
    [System.IO.File]::WriteAllText(
        $licenseSourcePath,
        $licenseText,
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        $noticeSourcePath,
        $noticeText,
        [System.Text.UTF8Encoding]::new($false))
    $licenseSha256 = (Get-FileHash -LiteralPath $licenseSourcePath -Algorithm SHA256).Hash
    $noticeSha256 = (Get-FileHash -LiteralPath $noticeSourcePath -Algorithm SHA256).Hash

    $generatedAt = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    $embeddedCount = @($componentArray | Where-Object {
        [string]$_.disposition -eq 'embedded'
    }).Count
    $looseCount = @($componentArray | Where-Object {
        [string]$_.disposition -eq 'loose'
    }).Count

    $thirdPartyHeader = @"
SIGHTADAPT EXACT-VERSION THIRD-PARTY NOTICES

Generated from the official Microsoft .NET SDK distribution associated with the exact runtime packs and component inventory used by this build.

SightAdapt product version: $productVersion
.NET SDK: $sdkVersion
.NET runtime and Windows Desktop Runtime: $runtimeVersion
Runtime identifier: $rid
Publish mode: $publishMode
Mapped runtime components: $($componentArray.Count) ($embeddedCount embedded, $looseCount loose)
Generated at (UTC): $generatedAt
Release metadata: $metadataUrl
Source package: $($legalSourcePackage.url)
Source package SHA-512: $actualPackageHash
Imported ThirdPartyNotices.txt SHA-256: $noticeSha256
Component coverage evidence: DOTNET-NOTICE-METADATA.json

The notice text below is imported without substantive modification from the exact official SDK archive.

================================================================================

"@
    $licenseHeader = @"
MICROSOFT .NET REDISTRIBUTION LICENSE NOTICE

SightAdapt redistributes components from the exact Microsoft .NET runtime packs identified below as part of a self-contained SightAdapt application. The authoritative license text is imported from the matching official SDK archive. Microsoft components are not offered as a standalone product and remain governed by Microsoft's terms, separate from SightAdapt's MIT License.

SightAdapt product version: $productVersion
.NET SDK: $sdkVersion
.NET runtime and Windows Desktop Runtime: $runtimeVersion
Runtime identifier: $rid
Publish mode: $publishMode
Mapped runtime components: $($componentArray.Count) ($embeddedCount embedded, $looseCount loose)
Generated at (UTC): $generatedAt
Release metadata: $metadataUrl
Source package: $($legalSourcePackage.url)
Source package SHA-512: $actualPackageHash
Imported LICENSE.txt SHA-256: $licenseSha256
Component coverage evidence: DOTNET-NOTICE-METADATA.json

The license text below is imported without substantive modification from the exact official SDK archive.

================================================================================

"@

    [System.IO.File]::WriteAllText(
        (Join-Path $resolvedPublish 'THIRD-PARTY-NOTICES.txt'),
        $thirdPartyHeader + $noticeText,
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText(
        (Join-Path $resolvedPublish 'DOTNET-LICENSE-NOTICE.txt'),
        $licenseHeader + $licenseText,
        [System.Text.UTF8Encoding]::new($false))

    $runtimePackEvidence = @(
        foreach ($pack in $runtimePackRecords) {
            $packComponents = @($componentArray | Where-Object {
                [string]$_.package -eq [string]$pack.package
            })
            [ordered]@{
                package = $pack.package
                packageUrl = $pack.packageUrl
                packageSha512 = $pack.packageSha512
                repositoryUrl = $pack.repositoryUrl
                repositoryCommit = $pack.repositoryCommit
                publishedComponentCount = $packComponents.Count
            }
        }
    )

    $metadata = [ordered]@{
        schemaVersion = 2
        productVersion = $productVersion
        sdkVersion = $sdkVersion
        runtimeVersion = $runtimeVersion
        runtimeIdentifier = $rid
        publishMode = $publishMode
        generatedAtUtc = $generatedAt
        assetsFramework = [string]$assetsFramework.Name
        runtimePackages = @($runtimePackages | Sort-Object)
        source = [ordered]@{
            releaseMetadataUrl = $metadataUrl
            releaseVersion = [string]$release.'release-version'
            releaseDate = [string]$release.'release-date'
            packageName = [string]$legalSourcePackage.name
            packageUrl = [string]$legalSourcePackage.url
            packageSha512 = $actualPackageHash
            importedLicenseSha256 = $licenseSha256
            importedThirdPartyNoticesSha256 = $noticeSha256
        }
        componentCoverage = [ordered]@{
            method = 'MSBuild PrepareForBundle/FilesToBundle plus SHA-256 matching of loose runtime binaries to exact restored runtime packs'
            bundleManifestSha256 = $bundleManifestSha256
            bundleEntryCount = $bundleLines.Count
            applicationBundleEntryCount = $applicationBundleEntryCount
            runtimeComponentCount = $componentArray.Count
            embeddedRuntimeComponentCount = $embeddedCount
            looseRuntimeComponentCount = $looseCount
            unmappedExternalComponentCount = 0
            noticeMapping = [ordered]@{
                id = 'exact-release-dotnet-bundle'
                licenseFile = 'DOTNET-LICENSE-NOTICE.txt'
                licenseSha256 = $licenseSha256
                thirdPartyNoticesFile = 'THIRD-PARTY-NOTICES.txt'
                thirdPartyNoticesSha256 = $noticeSha256
            }
            runtimePacks = $runtimePackEvidence
            components = $componentArray
        }
    }
    $metadataJson = $metadata | ConvertTo-Json -Depth 12
    [System.IO.File]::WriteAllText(
        (Join-Path $resolvedPublish 'DOTNET-NOTICE-METADATA.json'),
        $metadataJson + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host (
    "Generated exact-version .NET notices for SDK {0} and runtime {1} ({2}); mapped {3} runtime components." -f
    $sdkVersion,
    $runtimeVersion,
    $rid,
    $componentArray.Count)
