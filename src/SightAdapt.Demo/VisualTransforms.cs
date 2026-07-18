namespace SightAdapt.Demo;

internal interface IVisualTransform
{
    string Id { get; }

    MagColorEffect CreateColorEffect(VisualProfile profile);
}

internal sealed class InvertVisualTransform : IVisualTransform
{
    public const string TransformId = "invert";

    public string Id => TransformId;

    public MagColorEffect CreateColorEffect(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return MagColorEffect.Invert;
    }
}

internal sealed class SoftInvertVisualTransform : IVisualTransform
{
    public const string TransformId = "soft-invert";

    public string Id => TransformId;

    public MagColorEffect CreateColorEffect(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var outputBlack = VisualProfileLimits.ClampFinite(
            profile.OutputBlack,
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack,
            0.08f);
        var outputWhite = VisualProfileLimits.ClampFinite(
            profile.OutputWhite,
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite,
            0.92f);
        var brightness = VisualProfileLimits.ClampFinite(
            profile.Brightness,
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness,
            0.0f);
        var contrast = VisualProfileLimits.ClampFinite(
            profile.Contrast,
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast,
            1.0f);
        var saturation = VisualProfileLimits.ClampFinite(
            profile.Saturation,
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation,
            1.0f);
        var hueShift = VisualProfileLimits.ClampFinite(
            profile.HueShiftDegrees,
            VisualProfileLimits.MinimumHueShift,
            VisualProfileLimits.MaximumHueShift,
            0.0f);

        var outputRange = outputWhite - outputBlack;
        var matrix = ColorAffineMatrix.CreateScaleOffset(-outputRange, outputWhite)
            .Then(ColorAffineMatrix.CreateSaturation(saturation))
            .Then(ColorAffineMatrix.CreateHueRotation(hueShift))
            .Then(ColorAffineMatrix.CreateContrast(contrast))
            .Then(ColorAffineMatrix.CreateBrightness(brightness));

        return matrix.ToMagColorEffect();
    }
}

internal static class VisualProfileLimits
{
    public const float MinimumOutputBlack = 0.0f;
    public const float MaximumOutputBlack = 0.49f;
    public const float MinimumOutputWhite = 0.51f;
    public const float MaximumOutputWhite = 1.0f;
    public const float MinimumBrightness = -0.5f;
    public const float MaximumBrightness = 0.5f;
    public const float MinimumContrast = 0.5f;
    public const float MaximumContrast = 2.0f;
    public const float MinimumSaturation = 0.0f;
    public const float MaximumSaturation = 2.0f;
    public const float MinimumHueShift = -180.0f;
    public const float MaximumHueShift = 180.0f;

    public static float ClampFinite(float value, float minimum, float maximum, float fallback)
    {
        return float.IsFinite(value)
            ? Math.Clamp(value, minimum, maximum)
            : fallback;
    }
}

internal sealed class VisualTransformCatalog
{
    private readonly IReadOnlyDictionary<string, IVisualTransform> _transforms;

    public VisualTransformCatalog(IEnumerable<IVisualTransform>? transforms = null)
    {
        var availableTransforms = transforms?.ToArray() ??
            [new InvertVisualTransform(), new SoftInvertVisualTransform()];

        _transforms = availableTransforms.ToDictionary(
            transform => transform.Id,
            StringComparer.OrdinalIgnoreCase);
    }

    public IVisualTransform GetRequired(string transformId)
    {
        if (string.IsNullOrWhiteSpace(transformId) ||
            !_transforms.TryGetValue(transformId.Trim(), out var transform))
        {
            throw new InvalidOperationException(
                $"The visual transform '{transformId}' is not registered.");
        }

        return transform;
    }
}

internal sealed class ColorAffineMatrix
{
    private readonly float[,] _linear;
    private readonly float[] _offset;

    private ColorAffineMatrix(float[,] linear, float[] offset)
    {
        _linear = linear;
        _offset = offset;
    }

    public static ColorAffineMatrix CreateScaleOffset(float scale, float offset)
    {
        return new ColorAffineMatrix(
            new[,]
            {
                { scale, 0.0f, 0.0f },
                { 0.0f, scale, 0.0f },
                { 0.0f, 0.0f, scale },
            },
            [offset, offset, offset]);
    }

    public static ColorAffineMatrix CreateBrightness(float brightness)
    {
        return new ColorAffineMatrix(CreateIdentityLinear(), [brightness, brightness, brightness]);
    }

    public static ColorAffineMatrix CreateContrast(float contrast)
    {
        var offset = 0.5f * (1.0f - contrast);
        return CreateScaleOffset(contrast, offset);
    }

    public static ColorAffineMatrix CreateSaturation(float saturation)
    {
        const float luminanceRed = 0.2126f;
        const float luminanceGreen = 0.7152f;
        const float luminanceBlue = 0.0722f;

        var inverse = 1.0f - saturation;
        return new ColorAffineMatrix(
            new[,]
            {
                {
                    luminanceRed * inverse + saturation,
                    luminanceRed * inverse,
                    luminanceRed * inverse,
                },
                {
                    luminanceGreen * inverse,
                    luminanceGreen * inverse + saturation,
                    luminanceGreen * inverse,
                },
                {
                    luminanceBlue * inverse,
                    luminanceBlue * inverse,
                    luminanceBlue * inverse + saturation,
                },
            },
            [0.0f, 0.0f, 0.0f]);
    }

    public static ColorAffineMatrix CreateHueRotation(float degrees)
    {
        if (Math.Abs(degrees) < 0.0001f)
        {
            return new ColorAffineMatrix(CreateIdentityLinear(), [0.0f, 0.0f, 0.0f]);
        }

        var radians = degrees * MathF.PI / 180.0f;
        var cosine = MathF.Cos(radians);
        var sine = MathF.Sin(radians);

        var rgbToYiq = new[,]
        {
            { 0.299f, 0.587f, 0.114f },
            { 0.596f, -0.274f, -0.322f },
            { 0.211f, -0.523f, 0.312f },
        };
        var rotation = new[,]
        {
            { 1.0f, 0.0f, 0.0f },
            { 0.0f, cosine, -sine },
            { 0.0f, sine, cosine },
        };
        var yiqToRgb = new[,]
        {
            { 1.0f, 0.956f, 0.621f },
            { 1.0f, -0.272f, -0.647f },
            { 1.0f, -1.106f, 1.703f },
        };

        var columnMatrix = MultiplyColumnMatrices(
            MultiplyColumnMatrices(yiqToRgb, rotation),
            rgbToYiq);
        var rowMatrix = new float[3, 3];

        for (var source = 0; source < 3; source++)
        {
            for (var destination = 0; destination < 3; destination++)
            {
                rowMatrix[source, destination] = columnMatrix[destination, source];
            }
        }

        return new ColorAffineMatrix(rowMatrix, [0.0f, 0.0f, 0.0f]);
    }

    public ColorAffineMatrix Then(ColorAffineMatrix next)
    {
        ArgumentNullException.ThrowIfNull(next);

        var linear = new float[3, 3];
        var offset = new float[3];

        for (var source = 0; source < 3; source++)
        {
            for (var destination = 0; destination < 3; destination++)
            {
                for (var middle = 0; middle < 3; middle++)
                {
                    linear[source, destination] +=
                        _linear[source, middle] * next._linear[middle, destination];
                }
            }
        }

        for (var destination = 0; destination < 3; destination++)
        {
            offset[destination] = next._offset[destination];

            for (var middle = 0; middle < 3; middle++)
            {
                offset[destination] += _offset[middle] * next._linear[middle, destination];
            }
        }

        return new ColorAffineMatrix(linear, offset);
    }

    public MagColorEffect ToMagColorEffect()
    {
        return new MagColorEffect
        {
            M00 = _linear[0, 0],
            M01 = _linear[0, 1],
            M02 = _linear[0, 2],
            M10 = _linear[1, 0],
            M11 = _linear[1, 1],
            M12 = _linear[1, 2],
            M20 = _linear[2, 0],
            M21 = _linear[2, 1],
            M22 = _linear[2, 2],
            M33 = 1.0f,
            M40 = _offset[0],
            M41 = _offset[1],
            M42 = _offset[2],
            M44 = 1.0f,
        };
    }

    private static float[,] CreateIdentityLinear()
    {
        return new[,]
        {
            { 1.0f, 0.0f, 0.0f },
            { 0.0f, 1.0f, 0.0f },
            { 0.0f, 0.0f, 1.0f },
        };
    }

    private static float[,] MultiplyColumnMatrices(float[,] left, float[,] right)
    {
        var result = new float[3, 3];

        for (var row = 0; row < 3; row++)
        {
            for (var column = 0; column < 3; column++)
            {
                for (var middle = 0; middle < 3; middle++)
                {
                    result[row, column] += left[row, middle] * right[middle, column];
                }
            }
        }

        return result;
    }
}
