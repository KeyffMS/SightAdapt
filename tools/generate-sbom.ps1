[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory,

    [string]$AssetsPath,

    [string]$TestAssetsPath,

    [string]$PolicyPath,

    [string]$WorkflowPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($AssetsPath)) {
    $AssetsPath = Join-Path $root 'src\SightAdapt\obj\project.assets.json'
}
if ([string]::IsNullOrWhiteSpace($TestAssetsPath)) {
    $TestAssetsPath = Join-Path $root 'tests\SightAdapt.Tests\obj\project.assets.json'
}
if ([string]::IsNullOrWhiteSpace($PolicyPath)) {
    $PolicyPath = Join-Path $root 'release\dependency-policy.json'
}
if ([string]::IsNullOrWhiteSpace($WorkflowPath)) {
    $WorkflowPath = Join-Path $root '.github\workflows\build.yml'
}

$publish = [System.IO.Path]::GetFullPath($PublishDirectory)
if (-not [System.IO.Directory]::Exists($publish)) {
    throw "Publish directory does not exist: $publish"
}
$resolvedAssets = (Resolve-Path -LiteralPath $AssetsPath).Path
$resolvedTestAssets = (Resolve-Path -LiteralPath $TestAssetsPath).Path
$resolvedPolicy = (Resolve-Path -LiteralPath $PolicyPath).Path
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
if ([int]$noticeMetadata.schemaVersion -lt 2 -or $null -eq $noticeMetadata.componentCoverage) {
    throw 'DOTNET-NOTICE-METADATA.json does not contain component-level package coverage.'
}
if (-not [System.IO.File]::Exists((Join-Path $publish 'SightAdapt.exe'))) {
    throw 'SightAdapt.exe is missing from the publish directory.'
}

$policy = Get-Content -LiteralPath $resolvedPolicy -Raw | ConvertFrom-Json
if ([int]$policy.schemaVersion -ne 2) {
    throw "Unsupported dependency-policy schema '$($policy.schemaVersion)'."
}
$allowedLicenses = @($policy.allowedLicenseExpressions)
$deniedLicenses = @($policy.deniedLicenseExpressions)
$reviewLicenses = @($policy.reviewLicenseExpressions)
$policySha256 = (Get-FileHash -LiteralPath $resolvedPolicy -Algorithm SHA256).Hash.ToLowerInvariant()

$script:failures = [System.Collections.Generic.List[string]]::new()
$script:packageRecords = @{}
$script:graphEdges = [System.Collections.Generic.List[object]]::new()
$script:edgeKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$script:packageFolders = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
$script:coveragePackages = @{}

function Convert-Base64Sha512ToHex([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }
    try {
        $bytes = [Convert]::FromBase64String($Value.Trim())
    }
    catch {
        return $null
    }
    if ($bytes.Length -ne 64) {
        return $null
    }
    return [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
}

function Get-FileSha256([string]$Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash.ToLowerInvariant()
}

function Get-NormalizedIdentity([string]$Name, [string]$Version) {
    return "$Name/$Version"
}

function Split-PackageIdentity([string]$Identity) {
    $parts = $Identity -split '/', 2
    if ($parts.Count -ne 2 -or
        [string]::IsNullOrWhiteSpace($parts[0]) -or
        [string]::IsNullOrWhiteSpace($parts[1])) {
        throw "Invalid package identity '$Identity'."
    }
    return [pscustomobject]@{
        Name = $parts[0]
        Version = $parts[1]
    }
}

function Get-PolicyEntry([string]$Name) {
    $property = @(
        $policy.components.PSObject.Properties |
            Where-Object { [string]$_.Name -ieq $Name }
    ) | Select-Object -First 1
    if ($null -eq $property) {
        return $null
    }
    return $property.Value
}

function Test-AnyPattern([string]$Value, [object[]]$Patterns) {
    foreach ($pattern in $Patterns) {
        if ($Value -like [string]$pattern) {
            return $true
        }
    }
    return $false
}

function Register-Package(
    [string]$Name,
    [string]$Version,
    [string]$Scope,
    [bool]$Direct,
    [string]$Graph,
    [string]$PackageSha512,
    [string]$PackagePath) {
    if ([string]::IsNullOrWhiteSpace($Name) -or
        [string]::IsNullOrWhiteSpace($Version)) {
        $script:failures.Add("A dependency has an empty name or version: '$Name' '$Version'.")
        return $null
    }

    $identity = Get-NormalizedIdentity $Name $Version
    $key = $identity.ToLowerInvariant()
    if (-not $script:packageRecords.ContainsKey($key)) {
        $script:packageRecords[$key] = [ordered]@{
            name = $Name
            version = $Version
            identity = $identity
            scopes = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            graphs = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
            direct = $false
            shipped = $false
            packageSha512 = $null
            packagePath = $null
            packageUrl = $null
            repositoryUrl = $null
            repositoryCommit = $null
            coverageLicense = $null
        }
    }
    $record = $script:packageRecords[$key]
    if (-not [string]::IsNullOrWhiteSpace($Scope)) {
        $record.scopes.Add($Scope) | Out-Null
    }
    if (-not [string]::IsNullOrWhiteSpace($Graph)) {
        $record.graphs.Add($Graph) | Out-Null
    }
    if ($Direct) {
        $record.direct = $true
    }
    if (-not [string]::IsNullOrWhiteSpace($PackageSha512)) {
        $normalizedSha = $PackageSha512.ToLowerInvariant()
        if (-not [string]::IsNullOrWhiteSpace([string]$record.packageSha512) -and
            [string]$record.packageSha512 -ne $normalizedSha) {
            $script:failures.Add("Package '$identity' has conflicting SHA-512 evidence.")
        }
        else {
            $record.packageSha512 = $normalizedSha
        }
    }
    if (-not [string]::IsNullOrWhiteSpace($PackagePath)) {
        $record.packagePath = $PackagePath.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
    }
    return $record
}

function Add-GraphEdge([string]$FromIdentity, [string]$ToIdentity, [string]$Graph) {
    if ([string]::IsNullOrWhiteSpace($FromIdentity) -or
        [string]::IsNullOrWhiteSpace($ToIdentity) -or
        $FromIdentity -ieq $ToIdentity) {
        return
    }
    $key = "$Graph|$FromIdentity|$ToIdentity"
    if ($script:edgeKeys.Add($key)) {
        $script:graphEdges.Add([ordered]@{
            from = $FromIdentity
            to = $ToIdentity
            graph = $Graph
        })
    }
}

function Resolve-ExactDownloadVersion([string]$VersionRange) {
    $match = [regex]::Match($VersionRange, '^\[([^,\]]+),\s*\1\]$')
    if ($match.Success) {
        return $match.Groups[1].Value
    }
    return $null
}

function Read-RestoreGraph([string]$Path, [string]$GraphName) {
    $assets = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
    foreach ($folder in @($assets.packageFolders.PSObject.Properties)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$folder.Name)) {
            $script:packageFolders.Add([string]$folder.Name) | Out-Null
        }
    }

    $framework = @($assets.project.frameworks.PSObject.Properties) | Select-Object -First 1
    if ($null -eq $framework) {
        $script:failures.Add("Restore graph '$GraphName' has no project framework metadata.")
        return
    }
    $directNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $frameworkDependencies = $framework.Value.PSObject.Properties['dependencies']
    if ($null -ne $frameworkDependencies) {
        foreach ($dependency in @($frameworkDependencies.Value.PSObject.Properties)) {
            $directNames.Add([string]$dependency.Name) | Out-Null
        }
    }

    $downloadDependencies = $framework.Value.PSObject.Properties['downloadDependencies']
    foreach ($download in @($(if ($null -ne $downloadDependencies) { $downloadDependencies.Value } else { @() }))) {
        $name = [string]$download.name
        $version = Resolve-ExactDownloadVersion ([string]$download.version)
        if ([string]::IsNullOrWhiteSpace($version)) {
            $script:failures.Add("Framework download dependency '$name' in '$GraphName' is not exact: '$($download.version)'.")
            continue
        }
        Register-Package $name $version "$GraphName-restore" $false $GraphName $null $null | Out-Null
    }

    $graphIdentityByName = @{}
    foreach ($libraryProperty in @($assets.libraries.PSObject.Properties)) {
        $library = $libraryProperty.Value
        if ([string]$library.type -ne 'package') {
            continue
        }
        $parts = Split-PackageIdentity ([string]$libraryProperty.Name)
        $scope = if ($directNames.Contains($parts.Name)) {
            "$GraphName-direct"
        }
        else {
            "$GraphName-transitive"
        }
        $sha512 = Convert-Base64Sha512ToHex ([string]$library.sha512)
        Register-Package `
            $parts.Name `
            $parts.Version `
            $scope `
            ($directNames.Contains($parts.Name)) `
            $GraphName `
            $sha512 `
            ([string]$library.path) | Out-Null
        $graphIdentityByName[$parts.Name.ToLowerInvariant()] = Get-NormalizedIdentity $parts.Name $parts.Version
    }

    foreach ($targetProperty in @($assets.targets.PSObject.Properties)) {
        $targetIdentityByName = @{}
        foreach ($entry in @($targetProperty.Value.PSObject.Properties)) {
            $entryValue = $entry.Value
            if ([string]$entryValue.type -ne 'package') {
                continue
            }
            $parts = Split-PackageIdentity ([string]$entry.Name)
            $targetIdentityByName[$parts.Name.ToLowerInvariant()] = Get-NormalizedIdentity $parts.Name $parts.Version
        }
        foreach ($entry in @($targetProperty.Value.PSObject.Properties)) {
            $entryValue = $entry.Value
            if ([string]$entryValue.type -ne 'package') {
                continue
            }
            $parentParts = Split-PackageIdentity ([string]$entry.Name)
            $parentIdentity = Get-NormalizedIdentity $parentParts.Name $parentParts.Version
            $dependenciesProperty = $entryValue.PSObject.Properties['dependencies']
            if ($null -eq $dependenciesProperty) {
                continue
            }
            foreach ($dependency in @($dependenciesProperty.Value.PSObject.Properties)) {
                $dependencyName = [string]$dependency.Name
                $lookup = $dependencyName.ToLowerInvariant()
                if ($targetIdentityByName.ContainsKey($lookup)) {
                    Add-GraphEdge $parentIdentity ([string]$targetIdentityByName[$lookup]) $GraphName
                }
                elseif ($graphIdentityByName.ContainsKey($lookup)) {
                    Add-GraphEdge $parentIdentity ([string]$graphIdentityByName[$lookup]) $GraphName
                }
                else {
                    $script:failures.Add("Dependency '$dependencyName' referenced by '$parentIdentity' is missing from restore graph '$GraphName'.")
                }
            }
        }
    }
}

foreach ($coveragePackage in @($noticeMetadata.componentCoverage.packages)) {
    $parts = Split-PackageIdentity ([string]$coveragePackage.package)
    $record = Register-Package `
        $parts.Name `
        $parts.Version `
        'shipped' `
        $false `
        'publish' `
        ([string]$coveragePackage.packageSha512) `
        $null
    $record.shipped = $true
    $record.packageUrl = [string]$coveragePackage.packageUrl
    $record.repositoryUrl = [string]$coveragePackage.repositoryUrl
    $record.repositoryCommit = [string]$coveragePackage.repositoryCommit
    $record.coverageLicense = [string]$coveragePackage.policyLicense
    $script:coveragePackages[$record.identity.ToLowerInvariant()] = $coveragePackage
}

Read-RestoreGraph $resolvedAssets 'application'
Read-RestoreGraph $resolvedTestAssets 'test'

function Register-ManualComponent(
    [string]$Name,
    [string]$Version,
    [string]$Scope) {
    Register-Package $Name $Version $Scope $true $Scope $null $null | Out-Null
}

Register-ManualComponent 'Microsoft .NET SDK' $sdkVersion 'build'
$workflowText = Get-Content -LiteralPath $resolvedWorkflow -Raw
$actionMatches = [regex]::Matches(
    $workflowText,
    'uses:\s+([A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+)@([^\s#]+)')
foreach ($match in $actionMatches) {
    Register-ManualComponent `
        ([string]$match.Groups[1].Value) `
        ([string]$match.Groups[2].Value) `
        'build-action'
}

function Find-PackageRoot([System.Collections.IDictionary]$Record) {
    foreach ($folder in $script:packageFolders) {
        $candidate = if (-not [string]::IsNullOrWhiteSpace([string]$Record.packagePath)) {
            Join-Path $folder ([string]$Record.packagePath)
        }
        else {
            Join-Path (Join-Path $folder ([string]$Record.name).ToLowerInvariant()) ([string]$Record.version)
        }
        if ([System.IO.Directory]::Exists($candidate)) {
            return (Resolve-Path -LiteralPath $candidate).Path
        }
    }
    return $null
}

function Get-PrimaryScope([System.Collections.IDictionary]$Record) {
    if ([bool]$Record.shipped) {
        return 'shipped'
    }
    if (@($Record.scopes | Where-Object { $_ -like 'application-*' }).Count -gt 0) {
        return 'application-build'
    }
    if (@($Record.scopes | Where-Object { $_ -like 'test-*' }).Count -gt 0) {
        return 'test'
    }
    if (@($Record.scopes | Where-Object { $_ -like 'build*' }).Count -gt 0) {
        return 'build'
    }
    return 'other'
}

function Get-NuGetEvidence([System.Collections.IDictionary]$Record) {
    $rootPath = Find-PackageRoot $Record
    if ([string]::IsNullOrWhiteSpace($rootPath)) {
        return [ordered]@{
            evidenceType = 'missing-package-cache'
            packageSha512 = [string]$Record.packageSha512
            nuspecSha256 = $null
            declaredLicense = 'UNKNOWN'
            licenseType = 'missing'
            licenseValue = $null
            licenseEvidenceSha256 = $null
            repositoryUrl = [string]$Record.repositoryUrl
            repositoryCommit = [string]$Record.repositoryCommit
            authors = $null
        }
    }

    $nuspec = Get-ChildItem -LiteralPath $rootPath -File -Filter '*.nuspec' |
        Select-Object -First 1
    if ($null -eq $nuspec) {
        return [ordered]@{
            evidenceType = 'missing-nuspec'
            packageSha512 = [string]$Record.packageSha512
            nuspecSha256 = $null
            declaredLicense = 'UNKNOWN'
            licenseType = 'missing'
            licenseValue = $null
            licenseEvidenceSha256 = $null
            repositoryUrl = [string]$Record.repositoryUrl
            repositoryCommit = [string]$Record.repositoryCommit
            authors = $null
        }
    }

    [xml]$nuspecXml = Get-Content -LiteralPath $nuspec.FullName
    $licenseNode = $nuspecXml.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='license']")
    $licenseUrlNode = $nuspecXml.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='licenseUrl']")
    $repositoryNode = $nuspecXml.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='repository']")
    $authorsNode = $nuspecXml.SelectSingleNode("//*[local-name()='metadata']/*[local-name()='authors']")

    $licenseType = 'missing'
    $licenseValue = $null
    $declaredLicense = 'UNKNOWN'
    $licenseEvidenceSha256 = $null
    if ($null -ne $licenseNode) {
        $licenseType = [string]$licenseNode.type
        $licenseValue = ([string]$licenseNode.InnerText).Trim()
        if ($licenseType -ieq 'expression') {
            $declaredLicense = $licenseValue
        }
        elseif ($licenseType -ieq 'file') {
            $declaredLicense = 'LicenseRef-NuGet-License-File'
            $licensePath = Join-Path $rootPath $licenseValue.Replace('/', [System.IO.Path]::DirectorySeparatorChar)
            if ([System.IO.File]::Exists($licensePath)) {
                $licenseEvidenceSha256 = Get-FileSha256 $licensePath
            }
        }
    }
    elseif ($null -ne $licenseUrlNode -and
            -not [string]::IsNullOrWhiteSpace([string]$licenseUrlNode.InnerText)) {
        $licenseType = 'url'
        $licenseValue = ([string]$licenseUrlNode.InnerText).Trim()
        $declaredLicense = 'LicenseRef-NuGet-License-URL'
    }

    $packageSha512 = [string]$Record.packageSha512
    if ([string]::IsNullOrWhiteSpace($packageSha512)) {
        $shaFile = Get-ChildItem -LiteralPath $rootPath -File -Filter '*.nupkg.sha512' |
            Select-Object -First 1
        if ($null -ne $shaFile) {
            $packageSha512 = Convert-Base64Sha512ToHex (
                Get-Content -LiteralPath $shaFile.FullName -Raw)
        }
    }

    return [ordered]@{
        evidenceType = 'nuget-package'
        packageSha512 = $packageSha512
        nuspecSha256 = Get-FileSha256 $nuspec.FullName
        declaredLicense = $declaredLicense
        licenseType = $licenseType
        licenseValue = $licenseValue
        licenseEvidenceSha256 = $licenseEvidenceSha256
        repositoryUrl = if ($null -ne $repositoryNode) { [string]$repositoryNode.url } else { [string]$Record.repositoryUrl }
        repositoryCommit = if ($null -ne $repositoryNode) { [string]$repositoryNode.commit } else { [string]$Record.repositoryCommit }
        authors = if ($null -ne $authorsNode) { ([string]$authorsNode.InnerText).Trim() } else { $null }
    }
}

function Get-ExpectedVersion([object]$Entry) {
    if ($null -eq $Entry) {
        return $null
    }
    $versionSource = [string]$Entry.expectedVersionSource
    if ($versionSource -eq 'product') { return $productVersion }
    if ($versionSource -eq 'sdk') { return $sdkVersion }
    if ($versionSource -eq 'runtime') { return $runtimeVersion }
    return [string]$Entry.expectedVersion
}

function Resolve-LicenseDecision(
    [System.Collections.IDictionary]$Record,
    [System.Collections.IDictionary]$Evidence,
    [object]$PolicyEntry) {
    $declared = [string]$Evidence.declaredLicense
    $concluded = $declared
    $decisionSource = 'package-metadata'

    if (-not [string]::IsNullOrWhiteSpace([string]$Record.coverageLicense)) {
        $concluded = [string]$Record.coverageLicense
        $decisionSource = 'component-coverage'
    }
    elseif ($null -ne $PolicyEntry -and
            -not [string]::IsNullOrWhiteSpace([string]$PolicyEntry.license) -and
            ($declared -like 'LicenseRef-NuGet-*' -or $declared -eq 'UNKNOWN')) {
        $concluded = [string]$PolicyEntry.license
        $decisionSource = 'reviewed-policy-override'
    }
    elseif ($null -ne $PolicyEntry -and
            -not [string]::IsNullOrWhiteSpace([string]$PolicyEntry.license) -and
            [string]$PolicyEntry.license -ne $declared) {
        $script:failures.Add(
            "Package '$($Record.identity)' declares '$declared' but policy expects '$($PolicyEntry.license)'.")
    }

    if ([string]::IsNullOrWhiteSpace($concluded)) {
        $concluded = 'UNKNOWN'
    }
    $status = 'approved'
    if ($concluded -in @('UNKNOWN', 'NOASSERTION')) {
        $script:failures.Add("Dependency '$($Record.identity)' has no resolved license.")
        $status = 'failed'
    }
    elseif (Test-AnyPattern $concluded $deniedLicenses) {
        $script:failures.Add("Dependency '$($Record.identity)' uses denied license '$concluded'.")
        $status = 'failed'
    }
    elseif (Test-AnyPattern $concluded $allowedLicenses) {
        $status = 'approved'
    }
    elseif (Test-AnyPattern $concluded $reviewLicenses) {
        $script:failures.Add("Dependency '$($Record.identity)' requires explicit license review for '$concluded'.")
        $status = 'failed'
    }
    else {
        $script:failures.Add("Dependency '$($Record.identity)' uses unreviewed license '$concluded'.")
        $status = 'failed'
    }

    return [ordered]@{
        declared = $declared
        concluded = $concluded
        decisionSource = $decisionSource
        status = $status
    }
}

$components = [System.Collections.Generic.List[object]]::new()
foreach ($recordKey in @($script:packageRecords.Keys | Sort-Object)) {
    $record = $script:packageRecords[$recordKey]
    $policyEntry = Get-PolicyEntry ([string]$record.name)
    $expectedVersion = Get-ExpectedVersion $policyEntry
    if (-not [string]::IsNullOrWhiteSpace($expectedVersion) -and
        [string]$record.version -ne $expectedVersion) {
        $script:failures.Add(
            "Dependency '$($record.name)' has version '$($record.version)'; reviewed version is '$expectedVersion'.")
    }

    $isManual = [string]$record.name -eq 'Microsoft .NET SDK' -or
        ([string]$record.name).StartsWith('actions/', [StringComparison]::OrdinalIgnoreCase)
    $evidence = if ($isManual) {
        [ordered]@{
            evidenceType = 'reviewed-policy'
            packageSha512 = if ([string]$record.name -eq 'Microsoft .NET SDK') { [string]$noticeMetadata.source.packageSha512 } else { $null }
            nuspecSha256 = $null
            declaredLicense = if ($null -ne $policyEntry) { [string]$policyEntry.license } else { 'UNKNOWN' }
            licenseType = 'policy'
            licenseValue = if ($null -ne $policyEntry) { [string]$policyEntry.license } else { $null }
            licenseEvidenceSha256 = $policySha256
            repositoryUrl = if ($null -ne $policyEntry) { [string]$policyEntry.source } else { $null }
            repositoryCommit = [string]$record.version
            authors = if ($null -ne $policyEntry) { [string]$policyEntry.supplier } else { $null }
        }
    }
    else {
        Get-NuGetEvidence $record
    }

    $licenseDecision = Resolve-LicenseDecision $record $evidence $policyEntry
    if ([string]$evidence.packageSha512 -notmatch '^[0-9A-Fa-f]{128}$' -and -not $isManual) {
        $script:failures.Add("Dependency '$($record.identity)' lacks exact package SHA-512 evidence.")
    }
    if ([string]$evidence.nuspecSha256 -notmatch '^[0-9A-Fa-f]{64}$' -and -not $isManual) {
        $script:failures.Add("Dependency '$($record.identity)' lacks exact .nuspec SHA-256 evidence.")
    }

    $supplier = if ($null -ne $policyEntry -and
                    -not [string]::IsNullOrWhiteSpace([string]$policyEntry.supplier)) {
        [string]$policyEntry.supplier
    }
    elseif (-not [string]::IsNullOrWhiteSpace([string]$evidence.authors)) {
        "Organization: $($evidence.authors)"
    }
    else {
        'NOASSERTION'
    }
    $source = if ($null -ne $policyEntry -and
                  -not [string]::IsNullOrWhiteSpace([string]$policyEntry.source)) {
        [string]$policyEntry.source
    }
    elseif (-not [string]::IsNullOrWhiteSpace([string]$evidence.repositoryUrl)) {
        [string]$evidence.repositoryUrl
    }
    elseif (-not [string]::IsNullOrWhiteSpace([string]$record.packageUrl)) {
        [string]$record.packageUrl
    }
    else {
        "https://www.nuget.org/packages/$($record.name)/$($record.version)"
    }

    $scope = Get-PrimaryScope $record
    $components.Add([ordered]@{
        name = [string]$record.name
        version = [string]$record.version
        identity = [string]$record.identity
        scope = $scope
        scopes = @($record.scopes | Sort-Object)
        graphs = @($record.graphs | Sort-Object)
        direct = [bool]$record.direct
        transitive = -not [bool]$record.direct
        shipped = [bool]$record.shipped
        supplier = $supplier
        licenseDeclared = [string]$licenseDecision.declared
        licenseConcluded = [string]$licenseDecision.concluded
        licenseDecisionSource = [string]$licenseDecision.decisionSource
        source = $source
        repositoryCommit = [string]$evidence.repositoryCommit
        purpose = if ($scope -eq 'shipped') { 'LIBRARY' } else { 'OTHER' }
        status = [string]$licenseDecision.status
        evidence = $evidence
    })
}

$sightAdaptComponent = [ordered]@{
    name = 'SightAdapt'
    version = $productVersion
    identity = "SightAdapt/$productVersion"
    scope = 'shipped'
    scopes = @('shipped')
    graphs = @('application')
    direct = $true
    transitive = $false
    shipped = $true
    supplier = 'Organization: KeyffMS / aiteracja.pl'
    licenseDeclared = 'MIT'
    licenseConcluded = 'MIT'
    licenseDecisionSource = 'repository-license'
    source = 'https://github.com/KeyffMS/SightAdapt'
    repositoryCommit = $null
    purpose = 'APPLICATION'
    status = 'approved'
    evidence = [ordered]@{
        evidenceType = 'repository-license'
        packageSha512 = $null
        nuspecSha256 = $null
        declaredLicense = 'MIT'
        licenseType = 'file'
        licenseValue = 'LICENSE'
        licenseEvidenceSha256 = Get-FileSha256 (Join-Path $root 'LICENSE')
        repositoryUrl = 'https://github.com/KeyffMS/SightAdapt'
        repositoryCommit = $null
        authors = 'KeyffMS / aiteracja.pl'
    }
}
$components.Add($sightAdaptComponent)

$summaryLines = [System.Collections.Generic.List[string]]::new()
$summaryLines.Add('# SightAdapt dependency inventory')
$summaryLines.Add('')
$summaryLines.Add("Generated from the complete application and test restore graphs, exact publish-component coverage and reviewed build-tool policy for SightAdapt $productVersion ($rid).")
$summaryLines.Add('')
$summaryLines.Add('| Component | Version | Scope | Direct | Shipped | License | Evidence | Source |')
$summaryLines.Add('|---|---|---|---:|---:|---|---|---|')
foreach ($component in @($components | Sort-Object scope, name, version)) {
    $evidenceLabel = [string]$component.evidence.evidenceType
    if ([string]$component.evidence.nuspecSha256 -match '^[0-9A-Fa-f]{64}$') {
        $evidenceLabel += ('; nuspec SHA-256 `{0}`' -f [string]$component.evidence.nuspecSha256)
    }
    $summaryLines.Add((
        '| {0} | `{1}` | {2} | {3} | {4} | `{5}` | {6} | {7} |' -f
        $component.name,
        $component.version,
        $component.scope,
        $component.direct,
        $component.shipped,
        $component.licenseConcluded,
        $evidenceLabel,
        $component.source))
}
$summaryLines.Add('')
$summaryLines.Add('`SBOM.spdx.json` contains the same package inventory, dependency graph and packaged-file hashes. `LICENSE-REPORT.json` contains the complete evidence and policy result. Build and test components are not represented as runtime dependencies of SightAdapt.')
[System.IO.File]::WriteAllText(
    (Join-Path $publish 'DEPENDENCIES.md'),
    ($summaryLines -join [Environment]::NewLine) + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

$created = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
$applicationPackages = @($components | Where-Object { @($_.graphs) -contains 'application' })
$testPackages = @($components | Where-Object { @($_.graphs) -contains 'test' })
$transitivePackages = @($components | Where-Object { $_.transitive -and $_.name -ne 'SightAdapt' })
$report = [ordered]@{
    schemaVersion = 2
    generatedAtUtc = $created
    productVersion = $productVersion
    sdkVersion = $sdkVersion
    runtimeVersion = $runtimeVersion
    runtimeIdentifier = $rid
    policy = 'release/dependency-policy.json'
    policySha256 = $policySha256
    result = if ($script:failures.Count -eq 0) { 'pass' } else { 'fail' }
    inventory = [ordered]@{
        applicationPackageCount = $applicationPackages.Count
        testPackageCount = $testPackages.Count
        transitivePackageCount = $transitivePackages.Count
        shippedPackageCount = @($components | Where-Object { $_.shipped }).Count
        packageCount = $components.Count
        graphEdgeCount = $script:graphEdges.Count
        sources = @(
            'src/SightAdapt/obj/project.assets.json',
            'tests/SightAdapt.Tests/obj/project.assets.json',
            'DOTNET-NOTICE-METADATA.json',
            '.github/workflows/build.yml'
        )
    }
    allowedLicenseExpressions = $allowedLicenses
    deniedLicenseExpressions = $deniedLicenses
    reviewLicenseExpressions = $reviewLicenses
    components = @($components | Sort-Object scope, name, version)
    dependencyEdges = @($script:graphEdges | Sort-Object graph, from, to)
    failures = @($script:failures)
}
[System.IO.File]::WriteAllText(
    (Join-Path $publish 'LICENSE-REPORT.json'),
    ($report | ConvertTo-Json -Depth 14) + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

function Get-SpdxId([string]$Prefix, [string]$Value) {
    $safe = [regex]::Replace($Value, '[^A-Za-z0-9.-]', '-')
    return "SPDXRef-$Prefix-$safe"
}

$files = [System.Collections.Generic.List[object]]::new()
$fileRelationships = [System.Collections.Generic.List[object]]::new()
$sightAdaptId = Get-SpdxId 'Package' "SightAdapt-$productVersion"
foreach ($file in @(Get-ChildItem -LiteralPath $publish -File -Recurse | Sort-Object FullName)) {
    $relative = [System.IO.Path]::GetRelativePath($publish, $file.FullName).Replace('\', '/')
    if ($relative -eq 'SBOM.spdx.json') {
        continue
    }
    $fileId = Get-SpdxId 'File' $relative
    $files.Add([ordered]@{
        fileName = "./$relative"
        SPDXID = $fileId
        checksums = @([ordered]@{
            algorithm = 'SHA256'
            checksumValue = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash
        })
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
$packageIdByIdentity = @{}
foreach ($component in @($components | Sort-Object name, version)) {
    $packageId = Get-SpdxId 'Package' "$($component.name)-$($component.version)"
    $identityKey = ([string]$component.identity).ToLowerInvariant()
    $packageIdByIdentity[$identityKey] = $packageId
    $package = [ordered]@{
        name = $component.name
        SPDXID = $packageId
        versionInfo = $component.version
        supplier = $component.supplier
        downloadLocation = if ([string]::IsNullOrWhiteSpace([string]$component.source)) { 'NOASSERTION' } else { $component.source }
        filesAnalyzed = $false
        licenseConcluded = $component.licenseConcluded
        licenseDeclared = $component.licenseDeclared
        copyrightText = 'NOASSERTION'
        primaryPackagePurpose = $component.purpose
        comment = "Scope: $($component.scope); direct: $($component.direct); transitive: $($component.transitive); shipped: $($component.shipped); evidence: $($component.evidence.evidenceType)."
    }
    if ([string]$component.evidence.packageSha512 -match '^[0-9A-Fa-f]{128}$') {
        $package['checksums'] = @([ordered]@{
            algorithm = 'SHA512'
            checksumValue = [string]$component.evidence.packageSha512
        })
    }
    elseif ($component.name -eq 'SightAdapt') {
        $package['checksums'] = @([ordered]@{
            algorithm = 'SHA256'
            checksumValue = (Get-FileHash -LiteralPath (Join-Path $publish 'SightAdapt.exe') -Algorithm SHA256).Hash
        })
    }

    if ($component.name -like 'actions/*') {
        $package['externalRefs'] = @([ordered]@{
            referenceCategory = 'PACKAGE-MANAGER'
            referenceType = 'purl'
            referenceLocator = "pkg:github/$($component.name)@$($component.version)"
        })
    }
    elseif ($component.name -ne 'SightAdapt' -and $component.name -ne 'Microsoft .NET SDK') {
        $package['externalRefs'] = @([ordered]@{
            referenceCategory = 'PACKAGE-MANAGER'
            referenceType = 'purl'
            referenceLocator = "pkg:nuget/$($component.name)@$($component.version)"
        })
    }
    $packages.Add($package)
}

$relationships.Add([ordered]@{
    spdxElementId = 'SPDXRef-DOCUMENT'
    relationshipType = 'DESCRIBES'
    relatedSpdxElement = $sightAdaptId
})
$rootRelationshipKeys = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($component in @($components | Where-Object { $_.name -ne 'SightAdapt' })) {
    $identityKey = ([string]$component.identity).ToLowerInvariant()
    $packageId = $packageIdByIdentity[$identityKey]
    if ($component.shipped) {
        $relationship = [ordered]@{
            spdxElementId = $sightAdaptId
            relationshipType = 'DEPENDS_ON'
            relatedSpdxElement = $packageId
            comment = 'Published runtime/package dependency.'
        }
    }
    elseif ($component.scope -eq 'test') {
        $relationship = [ordered]@{
            spdxElementId = $packageId
            relationshipType = 'TEST_DEPENDENCY_OF'
            relatedSpdxElement = $sightAdaptId
        }
    }
    else {
        $relationship = [ordered]@{
            spdxElementId = $packageId
            relationshipType = 'BUILD_DEPENDENCY_OF'
            relatedSpdxElement = $sightAdaptId
        }
    }
    $relationshipKey = "$($relationship.spdxElementId)|$($relationship.relationshipType)|$($relationship.relatedSpdxElement)"
    if ($rootRelationshipKeys.Add($relationshipKey)) {
        $relationships.Add($relationship)
    }
}
foreach ($edge in @($script:graphEdges)) {
    $fromKey = ([string]$edge.from).ToLowerInvariant()
    $toKey = ([string]$edge.to).ToLowerInvariant()
    if ($packageIdByIdentity.ContainsKey($fromKey) -and $packageIdByIdentity.ContainsKey($toKey)) {
        $relationships.Add([ordered]@{
            spdxElementId = $packageIdByIdentity[$fromKey]
            relationshipType = 'DEPENDS_ON'
            relatedSpdxElement = $packageIdByIdentity[$toKey]
            comment = "Restore graph: $($edge.graph)."
        })
    }
}
foreach ($relationship in $fileRelationships) {
    $relationships.Add($relationship)
}

$extractedLicenses = [System.Collections.Generic.List[object]]::new()
foreach ($referenceProperty in @($policy.licenseReferences.PSObject.Properties)) {
    $reference = $referenceProperty.Value
    $extractedLicenses.Add([ordered]@{
        licenseId = [string]$referenceProperty.Name
        name = [string]$reference.name
        extractedText = [string]$reference.extractedText
        seeAlsos = @($reference.seeAlsos)
    })
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
        comment = 'Inventory derived from complete application/test NuGet restore graphs, exact publish-component coverage and reviewed build-tool policy.'
    }
    documentDescribes = @($sightAdaptId)
    packages = @($packages)
    files = @($files)
    relationships = @($relationships)
    hasExtractedLicensingInfos = @($extractedLicenses)
}
[System.IO.File]::WriteAllText(
    (Join-Path $publish 'SBOM.spdx.json'),
    ($sbom | ConvertTo-Json -Depth 16) + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

if ($script:failures.Count -gt 0) {
    $details = $script:failures | ForEach-Object { " - $_" }
    throw "Dependency license review failed:`n$($details -join "`n")"
}

Write-Host (
    "Generated SPDX 2.3 SBOM for {0} components ({1} transitive), {2} graph edges and {3} packaged files." -f
    $components.Count,
    $transitivePackages.Count,
    $script:graphEdges.Count,
    $files.Count)
