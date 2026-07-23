Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'tests/SightAdapt.Tests/MenuRendererRoleTests.cs'
$content = (Get-Content -Raw $path).Replace("`r`n", "`n")
$old = 'using System.Windows.Forms;'
$new = "using System.Drawing;`nusing System.Windows.Forms;"
$count = [regex]::Matches($content, [regex]::Escape($old)).Count
if ($count -ne 1) {
    throw "Expected one menu-test import marker, found $count."
}
$content = $content.Replace($old, $new)
$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($path, $content, $utf8)
