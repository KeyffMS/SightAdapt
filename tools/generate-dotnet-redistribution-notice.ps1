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
if ([int]$review.schemaVersion -ne 1) {
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

$templateHash = (Get-FileHash -LiteralPath $resolvedTemplate -Algorithm SHA256).Hash.ToLowerInvariant()
Assert-Equal 'Redistribution notice template SHA-256' `
    $templateHash `
    ([string]$review.noticeTemplate.sha256).ToLowerInvariant()

$professional = $review.professionalReview
$status = [string]$professional.status
$allowedStatuses = @('not-obtained', 'approved-with-conditions', 'approved')
if ($allowedStatuses -notcontains $status) {
    throw "Unsupported professional-review status '$status'."
}
$trackingIssue = [int]$professional.trackingIssue
if ($trackingIssue -le 0) {
    throw 'The professional-review tracking issue must be a positive number.'
}
$publicRecord = [string]$professional.publicRecord
if ($status -ne 'not-obtained') {
    if ([string]::IsNullOrWhiteSpace($publicRecord)) {
        throw "Professional-review status '$status' requires a public decision record."
    }
    $publicRecordPath = Join-Path $root $publicRecord
    if (-not [System.IO.File]::Exists($publicRecordPath)) {
        throw "Professional-review public record does not exist: $publicRecord"
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($publicRecord)) {
    throw 'Professional-review status not-obtained must not identify an approval record.'
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
    '{{PROFESSIONAL_REVIEW_STATUS}}' = $status
    '{{PROFESSIONAL_REVIEW_ISSUE}}' = [string]$trackingIssue
    '{{PROFESSIONAL_REVIEW_RECORD}}' = if ([string]::IsNullOrWhiteSpace($publicRecord)) {
        'none'
    }
    else {
        $publicRecord
    }
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

Write-Host "Generated reviewed Microsoft .NET redistribution notice: $outputPath"
