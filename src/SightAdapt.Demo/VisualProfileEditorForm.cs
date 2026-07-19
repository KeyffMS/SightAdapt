using System.Drawing.Drawing2D;

namespace SightAdapt.Demo;

internal sealed class VisualProfileEditorForm : Form
{
    private readonly VisualProfile _workingProfile;
    private readonly ColorProfilePreview _preview;
    private readonly NumericUpDown _outputBlackInput;
    private readonly NumericUpDown _outputWhiteInput;
    private readonly NumericUpDown _brightnessInput;
    private readonly NumericUpDown _contrastInput;
    private readonly NumericUpDown _saturationInput;
    private readonly NumericUpDown _hueInput;
    private bool _loadingValues;

    private VisualProfileEditorForm(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        if (!profile.SupportsTuning)
        {
            throw new ArgumentException(
                "Only editable visual profiles can be edited.",
                nameof(profile));
        }

        _workingProfile = profile.CreateWorkingCopy();
        _preview = new ColorProfilePreview
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Profile = _workingProfile,
        };
        _outputBlackInput = CreatePercentageInput(
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack);
        _outputWhiteInput = CreatePercentageInput(
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite);
        _brightnessInput = CreatePercentageInput(
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness);
        _contrastInput = CreatePercentageInput(
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast);
        _saturationInput = CreatePercentageInput(
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation);
        _hueInput = CreateNumericInput(
            (decimal)VisualProfileLimits.MinimumHueShift,
            (decimal)VisualProfileLimits.MaximumHueShift,
            0.5m,
            1);

        Text = $"{ProductInfo.DisplayName} · Edit {profile.Name}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 620);
        Size = new Size(940, 700);
        ShowIcon = false;
        BackColor = AppTheme.WindowBackground;
        AppTheme.ApplyTo(this);

        Controls.Add(CreateRootLayout());
        LoadValues();
        AttachChangeHandlers();
    }

    public static VisualProfile? Edit(
        IWin32Window owner,
        VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(profile);

        using var editor = new VisualProfileEditorForm(profile);
        return editor.ShowDialog(owner) == DialogResult.OK
            ? editor._workingProfile.CreateWorkingCopy()
            : null;
    }

    private Control CreateRootLayout()
    {
        var root = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(22, 18, 22, 18),
            RowCount = 4,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 176));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreatePreviewCard(), 0, 1);
        root.Controls.Add(CreateParametersCard(), 0, 2);
        root.Controls.Add(CreateActionBar(), 0, 3);
        return root;
    }

    private static Control CreateHeader()
    {
        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(17f, FontStyle.Bold),
            Text = "Soft color profile",
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.2f),
            Text = "Adjust output limits and color balance. Changes apply to every application using this visual profile.",
            TextAlign = ContentAlignment.TopLeft,
        }, 0, 1);
        return layout;
    }

    private Control CreatePreviewCard()
    {
        var host = new Panel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 10, 16, 14),
        };
        host.Controls.Add(_preview);
        host.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.5f, FontStyle.Bold),
            Height = 28,
            Text = "LIVE PROFILE PREVIEW",
            TextAlign = ContentAlignment.MiddleLeft,
        });

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 14),
            Padding = new Padding(1),
        };
        card.Controls.Add(host);
        return card;
    }

    private Control CreateParametersCard()
    {
        var grid = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(12),
            RowCount = 2,
        };
        for (var column = 0; column < 3; column++)
        {
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 3f));
        }
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        AddParameter(grid, 0, 0, "Output black", PercentageRange(
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack,
            "prevents pure black output"), "%", _outputBlackInput);
        AddParameter(grid, 1, 0, "Output white", PercentageRange(
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite,
            "limits maximum brightness"), "%", _outputWhiteInput);
        AddParameter(grid, 2, 0, "Brightness", PercentageRange(
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness,
            "moves the whole output range"), "%", _brightnessInput);
        AddParameter(grid, 0, 1, "Contrast", PercentageRange(
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast,
            "expands or compresses differences"), "%", _contrastInput);
        AddParameter(grid, 1, 1, "Saturation", PercentageRange(
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation,
            "grayscale to amplified color"), "%", _saturationInput);
        AddParameter(grid, 2, 1, "Hue shift", Range(
            VisualProfileLimits.MinimumHueShift,
            VisualProfileLimits.MaximumHueShift,
            "°",
            "rotates the color spectrum"), "°", _hueInput);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(1),
        };
        card.Controls.Add(grid);
        return card;
    }

    private Control CreateActionBar()
    {
        var reset = CreateButton(
            "Reset soft profile",
            ModernButtonStyle.Secondary,
            160);
        reset.Click += (_, _) => ResetValues();

        var cancel = CreateButton("Cancel", ModernButtonStyle.Ghost, 100);
        cancel.DialogResult = DialogResult.Cancel;
        cancel.Margin = new Padding(0, 0, 8, 0);
        CancelButton = cancel;

        var save = CreateButton("Save profile", ModernButtonStyle.Primary, 130);
        save.DialogResult = DialogResult.OK;
        AcceptButton = save;

        var right = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = AppTheme.WindowBackground,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        right.Controls.Add(cancel);
        right.Controls.Add(save);

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 0),
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(reset, 0, 0);
        layout.Controls.Add(right, 1, 0);
        reset.Anchor = AnchorStyles.Left;
        return layout;
    }

    private static ModernButton CreateButton(
        string text,
        ModernButtonStyle style,
        int minimumWidth)
    {
        return new ModernButton
        {
            Text = text,
            VisualStyle = style,
            MinimumSize = new Size(minimumWidth, 40),
            Margin = Padding.Empty,
        };
    }

    private static void AddParameter(
        TableLayoutPanel grid,
        int column,
        int row,
        string title,
        string description,
        string unit,
        NumericUpDown input)
    {
        var value = new FlowLayoutPanel
        {
            AutoSize = true,
            BackColor = AppTheme.SurfaceRaised,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        value.Controls.Add(input);
        value.Controls.Add(new Label
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.5f, FontStyle.Bold),
            Margin = new Padding(8, 0, 0, 0),
            Text = unit,
        });

        var cell = new TableLayoutPanel
        {
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(5),
            Padding = new Padding(12, 8, 12, 8),
            RowCount = 3,
        };
        cell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        cell.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        cell.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.5f, FontStyle.Bold),
            Text = title,
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);
        cell.Controls.Add(value, 0, 1);
        cell.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.CreateUiFont(8.2f),
            Text = description,
            TextAlign = ContentAlignment.TopLeft,
        }, 0, 2);
        grid.Controls.Add(cell, column, row);
    }

    private static NumericUpDown CreatePercentageInput(float minimum, float maximum)
    {
        return CreateNumericInput(
            (decimal)minimum * 100m,
            (decimal)maximum * 100m,
            0.25m,
            2);
    }

    private static NumericUpDown CreateNumericInput(
        decimal minimum,
        decimal maximum,
        decimal increment,
        int decimalPlaces)
    {
        return new NumericUpDown
        {
            BackColor = AppTheme.SurfaceRaised,
            BorderStyle = BorderStyle.FixedSingle,
            DecimalPlaces = decimalPlaces,
            Font = AppTheme.CreateUiFont(11f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Increment = increment,
            Maximum = maximum,
            Minimum = minimum,
            Size = new Size(108, 32),
            TextAlign = HorizontalAlignment.Right,
            ThousandsSeparator = false,
        };
    }

    private void AttachChangeHandlers()
    {
        AttachPercentage(_outputBlackInput, value => _workingProfile.OutputBlack = value);
        AttachPercentage(_outputWhiteInput, value => _workingProfile.OutputWhite = value);
        AttachPercentage(_brightnessInput, value => _workingProfile.Brightness = value);
        AttachPercentage(_contrastInput, value => _workingProfile.Contrast = value);
        AttachPercentage(_saturationInput, value => _workingProfile.Saturation = value);
        _hueInput.ValueChanged += (_, _) =>
        {
            if (_loadingValues)
            {
                return;
            }

            _workingProfile.HueShiftDegrees = (float)_hueInput.Value;
            _preview.Invalidate();
        };
    }

    private void AttachPercentage(NumericUpDown input, Action<float> setter)
    {
        input.ValueChanged += (_, _) =>
        {
            if (_loadingValues)
            {
                return;
            }

            setter((float)(input.Value / 100m));
            _preview.Invalidate();
        };
    }

    private void LoadValues()
    {
        _loadingValues = true;
        try
        {
            SetValue(_outputBlackInput, (decimal)_workingProfile.OutputBlack * 100m);
            SetValue(_outputWhiteInput, (decimal)_workingProfile.OutputWhite * 100m);
            SetValue(_brightnessInput, (decimal)_workingProfile.Brightness * 100m);
            SetValue(_contrastInput, (decimal)_workingProfile.Contrast * 100m);
            SetValue(_saturationInput, (decimal)_workingProfile.Saturation * 100m);
            SetValue(_hueInput, (decimal)_workingProfile.HueShiftDegrees);
        }
        finally
        {
            _loadingValues = false;
        }
    }

    private void ResetValues()
    {
        VisualProfileDefaults.ApplyTuning(
            _workingProfile,
            VisualProfileDefaults.SoftInvertTuning);
        LoadValues();
        _preview.Invalidate();
    }

    private static void SetValue(NumericUpDown input, decimal value)
    {
        input.Value = Math.Clamp(value, input.Minimum, input.Maximum);
    }

    private static string PercentageRange(float minimum, float maximum, string explanation)
    {
        return Range(minimum * 100f, maximum * 100f, "%", explanation);
    }

    private static string Range(float minimum, float maximum, string unit, string explanation)
    {
        return $"{minimum:0.##}–{maximum:0.##}{unit} · {explanation}";
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

    public ColorProfilePreview()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.SurfaceRaised;
        ForeColor = AppTheme.TextSecondary;
        Font = AppTheme.CreateUiFont(7.5f, FontStyle.Bold);
        MinimumSize = new Size(200, 100);
    }

    public VisualProfile? Profile { get; set; }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.None;
        eventArgs.Graphics.Clear(BackColor);

        if (Profile is null || Width < 120 || Height < 80)
        {
            return;
        }

        var transform = VisualTransformCatalog.Default.GetRequired(Profile.TransformId);
        var effect = transform.CreateColorEffect(Profile);
        const int labelWidth = 88;
        const int gap = 5;
        var stripWidth = Math.Max(1, Width - labelWidth - 8);
        var stripHeight = Math.Max(12, (Height - gap * 3 - 8) / 4);

        for (var row = 0; row < Labels.Length; row++)
        {
            var top = 4 + row * (stripHeight + gap);
            TextRenderer.DrawText(
                eventArgs.Graphics,
                Labels[row],
                Font,
                new Rectangle(4, top, labelWidth - 8, stripHeight),
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);

            for (var x = 0; x < stripWidth; x++)
            {
                var ratio = stripWidth <= 1 ? 0f : (float)x / (stripWidth - 1);
                var source = row < 2 ? CreateGray(ratio) : CreateHue(ratio * 360f);
                var color = row is 1 or 3 ? ApplyColorEffect(source, effect) : source;
                using var pen = new Pen(color);
                eventArgs.Graphics.DrawLine(
                    pen,
                    labelWidth + x,
                    top,
                    labelWidth + x,
                    top + stripHeight - 1);
            }
        }

        using var border = new Pen(AppTheme.Border);
        eventArgs.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    private static Color ApplyColorEffect(Color source, MagColorEffect effect)
    {
        var red = source.R / 255f;
        var green = source.G / 255f;
        var blue = source.B / 255f;
        return Color.FromArgb(
            ToByte(red * effect.M00 + green * effect.M10 + blue * effect.M20 + effect.M40),
            ToByte(red * effect.M01 + green * effect.M11 + blue * effect.M21 + effect.M41),
            ToByte(red * effect.M02 + green * effect.M12 + blue * effect.M22 + effect.M42));
    }

    private static Color CreateGray(float value)
    {
        var channel = ToByte(value);
        return Color.FromArgb(channel, channel, channel);
    }

    private static Color CreateHue(float degrees)
    {
        var hue = (degrees % 360f + 360f) % 360f;
        var sector = hue / 60f;
        var fraction = sector - MathF.Floor(sector);
        var descending = 1f - fraction;
        return (int)MathF.Floor(sector) switch
        {
            0 => Color.FromArgb(255, ToByte(fraction), 0),
            1 => Color.FromArgb(ToByte(descending), 255, 0),
            2 => Color.FromArgb(0, 255, ToByte(fraction)),
            3 => Color.FromArgb(0, ToByte(descending), 255),
            4 => Color.FromArgb(ToByte(fraction), 0, 255),
            _ => Color.FromArgb(255, 0, ToByte(descending)),
        };
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp(
            (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f),
            0,
            255);
    }
}
