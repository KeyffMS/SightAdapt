[CmdletBinding()]
param(
    [string]$PublishDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

$root = Split-Path -Parent $PSScriptRoot
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-negative-package-' + [Guid]::NewGuid().ToString('N'))

function Assert-PackageRejected(
    [string]$ArchivePath,
    [string]$Scenario) {
    $failedAsExpected = $false
    try {
        & (Join-Path $PSScriptRoot 'test-release-package.ps1') `
            -ArchivePath $ArchivePath
    }
    catch {
        $failedAsExpected = $true
        Write-Host "$Scenario was rejected as expected: $($_.Exception.Message)"
    }

    if (-not $failedAsExpected) {
        throw "$Scenario unexpectedly passed validation."
    }
}

try {
    $incompleteDirectory = Join-Path $tempRoot 'incomplete'
    $incompleteArchive = Join-Path $tempRoot 'incomplete.zip'
    [System.IO.Directory]::CreateDirectory($incompleteDirectory) | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $incompleteDirectory 'SightAdapt.exe'),
        'deliberately incomplete package',
        [System.Text.UTF8Encoding]::new($false))
    [System.IO.Compression.ZipFile]::CreateFromDirectory(
        $incompleteDirectory,
        $incompleteArchive)
    Assert-PackageRejected $incompleteArchive 'The deliberately incomplete package'

    if (-not [string]::IsNullOrWhiteSpace($PublishDirectory)) {
        $resolvedPublish = (Resolve-Path -LiteralPath $PublishDirectory).Path

        $staleDirectory = Join-Path $tempRoot 'stale-redistribution-notice'
        $staleArchive = Join-Path $tempRoot 'stale-redistribution-notice.zip'
        [System.IO.Directory]::CreateDirectory($staleDirectory) | Out-Null
        Copy-Item -Path (Join-Path $resolvedPublish '*') `
            -Destination $staleDirectory `
            -Recurse `
            -Force

        $noticePath = Join-Path $staleDirectory 'MICROSOFT-DOTNET-REDISTRIBUTION.txt'
        if (-not [System.IO.File]::Exists($noticePath)) {
            throw 'The published redistribution notice is unavailable for the stale-notice test.'
        }

        [xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
        $runtimeVersion = [string]$props.Project.PropertyGroup.SightAdaptDotNetRuntimeVersion
        $currentHeader = "Runtime version: $runtimeVersion"
        $notice = Get-Content -LiteralPath $noticePath -Raw
        if (-not $notice.Contains($currentHeader, [StringComparison]::Ordinal)) {
            throw "The stale-notice test could not locate '$currentHeader'."
        }
        $mutated = $notice.Replace(
            $currentHeader,
            'Runtime version: 0.0.0-stale')
        [System.IO.File]::WriteAllText(
            $noticePath,
            $mutated,
            [System.Text.UTF8Encoding]::new($false))

        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $staleDirectory,
            $staleArchive)
        Assert-PackageRejected $staleArchive 'The package with stale redistribution metadata'

        $unmappedDirectory = Join-Path $tempRoot 'unmapped-runtime-component'
        $unmappedArchive = Join-Path $tempRoot 'unmapped-runtime-component.zip'
        [System.IO.Directory]::CreateDirectory($unmappedDirectory) | Out-Null
        Copy-Item -Path (Join-Path $resolvedPublish '*') `
            -Destination $unmappedDirectory `
            -Recurse `
            -Force

        $metadataPath = Join-Path $unmappedDirectory 'DOTNET-NOTICE-METADATA.json'
        if (-not [System.IO.File]::Exists($metadataPath)) {
            throw 'The .NET notice metadata is unavailable for the unmapped-component test.'
        }
        $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
        $components = @($metadata.componentCoverage.components)
        $looseComponents = @($components | Where-Object {
            [string]$_.disposition -eq 'loose'
        })
        if ($looseComponents.Count -eq 0) {
            throw 'The unmapped-component test requires at least one loose runtime binary.'
        }
        $removed = $looseComponents[0]
        $remaining = @($components | Where-Object {
            -not (
                [string]$_.disposition -eq [string]$removed.disposition -and
                [string]$_.outputPath -eq [string]$removed.outputPath -and
                [string]$_.packageAssetPath -eq [string]$removed.packageAssetPath)
        })
        $metadata.componentCoverage.components = $remaining
        $metadata.componentCoverage.runtimeComponentCount = $remaining.Count
        $metadata.componentCoverage.looseRuntimeComponentCount = $looseComponents.Count - 1
        [System.IO.File]::WriteAllText(
            $metadataPath,
            ($metadata | ConvertTo-Json -Depth 12) + [Environment]::NewLine,
            [System.Text.UTF8Encoding]::new($false))

        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $unmappedDirectory,
            $unmappedArchive)
        Assert-PackageRejected $unmappedArchive (
            "The package with unmapped runtime binary '$($removed.outputPath)'")
    }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Negative release-package validation passed.'
