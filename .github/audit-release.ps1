Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

function Write-ExistingFile(
    [string]$Path,
    [string]$ExpectedMarker,
    [string]$Content) {
    if (-not (Test-Path $Path)) {
        throw "File '$Path' does not exist."
    }

    $current = Normalize-Newlines (Get-Content -Raw $Path)
    if (-not $current.Contains((Normalize-Newlines $ExpectedMarker))) {
        throw "Expected marker was not found in '$Path'."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

Replace-Exact 'src/SightAdapt/ProfileSliderControl.cs' @'

            ConfigureDefaultNeutralPoint();
            SetValue(_value, raiseEvent: false);
'@ @'

            SetValue(_value, raiseEvent: false);
'@ 2

Replace-Exact 'src/SightAdapt/ProfileSliderControl.cs' @'
    public string Unit
    {
        get => _unitLabel.Text;
        set
        {
            _unitLabel.Text = value ?? string.Empty;
            ConfigureDefaultNeutralPoint();
        }
    }
'@ @'
    public string Unit
    {
        get => _unitLabel.Text;
        set => _unitLabel.Text = value ?? string.Empty;
    }
'@

Replace-Exact 'src/SightAdapt/ProfileSliderControl.cs' @'
    private void ConfigureDefaultNeutralPoint()
    {
        if (Minimum < 0f && Maximum > 0f)
        {
            _neutralValue = 0f;
        }
        else if (string.Equals(Unit, "%", StringComparison.Ordinal) &&
                 Minimum < 100f &&
                 Maximum > 100f)
        {
            _neutralValue = 100f;
        }
        else
        {
            _neutralValue = null;
        }

        _track.Invalidate();
    }

'@ ''

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' `
    '    private VisualProfileEditorForm(VisualProfile profile)' `
    '    internal VisualProfileEditorForm(VisualProfile profile)'

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
        _outputBlackSlider = CreatePercentageSlider(
            "Output black",
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack);
        _outputWhiteSlider = CreatePercentageSlider(
            "Output white",
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite);
        _brightnessSlider = CreatePercentageSlider(
            "Brightness",
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness);
        _contrastSlider = CreatePercentageSlider(
            "Contrast",
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast);
        _saturationSlider = CreatePercentageSlider(
            "Saturation",
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation);
'@ @'
        _outputBlackSlider = CreatePercentageSlider(
            "Output black",
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack,
            neutralValue: null);
        _outputWhiteSlider = CreatePercentageSlider(
            "Output white",
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite,
            neutralValue: null);
        _brightnessSlider = CreatePercentageSlider(
            "Brightness",
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness,
            neutralValue: 0f);
        _contrastSlider = CreatePercentageSlider(
            "Contrast",
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast,
            neutralValue: 100f);
        _saturationSlider = CreatePercentageSlider(
            "Saturation",
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation,
            neutralValue: 100f);
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
            Maximum = VisualProfileLimits.MaximumHueShift,
            SmallChange = 0.5f,
            Unit = "°",
'@ @'
            Maximum = VisualProfileLimits.MaximumHueShift,
            SmallChange = 0.5f,
            NeutralValue = 0f,
            Unit = "°",
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
    private static ModernProfileSlider CreatePercentageSlider(
        string accessibleName,
        float minimum,
        float maximum)
'@ @'
    private static ModernProfileSlider CreatePercentageSlider(
        string accessibleName,
        float minimum,
        float maximum,
        float? neutralValue)
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
            Maximum = maximum * 100f,
            SmallChange = 0.25f,
            Unit = "%",
'@ @'
            Maximum = maximum * 100f,
            SmallChange = 0.25f,
            NeutralValue = neutralValue,
            Unit = "%",
'@

Write-ExistingFile 'tests/SightAdapt.Tests/ProfileSliderLayoutTests.cs' `
    'public sealed class ProfileSliderLayoutTests' @'
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProfileSliderLayoutTests
{
    [TestMethod]
    public void ExplicitNeutralPointUsesCenteredMapping()
    {
        RunOnSta(() =>
        {
            using var slider = new ModernProfileSlider
            {
                Minimum = -50f,
                Maximum = 200f,
                NeutralValue = 0f,
            };

            Assert.IsTrue(slider.HasNeutralPoint);
            Assert.AreEqual(
                0.5f,
                slider.ValueToRatio(0f),
                0.0001f);
            Assert.AreEqual(
                0f,
                slider.RatioToValue(0.5f),
                0.0001f);
        });
    }

    [TestMethod]
    public void UnitTextDoesNotChangeNeutralPoint()
    {
        RunOnSta(() =>
        {
            using var slider = new ModernProfileSlider
            {
                Minimum = 50f,
                Maximum = 200f,
                NeutralValue = 100f,
                Unit = "%",
            };

            slider.Unit = "percent";

            Assert.IsTrue(slider.HasNeutralPoint);
            Assert.AreEqual(
                100f,
                slider.NeutralValue!.Value);
        });
    }

    [TestMethod]
    public void SliderWithoutNeutralUsesLinearMapping()
    {
        RunOnSta(() =>
        {
            using var slider = new ModernProfileSlider
            {
                Minimum = 0f,
                Maximum = 50f,
                Unit = "%",
            };

            Assert.IsFalse(slider.HasNeutralPoint);
            Assert.IsNull(slider.NeutralValue);
            Assert.AreEqual(
                0.5f,
                slider.ValueToRatio(25f),
                0.0001f);
        });
    }

    [TestMethod]
    public void ProfileEditorDeclaresEveryNeutralPointExplicitly()
    {
        RunOnSta(() =>
        {
            using var editor = new VisualProfileEditorForm(
                VisualProfile.CreateDefaultSoftInvert());
            var sliders = FindControls<ModernProfileSlider>(editor)
                .ToDictionary(slider => slider.AccessibleName);

            Assert.IsNull(
                sliders["Output black"].NeutralValue);
            Assert.IsNull(
                sliders["Output white"].NeutralValue);
            Assert.AreEqual(
                0f,
                sliders["Brightness"].NeutralValue!.Value);
            Assert.AreEqual(
                100f,
                sliders["Contrast"].NeutralValue!.Value);
            Assert.AreEqual(
                100f,
                sliders["Saturation"].NeutralValue!.Value);
            Assert.AreEqual(
                0f,
                sliders["Hue shift"].NeutralValue!.Value);

            sliders["Contrast"].Unit = "percent";
            Assert.AreEqual(
                100f,
                sliders["Contrast"].NeutralValue!.Value);
        });
    }

    private static IEnumerable<T> FindControls<T>(
        Control root)
        where T : Control
    {
        if (root is T match)
        {
            yield return match;
        }

        foreach (Control child in root.Controls)
        {
            foreach (var nested in FindControls<T>(child))
            {
                yield return nested;
            }
        }
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(
            thread.Join(TimeSpan.FromSeconds(10)),
            "The profile-slider test did not finish in time.");
        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }
}
'@
