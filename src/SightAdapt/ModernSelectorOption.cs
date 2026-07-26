using System.Drawing.Drawing2D;

namespace SightAdapt;

internal sealed record ModernSelectorOption(
    string Id,
    string Name)
{
    public override string ToString()
    {
        return Name;
    }
}
