[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory,

    [string]$AssetsPath,

    [string]$AppProjectPath,

    [string]$PolicyPath,

    [string]$TestProjectPath,

    [string]$WorkflowPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($AssetsPath)) {
    $AssetsPath = Join-Path $root 'src\SightAdapt\obj\project.assets.json'
}
if ([string]::IsNullOrWhiteSpace($AppProjectPath)) {
    $AppProjectPath = Join-Path $root 'src\SightAdapt\SightAdapt.csproj'
}
if ([string]::IsNullOrWhiteSpace($PolicyPath)) {
    $PolicyPath = Join-Path $root 'release\dependency-policy.json'
}
if ([string]::IsNullOrWhiteSpace($TestProjectPath)) {
    $TestProjectPath = Join-Path $root 'tests\SightAdapt.Tests\SightAdapt.Tests.csproj'
}
if ([string]::IsNullOrWhiteSpace($WorkflowPath)) {
    $WorkflowPath = Join-Path $root '.github\workflows\build.yml'
}

$publish = [System.IO.Path]::GetFullPath($PublishDirectory)
if (-not [System.IO.Directory]::Exists($publish)) {
    throw "Publish directory does not exist: $publish"
}

$resolvedAssets = (Resolve-Path -LiteralPath $AssetsPath).Path
$resolvedAppProject = (Resolve-Path -LiteralPath $AppProjectPath).Path
$resolvedPolicy = (Resolve-Path -LiteralPath $PolicyPath).Path
$resolvedTestProject = (Resolve-Path -LiteralPath $TestProjectPath).Path
$resolvedWorkflow = (Resolve-Path -LiteralPath $WorkflowPath).Path

[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup
$productVersion = [string]$group.SightAdaptProductVersion
$sdkVersion = [string]$group.SightAdaptDotNetSdkVersion
$runtimeVersion = [string]$group.SightAdaptDotNetRuntimeVersion
$rid = [string]$group.SightAdaptRuntimeIdentifier

$noticeMetadataPath = Join-Path $publish 'DOTNET-NOTICE-METADATA.json'
if (-not [System.IO.File]::Exists($noticeMetadataPath)) {
    throw 'DOTNET-NOTICE-METADATA.json must be generated before SBOM generation.'
}

$noticeMetadata = Get-Content -LiteralPath $noticeMetadataPath -Raw | ConvertFrom-Json
if (-not [System.IO.File]::Exists((Join-Path $publish 'SightAdapt.exe'))) {
    throw 'SightAdapt.exe is missing from the publish directory.'
}
$assets = Get-Content -LiteralPath $resolvedAssets -Raw | ConvertFrom-Json
$policy = Get-Content -LiteralPath $resolvedPolicy -Raw | ConvertFrom-Json
$allowedLicenses = @($policy.allowedLicenseExpressions)
$deniedLicenses = @($policy.deniedLicenseExpressions)
$reviewLicenses = @($policy.reviewLicenseExpressions)
$failures = [System.Collections.Generic.List[string]]::new()
$components = [System.Collections.Generic.List[object]]::new()
$seen = @{}

function Get-PolicyEntry([string]$Name) {
    $property = @(
        $policy.components.PSObject.Properties |
            Where-Object { [string]$_.Name -eq $Name }
    ) | Select-Object -First 1
    if ($null -eq $property) {
        return $null
    }
    return $property.Value
}

function Add-Component(
    [string]$Name,
    [string]$Version,
    [string]$DetectedScope) {
    if ([string]::IsNullOrWhiteSpace($Name) -or
        [string]::IsNullOrWhiteSpace($Version)) {
        $failures.Add("A dependency has an empty name or version: '$Name' '$Version'.")
        return
    }

    $key = "$Name@$Version"
    if ($seen.ContainsKey($key)) {
        return
    }

    $entry = Get-PolicyEntry $Name
    if ($null -eq $entry) {
        $failures.Add("Dependency '$Name@$Version' has no reviewed policy entry.")
        return
    }

    $expectedVersion = [string]$entry.expectedVersion
    $versionSource = [string]$entry.expectedVersionSource
    if ($versionSource -eq 'product') {
        $expectedVersion = $productVersion
    }
    elseif ($versionSource -eq 'sdk') {
        $expectedVersion = $sdkVersion
    }
    elseif ($versionSource -eq 'runtime') {
        $expectedVersion = $runtimeVersion
    }

    if (-not [string]::IsNullOrWhiteSpace($expectedVersion) -and
        $Version -ne $expectedVersion) {
        $failures.Add(
            "Dependency '$Name' has version '$Version'; reviewed version is '$expectedVersion'.")
    }

    $license = [string]$entry.license
    if ($deniedLicenses -contains $license) {
        $failures.Add("Dependency '$Name@$Version' uses denied license '$license'.")
    }
    elseif ($allowedLicenses -notcontains $license) {
        $failures.Add(
            "Dependency '$Name@$Version' uses unreviewed license '$license'.")
    }

    $scope = [string]$entry.scope
    if ([string]::IsNullOrWhiteSpace($scope)) {
        $scope = $DetectedScope
    }

    $component = [pscustomobject]@{
        name = $Name
        version = $Version
        scope = $scope
        shipped = [bool]$entry.shipped
        supplier = [string]$entry.supplier
        license = $license
        source = [string]$entry.source
        purpose = [string]$entry.purpose
        status = if (($allowedLicenses -contains $license) -and
                     ($Version -eq $expectedVersion -or
                      [string]::IsNullOrWhiteSpace($expectedVersion))) {
            'approved'
        }
        else {
            'failed'
        }
    }
    $components.Add($component)
    $seen[$key] = $true
}

Add-Component 'SightAdapt' $productVersion 'shipped'
Add-Component 'Microsoft .NET SDK' $sdkVersion 'build'

foreach ($runtimePackage in @($noticeMetadata.runtimePackages)) {
    $parts = ([string]$runtimePackage) -split '/', 2
    if ($parts.Count -ne 2) {
        $failures.Add("Invalid runtime package identity '$runtimePackage'.")
        continue
    }
    Add-Component $parts[0] $parts[1] 'runtime'
}

$assetsFramework = @(
    $assets.project.frameworks.PSObject.Properties
) | Select-Object -First 1
if ($null -eq $assetsFramework) {
    $failures.Add('The application restore graph has no framework metadata.')
}
else {
    foreach ($dependency in @($assetsFramework.Value.downloadDependencies)) {
        $dependencyName = [string]$dependency.name
        $versionRange = [string]$dependency.version
        $versionMatch = [regex]::Match(
            $versionRange,
            '^\[([^,\]]+),\s*\1\]$')
        if (-not $versionMatch.Success) {
            $failures.Add(
                "Restore dependency '$dependencyName' does not use an exact version range: '$versionRange'.")
            continue
        }
        Add-Component $dependencyName $versionMatch.Groups[1].Value 'restore'
    }
}

[xml]$appProject = Get-Content -LiteralPath $resolvedAppProject
foreach ($reference in @($appProject.SelectNodes('/Project/ItemGroup/PackageReference'))) {
    Add-Component ([string]$reference.Include) ([string]$reference.Version) 'runtime'
}

[xml]$testProject = Get-Content -LiteralPath $resolvedTestProject
foreach ($reference in @($testProject.SelectNodes('/Project/ItemGroup/PackageReference'))) {
    Add-Component ([string]$reference.Include) ([string]$reference.Version) 'test'
}

$workflowText = Get-Content -LiteralPath $resolvedWorkflow -Raw
$actionMatches = [regex]::Matches(
    $workflowText,
    'uses:\s+([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)@([^\s#]+)')
foreach ($match in $actionMatches) {
    Add-Component ([string]$match.Groups[1].Value) ([string]$match.Groups[2].Value) 'build'
}

$summaryLines = [System.Collections.Generic.List[string]]::new()
$summaryLines.Add('# SightAdapt dependency inventory')
$summaryLines.Add('')
$summaryLines.Add(
    "Generated from the release dependency policy, the actual restore graph and exact .NET notice metadata for SightAdapt $productVersion ($rid).")
$summaryLines.Add('')
$summaryLines.Add('| Component | Version | Scope | Shipped | Supplier | License | Source |')
$summaryLines.Add('|---|---|---|---|---|---|---|')
foreach ($component in @($components | Sort-Object scope, name)) {
    $source = if ([string]::IsNullOrWhiteSpace($component.source)) {
        'not recorded'
    }
    else {
        $component.source
    }
    $summaryLines.Add(
        "| $($component.name) | `$($component.version)` | $($component.scope) | $($component.shipped) | $($component.supplier) | `$($component.license)` | $source |")
}
$summaryLines.Add('')
$summaryLines.Add('`SBOM.spdx.json` contains the machine-readable component and file inventory. `LICENSE-REPORT.json` records the policy result. Build and test dependencies are not included in the binary package unless `shipped` is `true`.')
$summaryText = $summaryLines -join [Environment]::NewLine
[System.IO.File]::WriteAllText(
    (Join-Path $publish 'DEPENDENCIES.md'),
    $summaryText + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

$created = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$report = [ordered]@{
    schemaVersion = 1
    generatedAtUtc = $created
    productVersion = $productVersion
    sdkVersion = $sdkVersion
    runtimeVersion = $runtimeVersion
    runtimeIdentifier = $rid
    policy = 'release/dependency-policy.json'
    result = if ($failures.Count -eq 0) { 'pass' } else { 'fail' }
    allowedLicenseExpressions = $allowedLicenses
    deniedLicenseExpressions = $deniedLicenses
    components = @($components | Sort-Object scope, name)
    reviewLicenseExpressions = $reviewLicenses
    failures = @($failures)
}
[System.IO.File]::WriteAllText(
    (Join-Path $publish 'LICENSE-REPORT.json'),
    ($report | ConvertTo-Json -Depth 8) + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

function Get-SpdxId([string]$Prefix, [string]$Value) {
    $safe = [regex]::Replace($Value, '[^A-Za-z0-9.-]', '-')
    return "SPDXRef-$Prefix-$safe"
}

$files = [System.Collections.Generic.List[object]]::new()
$fileRelationships = [System.Collections.Generic.List[object]]::new()
$sightAdaptId = Get-SpdxId 'Package' 'SightAdapt'
foreach ($file in @(Get-ChildItem -LiteralPath $publish -File -Recurse | Sort-Object FullName)) {
    $relative = [System.IO.Path]::GetRelativePath($publish, $file.FullName).Replace('\', '/')
    if ($relative -eq 'SBOM.spdx.json') {
        continue
    }
    $fileId = Get-SpdxId 'File' $relative
    $hash = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
    $files.Add([ordered]@{
        fileName = "./$relative"
        SPDXID = $fileId
        checksums = @(
            [ordered]@{
                algorithm = 'SHA256'
                checksumValue = $hash
            }
        )
        licenseConcluded = 'NOASSERTION'
        copyrightText = 'NOASSERTION'
    })
    $fileRelationships.Add([ordered]@{
        spdxElementId = $sightAdaptId
        relationshipType = 'CONTAINS'
        relatedSpdxElement = $fileId
    })
}

$packages = [System.Collections.Generic.List[object]]::new()
$relationships = [System.Collections.Generic.List[object]]::new()
foreach ($component in @($components | Sort-Object name)) {
    $packageId = Get-SpdxId 'Package' $component.name
    $package = [ordered]@{
        name = $component.name
        SPDXID = $packageId
        versionInfo = $component.version
        supplier = $component.supplier
        downloadLocation = if ([string]::IsNullOrWhiteSpace($component.source)) {
            'NOASSERTION'
        }
        else {
            $component.source
        }
        filesAnalyzed = $false
        licenseConcluded = $component.license
        licenseDeclared = $component.license
        copyrightText = 'NOASSERTION'
        primaryPackagePurpose = $component.purpose
        comment = "Scope: $($component.scope); shipped: $($component.shipped)."
    }

    if ($component.name -eq 'SightAdapt') {
        $exePath = Join-Path $publish 'SightAdapt.exe'
        if ([System.IO.File]::Exists($exePath)) {
            $package['checksums'] = @(
                [ordered]@{
                    algorithm = 'SHA256'
                    checksumValue = (Get-FileHash -LiteralPath $exePath -Algorithm SHA256).Hash
                }
            )
        }
    }
    elseif ($component.name -eq 'Microsoft .NET SDK' -and
            [string]$noticeMetadata.source.packageSha512 -match '^[0-9A-Fa-f]{128}$') {
        $package['checksums'] = @(
            [ordered]@{
                algorithm = 'SHA512'
                checksumValue = [string]$noticeMetadata.source.packageSha512
            }
        )
    }

    if ($component.name -like 'actions/*') {
        $package['externalRefs'] = @(
            [ordered]@{
                referenceCategory = 'PACKAGE-MANAGER'
                referenceType = 'purl'
                referenceLocator = "pkg:github/$($component.name)@$($component.version)"
            }
        )
    }
    elseif ($component.name -ne 'SightAdapt' -and
            $component.name -ne 'Microsoft .NET SDK') {
        $package['externalRefs'] = @(
            [ordered]@{
                referenceCategory = 'PACKAGE-MANAGER'
                referenceType = 'purl'
                referenceLocator = "pkg:nuget/$($component.name)@$($component.version)"
            }
        )
    }

    $packages.Add($package)
    if ($component.name -eq 'SightAdapt') {
        $relationships.Add([ordered]@{
            spdxElementId = 'SPDXRef-DOCUMENT'
            relationshipType = 'DESCRIBES'
            relatedSpdxElement = $packageId
        })
    }
    else {
        $relationships.Add([ordered]@{
            spdxElementId = $sightAdaptId
            relationshipType = 'DEPENDS_ON'
            relatedSpdxElement = $packageId
            comment = "Dependency scope: $($component.scope); shipped: $($component.shipped)."
        })
    }
}
foreach ($relationship in $fileRelationships) {
    $relationships.Add($relationship)
}

$namespaceId = [Guid]::NewGuid().ToString('N')
$sbom = [ordered]@{
    spdxVersion = 'SPDX-2.3'
    dataLicense = 'CC0-1.0'
    SPDXID = 'SPDXRef-DOCUMENT'
    name = "SightAdapt-$productVersion-$rid"
    documentNamespace = "https://github.com/KeyffMS/SightAdapt/sbom/$productVersion/$namespaceId"
    creationInfo = [ordered]@{
        created = $created
        creators = @(
            'Tool: SightAdapt tools/generate-sbom.ps1',
            'Organization: KeyffMS / aiteracja.pl'
        )
    }
    documentDescribes = @($sightAdaptId)
    packages = @($packages)
    files = @($files)
    relationships = @($relationships)
    hasExtractedLicensingInfos = @(
        [ordered]@{
            licenseId = 'LicenseRef-Microsoft-DotNet-Distribution'
            name = 'Microsoft .NET distribution terms and component notices'
            extractedText = 'See DOTNET-LICENSE-NOTICE.txt, THIRD-PARTY-NOTICES.txt, MICROSOFT-DOTNET-REDISTRIBUTION.txt and DOTNET-NOTICE-METADATA.json in the same package.'
            seeAlsos = @(
                'https://dotnet.microsoft.com/en-us/dotnet_library_license.htm',
                'https://github.com/dotnet/core/blob/main/license-information.md'
            )
        }
        [ordered]@{
            licenseId = 'LicenseRef-Microsoft-Windows-SDK-Reference-Terms'
            name = 'Microsoft Windows SDK .NET reference package terms'
            extractedText = 'Build-time reference package only; not shipped. Review the package terms and metadata at the recorded source URL.'
            seeAlsos = @(
                'https://www.nuget.org/packages/Microsoft.Windows.SDK.NET.Ref'
            )
        }
    )
}

[System.IO.File]::WriteAllText(
    (Join-Path $publish 'SBOM.spdx.json'),
    ($sbom | ConvertTo-Json -Depth 12) + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

if ($failures.Count -gt 0) {
    $details = $failures | ForEach-Object { " - $_" }
    throw "Dependency license review failed:`n$($details -join "`n")"
}

Write-Host "Generated SPDX 2.3 SBOM and approved license report for $($components.Count) components and $($files.Count) packaged files."
