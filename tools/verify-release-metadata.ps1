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

[xml]$project = Get-Content (Join-Path $root 'src/SightAdapt/SightAdapt.csproj')
$metadata = @(
    $project.Project.ItemGroup |
        ForEach-Object {
            if ($null -ne $_.AssemblyMetadata) {
                @($_.AssemblyMetadata)
            }
        }
)
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
if ([string]$projectGroup.RuntimeIdentifier -ne '$(SightAdaptRuntimeIdentifier)' -or
    [string]$projectGroup.RuntimeFrameworkVersion -ne '$(SightAdaptDotNetRuntimeVersion)' -or
    [string]$projectGroup.SelfContained -ne 'true' -or
    [string]$projectGroup.PublishSingleFile -ne 'true') {
    throw 'SightAdapt.csproj does not derive its exact self-contained publish inputs from release metadata.'
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

$requiredFiles = Get-Content (Join-Path $root 'release/required-files.txt') -Raw
if (-not $requiredFiles.Contains('DOTNET-NOTICE-METADATA.json', [StringComparison]::Ordinal)) {
    throw 'The release manifest does not require .NET notice metadata.'
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

Write-Host "Release metadata verified: version=$version; sdk=$sdkVersion; runtime=$runtimeVersion; rid=$rid; artifact=$artifact"
