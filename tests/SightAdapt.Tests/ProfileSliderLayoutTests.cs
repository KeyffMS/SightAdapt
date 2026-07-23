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
                .ToDictionary(
                    slider => slider.AccessibleName ??
                        throw new InvalidOperationException(
                            "Every profile slider requires an accessible name."),
                    StringComparer.Ordinal);

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