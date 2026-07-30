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
    [string]$WorkflowRunAttempt
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$directory = (Resolve-Path -LiteralPath $DirectoryPath).Path
$archiveFullPath = [System.IO.Path]::GetFullPath($ArchivePath)
$reportFullPath = [System.IO.Path]::GetFullPath($ReportPath)
$archiveDirectory = [System.IO.Path]::GetDirectoryName($archiveFullPath)
$reportDirectory = [System.IO.Path]::GetDirectoryName($reportFullPath)
[System.IO.Directory]::CreateDirectory($archiveDirectory) | Out-Null
[System.IO.Directory]::CreateDirectory($reportDirectory) | Out-Null

$files = @(Get-ChildItem -LiteralPath $directory -File -Recurse)
if ($files.Count -eq 0) {
    throw "Publish directory contains no files: $directory"
}

Remove-Item -LiteralPath $archiveFullPath -Force -ErrorAction SilentlyContinue
Remove-Item -LiteralPath $reportFullPath -Force -ErrorAction SilentlyContinue

Compress-Archive `
    -Path (Join-Path $directory '*') `
    -DestinationPath $archiveFullPath `
    -CompressionLevel Optimal

$arguments = @{
    DirectoryPath = $directory
    ArchivePath = $archiveFullPath
    ReportPath = $reportFullPath
    DistributionChannel = $DistributionChannel
}
if (-not [string]::IsNullOrWhiteSpace($SourceCommitSha)) {
    $arguments.SourceCommitSha = $SourceCommitSha
}
if (-not [string]::IsNullOrWhiteSpace($SourceRef)) {
    $arguments.SourceRef = $SourceRef
}
if (-not [string]::IsNullOrWhiteSpace($ReleaseTag)) {
    $arguments.ReleaseTag = $ReleaseTag
}
if (-not [string]::IsNullOrWhiteSpace($WorkflowName)) {
    $arguments.WorkflowName = $WorkflowName
}
if (-not [string]::IsNullOrWhiteSpace($WorkflowRunId)) {
    $arguments.WorkflowRunId = $WorkflowRunId
}
if (-not [string]::IsNullOrWhiteSpace($WorkflowRunAttempt)) {
    $arguments.WorkflowRunAttempt = $WorkflowRunAttempt
}

& (Join-Path $PSScriptRoot 'verify-final-package.ps1') @arguments

$report = Get-Content -LiteralPath $reportFullPath -Raw | ConvertFrom-Json
if ([int]$report.schemaVersion -ne 3 -or
    [string]$report.result -ne 'pass') {
    throw "Verified package report is not a passing schema-3 report: $reportFullPath"
}

Write-Host "Created verified package: $archiveFullPath"
Write-Host "Archive SHA-256: $($report.archive.sha256)"
Write-Host "Distribution channel: $($report.distribution.channel)"
