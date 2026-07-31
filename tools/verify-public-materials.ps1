[CmdletBinding()]
param(
    [string]$RegistryPath =
        (Join-Path $PSScriptRoot '..\release\public-materials.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$registryFullPath = (Resolve-Path -LiteralPath $RegistryPath).Path
$registry = Get-Content -LiteralPath $registryFullPath -Raw | ConvertFrom-Json
$failures = [System.Collections.Generic.List[string]]::new()

function Add-Failure([string]$Message) {
    $failures.Add($Message)
}

function Test-Date([string]$Value) {
    return $Value -match '^\d{4}-\d{2}-\d{2}$'
}

function Test-GitBlobEvidence($Evidence, [string]$Context) {
    $path = [string]$Evidence.path
    $expected = ([string]$Evidence.gitBlobSha).ToLowerInvariant()

    if ([string]::IsNullOrWhiteSpace($path)) {
        Add-Failure "$Context has an empty evidence path."
        return
    }
    if ($expected -notmatch '^[0-9a-f]{40}$') {
        Add-Failure "$Context evidence '$path' has an invalid Git blob SHA '$expected'."
        return
    }

    $fullPath = Join-Path $root $path
    if (-not [System.IO.File]::Exists($fullPath)) {
        Add-Failure "$Context evidence file '$path' does not exist."
        return
    }

    $actualOutput = & git -C $root hash-object -- $path 2>&1
    if ($LASTEXITCODE -ne 0) {
        Add-Failure "$Context evidence '$path' could not be hashed by Git: $($actualOutput -join ' ')"
        return
    }
    $actual = ([string]($actualOutput | Select-Object -Last 1)).Trim().ToLowerInvariant()
    if ($actual -ne $expected) {
        Add-Failure "$Context evidence '$path' has Git blob '$actual'; reviewed blob is '$expected'."
    }
}

if ([int]$registry.schemaVersion -ne 1) {
    Add-Failure "Unsupported public-material registry schema '$($registry.schemaVersion)'."
}
if (-not (Test-Date ([string]$registry.reviewedAt))) {
    Add-Failure 'The registry review date must use YYYY-MM-DD.'
}
if ([string]::IsNullOrWhiteSpace([string]$registry.decisionOwner)) {
    Add-Failure 'The registry requires a decision owner.'
}
if ([int]$registry.decisionIssue -ne 92) {
    Add-Failure "The registry decision issue must be #92, not '$($registry.decisionIssue)'."
}
if ([string]$registry.reviewType -ne 'internal-maintainer-risk-control') {
    Add-Failure 'The registry must identify the review as an internal maintainer risk control.'
}

Test-GitBlobEvidence $registry.authoritativeNotice 'Authoritative notice'
$noticePath = Join-Path $root ([string]$registry.authoritativeNotice.path)
if ([System.IO.File]::Exists($noticePath)) {
    $notice = Get-Content -LiteralPath $noticePath -Raw
    foreach ($requiredText in @(
        'property of their respective owners',
        'only to identify an application selected or configured by the user',
        'not affiliated with, sponsored by, certified by or endorsed by Microsoft',
        'not intended to circumvent',
        'Protected or DRM-controlled content may remain unavailable')) {
        if (-not $notice.Contains($requiredText, [StringComparison]::Ordinal)) {
            Add-Failure "The authoritative notice is missing required wording: '$requiredText'."
        }
    }
}

$summary = [string]$registry.authoritativeNotice.approvedSummary
foreach ($requiredText in @(
    'property of their respective owners',
    'only to identify applications selected or configured by the user',
    'not affiliated with, sponsored by or endorsed by Microsoft',
    'does not circumvent DRM or other access controls',
    'protected content may remain unavailable or unfilterable')) {
    if (-not $summary.Contains($requiredText, [StringComparison]::OrdinalIgnoreCase)) {
        Add-Failure "The approved reusable summary is missing: '$requiredText'."
    }
}

foreach ($authority in @($registry.reviewAuthorities)) {
    Test-GitBlobEvidence $authority 'Review authority'
}
Test-GitBlobEvidence $registry.objectionProcedure 'Objection procedure'

$knownMaintained = @(
    'repository-readme',
    'binary-third-party-names-notice',
    'repository-third-party-policy',
    'release-description-template',
    'application-about-window',
    'repository-brand-assets'
)
$seenIds = @{}
$allowedClaimPrefixes = @(
    'works with ',
    'tested with ',
    'compatible with ',
    'uses ',
    'applies an overlay to '
)
$prohibitedClaimPattern = '(?i)\b(official integration|partnered? with|approved by|certified by|endorsed by|supported by|authorized by)\b'

foreach ($surface in @($registry.maintainedSurfaces)) {
    $id = [string]$surface.id
    if ([string]::IsNullOrWhiteSpace($id)) {
        Add-Failure 'A maintained surface has an empty ID.'
        continue
    }
    if ($seenIds.ContainsKey($id)) {
        Add-Failure "Duplicate public-material surface ID '$id'."
        continue
    }
    $seenIds[$id] = $true

    if ([string]$surface.status -ne 'approved') {
        Add-Failure "Maintained surface '$id' is not approved."
    }
    if (-not (Test-Date ([string]$surface.reviewedAt))) {
        Add-Failure "Maintained surface '$id' has an invalid review date."
    }
    if ([string]::IsNullOrWhiteSpace([string]$surface.reviewer)) {
        Add-Failure "Maintained surface '$id' has no reviewer."
    }

    $evidence = @($surface.sourceEvidence)
    if ($evidence.Count -eq 0) {
        Add-Failure "Maintained surface '$id' has no immutable source evidence."
    }
    foreach ($entry in $evidence) {
        Test-GitBlobEvidence $entry "Maintained surface '$id'"
    }

    foreach ($thirdParty in @($surface.namedThirdParties)) {
        if ([string]::IsNullOrWhiteSpace([string]$thirdParty.name) -or
            [string]::IsNullOrWhiteSpace([string]$thirdParty.purpose)) {
            Add-Failure "Maintained surface '$id' contains an incomplete named-third-party record."
        }
    }

    foreach ($claim in @($surface.compatibilityClaims)) {
        $wording = [string]$claim.wording
        $allowedPrefix = $false
        foreach ($prefix in $allowedClaimPrefixes) {
            if ($wording.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)) {
                $allowedPrefix = $true
                break
            }
        }
        if (-not $allowedPrefix) {
            Add-Failure "Surface '$id' has compatibility wording outside the approved factual forms: '$wording'."
        }
        if ($wording -match $prohibitedClaimPattern) {
            Add-Failure "Surface '$id' contains prohibited relationship wording: '$wording'."
        }
        if ([string]::IsNullOrWhiteSpace([string]$claim.limitations)) {
            Add-Failure "Surface '$id' compatibility claim '$wording' has no limitations."
        }
    }

    foreach ($asset in @($surface.thirdPartyAssets)) {
        if ([string]::IsNullOrWhiteSpace([string]$asset.owner) -or
            [string]::IsNullOrWhiteSpace([string]$asset.useBasis) -or
            [string]::IsNullOrWhiteSpace([string]$asset.approvedPurpose) -or
            -not (Test-Date ([string]$asset.reviewedAt))) {
            Add-Failure "Surface '$id' contains a third-party asset without complete ownership, use-basis, purpose and review evidence."
        }
        if ($null -ne $asset.sourceEvidence) {
            Test-GitBlobEvidence $asset.sourceEvidence "Third-party asset on '$id'"
        }
        else {
            Add-Failure "Surface '$id' contains a third-party asset without immutable source evidence."
        }
    }
}

foreach ($requiredId in $knownMaintained) {
    if (-not $seenIds.ContainsKey($requiredId)) {
        Add-Failure "Required maintained surface '$requiredId' is absent from the registry."
    }
}

foreach ($planned in @($registry.plannedSurfaces)) {
    $id = [string]$planned.id
    if ([string]::IsNullOrWhiteSpace($id)) {
        Add-Failure 'A planned surface has an empty ID.'
        continue
    }
    if ($seenIds.ContainsKey($id)) {
        Add-Failure "Surface ID '$id' appears more than once."
        continue
    }
    $seenIds[$id] = $true

    if ([string]$planned.status -ne 'planned') {
        Add-Failure "Future surface '$id' is not allowed to become active without moving to maintainedSurfaces with exact review evidence."
    }
    $issues = @($planned.trackingIssues)
    if ($issues.Count -eq 0 -or @($issues | Where-Object { [int]$_ -le 0 }).Count -gt 0) {
        Add-Failure "Planned surface '$id' has no valid tracking Issue."
    }
    if (@($planned.activationRequirements).Count -eq 0) {
        Add-Failure "Planned surface '$id' has no activation requirements."
    }
}

if ($failures.Count -gt 0) {
    $details = $failures | ForEach-Object { " - $_" }
    throw "Public-material, trademark and DRM review failed:`n$($details -join "`n")"
}

Write-Host "Public-material review passed for $(@($registry.maintainedSurfaces).Count) maintained surfaces; $(@($registry.plannedSurfaces).Count) future surfaces remain planned."
