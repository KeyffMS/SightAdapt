[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory,

    [string]$TestAssetsPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($TestAssetsPath)) {
    $TestAssetsPath = Join-Path $root 'tests\SightAdapt.Tests\obj\project.assets.json'
}

$resolvedPublish = (Resolve-Path -LiteralPath $PublishDirectory).Path
$resolvedTestAssets = (Resolve-Path -LiteralPath $TestAssetsPath).Path
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-sbom-negative-' + [Guid]::NewGuid().ToString('N'))

try {
    $fixturePublish = Join-Path $tempRoot 'publish'
    $packageFolder = Join-Path $tempRoot 'packages'
    [System.IO.Directory]::CreateDirectory($fixturePublish) | Out-Null
    [System.IO.Directory]::CreateDirectory($packageFolder) | Out-Null
    Copy-Item -Path (Join-Path $resolvedPublish '*') `
        -Destination $fixturePublish `
        -Recurse `
        -Force

    $packageId = 'Unknown.Transitive'
    $packageVersion = '1.0.0'
    $identity = "$packageId/$packageVersion"
    $packageRelativePath = 'unknown.transitive/1.0.0'
    $packageRoot = Join-Path $packageFolder 'unknown.transitive\1.0.0'
    [System.IO.Directory]::CreateDirectory($packageRoot) | Out-Null

    $nuspec = @"
<?xml version="1.0"?>
<package>
  <metadata>
    <id>$packageId</id>
    <version>$packageVersion</version>
    <authors>Unknown Publisher</authors>
    <description>Deliberately missing license metadata.</description>
  </metadata>
</package>
"@
    [System.IO.File]::WriteAllText(
        (Join-Path $packageRoot 'unknown.transitive.nuspec'),
        $nuspec,
        [System.Text.UTF8Encoding]::new($false))
    $sha512Base64 = [Convert]::ToBase64String([byte[]]::new(64))
    [System.IO.File]::WriteAllText(
        (Join-Path $packageRoot 'unknown.transitive.1.0.0.nupkg.sha512'),
        $sha512Base64,
        [System.Text.UTF8Encoding]::new($false))

    $assets = Get-Content -LiteralPath $resolvedTestAssets -Raw | ConvertFrom-Json
    $folderPropertyName = $packageFolder.TrimEnd('\') + '\'
    $assets.packageFolders | Add-Member `
        -NotePropertyName $folderPropertyName `
        -NotePropertyValue ([pscustomobject]@{}) `
        -Force
    $assets.libraries | Add-Member `
        -NotePropertyName $identity `
        -NotePropertyValue ([pscustomobject]@{
            sha512 = $sha512Base64
            type = 'package'
            path = $packageRelativePath
            files = @('unknown.transitive.nuspec')
        }) `
        -Force

    $targetProperty = @($assets.targets.PSObject.Properties) | Select-Object -First 1
    if ($null -eq $targetProperty) {
        throw 'The test restore graph has no target for the transitive-license fixture.'
    }
    $targetProperty.Value | Add-Member `
        -NotePropertyName $identity `
        -NotePropertyValue ([pscustomobject]@{
            type = 'package'
            compile = [pscustomobject]@{}
            runtime = [pscustomobject]@{}
        }) `
        -Force

    $parentProperty = @(
        $targetProperty.Value.PSObject.Properties |
            Where-Object { [string]$_.Value.type -eq 'package' }
    ) | Select-Object -First 1
    if ($null -eq $parentProperty) {
        throw 'The test restore graph has no package node for the transitive-license fixture.'
    }
    $dependenciesProperty = $parentProperty.Value.PSObject.Properties['dependencies']
    if ($null -eq $dependenciesProperty) {
        $parentProperty.Value | Add-Member `
            -NotePropertyName 'dependencies' `
            -NotePropertyValue ([pscustomobject]@{})
    }
    $parentProperty.Value.dependencies | Add-Member `
        -NotePropertyName $packageId `
        -NotePropertyValue $packageVersion `
        -Force

    $fixtureAssets = Join-Path $tempRoot 'project.assets.json'
    [System.IO.File]::WriteAllText(
        $fixtureAssets,
        ($assets | ConvertTo-Json -Depth 100),
        [System.Text.UTF8Encoding]::new($false))

    $failedAsExpected = $false
    try {
        & (Join-Path $PSScriptRoot 'generate-sbom.ps1') `
            -PublishDirectory $fixturePublish `
            -TestAssetsPath $fixtureAssets
    }
    catch {
        if ($_.Exception.Message -notmatch 'Unknown\.Transitive' -or
            $_.Exception.Message -notmatch 'no resolved license') {
            throw "The transitive-license fixture failed for an unexpected reason: $($_.Exception.Message)"
        }
        $failedAsExpected = $true
        Write-Host "Unknown transitive license was rejected as expected: $($_.Exception.Message)"
    }

    if (-not $failedAsExpected) {
        throw 'A transitive package with no license metadata unexpectedly passed SBOM/license review.'
    }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Negative transitive dependency-license test passed.'
