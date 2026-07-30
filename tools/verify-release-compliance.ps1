[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DirectoryPath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ArchivePath,

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$ReportPath,

    [string]$ManifestPath =
        (Join-Path $PSScriptRoot '..\release\required-files.txt')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

$root = Split-Path -Parent $PSScriptRoot
$directory = (Resolve-Path -LiteralPath $DirectoryPath).Path
$archivePathResolved = (Resolve-Path -LiteralPath $ArchivePath).Path
$manifest = (Resolve-Path -LiteralPath $ManifestPath).Path
$reportFullPath = [System.IO.Path]::GetFullPath($ReportPath)
[System.IO.Directory]::CreateDirectory(
    [System.IO.Path]::GetDirectoryName($reportFullPath)) | Out-Null

[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup
$productVersion = [string]$group.SightAdaptProductVersion
$sdkVersion = [string]$group.SightAdaptDotNetSdkVersion
$runtimeVersion = [string]$group.SightAdaptDotNetRuntimeVersion
$rid = [string]$group.SightAdaptRuntimeIdentifier
$publishMode = [string]$group.SightAdaptPublishMode
$artifactName = ([string]$group.SightAdaptArtifactName).Replace(
    '$(SightAdaptProductVersion)',
    $productVersion)

[xml]$project = Get-Content -LiteralPath (Join-Path $root 'src\SightAdapt\SightAdapt.csproj')
$projectGroup = @($project.Project.PropertyGroup) | Select-Object -First 1
$targetFramework = [string]$projectGroup.TargetFramework

$requiredFiles = @(
    Get-Content -LiteralPath $manifest |
        ForEach-Object { $_.Trim() } |
        Where-Object {
            -not [string]::IsNullOrWhiteSpace($_) -and
            -not $_.StartsWith('#', [StringComparison]::Ordinal)
        }
)

$failures = [System.Collections.Generic.List[string]]::new()
$directoryFiles = @(
    Get-ChildItem -LiteralPath $directory -File -Recurse |
        ForEach-Object {
            [System.IO.Path]::GetRelativePath(
                $directory,
                $_.FullName).Replace('\', '/')
        } |
        Sort-Object
)

foreach ($requiredFile in $requiredFiles) {
    $path = Join-Path $directory $requiredFile
    if (-not [System.IO.File]::Exists($path)) {
        $failures.Add("Publish directory is missing required file '$requiredFile'.")
        continue
    }
    $item = Get-Item -LiteralPath $path
    if ($item.Length -le 0) {
        $failures.Add("Required file '$requiredFile' is empty in the publish directory.")
        continue
    }
    if ($requiredFile -match '\.(txt|md|json)$') {
        try {
            $text = Get-Content -LiteralPath $path -Raw -Encoding utf8
            if ([string]::IsNullOrWhiteSpace($text)) {
                $failures.Add("Required text file '$requiredFile' has no readable content.")
            }
            if ($text -match '(?im)\b(TODO|TBD|REPLACE_ME)\b|<\s*(version|date|name|url|email)\s*>|\{\{[^}\r\n]+\}\}') {
                $failures.Add("Required text file '$requiredFile' contains an unresolved template marker.")
            }
        }
        catch {
            $failures.Add("Required text file '$requiredFile' cannot be read as UTF-8: $($_.Exception.Message)")
        }
    }
}

$expectedArchiveName = "$artifactName.zip"
if ([System.IO.Path]::GetFileName($archivePathResolved) -ne $expectedArchiveName) {
    $failures.Add("Archive name '$([System.IO.Path]::GetFileName($archivePathResolved))' does not match '$expectedArchiveName'.")
}

try {
    & (Join-Path $PSScriptRoot 'test-release-package.ps1') `
        -ArchivePath $archivePathResolved `
        -ManifestPath $manifest
}
catch {
    $failures.Add("Final archive validation failed: $($_.Exception.Message)")
}

$archive = [System.IO.Compression.ZipFile]::OpenRead($archivePathResolved)
try {
    $archiveFiles = @(
        $archive.Entries |
            Where-Object { -not [string]::IsNullOrWhiteSpace($_.Name) } |
            ForEach-Object { $_.FullName.Replace('\', '/').TrimStart('/') } |
            Sort-Object
    )
}
finally {
    $archive.Dispose()
}

foreach ($path in @($directoryFiles | Where-Object { $archiveFiles -notcontains $_ })) {
    $failures.Add("Published file '$path' is missing from the final archive.")
}
foreach ($path in @($archiveFiles | Where-Object { $directoryFiles -notcontains $_ })) {
    $failures.Add("Final archive contains unexpected file '$path' not present in the staged directory.")
}

$noticeMetadataPath = Join-Path $directory 'DOTNET-NOTICE-METADATA.json'
$redistributionNoticePath = Join-Path $directory 'MICROSOFT-DOTNET-REDISTRIBUTION.txt'
$redistributionReviewPath = Join-Path $root 'release\dotnet-redistribution-review.json'
$licenseReportPath = Join-Path $directory 'LICENSE-REPORT.json'
$sbomPath = Join-Path $directory 'SBOM.spdx.json'

try {
    $noticeMetadata = Get-Content -LiteralPath $noticeMetadataPath -Raw | ConvertFrom-Json
    if ([string]$noticeMetadata.productVersion -ne $productVersion -or
        [string]$noticeMetadata.sdkVersion -ne $sdkVersion -or
        [string]$noticeMetadata.runtimeVersion -ne $runtimeVersion -or
        [string]$noticeMetadata.runtimeIdentifier -ne $rid -or
        [string]$noticeMetadata.publishMode -ne $publishMode) {
        $failures.Add('Exact-version notice metadata does not match release metadata.')
    }
}
catch {
    $failures.Add("DOTNET-NOTICE-METADATA.json cannot be validated: $($_.Exception.Message)")
}

$redistributionReviewDate = $null
$redistributionDecision = $null
$redistributionDecisionOwner = $null
$redistributionDecisionIssue = $null
try {
    $redistributionReview = Get-Content -LiteralPath $redistributionReviewPath -Raw | ConvertFrom-Json
    $redistributionReviewDate = [string]$redistributionReview.reviewedAt
    $redistributionDecision = [string]$redistributionReview.maintainerDecision.status
    $redistributionDecisionOwner = [string]$redistributionReview.maintainerDecision.decisionOwner
    $redistributionDecisionIssue = [int]$redistributionReview.maintainerDecision.decisionIssue
    if ($redistributionDecision -eq 'blocked') {
        $failures.Add('The .NET redistribution maintainer decision blocks release packaging.')
    }
}
catch {
    $failures.Add("The .NET redistribution review record cannot be read: $($_.Exception.Message)")
}

$redistributionNoticeSha256 = $null
if ([System.IO.File]::Exists($redistributionNoticePath)) {
    $redistributionNoticeSha256 =
        (Get-FileHash -LiteralPath $redistributionNoticePath -Algorithm SHA256).Hash
}

$licenseReport = $null
try {
    $licenseReport = Get-Content -LiteralPath $licenseReportPath -Raw | ConvertFrom-Json
    if ([int]$licenseReport.schemaVersion -ne 2) {
        $failures.Add("LICENSE-REPORT.json uses schema '$($licenseReport.schemaVersion)' instead of schema 2.")
    }
    if ([string]$licenseReport.result -ne 'pass') {
        $failures.Add("LICENSE-REPORT.json result is '$($licenseReport.result)', not 'pass'.")
    }
    if ([string]$licenseReport.productVersion -ne $productVersion -or
        [string]$licenseReport.sdkVersion -ne $sdkVersion -or
        [string]$licenseReport.runtimeVersion -ne $runtimeVersion -or
        [string]$licenseReport.runtimeIdentifier -ne $rid) {
        $failures.Add('License report metadata does not match release metadata.')
    }

    $reportComponents = @($licenseReport.components)
    if ($reportComponents.Count -ne [int]$licenseReport.inventory.packageCount) {
        $failures.Add('License report component count does not match its inventory summary.')
    }
    $calculatedTransitive = @($reportComponents | Where-Object {
        [bool]$_.transitive -and [string]$_.name -ne 'SightAdapt'
    }).Count
    if ($calculatedTransitive -ne [int]$licenseReport.inventory.transitivePackageCount) {
        $failures.Add('License report transitive-package count does not match its component inventory.')
    }
    if (@($licenseReport.dependencyEdges).Count -ne [int]$licenseReport.inventory.graphEdgeCount) {
        $failures.Add('License report dependency-edge count does not match its inventory summary.')
    }

    foreach ($component in $reportComponents) {
        $identity = "$($component.name)/$($component.version)"
        if ([string]$component.status -ne 'approved') {
            $failures.Add("License report component '$identity' is not approved.")
        }
        if ([string]$component.licenseConcluded -in @('', 'UNKNOWN', 'NOASSERTION')) {
            $failures.Add("License report component '$identity' has no resolved concluded license.")
        }
        $evidenceType = [string]$component.evidence.evidenceType
        if ([string]::IsNullOrWhiteSpace($evidenceType)) {
            $failures.Add("License report component '$identity' has no evidence type.")
        }
        if ($evidenceType -eq 'nuget-package') {
            if ([string]$component.evidence.packageSha512 -notmatch '^[0-9A-Fa-f]{128}$') {
                $failures.Add("NuGet component '$identity' lacks package SHA-512 evidence.")
            }
            if ([string]$component.evidence.nuspecSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
                $failures.Add("NuGet component '$identity' lacks nuspec SHA-256 evidence.")
            }
        }
        elseif ([string]$component.evidence.licenseEvidenceSha256 -notmatch '^[0-9A-Fa-f]{64}$' -and
                [string]$component.name -ne 'Microsoft .NET SDK') {
            $failures.Add("Component '$identity' lacks a policy or repository license-evidence hash.")
        }
    }
}
catch {
    $failures.Add("LICENSE-REPORT.json cannot be validated: $($_.Exception.Message)")
}

$sbom = $null
try {
    $sbom = Get-Content -LiteralPath $sbomPath -Raw | ConvertFrom-Json
    if ([string]$sbom.spdxVersion -ne 'SPDX-2.3') {
        $failures.Add("SBOM uses '$($sbom.spdxVersion)' instead of SPDX-2.3.")
    }
    if ([string]$sbom.name -ne "SightAdapt-$productVersion-$rid") {
        $failures.Add("SBOM name '$($sbom.name)' does not match the release.")
    }
    $sbomFiles = @($sbom.files | ForEach-Object { ([string]$_.fileName).TrimStart('.', '/') })
    foreach ($path in $directoryFiles) {
        if ($path -ne 'SBOM.spdx.json' -and $sbomFiles -notcontains $path) {
            $failures.Add("SBOM does not contain shipped file '$path'.")
        }
    }
    $sightAdaptPackage = @($sbom.packages | Where-Object {
        [string]$_.name -eq 'SightAdapt' -and
        [string]$_.versionInfo -eq $productVersion
    }) | Select-Object -First 1
    if ($null -eq $sightAdaptPackage) {
        $failures.Add('SBOM does not identify the SightAdapt single-file packaging container.')
    }
    elseif ($null -ne $licenseReport) {
        $reportComponents = @($licenseReport.components)
        if (@($sbom.packages).Count -ne $reportComponents.Count) {
            $failures.Add('SBOM package count does not match LICENSE-REPORT.json.')
        }
        foreach ($component in $reportComponents) {
            $package = @($sbom.packages | Where-Object {
                [string]$_.name -eq [string]$component.name -and
                [string]$_.versionInfo -eq [string]$component.version
            }) | Select-Object -First 1
            if ($null -eq $package) {
                $failures.Add("SBOM does not contain component '$($component.name)/$($component.version)'.")
                continue
            }
            if ([string]$package.licenseConcluded -ne [string]$component.licenseConcluded -or
                [string]$package.licenseDeclared -ne [string]$component.licenseDeclared) {
                $failures.Add("SBOM licenses for '$($component.name)/$($component.version)' do not match the license report.")
            }
            if ([string]$component.name -eq 'SightAdapt') {
                continue
            }

            $runtimeRelationship = @($sbom.relationships | Where-Object {
                [string]$_.spdxElementId -eq [string]$sightAdaptPackage.SPDXID -and
                [string]$_.relationshipType -eq 'DEPENDS_ON' -and
                [string]$_.relatedSpdxElement -eq [string]$package.SPDXID
            }).Count -gt 0
            $testRelationship = @($sbom.relationships | Where-Object {
                [string]$_.spdxElementId -eq [string]$package.SPDXID -and
                [string]$_.relationshipType -eq 'TEST_DEPENDENCY_OF' -and
                [string]$_.relatedSpdxElement -eq [string]$sightAdaptPackage.SPDXID
            }).Count -gt 0
            $buildRelationship = @($sbom.relationships | Where-Object {
                [string]$_.spdxElementId -eq [string]$package.SPDXID -and
                [string]$_.relationshipType -eq 'BUILD_DEPENDENCY_OF' -and
                [string]$_.relatedSpdxElement -eq [string]$sightAdaptPackage.SPDXID
            }).Count -gt 0

            if ([bool]$component.shipped) {
                if (-not $runtimeRelationship) {
                    $failures.Add("Shipped component '$($component.identity)' lacks SightAdapt DEPENDS_ON relationship.")
                }
            }
            elseif ([string]$component.scope -eq 'test') {
                if (-not $testRelationship -or $runtimeRelationship) {
                    $failures.Add("Test component '$($component.identity)' is not represented exclusively as TEST_DEPENDENCY_OF SightAdapt.")
                }
            }
            elseif (-not $buildRelationship -or $runtimeRelationship) {
                $failures.Add("Build component '$($component.identity)' is not represented exclusively as BUILD_DEPENDENCY_OF SightAdapt.")
            }
        }
    }
}
catch {
    $failures.Add("SBOM.spdx.json cannot be validated: $($_.Exception.Message)")
}

$report = [ordered]@{
    schemaVersion = 2
    generatedAtUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
    result = if ($failures.Count -eq 0) { 'pass' } else { 'fail' }
    productVersion = $productVersion
    sdkVersion = $sdkVersion
    runtimeVersion = $runtimeVersion
    targetFramework = $targetFramework
    runtimeIdentifier = $rid
    publishMode = $publishMode
    redistributionReviewDate = $redistributionReviewDate
    redistributionMaintainerDecision = $redistributionDecision
    redistributionDecisionOwner = $redistributionDecisionOwner
    redistributionDecisionIssue = $redistributionDecisionIssue
    redistributionNoticeSha256 = $redistributionNoticeSha256
    licenseReportSchemaVersion = if ($null -ne $licenseReport) { [int]$licenseReport.schemaVersion } else { $null }
    licenseReportPackageCount = if ($null -ne $licenseReport) { @($licenseReport.components).Count } else { $null }
    sbomPackageCount = if ($null -ne $sbom) { @($sbom.packages).Count } else { $null }
    sbomRelationshipCount = if ($null -ne $sbom) { @($sbom.relationships).Count } else { $null }
    artifactName = $artifactName
    archiveFile = [System.IO.Path]::GetFileName($archivePathResolved)
    archiveSha256 = (Get-FileHash -LiteralPath $archivePathResolved -Algorithm SHA256).Hash
    manifest = 'release/required-files.txt'
    requiredFiles = $requiredFiles
    stagedFiles = $directoryFiles
    archiveFiles = $archiveFiles
    failures = @($failures)
}

[System.IO.File]::WriteAllText(
    $reportFullPath,
    ($report | ConvertTo-Json -Depth 8) + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

if ($failures.Count -gt 0) {
    $details = $failures | ForEach-Object { " - $_" }
    throw "Release compliance gate failed:`n$($details -join "`n")"
}

Write-Host "Release compliance gate passed. Report: $reportFullPath"
