[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory,

    [string]$AssetsPath,

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

$resolvedProps = (Resolve-Path -LiteralPath $PropsPath).Path
$resolvedAssets = (Resolve-Path -LiteralPath $AssetsPath).Path
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
$packageKeys = @(
    $assets.libraries.PSObject.Properties |
        Where-Object { [string]$_.Value.type -eq 'package' } |
        ForEach-Object { [string]$_.Name }
)

$requiredRuntimePacks = @(
    "Microsoft.NETCore.App.Runtime.$rid/$runtimeVersion",
    "Microsoft.WindowsDesktop.App.Runtime.$rid/$runtimeVersion"
)
foreach ($requiredPack in $requiredRuntimePacks) {
    if (-not ($packageKeys -contains $requiredPack)) {
        throw "The restored assets do not contain required runtime pack '$requiredPack'."
    }
}

$reviewedRuntimePackagePatterns = @(
    '^Microsoft\.NETCore\.App\.Host\.',
    '^Microsoft\.NETCore\.App\.Runtime\.',
    '^Microsoft\.WindowsDesktop\.App\.Runtime\.',
    '^runtime\.[^.]+-[^.]+\.Microsoft\.NETCore\.DotNet'
)
$reviewedRuntimePackageNames = @(
    "Microsoft.NETCore.App.Host.$rid/$runtimeVersion",
    "Microsoft.NETCore.App.Runtime.$rid/$runtimeVersion",
    "Microsoft.WindowsDesktop.App.Runtime.$rid/$runtimeVersion",
    "runtime.$rid.Microsoft.NETCore.DotNetAppHost/$runtimeVersion",
    "runtime.$rid.Microsoft.NETCore.DotNetHost/$runtimeVersion",
    "runtime.$rid.Microsoft.NETCore.DotNetHostPolicy/$runtimeVersion",
    "runtime.$rid.Microsoft.NETCore.DotNetHostResolver/$runtimeVersion"
)
$runtimePackages = @(
    $packageKeys | Where-Object {
        $candidate = $_
        $reviewedRuntimePackagePatterns | Where-Object { $candidate -match $_ }
    }
)
$unknownRuntimePackages = @(
    $runtimePackages | Where-Object { $reviewedRuntimePackageNames -notcontains $_ }
)
if ($unknownRuntimePackages.Count -gt 0) {
    throw "Unreviewed runtime packages were restored:`n$($unknownRuntimePackages -join "`n")"
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

$desktopPackage = @($release.windowsdesktop.files | Where-Object {
    [string]$_.rid -eq $rid -and
    [string]$_.name -like '*.zip'
}) | Select-Object -First 1
if ($null -eq $desktopPackage) {
    throw "No official Windows Desktop Runtime ZIP was found for $runtimeVersion/$rid."
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-dotnet-notices-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null
$packagePath = Join-Path $tempRoot ([string]$desktopPackage.name)

try {
    Invoke-WebRequest -Uri ([string]$desktopPackage.url) -OutFile $packagePath
    $actualPackageHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA512).Hash
    $expectedPackageHash = ([string]$desktopPackage.hash).ToUpperInvariant()
    if ($actualPackageHash -ne $expectedPackageHash) {
        throw "Windows Desktop Runtime package SHA-512 mismatch. Expected $expectedPackageHash; received $actualPackageHash."
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
            throw 'The official Windows Desktop Runtime archive does not contain LICENSE.txt and ThirdPartyNotices.txt.'
        }

        function Read-ZipText([System.IO.Compression.ZipArchiveEntry]$Entry) {
            $stream = $Entry.Open()
            try {
                $reader = [System.IO.StreamReader]::new($stream, [System.Text.Encoding]::UTF8, $true)
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
    [System.IO.File]::WriteAllText($licenseSourcePath, $licenseText, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($noticeSourcePath, $noticeText, [System.Text.UTF8Encoding]::new($false))
    $licenseSha256 = (Get-FileHash -LiteralPath $licenseSourcePath -Algorithm SHA256).Hash
    $noticeSha256 = (Get-FileHash -LiteralPath $noticeSourcePath -Algorithm SHA256).Hash

    $generatedAt = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    $thirdPartyHeader = @"
SIGHTADAPT EXACT-VERSION THIRD-PARTY NOTICES

Generated from the official Microsoft .NET Windows Desktop Runtime distribution used by this build.

SightAdapt product version: $productVersion
.NET SDK: $sdkVersion
.NET runtime and Windows Desktop Runtime: $runtimeVersion
Runtime identifier: $rid
Publish mode: $publishMode
Generated at (UTC): $generatedAt
Release metadata: $metadataUrl
Source package: $($desktopPackage.url)
Source package SHA-512: $actualPackageHash
Imported ThirdPartyNotices.txt SHA-256: $noticeSha256

The notice text below is imported without substantive modification from the exact official runtime archive.

================================================================================

"@
    $licenseHeader = @"
MICROSOFT .NET REDISTRIBUTION LICENSE NOTICE

SightAdapt redistributes components from the exact Microsoft .NET Windows Desktop Runtime package identified below as part of a self-contained SightAdapt application. Microsoft components are not offered as a standalone product and remain governed by Microsoft's terms, separate from SightAdapt's MIT License.

SightAdapt product version: $productVersion
.NET SDK: $sdkVersion
.NET runtime and Windows Desktop Runtime: $runtimeVersion
Runtime identifier: $rid
Publish mode: $publishMode
Generated at (UTC): $generatedAt
Release metadata: $metadataUrl
Source package: $($desktopPackage.url)
Source package SHA-512: $actualPackageHash
Imported LICENSE.txt SHA-256: $licenseSha256

The license text below is imported without substantive modification from the exact official runtime archive.

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

    $metadata = [ordered]@{
        schemaVersion = 1
        productVersion = $productVersion
        sdkVersion = $sdkVersion
        runtimeVersion = $runtimeVersion
        runtimeIdentifier = $rid
        publishMode = $publishMode
        generatedAtUtc = $generatedAt
        runtimePackages = @($runtimePackages | Sort-Object)
        source = [ordered]@{
            releaseMetadataUrl = $metadataUrl
            releaseVersion = [string]$release.'release-version'
            releaseDate = [string]$release.'release-date'
            packageName = [string]$desktopPackage.name
            packageUrl = [string]$desktopPackage.url
            packageSha512 = $actualPackageHash
            importedLicenseSha256 = $licenseSha256
            importedThirdPartyNoticesSha256 = $noticeSha256
        }
    }
    $metadataJson = $metadata | ConvertTo-Json -Depth 8
    [System.IO.File]::WriteAllText(
        (Join-Path $resolvedPublish 'DOTNET-NOTICE-METADATA.json'),
        $metadataJson + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host "Generated exact-version .NET notices for SDK $sdkVersion and runtime $runtimeVersion ($rid)."
