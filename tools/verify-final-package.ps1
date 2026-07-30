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

    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$DistributionChannel,

    [string]$SourceCommitSha,
    [string]$SourceRef,
    [string]$ReleaseTag,
    [string]$WorkflowName,
    [string]$WorkflowRunId,
    [string]$WorkflowRunAttempt,

    [string]$ManifestPath =
        (Join-Path $PSScriptRoot '..\release\required-files.txt'),

    [string]$ChannelsPath =
        (Join-Path $PSScriptRoot '..\release\distribution-channels.json')
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
$channelsFile = (Resolve-Path -LiteralPath $ChannelsPath).Path
$reportFullPath = [System.IO.Path]::GetFullPath($ReportPath)
[System.IO.Directory]::CreateDirectory(
    [System.IO.Path]::GetDirectoryName($reportFullPath)) | Out-Null

$failures = [System.Collections.Generic.List[string]]::new()
$fileEvidence = [System.Collections.Generic.List[object]]::new()
$baseReport = $null
$componentCoverageResult = 'not-run'
$distributionFormat = $null
$channelRecord = $null

function Invoke-GitValue([string[]]$Arguments) {
    try {
        $output = & git -C $root @Arguments 2>$null
        if ($LASTEXITCODE -eq 0) {
            return (($output | Out-String).Trim())
        }
    }
    catch {
        return $null
    }
    return $null
}

function Get-ArchiveEntrySha256(
    [System.IO.Compression.ZipArchiveEntry]$Entry) {
    $stream = $Entry.Open()
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [System.BitConverter]::ToString(
            $sha256.ComputeHash($stream)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $sha256.Dispose()
        $stream.Dispose()
    }
}

if ([string]::IsNullOrWhiteSpace($SourceCommitSha)) {
    $SourceCommitSha = if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_SHA)) {
        $env:GITHUB_SHA
    }
    else {
        Invoke-GitValue @('rev-parse', 'HEAD')
    }
}
if ([string]::IsNullOrWhiteSpace($SourceRef)) {
    if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_REF)) {
        $SourceRef = $env:GITHUB_REF
    }
    else {
        $branch = Invoke-GitValue @('rev-parse', '--abbrev-ref', 'HEAD')
        $SourceRef = if (-not [string]::IsNullOrWhiteSpace($branch) -and
                        $branch -ne 'HEAD') {
            "refs/heads/$branch"
        }
        else {
            'local-detached'
        }
    }
}
if ([string]::IsNullOrWhiteSpace($ReleaseTag) -and
    [string]$env:GITHUB_REF_TYPE -eq 'tag') {
    $ReleaseTag = [string]$env:GITHUB_REF_NAME
}
if ([string]::IsNullOrWhiteSpace($WorkflowName)) {
    $WorkflowName = if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_WORKFLOW)) {
        $env:GITHUB_WORKFLOW
    }
    else {
        'local'
    }
}
if ([string]::IsNullOrWhiteSpace($WorkflowRunId)) {
    $WorkflowRunId = if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_RUN_ID)) {
        $env:GITHUB_RUN_ID
    }
    else {
        'local'
    }
}
if ([string]::IsNullOrWhiteSpace($WorkflowRunAttempt)) {
    $WorkflowRunAttempt = if (-not [string]::IsNullOrWhiteSpace($env:GITHUB_RUN_ATTEMPT)) {
        $env:GITHUB_RUN_ATTEMPT
    }
    else {
        '1'
    }
}

try {
    $channels = Get-Content -LiteralPath $channelsFile -Raw | ConvertFrom-Json
    if ([int]$channels.schemaVersion -ne 1) {
        $failures.Add("Unsupported distribution-channel schema '$($channels.schemaVersion)'.")
    }
    $channelRecord = @($channels.maintainedChannels | Where-Object {
        [string]$_.id -eq $DistributionChannel
    }) | Select-Object -First 1
    if ($null -eq $channelRecord) {
        $planned = @($channels.plannedChannels | Where-Object {
            [string]$_.id -eq $DistributionChannel
        }) | Select-Object -First 1
        if ($null -ne $planned) {
            $failures.Add("Distribution channel '$DistributionChannel' is planned but not maintained.")
            $distributionFormat = [string]$planned.format
        }
        else {
            $failures.Add("Distribution channel '$DistributionChannel' is not registered.")
        }
    }
    else {
        $distributionFormat = [string]$channelRecord.format
        if ([string]$channelRecord.entrypoint -ne
            'tools/new-verified-release-package.ps1') {
            $failures.Add("Maintained channel '$DistributionChannel' does not use the reusable package entrypoint.")
        }
        if ([string]$channelRecord.manifest -ne
            'release/required-files.txt') {
            $failures.Add("Maintained channel '$DistributionChannel' does not use the canonical manifest.")
        }
    }
}
catch {
    $failures.Add("Distribution-channel registry cannot be validated: $($_.Exception.Message)")
}

if ([string]$SourceCommitSha -notmatch '^[0-9A-Fa-f]{40}$') {
    $failures.Add("Source commit '$SourceCommitSha' is not a full 40-character Git SHA.")
}
$repositoryHead = Invoke-GitValue @('rev-parse', 'HEAD')
if (-not [string]::IsNullOrWhiteSpace($repositoryHead) -and
    [string]$SourceCommitSha -match '^[0-9A-Fa-f]{40}$' -and
    -not $repositoryHead.Equals($SourceCommitSha, [StringComparison]::OrdinalIgnoreCase)) {
    $failures.Add("Source commit '$SourceCommitSha' does not match repository HEAD '$repositoryHead'.")
}
if ([string]::IsNullOrWhiteSpace($SourceRef)) {
    $failures.Add('Source ref is required.')
}
$tagFromRef = $null
if ([string]$SourceRef -match '^refs/tags/(?<tag>.+)$') {
    $tagFromRef = $Matches['tag']
    if ([string]::IsNullOrWhiteSpace($ReleaseTag)) {
        $failures.Add("Source ref '$SourceRef' requires a release tag value.")
    }
    elseif ($ReleaseTag -ne $tagFromRef) {
        $failures.Add("Release tag '$ReleaseTag' does not match source ref '$SourceRef'.")
    }
}
elseif (-not [string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $failures.Add("Release tag '$ReleaseTag' is present but source ref '$SourceRef' is not a tag ref.")
}
if ($null -ne $channelRecord -and
    [bool]$channelRecord.requiresTag -and
    [string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $failures.Add("Distribution channel '$DistributionChannel' requires a release tag.")
}
if ($null -ne $channelRecord -and
    [bool]$channelRecord.requiresWorkflowRun -and
    ([string]::IsNullOrWhiteSpace($WorkflowName) -or
     [string]::IsNullOrWhiteSpace($WorkflowRunId) -or
     $WorkflowRunId -eq 'local')) {
    $failures.Add("Distribution channel '$DistributionChannel' requires GitHub workflow/run provenance.")
}

$baseReportPath = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-base-compliance-' + [Guid]::NewGuid().ToString('N') + '.json')
try {
    try {
        & (Join-Path $PSScriptRoot 'verify-release-compliance.ps1') `
            -DirectoryPath $directory `
            -ArchivePath $archivePathResolved `
            -ReportPath $baseReportPath `
            -ManifestPath $manifest
        $baseReport = Get-Content -LiteralPath $baseReportPath -Raw | ConvertFrom-Json
    }
    catch {
        $failures.Add("Base release compliance failed: $($_.Exception.Message)")
        if ([System.IO.File]::Exists($baseReportPath)) {
            try {
                $baseReport = Get-Content -LiteralPath $baseReportPath -Raw | ConvertFrom-Json
            }
            catch {
                $baseReport = $null
            }
        }
    }

    try {
        & (Join-Path $PSScriptRoot 'verify-dotnet-component-coverage.ps1') `
            -ArchivePath $archivePathResolved
        $componentCoverageResult = 'pass'
    }
    catch {
        $componentCoverageResult = 'fail'
        $failures.Add("Component-coverage validation failed: $($_.Exception.Message)")
    }

    try {
        $stagedFiles = @(
            Get-ChildItem -LiteralPath $directory -File -Recurse |
                Sort-Object FullName
        )
        $archiveEntries = @{}
        $archive = [System.IO.Compression.ZipFile]::OpenRead($archivePathResolved)
        try {
            foreach ($entry in $archive.Entries) {
                if ([string]::IsNullOrWhiteSpace($entry.Name)) {
                    continue
                }
                $path = $entry.FullName.Replace('\', '/').TrimStart('/')
                $key = $path.ToLowerInvariant()
                if ($archiveEntries.ContainsKey($key)) {
                    $failures.Add("Archive contains duplicate path '$path'.")
                    continue
                }
                $archiveEntries[$key] = [pscustomobject]@{
                    path = $path
                    size = [long]$entry.Length
                    sha256 = Get-ArchiveEntrySha256 $entry
                }
            }
        }
        finally {
            $archive.Dispose()
        }

        $stagedKeys = [System.Collections.Generic.HashSet[string]]::new(
            [StringComparer]::OrdinalIgnoreCase)
        foreach ($file in $stagedFiles) {
            $relative = [System.IO.Path]::GetRelativePath(
                $directory,
                $file.FullName).Replace('\', '/')
            $key = $relative.ToLowerInvariant()
            [void]$stagedKeys.Add($key)
            $stagedSha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
            $archiveRecord = if ($archiveEntries.ContainsKey($key)) {
                $archiveEntries[$key]
            }
            else {
                $null
            }
            $archiveSha256 = if ($null -ne $archiveRecord) {
                [string]$archiveRecord.sha256
            }
            else {
                $null
            }
            $matches = $null -ne $archiveRecord -and
                $stagedSha256 -eq $archiveSha256
            if ($null -eq $archiveRecord) {
                $failures.Add("Staged file '$relative' is missing from the final archive.")
            }
            elseif (-not $matches) {
                $failures.Add("Staged/archive SHA-256 mismatch for '$relative'.")
            }
            $fileEvidence.Add([ordered]@{
                path = $relative
                size = [long]$file.Length
                stagedSha256 = $stagedSha256
                archiveSha256 = $archiveSha256
                match = $matches
            })
        }
        foreach ($archiveRecord in $archiveEntries.Values) {
            if (-not $stagedKeys.Contains([string]$archiveRecord.path)) {
                $failures.Add("Archive file '$($archiveRecord.path)' is not present in the staged directory.")
            }
        }
    }
    catch {
        $failures.Add("Staged/archive hash comparison failed: $($_.Exception.Message)")
    }

    $archiveSha256 = (Get-FileHash -LiteralPath $archivePathResolved -Algorithm SHA256).Hash
    $report = [ordered]@{
        schemaVersion = 3
        generatedAtUtc = [DateTime]::UtcNow.ToString('yyyy-MM-ddTHH:mm:ssZ')
        result = if ($failures.Count -eq 0) { 'pass' } else { 'fail' }
        productVersion = if ($null -ne $baseReport) { [string]$baseReport.productVersion } else { $null }
        sdkVersion = if ($null -ne $baseReport) { [string]$baseReport.sdkVersion } else { $null }
        runtimeVersion = if ($null -ne $baseReport) { [string]$baseReport.runtimeVersion } else { $null }
        targetFramework = if ($null -ne $baseReport) { [string]$baseReport.targetFramework } else { $null }
        runtimeIdentifier = if ($null -ne $baseReport) { [string]$baseReport.runtimeIdentifier } else { $null }
        publishMode = if ($null -ne $baseReport) { [string]$baseReport.publishMode } else { $null }
        distribution = [ordered]@{
            channel = $DistributionChannel
            format = $distributionFormat
            registry = 'release/distribution-channels.json'
            entrypoint = 'tools/new-verified-release-package.ps1'
            manifest = 'release/required-files.txt'
        }
        provenance = [ordered]@{
            sourceCommitSha = $SourceCommitSha
            repositoryHeadSha = $repositoryHead
            sourceRef = $SourceRef
            releaseTag = if ([string]::IsNullOrWhiteSpace($ReleaseTag)) { $null } else { $ReleaseTag }
            workflowName = $WorkflowName
            workflowRunId = $WorkflowRunId
            workflowRunAttempt = $WorkflowRunAttempt
        }
        archive = [ordered]@{
            file = [System.IO.Path]::GetFileName($archivePathResolved)
            size = (Get-Item -LiteralPath $archivePathResolved).Length
            sha256 = $archiveSha256
        }
        validation = [ordered]@{
            baseComplianceSchemaVersion = if ($null -ne $baseReport) { [int]$baseReport.schemaVersion } else { $null }
            baseComplianceResult = if ($null -ne $baseReport) { [string]$baseReport.result } else { 'unavailable' }
            componentCoverageResult = $componentCoverageResult
            stagedArchiveHashComparison = if (@($fileEvidence | Where-Object { -not $_.match }).Count -eq 0) { 'pass' } else { 'fail' }
        }
        licenseReportPackageCount = if ($null -ne $baseReport) { $baseReport.licenseReportPackageCount } else { $null }
        sbomPackageCount = if ($null -ne $baseReport) { $baseReport.sbomPackageCount } else { $null }
        sbomRelationshipCount = if ($null -ne $baseReport) { $baseReport.sbomRelationshipCount } else { $null }
        fileEvidence = @($fileEvidence)
        failures = @($failures)
    }
    [System.IO.File]::WriteAllText(
        $reportFullPath,
        ($report | ConvertTo-Json -Depth 12) + [Environment]::NewLine,
        [System.Text.UTF8Encoding]::new($false))
}
finally {
    Remove-Item -LiteralPath $baseReportPath -Force -ErrorAction SilentlyContinue
}

if ($failures.Count -gt 0) {
    $details = $failures | ForEach-Object { " - $_" }
    throw "Final package gate failed:`n$($details -join "`n")"
}

Write-Host "Final package gate passed: $archivePathResolved"
Write-Host "Compliance report: $reportFullPath"
