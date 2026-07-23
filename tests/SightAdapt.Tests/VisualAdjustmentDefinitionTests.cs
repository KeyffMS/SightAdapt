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