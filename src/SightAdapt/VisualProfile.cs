namespace SightAdapt;

internal sealed class VisualProfile
{
    public string Id { get; set; } =
        VisualProfileCatalog.DefaultSoftInvertId;

    public string Name { get; set; } = string.Empty;

    public string TransformId { get; set; } =
        SoftInvertVisualTransform.TransformId;

    public float OutputBlack { get; set; } =
        VisualProfileDefaults.SoftOutputBlack;

    public float OutputWhite { get; set; } =
        VisualProfileDefaults.SoftOutputWhite;

    public float Brightness { get; set; } =
        VisualProfileDefaults.SoftBrightness;

    public float Contrast { get; set; } =
        VisualProfileDefaults.SoftContrast;

    public float Saturation { get; set; } =
        VisualProfileDefaults.SoftSaturation;

    public float HueShiftDegrees { get; set; } =
        VisualProfileDefaults.SoftHueShiftDegrees;

    public bool SupportsTuning =>
        VisualProfileCatalog.Default.SupportsTuning(TransformId);

    public VisualProfile CreateWorkingCopy()
    {
        return new VisualProfile
        {
            Id = Id,
            Name = Name,
            TransformId = TransformId,
            OutputBlack = OutputBlack,
            OutputWhite = OutputWhite,
            Brightness = Brightness,
            Contrast = Contrast,
            Saturation = Saturation,
            HueShiftDegrees = HueShiftDegrees,
        };
    }

    public override string ToString()
    {
        return string.IsNullOrWhiteSpace(Name) ? Id : Name;
    }
}
