Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'tests/SightAdapt.Tests/ProfilePreviewCacheTests.cs'
$content = (Get-Content -Raw $path).Replace("`r`n", "`n")
$old = "using Microsoft.VisualStudio.TestTools.UnitTesting;"
$new = "using System.Windows.Forms;`nusing Microsoft.VisualStudio.TestTools.UnitTesting;"
$count = [regex]::Matches($content, [regex]::Escape($old)).Count
if ($count -ne 1) {
    throw "Expected one test import marker, found $count."
}
$content = $content.Replace($old, $new)
$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($path, $content, $utf8)
