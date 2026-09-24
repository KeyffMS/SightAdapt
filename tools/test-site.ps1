param(
    [string]$SiteRoot = (Join-Path $PSScriptRoot '..\site')
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$SiteRoot = (Resolve-Path $SiteRoot).Path

$requiredPages = @(
    '',
    'features',
    'download',
    'docs',
    'how-it-works',
    'faq',
    'releases',
    'privacy',
    'legal',
    'security',
    'support'
)

$canonicalRoot = 'https://aiteracja.pl/sightadapt/'
$errors = New-Object System.Collections.Generic.List[string]

function Add-Error([string]$Message) {
    $errors.Add($Message)
}

function Get-ExpectedCanonical([string]$PageName) {
    if ([string]::IsNullOrWhiteSpace($PageName)) {
        return $canonicalRoot
    }

    return "$canonicalRoot$PageName/"
}

function Resolve-LocalReference(
    [System.IO.FileInfo]$HtmlFile,
    [string]$Reference
) {
    $clean = ($Reference -split '[?#]', 2)[0]
    if ([string]::IsNullOrWhiteSpace($clean)) {
        return $null
    }

    if ($clean -match '^(?:https?:|mailto:|tel:|data:)') {
        return $null
    }

    if ($clean.StartsWith('/')) {
        Add-Error "$($HtmlFile.FullName): root-relative URL '$Reference' breaks the dual /SightAdapt/ and /sightadapt/ hosting model."
        return $null
    }

    $candidate = [System.IO.Path]::GetFullPath((Join-Path $HtmlFile.DirectoryName $clean))
    if ($clean.EndsWith('/')) {
        $candidate = Join-Path $candidate 'index.html'
    }

    return $candidate
}

foreach ($pageName in $requiredPages) {
    $path = if ([string]::IsNullOrWhiteSpace($pageName)) {
        Join-Path $SiteRoot 'index.html'
    }
    else {
        Join-Path $SiteRoot "$pageName\index.html"
    }

    if (-not (Test-Path $path -PathType Leaf)) {
        Add-Error "Missing required page: $path"
        continue
    }

    $file = Get-Item $path
    $html = Get-Content $path -Raw

    $titleMatches = [regex]::Matches($html, '<title>[^<]+</title>', 'IgnoreCase')
    if ($titleMatches.Count -ne 1) {
        Add-Error ($path + ": expected exactly one non-empty <title>; found $($titleMatches.Count).")
    }

    $h1Matches = [regex]::Matches($html, '<h1(?:\s[^>]*)?>.*?</h1>', 'IgnoreCase,Singleline')
    if ($h1Matches.Count -ne 1) {
        Add-Error ($path + ": expected exactly one <h1>; found $($h1Matches.Count).")
    }

    if ($html -notmatch '<html\s+lang="en"') {
        Add-Error ($path + ': missing html lang=en.')
    }

    if ($html -notmatch '<meta\s+name="viewport"') {
        Add-Error ($path + ': missing viewport metadata.')
    }

    if ($html -notmatch '<meta\s+name="description"\s+content="[^"]+"') {
        Add-Error ($path + ': missing non-empty meta description.')
    }

    $expectedCanonical = [regex]::Escape((Get-ExpectedCanonical $pageName))
    $canonicalPattern = '<link\s+rel="canonical"\s+href="' + $expectedCanonical + '"'
    if ($html -notmatch $canonicalPattern) {
        Add-Error ($path + ': canonical URL is missing or inconsistent.')
    }

    if ($html -notmatch 'class="skip-link"\s+href="#main"') {
        Add-Error ($path + ': missing visible-on-focus skip navigation.')
    }

    if ($html -notmatch '<main\s+id="main"') {
        Add-Error ($path + ': missing main landmark with id=main.')
    }

    if ($html -notmatch [regex]::Escape('SightAdapt™ is an unregistered product mark used by KeyffMS / aiteracja.pl.')) {
        Add-Error ($path + ': missing required SightAdapt trademark footer notice.')
    }

    if ($html -notmatch [regex]::Escape('SightAdapt is free and open-source software distributed under the MIT License.')) {
        Add-Error ($path + ': missing required open-source/license footer statement.')
    }

    if ($html -match '<(?:script|iframe)\b[^>]*(?:src)=["'']https?://') {
        Add-Error ($path + ': remote active content is not permitted.')
    }

    foreach ($imageMatch in [regex]::Matches($html, '<img\b[^>]*>', 'IgnoreCase')) {
        if ($imageMatch.Value -notmatch '\balt="[^"]*"') {
            Add-Error ($path + ": image is missing an alt attribute: $($imageMatch.Value)")
        }
    }

    $references = New-Object System.Collections.Generic.List[string]
    foreach ($match in [regex]::Matches($html, '\b(?:href|src)="([^"]+)"', 'IgnoreCase')) {
        $references.Add($match.Groups[1].Value)
    }

    foreach ($reference in $references) {
        $resolved = Resolve-LocalReference -HtmlFile $file -Reference $reference
        if ($null -ne $resolved -and -not (Test-Path $resolved)) {
            Add-Error ($path + ": local reference '$reference' resolves to missing '$resolved'.")
        }
    }
}

$home = Get-Content (Join-Path $SiteRoot 'index.html') -Raw
foreach ($claim in @(
    'No code injection',
    'No driver installation',
    'No screen-content telemetry',
    "Runs with the current user's privileges"
)) {
    if ($home -notmatch [regex]::Escape($claim)) {
        Add-Error "Home page is missing trust statement '$claim'."
    }
}

$download = Get-Content (Join-Path $SiteRoot 'download\index.html') -Raw
foreach ($requiredReleaseValue in @(
    '0.5.0.50-alpha',
    'c6dc5ba5c0356c84b32a74d298015d63158c57886b835e411da0b60f306fd711',
    'SightAdapt-0.5.0.50-alpha-win-x64.zip'
)) {
    if ($download -notmatch [regex]::Escape($requiredReleaseValue)) {
        Add-Error "Download page is missing release authority value '$requiredReleaseValue'."
    }
}

if ($errors.Count -gt 0) {
    foreach ($siteError in $errors) {
        Write-Host "::error::$siteError"
    }

    throw "SightAdapt site validation failed with $($errors.Count) error(s)."
}

Write-Host "SightAdapt site validation passed for $($requiredPages.Count) pages."
