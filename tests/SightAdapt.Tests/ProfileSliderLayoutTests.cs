using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProfileSliderLayoutTests
{
    [TestMethod]
    public void SliderUsesFullWidthTrack()
    {
        var source = ReadSource("ProfileSliderControl.cs");
        StringAssert.Contains(source, "Dock = DockStyle.Fill;");
        StringAssert.Contains(source, "Margin = Padding.Empty;");
        StringAssert.Contains(source, "Math.Max(1, Width - 10)");
    }

    [TestMethod]
    public void SliderShowsAndSnapsToNeutralPoint()
    {
        var source = ReadSource("ProfileSliderControl.cs");
        StringAssert.Contains(source, "ConfigureDefaultNeutralPoint");
        StringAssert.Contains(source, "DrawNeutralMarker");
        StringAssert.Contains(source, "NeutralMagnetRatio");
        StringAssert.Contains(source, "Value = neutral;");
    }

    [TestMethod]
    public void NeutralRangesUseCenteredMapping()
    {
        var source = ReadSource("ProfileSliderControl.cs");
        StringAssert.Contains(source, "ValueToRatio");
        StringAssert.Contains(source, "RatioToValue");
        StringAssert.Contains(source, "0.5f");
    }

    private static string ReadSource(string fileName)
    {
        return File.ReadAllText(Path.Combine(SourceDirectory, fileName));
    }

    private static string SourceDirectory =>
        Path.Combine(RepositoryRoot, "src", "SightAdapt");

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "src", "SightAdapt")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("The SightAdapt repository root could not be located.");
        }
    }
}
