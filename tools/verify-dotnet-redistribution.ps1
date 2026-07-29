[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$noticePath = Join-Path $root 'MICROSOFT-DOTNET-REDISTRIBUTION.txt'
$analysisPath = Join-Path $root 'docs\legal\DOTNET-REDISTRIBUTION.md'
$manifestPath = Join-Path $root 'release\required-files.txt'
$projectPath = Join-Path $root 'src\SightAdapt\SightAdapt.csproj'

[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup
$productVersion = [string]$group.SightAdaptProductVersion
$sdkVersion = [string]$group.SightAdaptDotNetSdkVersion
$runtimeVersion = [string]$group.SightAdaptDotNetRuntimeVersion
$rid = [string]$group.SightAdaptRuntimeIdentifier
$publishMode = [string]$group.SightAdaptPublishMode

[xml]$project = Get-Content -LiteralPath $projectPath
$projectGroup = @($project.Project.PropertyGroup) | Select-Object -First 1
$targetFramework = [string]$projectGroup.TargetFramework

$notice = Get-Content -LiteralPath $noticePath -Raw
$analysis = Get-Content -LiteralPath $analysisPath -Raw
$manifest = Get-Content -LiteralPath $manifestPath -Raw
$projectText = Get-Content -LiteralPath $projectPath -Raw

$noticeExpectations = @(
    "SightAdapt version: $productVersion",
    ".NET SDK used to build: $sdkVersion",
    ".NET Runtime: $runtimeVersion",
    "Windows Desktop Runtime: $runtimeVersion",
    "Target framework: $targetFramework",
    "Runtime identifier: $rid",
    'Publication: self-contained, single-file Windows application',
    'Microsoft .NET components are included in object-code form only as part of the',
    'not offered, branded or distributed by the',
    'standalone Microsoft .NET product',
    'does not replace, expand or modify the separate terms',
    'Microsoft does not publish, sponsor, certify or endorse SightAdapt',
    'https://github.com/dotnet/core/blob/main/license-information.md',
    'https://dotnet.microsoft.com/en-us/dotnet_library_license.htm',
    'not a legal opinion',
    'Issue #93'
)
foreach ($expectation in $noticeExpectations) {
    if (-not $notice.Contains($expectation, [StringComparison]::Ordinal)) {
        throw "MICROSOFT-DOTNET-REDISTRIBUTION.txt is missing required text: '$expectation'."
    }
}

$analysisExpectations = @(
    "| .NET SDK | `$sdkVersion` |",
    "| .NET Runtime | `$runtimeVersion` |",
    "| Windows Desktop Runtime | `$runtimeVersion` |",
    "| Target framework | `$targetFramework` |",
    "| Runtime identifier | `$rid` |",
    "Microsoft.NETCore.App.Runtime.$rid/$runtimeVersion",
    "Microsoft.WindowsDesktop.App.Runtime.$rid/$runtimeVersion",
    'Maintainer review date | 2026-07-29',
    'Professional legal review | Required before production or paid distribution under Issue #93',
    'The SightAdapt MIT License is clearly limited to SightAdapt project code',
    'Repeat this analysis and update the package notice'
)
foreach ($expectation in $analysisExpectations) {
    if (-not $analysis.Contains($expectation, [StringComparison]::Ordinal)) {
        throw "docs/legal/DOTNET-REDISTRIBUTION.md is missing required text: '$expectation'."
    }
}

if (-not $manifest.Contains(
    'MICROSOFT-DOTNET-REDISTRIBUTION.txt',
    [StringComparison]::Ordinal)) {
    throw 'The release manifest does not require MICROSOFT-DOTNET-REDISTRIBUTION.txt.'
}

if (-not $projectText.Contains(
    '..\..\MICROSOFT-DOTNET-REDISTRIBUTION.txt',
    [StringComparison]::Ordinal) -or
    -not $projectText.Contains(
        'Link="MICROSOFT-DOTNET-REDISTRIBUTION.txt"',
        [StringComparison]::Ordinal)) {
    throw 'SightAdapt.csproj does not publish MICROSOFT-DOTNET-REDISTRIBUTION.txt.'
}

$forbiddenPlaceholders = @('TODO', 'TBD', '<version>', '<date>', '[insert')
foreach ($placeholder in $forbiddenPlaceholders) {
    if ($notice.Contains($placeholder, [StringComparison]::OrdinalIgnoreCase) -or
        $analysis.Contains($placeholder, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Redistribution documentation contains unresolved placeholder '$placeholder'."
    }
}

if ($publishMode -ne 'self-contained-single-file') {
    throw "The reviewed notice assumes self-contained-single-file, but Directory.Build.props specifies '$publishMode'."
}

Write-Host "Microsoft .NET redistribution controls verified for SightAdapt $productVersion, SDK $sdkVersion, runtime $runtimeVersion, $rid."
