[CmdletBinding()]
param(
    [string]$RegistryPath =
        (Join-Path $PSScriptRoot '..\release\public-materials.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$resolvedRegistry = (Resolve-Path -LiteralPath $RegistryPath).Path
$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-public-materials-negative-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

function Assert-Rejected(
    [string]$Scenario,
    [scriptblock]$Mutation) {
    $scenarioPath = Join-Path $tempRoot (([regex]::Replace($Scenario, '[^A-Za-z0-9.-]', '-')) + '.json')
    $registry = Get-Content -LiteralPath $resolvedRegistry -Raw | ConvertFrom-Json
    & $Mutation $registry
    [System.IO.File]::WriteAllText(
        $scenarioPath,
        ($registry | ConvertTo-Json -Depth 16) + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))

    $rejected = $false
    try {
        & (Join-Path $PSScriptRoot 'verify-public-materials.ps1') `
            -RegistryPath $scenarioPath
    }
    catch {
        $rejected = $true
        Write-Host "$Scenario was rejected as expected: $($_.Exception.Message)"
    }

    if (-not $rejected) {
        throw "$Scenario unexpectedly passed public-material review."
    }
}

try {
    Assert-Rejected 'A stale reviewed source hash' {
        param($registry)
        $registry.authoritativeNotice.gitBlobSha = '0000000000000000000000000000000000000000'
    }

    Assert-Rejected 'An implied official integration claim' {
        param($registry)
        $surface = $registry.maintainedSurfaces | Where-Object {
            [string]$_.id -eq 'repository-readme'
        } | Select-Object -First 1
        $surface.compatibilityClaims = @($surface.compatibilityClaims) + @(
            [pscustomobject]@{
                wording = 'official integration with Example Product'
                limitations = 'None'
            }
        )
    }

    Assert-Rejected 'A third-party logo without use-basis evidence' {
        param($registry)
        $surface = $registry.maintainedSurfaces | Where-Object {
            [string]$_.id -eq 'repository-brand-assets'
        } | Select-Object -First 1
        $surface.thirdPartyAssets = @(
            [pscustomobject]@{
                owner = 'Example Corporation'
                useBasis = ''
                approvedPurpose = ''
                reviewedAt = ''
            }
        )
    }

    Assert-Rejected 'An unreviewed activation of the GitHub Release surface' {
        param($registry)
        $surface = $registry.plannedSurfaces | Where-Object {
            [string]$_.id -eq 'github-release'
        } | Select-Object -First 1
        $surface.status = 'approved'
    }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Negative public-material, trademark and DRM review tests passed.'
