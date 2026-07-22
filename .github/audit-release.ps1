Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Write-ExistingFile(
    [string]$Path,
    [string]$ExpectedMarker,
    [string]$Content) {
    if (-not (Test-Path $Path)) {
        throw "File '$Path' does not exist."
    }

    $current = Normalize-Newlines (Get-Content -Raw $Path)
    if (-not $current.Contains((Normalize-Newlines $ExpectedMarker))) {
        throw "Expected marker was not found in '$Path'."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

function Write-NewFile([string]$Path, [string]$Content) {
    if (Test-Path $Path) {
        throw "File '$Path' already exists."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

Write-ExistingFile 'src/SightAdapt/ProfileEditorControls.cs' 'using var pen = new Pen(color);' @'
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SightAdapt;

internal readonly record struct ProfilePreviewKey(
    string TransformId,
    float OutputBlack,
    float OutputWhite,
    float Brightness,
    float Contrast,
    float Saturation,
    float HueShiftDegrees)
{
    public static ProfilePreviewKey From(
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new ProfilePreviewKey(
            profile.TransformId,
            profile.OutputBlack,
            profile.OutputWhite,
            profile.Brightness,
            profile.Contrast,
            profile.Saturation,
            profile.HueShiftDegrees);
    }
}

internal sealed class OutputLimitPreview : Control
{
    private readonly Font _captionFont;
    private readonly Font _sampleFont;
    private VisualProfile? _profile;
    private VisualTransformCatalog _transformCatalog =
        VisualTransformCatalog.Default;
    private Bitmap? _cachedBitmap;
    private ProfilePreviewKey? _cachedProfileKey;
    private Size _cachedSize;
    private VisualTransformCatalog? _cachedCatalog;

    public OutputLimitPreview()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.SurfaceRaised;
        MinimumSize = new Size(220, 84);
        _captionFont =
            AppTheme.CreateUiFont(7.5f, FontStyle.Bold);
        _sampleFont =
            AppTheme.CreateUiFont(13f, FontStyle.Bold);
    }

    public VisualProfile? Profile
    {
        get => _profile;
        set
        {
            if (ReferenceEquals(_profile, value))
            {
                return;
            }

            _profile = value;
            InvalidateCache();
            Invalidate();
        }
    }

    public VisualTransformCatalog TransformCatalog
    {
        get => _transformCatalog;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_transformCatalog, value))
            {
                return;
            }

            _transformCatalog = value;
            InvalidateCache();
            Invalidate();
        }
    }

    internal int CacheGeneration { get; private set; }

    internal bool HasCachedBitmap =>
        _cachedBitmap is not null;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.Clear(BackColor);

        EnsureCache();
        if (_cachedBitmap is not null)
        {
            eventArgs.Graphics.DrawImageUnscaled(
                _cachedBitmap,
                Point.Empty);
        }
    }

    protected override void OnSizeChanged(EventArgs eventArgs)
    {
        base.OnSizeChanged(eventArgs);
        InvalidateCache();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InvalidateCache();
            _captionFont.Dispose();
            _sampleFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void EnsureCache()
    {
        if (_profile is null || Width < 160 || Height < 60)
        {
            InvalidateCache();
            return;
        }

        var profileKey = ProfilePreviewKey.From(_profile);
        if (_cachedBitmap is not null &&
            _cachedProfileKey == profileKey &&
            _cachedSize == ClientSize &&
            ReferenceEquals(_cachedCatalog, _transformCatalog))
        {
            return;
        }

        InvalidateCache();
        _cachedBitmap = BuildCache(_profile);
        _cachedProfileKey = profileKey;
        _cachedSize = ClientSize;
        _cachedCatalog = _transformCatalog;
        CacheGeneration++;
    }

    private Bitmap BuildCache(VisualProfile profile)
    {
        var bitmap = new Bitmap(
            Width,
            Height,
            PixelFormat.Format32bppPArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        var effect = _transformCatalog
            .GetRequired(profile.TransformId)
            .CreateColorEffect(profile);
        var sourceForeground = Color.Black;
        var sourceBackground = Color.White;
        var outputForeground =
            ColorEffectPreviewMath.Apply(
                sourceForeground,
                effect);
        var outputBackground =
            ColorEffectPreviewMath.Apply(
                sourceBackground,
                effect);

        const int gap = 10;
        var sampleWidth =
            Math.Max(60, (Width - gap * 3) / 2);
        var sampleHeight = Height - 14;
        DrawSample(
            graphics,
            new Rectangle(
                gap,
                7,
                sampleWidth,
                sampleHeight),
            "SOURCE",
            sourceForeground,
            sourceBackground);
        DrawSample(
            graphics,
            new Rectangle(
                gap * 2 + sampleWidth,
                7,
                sampleWidth,
                sampleHeight),
            "OUTPUT",
            outputForeground,
            outputBackground);
        return bitmap;
    }

    private void DrawSample(
        Graphics graphics,
        Rectangle bounds,
        string caption,
        Color foreground,
        Color background)
    {
        using var backgroundBrush =
            new SolidBrush(background);
        using var borderPen =
            new Pen(AppTheme.Border);
        graphics.FillRectangle(backgroundBrush, bounds);
        graphics.DrawRectangle(borderPen, bounds);

        var captionColor =
            ColorEffectPreviewMath.ContrastText(background);
        TextRenderer.DrawText(
            graphics,
            caption,
            _captionFont,
            new Rectangle(
                bounds.Left + 8,
                bounds.Top + 5,
                bounds.Width - 16,
                18),
            captionColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);
        TextRenderer.DrawText(
            graphics,
            "SightAdapt",
            _sampleFont,
            new Rectangle(
                bounds.Left + 8,
                bounds.Top + 24,
                bounds.Width - 16,
                bounds.Height - 30),
            foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
    }

    private void InvalidateCache()
    {
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
        _cachedProfileKey = null;
        _cachedSize = Size.Empty;
        _cachedCatalog = null;
    }
}

internal sealed class ColorProfilePreview : Control
{
    private static readonly string[] Labels =
    [
        "SOURCE GRAY",
        "OUTPUT GRAY",
        "SOURCE HUE",
        "OUTPUT HUE",
    ];

    private readonly Font _labelFont;
    private VisualProfile? _profile;
    private VisualTransformCatalog _transformCatalog =
        VisualTransformCatalog.Default;
    private Bitmap? _cachedBitmap;
    private ProfilePreviewKey? _cachedProfileKey;
    private Size _cachedSize;
    private VisualTransformCatalog? _cachedCatalog;

    public ColorProfilePreview()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.SurfaceRaised;
        ForeColor = AppTheme.TextSecondary;
        MinimumSize = new Size(200, 100);
        _labelFont =
            AppTheme.CreateUiFont(7.5f, FontStyle.Bold);
    }

    public VisualProfile? Profile
    {
        get => _profile;
        set
        {
            if (ReferenceEquals(_profile, value))
            {
                return;
            }

            _profile = value;
            InvalidateCache();
            Invalidate();
        }
    }

    public VisualTransformCatalog TransformCatalog
    {
        get => _transformCatalog;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            if (ReferenceEquals(_transformCatalog, value))
            {
                return;
            }

            _transformCatalog = value;
            InvalidateCache();
            Invalidate();
        }
    }

    internal int CacheGeneration { get; private set; }

    internal bool HasCachedBitmap =>
        _cachedBitmap is not null;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.Clear(BackColor);

        EnsureCache();
        if (_cachedBitmap is not null)
        {
            eventArgs.Graphics.DrawImageUnscaled(
                _cachedBitmap,
                Point.Empty);
        }
    }

    protected override void OnSizeChanged(EventArgs eventArgs)
    {
        base.OnSizeChanged(eventArgs);
        InvalidateCache();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            InvalidateCache();
            _labelFont.Dispose();
        }

        base.Dispose(disposing);
    }

    private void EnsureCache()
    {
        if (_profile is null || Width < 120 || Height < 80)
        {
            InvalidateCache();
            return;
        }

        var profileKey = ProfilePreviewKey.From(_profile);
        if (_cachedBitmap is not null &&
            _cachedProfileKey == profileKey &&
            _cachedSize == ClientSize &&
            ReferenceEquals(_cachedCatalog, _transformCatalog))
        {
            return;
        }

        InvalidateCache();
        _cachedBitmap = BuildCache(_profile);
        _cachedProfileKey = profileKey;
        _cachedSize = ClientSize;
        _cachedCatalog = _transformCatalog;
        CacheGeneration++;
    }

    private Bitmap BuildCache(VisualProfile profile)
    {
        var bitmap = new Bitmap(
            Width,
            Height,
            PixelFormat.Format32bppPArgb);
        var effect = _transformCatalog
            .GetRequired(profile.TransformId)
            .CreateColorEffect(profile);
        const int labelWidth = 96;
        const int gap = 5;
        var stripWidth =
            Math.Max(1, Width - labelWidth - 8);
        var stripHeight =
            Math.Max(12, (Height - gap * 3 - 8) / 4);

        FillPixelBuffer(
            bitmap,
            effect,
            labelWidth,
            gap,
            stripWidth,
            stripHeight);

        using var graphics = Graphics.FromImage(bitmap);
        for (var row = 0; row < Labels.Length; row++)
        {
            var top =
                4 + row * (stripHeight + gap);
            TextRenderer.DrawText(
                graphics,
                Labels[row],
                _labelFont,
                new Rectangle(
                    4,
                    top,
                    labelWidth - 8,
                    stripHeight),
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);
        }

        using var border = new Pen(AppTheme.Border);
        graphics.DrawRectangle(
            border,
            0,
            0,
            Width - 1,
            Height - 1);
        return bitmap;
    }

    private void FillPixelBuffer(
        Bitmap bitmap,
        MagColorEffect effect,
        int labelWidth,
        int gap,
        int stripWidth,
        int stripHeight)
    {
        var bounds = new Rectangle(
            0,
            0,
            bitmap.Width,
            bitmap.Height);
        var data = bitmap.LockBits(
            bounds,
            ImageLockMode.WriteOnly,
            PixelFormat.Format32bppPArgb);

        try
        {
            var stridePixels = data.Stride / 4;
            var pixels = new int[
                stridePixels * bitmap.Height];
            Array.Fill(pixels, BackColor.ToArgb());

            for (var row = 0; row < Labels.Length; row++)
            {
                var top =
                    4 + row * (stripHeight + gap);
                for (var x = 0; x < stripWidth; x++)
                {
                    var ratio = stripWidth <= 1
                        ? 0f
                        : (float)x / (stripWidth - 1);
                    var source = row < 2
                        ? ColorEffectPreviewMath.CreateGray(ratio)
                        : ColorEffectPreviewMath.CreateHue(
                            ratio * 360f);
                    var color = row is 1 or 3
                        ? ColorEffectPreviewMath.Apply(
                            source,
                            effect)
                        : source;
                    var argb = color.ToArgb();
                    var destinationX = labelWidth + x;

                    for (var y = 0;
                         y < stripHeight;
                         y++)
                    {
                        pixels[
                            (top + y) * stridePixels +
                            destinationX] = argb;
                    }
                }
            }

            Marshal.Copy(
                pixels,
                0,
                data.Scan0,
                pixels.Length);
        }
        finally
        {
            bitmap.UnlockBits(data);
        }
    }

    private void InvalidateCache()
    {
        _cachedBitmap?.Dispose();
        _cachedBitmap = null;
        _cachedProfileKey = null;
        _cachedSize = Size.Empty;
        _cachedCatalog = null;
    }
}

internal static class ColorEffectPreviewMath
{
    public static Color Apply(
        Color source,
        MagColorEffect effect)
    {
        var red = source.R / 255f;
        var green = source.G / 255f;
        var blue = source.B / 255f;
        return Color.FromArgb(
            ToByte(
                red * effect.M00 +
                green * effect.M10 +
                blue * effect.M20 +
                effect.M40),
            ToByte(
                red * effect.M01 +
                green * effect.M11 +
                blue * effect.M21 +
                effect.M41),
            ToByte(
                red * effect.M02 +
                green * effect.M12 +
                blue * effect.M22 +
                effect.M42));
    }

    public static Color CreateGray(float value)
    {
        var channel = ToByte(value);
        return Color.FromArgb(
            channel,
            channel,
            channel);
    }

    public static Color CreateHue(float degrees)
    {
        var hue =
            (degrees % 360f + 360f) % 360f;
        var sector = hue / 60f;
        var fraction =
            sector - MathF.Floor(sector);
        var descending = 1f - fraction;
        return (int)MathF.Floor(sector) switch
        {
            0 => Color.FromArgb(
                255,
                ToByte(fraction),
                0),
            1 => Color.FromArgb(
                ToByte(descending),
                255,
                0),
            2 => Color.FromArgb(
                0,
                255,
                ToByte(fraction)),
            3 => Color.FromArgb(
                0,
                ToByte(descending),
                255),
            4 => Color.FromArgb(
                ToByte(fraction),
                0,
                255),
            _ => Color.FromArgb(
                255,
                0,
                ToByte(descending)),
        };
    }

    public static Color ContrastText(Color background)
    {
        var luminance =
            0.2126f * background.R +
            0.7152f * background.G +
            0.0722f * background.B;
        return luminance > 145f
            ? Color.Black
            : Color.White;
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp(
            (int)MathF.Round(
                Math.Clamp(value, 0f, 1f) * 255f),
            0,
            255);
    }
}
'@

Write-NewFile 'tests/SightAdapt.Tests/ProfilePreviewCacheTests.cs' @'
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
'@

Replace-Exact 'tests/SightAdapt.Tests/ArchitectureComplianceTests.cs' @'
        StringAssert.Contains(source, "OutputLimitPreview");
        Assert.IsFalse(source.Contains("NumericUpDown", StringComparison.Ordinal));
'@ @'
        StringAssert.Contains(source, "OutputLimitPreview");
        Assert.IsFalse(source.Contains("NumericUpDown", StringComparison.Ordinal));

        var preview = ReadSource("ProfileEditorControls.cs");
        StringAssert.Contains(preview, "LockBits");
        StringAssert.Contains(preview, "ProfilePreviewKey");
        Assert.IsFalse(
            preview.Contains(
                "using var pen = new Pen(color)",
                StringComparison.Ordinal));
'@
