Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'src/SightAdapt/ConfigurationForm.cs'
$content = (Get-Content -Raw $path).Replace("`r`n", "`n")
$old = @'
        _profilesGrid.UpdateApplication(FindAssignment(
            Settings,
            executablePath));
'@
$new = @'
        _profilesGrid.UpdateApplication(
            ProfileResolver.RequireAssignmentByExecutablePath(
                Settings,
                executablePath));
'@
$count = [regex]::Matches(
    $content,
    [regex]::Escape($old.Replace("`r`n", "`n"))).Count
if ($count -ne 1) {
    throw "Expected one remaining assignment refresh lookup, found $count."
}
$content = $content.Replace(
    $old.Replace("`r`n", "`n"),
    $new.Replace("`r`n", "`n"))
$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($path, $content, $utf8)
