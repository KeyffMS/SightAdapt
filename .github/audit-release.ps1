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

function Write-NewFile([string]$Path, [string]$Content) {
    if (Test-Path $Path) {
        throw "File '$Path' already exists."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

Write-NewFile 'src/SightAdapt/VisualAdjustmentDefinitions.cs' @'
namespace SightAdapt;

internal sealed record VisualAdjustmentDefinition(
    string Id,
    string Title,
    string Description,
    float Minimum,
    float Maximum,
    float EditorScale,
    int DecimalPlaces,
    float SmallChange,
    string Unit,
    float? NeutralValue,
    Func<VisualProfile, float> GetValue,
    Action<VisualProfile, float> SetValue)
{
    public float MinimumEditorValue =>
        Minimum * EditorScale;

    public float MaximumEditorValue =>
        Maximum * EditorScale;

    public float? NeutralEditorValue =>
        NeutralValue * EditorScale;

    public string RangeDescription =>
        $"{MinimumEditorValue:0.##}–{MaximumEditorValue:0.##}{Unit} · {Description}";

    public ModernProfileSlider CreateSlider()
    {
        return new ModernProfileSlider
        {
            AccessibleName = Title,
            DecimalPlaces = DecimalPlaces,
            Minimum = MinimumEditorValue,
            Maximum = MaximumEditorValue,
            SmallChange = SmallChange,
            NeutralValue = NeutralEditorValue,
            Unit = Unit,
        };
    }

    public float ReadEditorValue(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return GetValue(profile) * EditorScale;
    }

    public void WriteEditorValue(
        VisualProfile profile,
        float editorValue)
    {
        ArgumentNullException.ThrowIfNull(profile);
        SetValue(profile, editorValue / EditorScale);
    }
}

internal static class VisualAdjustmentDefinitions
{
    public static VisualAdjustmentDefinition OutputBlack { get; } =
        new(
            "output-black",
            "Output black",
            "minimum output level",
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack,
            100f,
            2,
            0.25f,
            "%",
            null,
            profile => profile.OutputBlack,
            (profile, value) => profile.OutputBlack = value);

    public static VisualAdjustmentDefinition OutputWhite { get; } =
        new(
            "output-white",
            "Output white",
            "maximum output level",
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite,
            100f,
            2,
            0.25f,
            "%",
            null,
            profile => profile.OutputWhite,
            (profile, value) => profile.OutputWhite = value);

    public static VisualAdjustmentDefinition Brightness { get; } =
        new(
            "brightness",
            "Brightness",
            "moves the whole output range",
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness,
            100f,
            2,
            0.25f,
            "%",
            0f,
            profile => profile.Brightness,
            (profile, value) => profile.Brightness = value);

    public static VisualAdjustmentDefinition Contrast { get; } =
        new(
            "contrast",
            "Contrast",
            "expands or compresses differences",
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast,
            100f,
            2,
            0.25f,
            "%",
            1f,
            profile => profile.Contrast,
            (profile, value) => profile.Contrast = value);

    public static VisualAdjustmentDefinition Saturation { get; } =
        new(
            "saturation",
            "Saturation",
            "grayscale to amplified color",
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation,
            100f,
            2,
            0.25f,
            "%",
            1f,
            profile => profile.Saturation,
            (profile, value) => profile.Saturation = value);

    public static VisualAdjustmentDefinition HueShift { get; } =
        new(
            "hue-shift",
            "Hue shift",
            "rotates the color spectrum",
            VisualProfileLimits.MinimumHueShift,
            VisualProfileLimits.MaximumHueShift,
            1f,
            1,
            0.5f,
            "°",
            0f,
            profile => profile.HueShiftDegrees,
            (profile, value) =>
                profile.HueShiftDegrees = value);

    public static IReadOnlyList<VisualAdjustmentDefinition> All { get; } =
    [
        OutputBlack,
        OutputWhite,
        Brightness,
        Contrast,
        Saturation,
        HueShift,
    ];
}

internal sealed record VisualAdjustmentBinding(
    VisualAdjustmentDefinition Definition,
    ModernProfileSlider Slider);
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
    private readonly OutputLimitPreview _outputPreview;
    private readonly ModernProfileSlider _outputBlackSlider;
'@ @'
    private readonly OutputLimitPreview _outputPreview;
    private readonly IReadOnlyDictionary<string, VisualAdjustmentBinding>
        _adjustments;
    private readonly ModernProfileSlider _outputBlackSlider;
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
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
        _hueSlider = new ModernProfileSlider
        {
            AccessibleName = "Hue shift",
            DecimalPlaces = 1,
            Minimum = VisualProfileLimits.MinimumHueShift,
            Maximum = VisualProfileLimits.MaximumHueShift,
            SmallChange = 0.5f,
            NeutralValue = 0f,
            Unit = "°",
        };
'@ @'
        _adjustments = VisualAdjustmentDefinitions.All
            .Select(definition => new VisualAdjustmentBinding(
                definition,
                definition.CreateSlider()))
            .ToDictionary(
                binding => binding.Definition.Id,
                StringComparer.Ordinal);
        _outputBlackSlider = SliderFor(
            VisualAdjustmentDefinitions.OutputBlack);
        _outputWhiteSlider = SliderFor(
            VisualAdjustmentDefinitions.OutputWhite);
        _brightnessSlider = SliderFor(
            VisualAdjustmentDefinitions.Brightness);
        _contrastSlider = SliderFor(
            VisualAdjustmentDefinitions.Contrast);
        _saturationSlider = SliderFor(
            VisualAdjustmentDefinitions.Saturation);
        _hueSlider = SliderFor(
            VisualAdjustmentDefinitions.HueShift);
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
        controls.Controls.Add(CreateSliderPanel(
            "Output black",
            PercentageRange(
                VisualProfileLimits.MinimumOutputBlack,
                VisualProfileLimits.MaximumOutputBlack,
                "minimum output level"),
            _outputBlackSlider), 0, 0);
        controls.Controls.Add(CreateSliderPanel(
            "Output white",
            PercentageRange(
                VisualProfileLimits.MinimumOutputWhite,
                VisualProfileLimits.MaximumOutputWhite,
                "maximum output level"),
            _outputWhiteSlider), 1, 0);
'@ @'
        controls.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.OutputBlack), 0, 0);
        controls.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.OutputWhite), 1, 0);
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
        grid.Controls.Add(CreateSliderPanel(
            "Brightness",
            PercentageRange(
                VisualProfileLimits.MinimumBrightness,
                VisualProfileLimits.MaximumBrightness,
                "moves the whole output range"),
            _brightnessSlider), 0, 0);
        grid.Controls.Add(CreateSliderPanel(
            "Contrast",
            PercentageRange(
                VisualProfileLimits.MinimumContrast,
                VisualProfileLimits.MaximumContrast,
                "expands or compresses differences"),
            _contrastSlider), 1, 0);
        grid.Controls.Add(CreateSliderPanel(
            "Saturation",
            PercentageRange(
                VisualProfileLimits.MinimumSaturation,
                VisualProfileLimits.MaximumSaturation,
                "grayscale to amplified color"),
            _saturationSlider), 0, 1);
        grid.Controls.Add(CreateSliderPanel(
            "Hue shift",
            Range(
                VisualProfileLimits.MinimumHueShift,
                VisualProfileLimits.MaximumHueShift,
                "°",
                "rotates the color spectrum"),
            _hueSlider), 1, 1);
'@ @'
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.Brightness), 0, 0);
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.Contrast), 1, 0);
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.Saturation), 0, 1);
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.HueShift), 1, 1);
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
    private static ModernProfileSlider CreatePercentageSlider(
        string accessibleName,
        float minimum,
        float maximum,
        float? neutralValue)
    {
        return new ModernProfileSlider
        {
            AccessibleName = accessibleName,
            DecimalPlaces = 2,
            Minimum = minimum * 100f,
            Maximum = maximum * 100f,
            SmallChange = 0.25f,
            NeutralValue = neutralValue,
            Unit = "%",
        };
    }

    private void AttachChangeHandlers()
    {
        AttachPercentage(
            _outputBlackSlider,
            value => _workingProfile.OutputBlack = value);
        AttachPercentage(
            _outputWhiteSlider,
            value => _workingProfile.OutputWhite = value);
        AttachPercentage(
            _brightnessSlider,
            value => _workingProfile.Brightness = value);
        AttachPercentage(
            _contrastSlider,
            value => _workingProfile.Contrast = value);
        AttachPercentage(
            _saturationSlider,
            value => _workingProfile.Saturation = value);
        AttachSlider(
            _hueSlider,
            value => _workingProfile.HueShiftDegrees = value);
    }

    private void AttachPercentage(
        ModernProfileSlider slider,
        Action<float> setter)
    {
        AttachSlider(slider, value => setter(value / 100f));
    }

    private void AttachSlider(
        ModernProfileSlider slider,
        Action<float> setter)
    {
        slider.ValueChanged += (_, _) =>
        {
            if (_loadingValues)
            {
                return;
            }

            setter(slider.Value);
            InvalidatePreviews();
        };
    }

    private void LoadValues()
    {
        _loadingValues = true;
        try
        {
            _outputBlackSlider.Value = _workingProfile.OutputBlack * 100f;
            _outputWhiteSlider.Value = _workingProfile.OutputWhite * 100f;
            _brightnessSlider.Value = _workingProfile.Brightness * 100f;
            _contrastSlider.Value = _workingProfile.Contrast * 100f;
            _saturationSlider.Value = _workingProfile.Saturation * 100f;
            _hueSlider.Value = _workingProfile.HueShiftDegrees;
        }
        finally
        {
            _loadingValues = false;
        }
    }
'@ @'
    private ModernProfileSlider SliderFor(
        VisualAdjustmentDefinition definition)
    {
        return _adjustments[definition.Id].Slider;
    }

    private Control CreateAdjustmentPanel(
        VisualAdjustmentDefinition definition)
    {
        return CreateSliderPanel(
            definition.Title,
            definition.RangeDescription,
            SliderFor(definition));
    }

    private void AttachChangeHandlers()
    {
        foreach (var binding in _adjustments.Values)
        {
            binding.Slider.ValueChanged += (_, _) =>
            {
                if (_loadingValues)
                {
                    return;
                }

                binding.Definition.WriteEditorValue(
                    _workingProfile,
                    binding.Slider.Value);
                InvalidatePreviews();
            };
        }
    }

    private void LoadValues()
    {
        _loadingValues = true;
        try
        {
            foreach (var binding in _adjustments.Values)
            {
                binding.Slider.Value =
                    binding.Definition.ReadEditorValue(
                        _workingProfile);
            }
        }
        finally
        {
            _loadingValues = false;
        }
    }
'@

Replace-Exact 'src/SightAdapt/VisualProfileEditorForm.cs' @'
    private static string PercentageRange(
        float minimum,
        float maximum,
        string explanation)
    {
        return Range(minimum * 100f, maximum * 100f, "%", explanation);
    }

    private static string Range(
        float minimum,
        float maximum,
        string unit,
        string explanation)
    {
        return $"{minimum:0.##}–{maximum:0.##}{unit} · {explanation}";
    }
'@ ''

Write-NewFile 'tests/SightAdapt.Tests/VisualAdjustmentDefinitionTests.cs' @'
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualAdjustmentDefinitionTests
{
    [TestMethod]
    public void DefinitionsAreCompleteAndHaveUniqueIdentity()
    {
        Assert.AreEqual(
            6,
            VisualAdjustmentDefinitions.All.Count);
        Assert.AreEqual(
            6,
            VisualAdjustmentDefinitions.All
                .Select(definition => definition.Id)
                .Distinct(StringComparer.Ordinal)
                .Count());
        CollectionAssert.AreEquivalent(
            new[]
            {
                "Output black",
                "Output white",
                "Brightness",
                "Contrast",
                "Saturation",
                "Hue shift",
            },
            VisualAdjustmentDefinitions.All
                .Select(definition => definition.Title)
                .ToArray());
    }

    [TestMethod]
    public void DefinitionsCreateConfiguredSliders()
    {
        RunOnSta(() =>
        {
            foreach (var definition in
                     VisualAdjustmentDefinitions.All)
            {
                using var slider =
                    definition.CreateSlider();
                Assert.AreEqual(
                    definition.Title,
                    slider.AccessibleName);
                Assert.AreEqual(
                    definition.MinimumEditorValue,
                    slider.Minimum);
                Assert.AreEqual(
                    definition.MaximumEditorValue,
                    slider.Maximum);
                Assert.AreEqual(
                    definition.SmallChange,
                    slider.SmallChange);
                Assert.AreEqual(
                    definition.Unit,
                    slider.Unit);
                Assert.AreEqual(
                    definition.NeutralEditorValue,
                    slider.NeutralValue);
            }
        });
    }

    [TestMethod]
    public void DefinitionBindingsRoundTripProfileValues()
    {
        var source = VisualProfile.CreateDefaultSoftInvert();
        source.OutputBlack = 0.12f;
        source.OutputWhite = 0.88f;
        source.Brightness = 0.15f;
        source.Contrast = 1.4f;
        source.Saturation = 0.65f;
        source.HueShiftDegrees = 25f;
        var target = VisualProfile.CreateDefaultSoftInvert();

        foreach (var definition in
                 VisualAdjustmentDefinitions.All)
        {
            definition.WriteEditorValue(
                target,
                definition.ReadEditorValue(source));
            Assert.AreEqual(
                definition.GetValue(source),
                definition.GetValue(target),
                0.0001f,
                definition.Id);
        }
    }

    [TestMethod]
    public void EditorRendersEveryDeclaredAdjustment()
    {
        RunOnSta(() =>
        {
            using var editor = new VisualProfileEditorForm(
                VisualProfile.CreateDefaultSoftInvert());
            var names = FindControls<ModernProfileSlider>(editor)
                .Select(slider => slider.AccessibleName)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Cast<string>()
                .ToArray();

            CollectionAssert.AreEquivalent(
                VisualAdjustmentDefinitions.All
                    .Select(definition => definition.Title)
                    .ToArray(),
                names);
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
            "The visual-adjustment test did not finish in time.");
        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }
}
'@
