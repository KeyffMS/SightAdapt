[CmdletBinding()]
param(
    [switch]$WriteGitHubOutput
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup

$version = [string]$group.SightAdaptProductVersion
$fileVersion = [string]$group.SightAdaptFileVersion
$milestone = [string]$group.SightAdaptMilestone
$schema = [int]$group.SightAdaptSettingsSchema
$sdkVersion = [string]$group.SightAdaptDotNetSdkVersion
$runtimeVersion = [string]$group.SightAdaptDotNetRuntimeVersion
$rid = [string]$group.SightAdaptRuntimeIdentifier
$publishMode = [string]$group.SightAdaptPublishMode
$releaseMetadataUrl = [string]$group.SightAdaptDotNetReleaseMetadataUrl
$artifact = ([string]$group.SightAdaptArtifactName).Replace(
    '$(SightAdaptProductVersion)',
    $version)

function Assert-Equal(
    [string]$Name,
    [string]$Actual,
    [string]$Expected) {
    if ($Actual -ne $Expected) {
        throw "$Name is '$Actual'; reviewed value is '$Expected'. Update and review release/dotnet-redistribution-review.json before release."
    }
}

if ([string]::IsNullOrWhiteSpace($version) -or
    [string]::IsNullOrWhiteSpace($fileVersion) -or
    [string]::IsNullOrWhiteSpace($milestone) -or
    $schema -le 0 -or
    [string]::IsNullOrWhiteSpace($sdkVersion) -or
    [string]::IsNullOrWhiteSpace($runtimeVersion) -or
    [string]::IsNullOrWhiteSpace($rid) -or
    [string]::IsNullOrWhiteSpace($publishMode) -or
    [string]::IsNullOrWhiteSpace($releaseMetadataUrl) -or
    [string]::IsNullOrWhiteSpace($artifact)) {
    throw 'Directory.Build.props does not contain complete release metadata.'
}

$readme = Get-Content (Join-Path $root 'README.md') -Raw
$expectations = @(
    "Product version: $version",
    "File version:    $fileVersion",
    "Milestone:       $milestone",
    "Settings schema: $schema"
)
foreach ($expectation in $expectations) {
    if (-not $readme.Contains($expectation, [StringComparison]::Ordinal)) {
        throw "README.md is out of sync: '$expectation' was not found."
    }
}

$globalJson = Get-Content (Join-Path $root 'global.json') -Raw | ConvertFrom-Json
if ([string]$globalJson.sdk.version -ne $sdkVersion -or
    [string]$globalJson.sdk.rollForward -ne 'disable' -or
    [bool]$globalJson.sdk.allowPrerelease) {
    throw 'global.json does not pin the exact reviewed .NET SDK.'
}

$projectPath = Join-Path $root 'src/SightAdapt/SightAdapt.csproj'
[xml]$project = Get-Content $projectPath
$metadata = @($project.SelectNodes('/Project/ItemGroup/AssemblyMetadata'))
if (-not ($metadata | Where-Object {
    $_.Include -eq 'Milestone' -and
    $_.Value -eq '$(SightAdaptMilestone)'
})) {
    throw 'SightAdapt.csproj does not derive Milestone from Directory.Build.props.'
}
if (-not ($metadata | Where-Object {
    $_.Include -eq 'SettingsSchema' -and
    $_.Value -eq '$(SightAdaptSettingsSchema)'
})) {
    throw 'SightAdapt.csproj does not derive SettingsSchema from Directory.Build.props.'
}
$projectGroup = @($project.Project.PropertyGroup) | Select-Object -First 1
$targetFramework = [string]$projectGroup.TargetFramework
if ([string]$projectGroup.RuntimeIdentifier -ne '$(SightAdaptRuntimeIdentifier)' -or
    [string]$projectGroup.SelfContained -ne 'true' -or
    [string]$projectGroup.PublishSingleFile -ne 'true') {
    throw 'SightAdapt.csproj does not derive its reviewed self-contained publish inputs from release metadata.'
}

$reviewPath = Join-Path $root 'release/dotnet-redistribution-review.json'
$review = Get-Content -LiteralPath $reviewPath -Raw | ConvertFrom-Json
if ([int]$review.schemaVersion -ne 1) {
    throw "Unsupported .NET redistribution review schema '$($review.schemaVersion)'."
}
if ([string]$review.reviewedAt -notmatch '^\d{4}-\d{2}-\d{2}$') {
    throw 'The .NET redistribution review date must use YYYY-MM-DD.'
}
$config = $review.reviewedConfiguration
Assert-Equal 'Reviewed .NET SDK version' $sdkVersion ([string]$config.sdkVersion)
Assert-Equal 'Reviewed .NET Runtime version' $runtimeVersion ([string]$config.runtimeVersion)
Assert-Equal 'Reviewed target framework' $targetFramework ([string]$config.targetFramework)
Assert-Equal 'Reviewed runtime identifier' $rid ([string]$config.runtimeIdentifier)
Assert-Equal 'Reviewed publish mode' $publishMode ([string]$config.publishMode)

$templateRelativePath = ([string]$review.noticeTemplate.path).Replace('/', [System.IO.Path]::DirectorySeparatorChar)
$templatePath = Join-Path $root $templateRelativePath
if (-not [System.IO.File]::Exists($templatePath)) {
    throw "The reviewed redistribution notice template does not exist: $($review.noticeTemplate.path)"
}
$templateHash = (Get-FileHash -LiteralPath $templatePath -Algorithm SHA256).Hash.ToLowerInvariant()
Assert-Equal 'Reviewed redistribution notice template SHA-256' `
    $templateHash `
    ([string]$review.noticeTemplate.sha256).ToLowerInvariant()

$professional = $review.professionalReview
$status = [string]$professional.status
$allowedStatuses = @('not-obtained', 'approved-with-conditions', 'approved')
if ($allowedStatuses -notcontains $status) {
    throw "Unsupported professional-review status '$status'."
}
if ([int]$professional.trackingIssue -ne 93) {
    throw 'The .NET redistribution professional review must remain linked to Issue #93.'
}
$publicRecord = [string]$professional.publicRecord
if ($status -eq 'not-obtained') {
    if (-not [string]::IsNullOrWhiteSpace($publicRecord)) {
        throw 'Professional-review status not-obtained must not identify an approval record.'
    }
}
else {
    if ([string]::IsNullOrWhiteSpace($publicRecord) -or
        -not [System.IO.File]::Exists((Join-Path $root $publicRecord))) {
        throw "Professional-review status '$status' requires an existing public decision record."
    }
}

$workflow = Get-Content (Join-Path $root '.github/workflows/build.yml') -Raw
if (-not $workflow.Contains('steps.release.outputs.artifact_name', [StringComparison]::Ordinal)) {
    throw 'The build workflow does not derive its artifact name from release metadata.'
}
if (-not $workflow.Contains("dotnet-version: $sdkVersion", [StringComparison]::Ordinal)) {
    throw 'The build workflow does not install the pinned .NET SDK.'
}
if (-not $workflow.Contains('generate-dotnet-notices.ps1', [StringComparison]::Ordinal)) {
    throw 'The build workflow does not generate exact-version .NET notices.'
}
if (-not $workflow.Contains('generate-dotnet-redistribution-notice.ps1', [StringComparison]::Ordinal)) {
    throw 'The build workflow does not generate the reviewed .NET redistribution notice.'
}

$requiredFiles = Get-Content (Join-Path $root 'release/required-files.txt') -Raw
if (-not $requiredFiles.Contains('DOTNET-NOTICE-METADATA.json', [StringComparison]::Ordinal)) {
    throw 'The release manifest does not require .NET notice metadata.'
}
if (-not $requiredFiles.Contains('MICROSOFT-DOTNET-REDISTRIBUTION.txt', [StringComparison]::Ordinal)) {
    throw 'The release manifest does not require the Microsoft .NET redistribution notice.'
}

if ($WriteGitHubOutput) {
    if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        throw 'GITHUB_OUTPUT is unavailable.'
    }
    "artifact_name=$artifact" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "product_version=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "settings_schema=$schema" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "dotnet_sdk_version=$sdkVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "dotnet_runtime_version=$runtimeVersion" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "runtime_identifier=$rid" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}

Write-Host "Release metadata verified: version=$version; sdk=$sdkVersion; runtime=$runtimeVersion; rid=$rid; artifact=$artifact; redistribution-review=$($review.reviewedAt)"
