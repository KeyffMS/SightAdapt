[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-incomplete-package-' + [Guid]::NewGuid().ToString('N'))
$staging = Join-Path $tempRoot 'staging'
$archive = Join-Path $tempRoot 'incomplete.zip'

try {
    [System.IO.Directory]::CreateDirectory($staging) | Out-Null
    [System.IO.File]::WriteAllText(
        (Join-Path $staging 'SightAdapt.exe'),
        'deliberately incomplete package',
        [System.Text.UTF8Encoding]::new($false))

    [System.IO.Compression.ZipFile]::CreateFromDirectory($staging, $archive)

    $failedAsExpected = $false
    try {
        & (Join-Path $PSScriptRoot 'test-release-package.ps1') `
            -ArchivePath $archive
    }
    catch {
        $failedAsExpected = $true
        Write-Host "Incomplete package was rejected as expected: $($_.Exception.Message)"
    }

    if (-not $failedAsExpected) {
        throw 'The deliberately incomplete package unexpectedly passed validation.'
    }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Negative release-package validation passed.'
