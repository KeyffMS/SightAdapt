[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateNotNullOrEmpty()]
    [string]$PublishDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($null -eq ('System.IO.Compression.ZipFile' -as [type])) {
    Add-Type -AssemblyName System.IO.Compression.FileSystem
}

$root = Split-Path -Parent $PSScriptRoot
$publish = (Resolve-Path -LiteralPath $PublishDirectory).Path
[xml]$props = Get-Content -LiteralPath (Join-Path $root 'Directory.Build.props')
$group = $props.Project.PropertyGroup
$productVersion = [string]$group.SightAdaptProductVersion
$artifactName = ([string]$group.SightAdaptArtifactName).Replace(
    '$(SightAdaptProductVersion)',
    $productVersion)
$sourceCommit = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or
    $sourceCommit -notmatch '^[0-9A-Fa-f]{40}$') {
    throw 'The negative final-package test cannot resolve repository HEAD.'
}

$tempRoot = Join-Path ([System.IO.Path]::GetTempPath()) (
    'sightadapt-final-gate-negative-' + [Guid]::NewGuid().ToString('N'))
[System.IO.Directory]::CreateDirectory($tempRoot) | Out-Null

function Assert-FinalGateRejected(
    [scriptblock]$Action,
    [string]$Scenario,
    [string]$ExpectedMessage) {
    $rejected = $false
    try {
        & $Action
    }
    catch {
        $rejected = $true
        $message = [string]$_.Exception.Message
        if (-not $message.Contains($ExpectedMessage, [StringComparison]::OrdinalIgnoreCase)) {
            throw "$Scenario was rejected for an unexpected reason: $message"
        }
        Write-Host "$Scenario was rejected as expected: $message"
    }
    if (-not $rejected) {
        throw "$Scenario unexpectedly passed validation."
    }
}

try {
    $baselineDirectory = Join-Path $tempRoot 'baseline'
    [System.IO.Directory]::CreateDirectory($baselineDirectory) | Out-Null
    $baselineArchive = Join-Path $baselineDirectory "$artifactName.zip"
    $baselineReport = Join-Path $baselineDirectory "$artifactName-compliance.json"

    & (Join-Path $PSScriptRoot 'new-verified-release-package.ps1') `
        -DirectoryPath $publish `
        -ArchivePath $baselineArchive `
        -ReportPath $baselineReport `
        -DistributionChannel 'local-portable-zip' `
        -SourceCommitSha $sourceCommit `
        -SourceRef 'refs/heads/final-gate-negative' `
        -WorkflowName 'negative-test' `
        -WorkflowRunId 'local' `
        -WorkflowRunAttempt '1'

    $tamperedDirectory = Join-Path $tempRoot 'tampered'
    [System.IO.Directory]::CreateDirectory($tamperedDirectory) | Out-Null
    $tamperedArchive = Join-Path $tamperedDirectory "$artifactName.zip"
    Copy-Item -LiteralPath $baselineArchive -Destination $tamperedArchive

    $zip = [System.IO.Compression.ZipFile]::Open(
        $tamperedArchive,
        [System.IO.Compression.ZipArchiveMode]::Update)
    try {
        $entry = $zip.GetEntry('PRIVACY.md')
        if ($null -eq $entry) {
            throw 'The hash-mismatch test requires PRIVACY.md in the archive.'
        }
        $stream = $entry.Open()
        $reader = [System.IO.StreamReader]::new($stream)
        try {
            $text = $reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
            $stream.Dispose()
        }
        $entry.Delete()
        $replacement = $zip.CreateEntry('PRIVACY.md')
        $replacementStream = $replacement.Open()
        $writer = [System.IO.StreamWriter]::new(
            $replacementStream,
            [System.Text.UTF8Encoding]::new($false))
        try {
            $writer.Write($text + [Environment]::NewLine + 'tampered archive copy')
        }
        finally {
            $writer.Dispose()
            $replacementStream.Dispose()
        }
    }
    finally {
        $zip.Dispose()
    }

    Assert-FinalGateRejected `
        -Scenario 'The archive with modified file bytes' `
        -ExpectedMessage 'SHA-256 mismatch' `
        -Action {
            & (Join-Path $PSScriptRoot 'verify-final-package.ps1') `
                -DirectoryPath $publish `
                -ArchivePath $tamperedArchive `
                -ReportPath (Join-Path $tamperedDirectory 'tampered-report.json') `
                -DistributionChannel 'local-portable-zip' `
                -SourceCommitSha $sourceCommit `
                -SourceRef 'refs/heads/final-gate-negative' `
                -WorkflowName 'negative-test' `
                -WorkflowRunId 'local' `
                -WorkflowRunAttempt '1'
        }

    Assert-FinalGateRejected `
        -Scenario 'The package with incorrect source commit provenance' `
        -ExpectedMessage 'does not match repository HEAD' `
        -Action {
            & (Join-Path $PSScriptRoot 'verify-final-package.ps1') `
                -DirectoryPath $publish `
                -ArchivePath $baselineArchive `
                -ReportPath (Join-Path $baselineDirectory 'wrong-commit-report.json') `
                -DistributionChannel 'local-portable-zip' `
                -SourceCommitSha '0000000000000000000000000000000000000000' `
                -SourceRef 'refs/heads/final-gate-negative' `
                -WorkflowName 'negative-test' `
                -WorkflowRunId 'local' `
                -WorkflowRunAttempt '1'
        }

    Assert-FinalGateRejected `
        -Scenario 'The package with inconsistent tag provenance' `
        -ExpectedMessage 'does not match source ref' `
        -Action {
            & (Join-Path $PSScriptRoot 'verify-final-package.ps1') `
                -DirectoryPath $publish `
                -ArchivePath $baselineArchive `
                -ReportPath (Join-Path $baselineDirectory 'wrong-tag-report.json') `
                -DistributionChannel 'local-portable-zip' `
                -SourceCommitSha $sourceCommit `
                -SourceRef 'refs/tags/v0.0.0-test' `
                -ReleaseTag 'v0.0.0-other' `
                -WorkflowName 'negative-test' `
                -WorkflowRunId 'local' `
                -WorkflowRunAttempt '1'
        }

    Assert-FinalGateRejected `
        -Scenario 'The unimplemented GitHub Release channel' `
        -ExpectedMessage 'planned but not maintained' `
        -Action {
            & (Join-Path $PSScriptRoot 'verify-final-package.ps1') `
                -DirectoryPath $publish `
                -ArchivePath $baselineArchive `
                -ReportPath (Join-Path $baselineDirectory 'planned-channel-report.json') `
                -DistributionChannel 'github-release' `
                -SourceCommitSha $sourceCommit `
                -SourceRef 'refs/heads/final-gate-negative' `
                -WorkflowName 'negative-test' `
                -WorkflowRunId 'local' `
                -WorkflowRunAttempt '1'
        }
}
finally {
    Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
}

Write-Host 'Negative final-package hash and provenance validation passed.'
