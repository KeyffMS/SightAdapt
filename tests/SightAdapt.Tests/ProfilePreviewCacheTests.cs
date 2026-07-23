using System.Drawing;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ProfilePreviewCacheTests
{
    [TestMethod]
    public void ColorPreviewReusesAndInvalidatesCachedBitmap()
    {
        RunOnSta(() =>
        {
            var profile =
                VisualProfile.CreateDefaultSoftInvert();
            using var preview = new ColorProfilePreview
            {
                Profile = profile,
                Size = new Size(420, 120),
            };

            Render(preview);
            Assert.AreEqual(1, preview.CacheGeneration);
            Assert.IsTrue(preview.HasCachedBitmap);

            Render(preview);
            Assert.AreEqual(1, preview.CacheGeneration);

            profile.Brightness = 0.1f;
            preview.Invalidate();
            Render(preview);
            Assert.AreEqual(2, preview.CacheGeneration);

            preview.Width += 20;
            Render(preview);
            Assert.AreEqual(3, preview.CacheGeneration);
        });
    }

    [TestMethod]
    public void OutputPreviewReusesCacheAndDisposesOwnedBitmap()
    {
        RunOnSta(() =>
        {
            var profile =
                VisualProfile.CreateDefaultSoftInvert();
            var preview = new OutputLimitPreview
            {
                Profile = profile,
                Size = new Size(420, 100),
            };

            Render(preview);
            Render(preview);
            Assert.AreEqual(1, preview.CacheGeneration);
            Assert.IsTrue(preview.HasCachedBitmap);

            profile.OutputBlack = 0.12f;
            preview.Invalidate();
            Render(preview);
            Assert.AreEqual(2, preview.CacheGeneration);

            preview.Dispose();
            Assert.IsFalse(preview.HasCachedBitmap);
        });
    }

    private static void Render(Control control)
    {
        control.CreateControl();
        using var bitmap = new Bitmap(
            control.Width,
            control.Height);
        control.DrawToBitmap(
            bitmap,
            control.ClientRectangle);
    }

    private static void RunOnSta(Action scenario)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                scenario();
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
            "The profile-preview cache test did not finish in time.");

        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }
}