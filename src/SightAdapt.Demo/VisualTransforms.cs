namespace SightAdapt.Demo;

internal interface IVisualTransform
{
    string Id { get; }

    MagColorEffect CreateColorEffect();
}

internal sealed class InvertVisualTransform : IVisualTransform
{
    public const string TransformId = "invert";

    public string Id => TransformId;

    public MagColorEffect CreateColorEffect()
    {
        return MagColorEffect.Invert;
    }
}

internal sealed class VisualTransformCatalog
{
    private readonly IReadOnlyDictionary<string, IVisualTransform> _transforms;

    public VisualTransformCatalog(IEnumerable<IVisualTransform>? transforms = null)
    {
        var availableTransforms = transforms?.ToArray() ??
            [new InvertVisualTransform()];

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
