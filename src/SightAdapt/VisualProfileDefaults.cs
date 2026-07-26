namespace SightAdapt;

internal readonly record struct VisualProfileTuning(
    float OutputBlack,
    float OutputWhite,
    float Brightness,
    float Contrast,
    float Saturation,
    float HueShiftDegrees);

internal static class VisualProfileDefaults
{
    public const float ExactOutputBlack = 0.0f;
    public const float ExactOutputWhite = 1.0f;
    public const float ExactBrightness = 0.0f;
    public const float ExactContrast = 1.0f;
    public const float ExactSaturation = 1.0f;
    public const float ExactHueShiftDegrees = 0.0f;

    public const float SoftOutputBlack = 0.08f;
    public const float SoftOutputWhite = 0.92f;
    public const float SoftBrightness = 0.0f;
    public const float SoftContrast = 1.0f;
    public const float SoftSaturation = 1.0f;
    public const float SoftHueShiftDegrees = 0.0f;

    public static VisualProfileTuning ExactInvertTuning { get; } = new(
        ExactOutputBlack,
        ExactOutputWhite,
        ExactBrightness,
        ExactContrast,
        ExactSaturation,
        ExactHueShiftDegrees);

    public static VisualProfileTuning SoftInvertTuning { get; } = new(
        SoftOutputBlack,
        SoftOutputWhite,
        SoftBrightness,
        SoftContrast,
        SoftSaturation,
        SoftHueShiftDegrees);

    public static VisualProfileTuning NormalizeSoftInvertTuning(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new VisualProfileTuning(
            VisualProfileLimits.ClampFinite(
                profile.OutputBlack,
                VisualProfileLimits.MinimumOutputBlack,
                VisualProfileLimits.MaximumOutputBlack,
                SoftOutputBlack),
            VisualProfileLimits.ClampFinite(
                profile.OutputWhite,
                VisualProfileLimits.MinimumOutputWhite,
                VisualProfileLimits.MaximumOutputWhite,
                SoftOutputWhite),
            VisualProfileLimits.ClampFinite(
                profile.Brightness,
                VisualProfileLimits.MinimumBrightness,
                VisualProfileLimits.MaximumBrightness,
                SoftBrightness),
            VisualProfileLimits.ClampFinite(
                profile.Contrast,
                VisualProfileLimits.MinimumContrast,
                VisualProfileLimits.MaximumContrast,
                SoftContrast),
            VisualProfileLimits.ClampFinite(
                profile.Saturation,
                VisualProfileLimits.MinimumSaturation,
                VisualProfileLimits.MaximumSaturation,
                SoftSaturation),
            VisualProfileLimits.ClampFinite(
                profile.HueShiftDegrees,
                VisualProfileLimits.MinimumHueShift,
                VisualProfileLimits.MaximumHueShift,
                SoftHueShiftDegrees));
    }

    public static void ApplyTuning(
        VisualProfile profile,
        VisualProfileTuning tuning)
    {
        ArgumentNullException.ThrowIfNull(profile);

        profile.OutputBlack = tuning.OutputBlack;
        profile.OutputWhite = tuning.OutputWhite;
        profile.Brightness = tuning.Brightness;
        profile.Contrast = tuning.Contrast;
        profile.Saturation = tuning.Saturation;
        profile.HueShiftDegrees = tuning.HueShiftDegrees;
    }

    internal static bool ApplyTuningIfChanged(
        VisualProfile profile,
        VisualProfileTuning tuning)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var changed = profile.OutputBlack != tuning.OutputBlack ||
            profile.OutputWhite != tuning.OutputWhite ||
            profile.Brightness != tuning.Brightness ||
            profile.Contrast != tuning.Contrast ||
            profile.Saturation != tuning.Saturation ||
            profile.HueShiftDegrees != tuning.HueShiftDegrees;

        ApplyTuning(profile, tuning);
        return changed;
    }
}
