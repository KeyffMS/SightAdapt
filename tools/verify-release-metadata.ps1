[CmdletBinding()]
param(
    [switch]$WriteGitHubOutput
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
[xml]$props = Get-Content (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup

$version = [string]$group.SightAdaptProductVersion
$fileVersion = [string]$group.SightAdaptFileVersion
$milestone = [string]$group.SightAdaptMilestone
$schema = [int]$group.SightAdaptSettingsSchema
$artifact = ([string]$group.SightAdaptArtifactName).Replace(
    '$(SightAdaptProductVersion)',
    $version)

if ([string]::IsNullOrWhiteSpace($version) -or
    [string]::IsNullOrWhiteSpace($fileVersion) -or
    [string]::IsNullOrWhiteSpace($milestone) -or
    $schema -le 0 -or
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

[xml]$project = Get-Content (Join-Path $root 'src/SightAdapt/SightAdapt.csproj')
$metadata = @($project.Project.ItemGroup.AssemblyMetadata)
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

$workflow = Get-Content (Join-Path $root '.github/workflows/build.yml') -Raw
if (-not $workflow.Contains('steps.release.outputs.artifact_name', [StringComparison]::Ordinal)) {
    throw 'The build workflow does not derive its artifact name from release metadata.'
}

if ($WriteGitHubOutput) {
    if ([string]::IsNullOrWhiteSpace($env:GITHUB_OUTPUT)) {
        throw 'GITHUB_OUTPUT is unavailable.'
    }
    "artifact_name=$artifact" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "product_version=$version" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "settings_schema=$schema" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}

Write-Host "Release metadata verified: version=$version; schema=$schema; artifact=$artifact"
