namespace SightAdapt.Demo;

internal readonly record struct VisualProfileTuning(
    float OutputBlack,
    float OutputWhite,
    float Brightness,
    float Contrast,
    float Saturation,
    float HueShiftDegrees);

internal static class VisualProfileDefaults
{
    public const string ExactInvertName = "Exact invert";
    public const string SoftInvertName = "Soft invert";

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

    public static VisualProfile CreateExactInvert()
    {
        var profile = new VisualProfile
        {
            Id = VisualProfile.DefaultInvertId,
            Name = ExactInvertName,
            TransformId = InvertVisualTransform.TransformId,
        };
        ApplyTuning(profile, ExactInvertTuning);
        return profile;
    }

    public static VisualProfile CreateSoftInvert()
    {
        var profile = new VisualProfile
        {
            Id = VisualProfile.DefaultSoftInvertId,
            Name = SoftInvertName,
            TransformId = SoftInvertVisualTransform.TransformId,
        };
        ApplyTuning(profile, SoftInvertTuning);
        return profile;
    }

    public static bool CanonicalizeExactInvert(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var changed = !string.Equals(
                profile.Id,
                VisualProfile.DefaultInvertId,
                StringComparison.Ordinal) ||
            !string.Equals(profile.Name, ExactInvertName, StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                InvertVisualTransform.TransformId,
                StringComparison.Ordinal);

        profile.Id = VisualProfile.DefaultInvertId;
        profile.Name = ExactInvertName;
        profile.TransformId = InvertVisualTransform.TransformId;
        return ApplyTuningIfChanged(profile, ExactInvertTuning) || changed;
    }

    public static bool CanonicalizeSoftInvert(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var changed = !string.Equals(
                profile.Id,
                VisualProfile.DefaultSoftInvertId,
                StringComparison.Ordinal) ||
            !string.Equals(profile.Name, SoftInvertName, StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                SoftInvertVisualTransform.TransformId,
                StringComparison.Ordinal);

        profile.Id = VisualProfile.DefaultSoftInvertId;
        profile.Name = SoftInvertName;
        profile.TransformId = SoftInvertVisualTransform.TransformId;
        return ApplyTuningIfChanged(
            profile,
            NormalizeSoftInvertTuning(profile)) || changed;
    }

    public static bool NormalizeTuningForTransform(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var tuning = string.Equals(
            profile.TransformId,
            InvertVisualTransform.TransformId,
            StringComparison.OrdinalIgnoreCase)
                ? ExactInvertTuning
                : NormalizeSoftInvertTuning(profile);

        return ApplyTuningIfChanged(profile, tuning);
    }

    public static VisualProfileTuning NormalizeSoftInvertTuning(VisualProfile profile)
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

    private static bool ApplyTuningIfChanged(
        VisualProfile profile,
        VisualProfileTuning tuning)
    {
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
