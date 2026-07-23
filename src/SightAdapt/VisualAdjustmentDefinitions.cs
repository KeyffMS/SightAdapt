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