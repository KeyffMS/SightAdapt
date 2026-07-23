Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$path = 'tests/SightAdapt.Tests/ProfileSliderLayoutTests.cs'
$content = (Get-Content -Raw $path).Replace("`r`n", "`n")
$old = @'
            var sliders = FindControls<ModernProfileSlider>(editor)
                .ToDictionary(slider => slider.AccessibleName);
'@
$new = @'
            var sliders = FindControls<ModernProfileSlider>(editor)
                .ToDictionary(
                    slider => slider.AccessibleName ??
                        throw new InvalidOperationException(
                            "Every profile slider requires an accessible name."),
                    StringComparer.Ordinal);
'@
$count = [regex]::Matches(
    $content,
    [regex]::Escape($old.Replace("`r`n", "`n"))).Count
if ($count -ne 1) {
    throw "Expected one slider dictionary marker, found $count."
}
$content = $content.Replace(
    $old.Replace("`r`n", "`n"),
    $new.Replace("`r`n", "`n"))
$utf8 = [System.Text.UTF8Encoding]::new($false)
[System.IO.File]::WriteAllText($path, $content, $utf8)
