[CmdletBinding()]
param(
    [string]$PublishDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

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
        $notice = Get-Content -LiteralPath $noticePath -Raw
        $mutated = [regex]::Replace(
            $notice,
            '(?m)^Runtime version:\s*[^\r\n]+$',
            'Runtime version: 0.0.0-stale')
        if ($mutated -eq $notice) {
            throw 'The stale-notice test could not locate the Runtime version header.'
        }
        [System.IO.File]::WriteAllText(
            $noticePath,
            $mutated,
            [System.Text.UTF8Encoding]::new($false))

        [System.IO.Compression.ZipFile]::CreateFromDirectory(
            $staleDirectory,
            $staleArchive)
        Assert-PackageRejected $staleArchive 'The package with stale redistribution metadata'
    }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Negative release-package validation passed.'
