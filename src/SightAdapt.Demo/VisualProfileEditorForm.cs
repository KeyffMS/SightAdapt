using System.Drawing.Drawing2D;

namespace SightAdapt.Demo;

internal sealed class VisualProfileEditorForm : Form
{
    private readonly VisualProfile _sourceProfile;
    private readonly VisualProfile _workingProfile;
    private readonly ColorProfilePreview _preview;
    private readonly NumericUpDown _outputBlackInput;
    private readonly NumericUpDown _outputWhiteInput;
    private readonly NumericUpDown _brightnessInput;
    private readonly NumericUpDown _contrastInput;
    private readonly NumericUpDown _saturationInput;
    private readonly NumericUpDown _hueInput;

    private VisualProfileEditorForm(VisualProfile profile)
    {
        _sourceProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (!profile.SupportsTuning)
        {
            throw new ArgumentException(
                "Only Soft Invert profiles can be edited.",
                nameof(profile));
        }

        _workingProfile = profile.CreateWorkingCopy();

        Text = $"{ProductInfo.DisplayName} · Edit {profile.Name}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 620);
        Size = new Size(940, 700);
        ShowIcon = false;
        BackColor = AppTheme.WindowBackground;
        AppTheme.ApplyTo(this);

        _preview = new ColorProfilePreview
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Profile = _workingProfile,
        };

        _outputBlackInput = CreateNumericInput(0, 49, 1);
        _outputWhiteInput = CreateNumericInput(51, 100, 1);
        _brightnessInput = CreateNumericInput(-50, 50, 1);
        _contrastInput = CreateNumericInput(50, 200, 1);
        _saturationInput = CreateNumericInput(0, 200, 1);
        _hueInput = CreateNumericInput(-180, 180, 1);

        LoadValuesFromWorkingProfile();
        AttachValueChangedHandlers();

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

        Controls.Add(root);
    }

    public static bool Edit(IWin32Window owner, VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(profile);

        using var editor = new VisualProfileEditorForm(profile);
        return editor.ShowDialog(owner) == DialogResult.OK;
    }

    private Control CreateHeader()
    {
        var title = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(17f, FontStyle.Bold),
            Text = "Soft color profile",
            TextAlign = ContentAlignment.BottomLeft,
        };
        var subtitle = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.2f),
            Text = "Adjust output limits and color balance. Changes apply to every application using this visual profile.",
            TextAlign = ContentAlignment.TopLeft,
        };

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
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);
        return layout;
    }

    private Control CreatePreviewCard()
    {
        var title = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Top,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.5f, FontStyle.Bold),
            Height = 28,
            Text = "LIVE PROFILE PREVIEW",
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var host = new Panel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 10, 16, 14),
        };
        host.Controls.Add(_preview);
        host.Controls.Add(title);

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
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));

        grid.Controls.Add(CreateParameterCell(
            "Output black",
            "0–49% · prevents pure black output",
            "%",
            _outputBlackInput), 0, 0);
        grid.Controls.Add(CreateParameterCell(
            "Output white",
            "51–100% · limits maximum brightness",
            "%",
            _outputWhiteInput), 1, 0);
        grid.Controls.Add(CreateParameterCell(
            "Brightness",
            "-50–50% · moves the whole output range",
            "%",
            _brightnessInput), 2, 0);
        grid.Controls.Add(CreateParameterCell(
            "Contrast",
            "50–200% · expands or compresses differences",
            "%",
            _contrastInput), 0, 1);
        grid.Controls.Add(CreateParameterCell(
            "Saturation",
            "0–200% · grayscale to amplified color",
            "%",
            _saturationInput), 1, 1);
        grid.Controls.Add(CreateParameterCell(
            "Hue shift",
            "-180–180° · rotates the color spectrum",
            "°",
            _hueInput), 2, 1);

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
        var resetButton = new ModernButton
        {
            Text = "Reset soft profile",
            VisualStyle = ModernButtonStyle.Secondary,
            MinimumSize = new Size(160, 40),
            Margin = Padding.Empty,
        };
        resetButton.Click += (_, _) => ResetValues();

        var cancelButton = new ModernButton
        {
            DialogResult = DialogResult.Cancel,
            Text = "Cancel",
            VisualStyle = ModernButtonStyle.Ghost,
            MinimumSize = new Size(100, 40),
            Margin = new Padding(0, 0, 8, 0),
        };
        CancelButton = cancelButton;

        var saveButton = new ModernButton
        {
            DialogResult = DialogResult.OK,
            Text = "Save profile",
            VisualStyle = ModernButtonStyle.Primary,
            MinimumSize = new Size(130, 40),
            Margin = Padding.Empty,
        };
        saveButton.Click += (_, _) => _sourceProfile.CopyTuningFrom(_workingProfile);
        AcceptButton = saveButton;

        var rightButtons = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = AppTheme.WindowBackground,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        rightButtons.Controls.Add(cancelButton);
        rightButtons.Controls.Add(saveButton);

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
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(resetButton, 0, 0);
        layout.Controls.Add(rightButtons, 1, 0);
        resetButton.Anchor = AnchorStyles.Left;
        return layout;
    }

    private static Control CreateParameterCell(
        string title,
        string description,
        string unit,
        NumericUpDown input)
    {
        var titleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.5f, FontStyle.Bold),
            Text = title,
            TextAlign = ContentAlignment.BottomLeft,
        };
        var unitLabel = new Label
        {
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.5f, FontStyle.Bold),
            Margin = new Padding(8, 0, 0, 0),
            Text = unit,
        };
        var valueLayout = new FlowLayoutPanel
        {
            AutoSize = true,
            BackColor = AppTheme.SurfaceRaised,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        valueLayout.Controls.Add(input);
        valueLayout.Controls.Add(unitLabel);

        var descriptionLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.CreateUiFont(8.2f),
            Text = description,
            TextAlign = ContentAlignment.TopLeft,
        };

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(5),
            Padding = new Padding(12, 8, 12, 8),
            RowCount = 3,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        layout.Controls.Add(titleLabel, 0, 0);
        layout.Controls.Add(valueLayout, 0, 1);
        layout.Controls.Add(descriptionLabel, 0, 2);
        return layout;
    }

    private static NumericUpDown CreateNumericInput(
        decimal minimum,
        decimal maximum,
        decimal increment)
    {
        return new NumericUpDown
        {
            BackColor = AppTheme.SurfaceRaised,
            BorderStyle = BorderStyle.FixedSingle,
            DecimalPlaces = 0,
            Font = AppTheme.CreateUiFont(11f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Increment = increment,
            Maximum = maximum,
            Minimum = minimum,
            Size = new Size(92, 32),
            TextAlign = HorizontalAlignment.Right,
            ThousandsSeparator = false,
        };
    }

    private void AttachValueChangedHandlers()
    {
        _outputBlackInput.ValueChanged += ParameterValueChanged;
        _outputWhiteInput.ValueChanged += ParameterValueChanged;
        _brightnessInput.ValueChanged += ParameterValueChanged;
        _contrastInput.ValueChanged += ParameterValueChanged;
        _saturationInput.ValueChanged += ParameterValueChanged;
        _hueInput.ValueChanged += ParameterValueChanged;
    }

    private void ParameterValueChanged(object? sender, EventArgs eventArgs)
    {
        _workingProfile.OutputBlack = (float)_outputBlackInput.Value / 100.0f;
        _workingProfile.OutputWhite = (float)_outputWhiteInput.Value / 100.0f;
        _workingProfile.Brightness = (float)_brightnessInput.Value / 100.0f;
        _workingProfile.Contrast = (float)_contrastInput.Value / 100.0f;
        _workingProfile.Saturation = (float)_saturationInput.Value / 100.0f;
        _workingProfile.HueShiftDegrees = (float)_hueInput.Value;
        _preview.Invalidate();
    }

    private void LoadValuesFromWorkingProfile()
    {
        _outputBlackInput.Value = ToDecimalPercentage(_workingProfile.OutputBlack);
        _outputWhiteInput.Value = ToDecimalPercentage(_workingProfile.OutputWhite);
        _brightnessInput.Value = ToDecimalPercentage(_workingProfile.Brightness);
        _contrastInput.Value = ToDecimalPercentage(_workingProfile.Contrast);
        _saturationInput.Value = ToDecimalPercentage(_workingProfile.Saturation);
        _hueInput.Value = (decimal)_workingProfile.HueShiftDegrees;
    }

    private void ResetValues()
    {
        var defaults = VisualProfile.CreateDefaultSoftInvert();
        _workingProfile.CopyTuningFrom(defaults);
        LoadValuesFromWorkingProfile();
        _preview.Invalidate();
    }

    private static decimal ToDecimalPercentage(float value)
    {
        return Math.Round((decimal)value * 100m, MidpointRounding.AwayFromZero);
    }
}

internal sealed class ColorProfilePreview : Control
{
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

        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.None;
        graphics.Clear(BackColor);

        if (Profile is null || Width < 120 || Height < 80)
        {
            return;
        }

        var effect = new SoftInvertVisualTransform().CreateColorEffect(Profile);
        const int labelWidth = 88;
        const int gap = 5;
        var stripWidth = Math.Max(1, Width - labelWidth - 8);
        var stripHeight = Math.Max(12, (Height - gap * 3 - 8) / 4);
        var labels = new[] { "SOURCE GRAY", "OUTPUT GRAY", "SOURCE HUE", "OUTPUT HUE" };

        for (var row = 0; row < 4; row++)
        {
            var top = 4 + row * (stripHeight + gap);
            TextRenderer.DrawText(
                graphics,
                labels[row],
                Font,
                new Rectangle(4, top, labelWidth - 8, stripHeight),
                ForeColor,
                TextFormatFlags.Left |
                TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPadding);

            for (var x = 0; x < stripWidth; x++)
            {
                var ratio = stripWidth <= 1 ? 0.0f : (float)x / (stripWidth - 1);
                var source = row < 2
                    ? CreateGray(ratio)
                    : CreateHue(ratio * 360.0f);
                var color = row is 1 or 3
                    ? ApplyColorEffect(source, effect)
                    : source;

                using var pen = new Pen(color);
                graphics.DrawLine(
                    pen,
                    labelWidth + x,
                    top,
                    labelWidth + x,
                    top + stripHeight - 1);
            }
        }

        using var borderPen = new Pen(AppTheme.Border);
        graphics.DrawRectangle(borderPen, 0, 0, Width - 1, Height - 1);
    }

    private static Color ApplyColorEffect(Color source, MagColorEffect effect)
    {
        var red = source.R / 255.0f;
        var green = source.G / 255.0f;
        var blue = source.B / 255.0f;

        var outputRed = red * effect.M00 +
            green * effect.M10 +
            blue * effect.M20 +
            effect.M40;
        var outputGreen = red * effect.M01 +
            green * effect.M11 +
            blue * effect.M21 +
            effect.M41;
        var outputBlue = red * effect.M02 +
            green * effect.M12 +
            blue * effect.M22 +
            effect.M42;

        return Color.FromArgb(
            ToByte(outputRed),
            ToByte(outputGreen),
            ToByte(outputBlue));
    }

    private static Color CreateGray(float value)
    {
        var channel = ToByte(value);
        return Color.FromArgb(channel, channel, channel);
    }

    private static Color CreateHue(float hueDegrees)
    {
        var hue = (hueDegrees % 360.0f + 360.0f) % 360.0f;
        var sector = hue / 60.0f;
        var index = (int)MathF.Floor(sector);
        var fraction = sector - index;
        var descending = 1.0f - fraction;

        return index switch
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
            (int)MathF.Round(Math.Clamp(value, 0.0f, 1.0f) * 255.0f),
            0,
            255);
    }
}
