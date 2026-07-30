[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory,

    [string]$ReviewPath,

    [string]$TemplatePath,

    [string]$ProjectPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ReviewPath)) {
    $ReviewPath = Join-Path $root 'release\dotnet-redistribution-review.json'
}
if ([string]::IsNullOrWhiteSpace($TemplatePath)) {
    $TemplatePath = Join-Path $root 'release\MICROSOFT-DOTNET-REDISTRIBUTION.template.txt'
}
if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $root 'src\SightAdapt\SightAdapt.csproj'
}

function Assert-Equal(
    [string]$Name,
    [string]$Actual,
    [string]$Expected) {
    if ($Actual -ne $Expected) {
        throw "$Name is '$Actual'; reviewed value is '$Expected'. Update and review release/dotnet-redistribution-review.json before packaging."
    }
}

function Get-NormalizedTextSha256([string]$Path) {
    $text = Get-Content -LiteralPath $Path -Raw
    $normalized = $text.Replace("`r`n", "`n").Replace("`r", "`n")
    $bytes = [System.Text.UTF8Encoding]::new($false).GetBytes($normalized)
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString(
            $sha256.ComputeHash($bytes)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}

$publish = [System.IO.Path]::GetFullPath($PublishDirectory)
[System.IO.Directory]::CreateDirectory($publish) | Out-Null
$resolvedReview = (Resolve-Path -LiteralPath $ReviewPath).Path
$resolvedTemplate = (Resolve-Path -LiteralPath $TemplatePath).Path
$resolvedProject = (Resolve-Path -LiteralPath $ProjectPath).Path

[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup
$productVersion = [string]$group.SightAdaptProductVersion
$sdkVersion = [string]$group.SightAdaptDotNetSdkVersion
$runtimeVersion = [string]$group.SightAdaptDotNetRuntimeVersion
$rid = [string]$group.SightAdaptRuntimeIdentifier
$publishMode = [string]$group.SightAdaptPublishMode

[xml]$project = Get-Content -LiteralPath $resolvedProject
$projectGroup = @($project.Project.PropertyGroup) | Select-Object -First 1
$targetFramework = [string]$projectGroup.TargetFramework

$review = Get-Content -LiteralPath $resolvedReview -Raw | ConvertFrom-Json
if ([int]$review.schemaVersion -ne 2) {
    throw "Unsupported .NET redistribution review schema '$($review.schemaVersion)'."
}
if ([string]$review.reviewedAt -notmatch '^\d{4}-\d{2}-\d{2}$') {
    throw 'The .NET redistribution review date must use YYYY-MM-DD.'
}

$config = $review.reviewedConfiguration
Assert-Equal 'SDK version' $sdkVersion ([string]$config.sdkVersion)
Assert-Equal 'Runtime version' $runtimeVersion ([string]$config.runtimeVersion)
Assert-Equal 'Target framework' $targetFramework ([string]$config.targetFramework)
Assert-Equal 'Runtime identifier' $rid ([string]$config.runtimeIdentifier)
Assert-Equal 'Publish mode' $publishMode ([string]$config.publishMode)

$templateRelativePath = [System.IO.Path]::GetRelativePath(
    $root,
    $resolvedTemplate).Replace('\', '/')
Assert-Equal 'Redistribution notice template path' `
    $templateRelativePath `
    ([string]$review.noticeTemplate.path)

$templateHash = Get-NormalizedTextSha256 $resolvedTemplate
Assert-Equal 'Redistribution notice template SHA-256' `
    $templateHash `
    ([string]$review.noticeTemplate.sha256).ToLowerInvariant()

$decision = $review.maintainerDecision
$decisionStatus = [string]$decision.status
$allowedStatuses = @(
    'accepted-for-current-distribution',
    'accepted-with-conditions',
    'blocked')
if ($allowedStatuses -notcontains $decisionStatus) {
    throw "Unsupported maintainer decision '$decisionStatus'."
}
$decisionOwner = [string]$decision.decisionOwner
if ([string]::IsNullOrWhiteSpace($decisionOwner)) {
    throw 'The .NET redistribution maintainer decision requires a decision owner.'
}
$decisionIssue = [int]$decision.decisionIssue
if ($decisionIssue -le 0) {
    throw 'The .NET redistribution maintainer decision requires a positive issue number.'
}
if ($decisionStatus -eq 'blocked') {
    throw 'The .NET redistribution maintainer decision blocks packaging.'
}
if ($decisionStatus -eq 'accepted-with-conditions' -and
    @($decision.conditions).Count -eq 0) {
    throw 'A conditional maintainer decision must list its conditions.'
}

$dotnetMetadataPath = Join-Path $publish 'DOTNET-NOTICE-METADATA.json'
if (-not [System.IO.File]::Exists($dotnetMetadataPath)) {
    throw 'Generate exact-version .NET notices before the redistribution notice.'
}
$dotnetMetadata = Get-Content -LiteralPath $dotnetMetadataPath -Raw | ConvertFrom-Json
Assert-Equal 'Generated notice product version' `
    ([string]$dotnetMetadata.productVersion) $productVersion
Assert-Equal 'Generated notice SDK version' `
    ([string]$dotnetMetadata.sdkVersion) $sdkVersion
Assert-Equal 'Generated notice runtime version' `
    ([string]$dotnetMetadata.runtimeVersion) $runtimeVersion
Assert-Equal 'Generated notice runtime identifier' `
    ([string]$dotnetMetadata.runtimeIdentifier) $rid
Assert-Equal 'Generated notice publish mode' `
    ([string]$dotnetMetadata.publishMode) $publishMode

$rendered = Get-Content -LiteralPath $resolvedTemplate -Raw
$replacements = [ordered]@{
    '{{PRODUCT_VERSION}}' = $productVersion
    '{{SDK_VERSION}}' = $sdkVersion
    '{{RUNTIME_VERSION}}' = $runtimeVersion
    '{{TARGET_FRAMEWORK}}' = $targetFramework
    '{{RUNTIME_IDENTIFIER}}' = $rid
    '{{PUBLISH_MODE}}' = $publishMode
    '{{REVIEW_DATE}}' = [string]$review.reviewedAt
    '{{MAINTAINER_DECISION}}' = $decisionStatus
    '{{DECISION_OWNER}}' = $decisionOwner
    '{{DECISION_ISSUE}}' = [string]$decisionIssue
}
foreach ($replacement in $replacements.GetEnumerator()) {
    $rendered = $rendered.Replace(
        [string]$replacement.Key,
        [string]$replacement.Value)
}
if ($rendered -match '\{\{[A-Z0-9_]+\}\}') {
    throw 'The redistribution notice contains an unresolved template marker.'
}

$outputPath = Join-Path $publish 'MICROSOFT-DOTNET-REDISTRIBUTION.txt'
[System.IO.File]::WriteAllText(
    $outputPath,
    $rendered,
    [System.Text.UTF8Encoding]::new($false))

Write-Host "Generated maintainer-reviewed Microsoft .NET redistribution notice: $outputPath"
