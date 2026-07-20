using System.Drawing.Drawing2D;
using System.Globalization;

namespace SightAdapt.Demo;

internal sealed class VisualProfileEditorForm : Form
{
    private readonly VisualProfile _workingProfile;
    private readonly ColorProfilePreview _preview;
    private readonly OutputLimitPreview _outputPreview;
    private readonly ModernProfileSlider _outputBlackSlider;
    private readonly ModernProfileSlider _outputWhiteSlider;
    private readonly ModernProfileSlider _brightnessSlider;
    private readonly ModernProfileSlider _contrastSlider;
    private readonly ModernProfileSlider _saturationSlider;
    private readonly ModernProfileSlider _hueSlider;
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
        _outputPreview = new OutputLimitPreview
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Profile = _workingProfile,
        };
        _outputBlackSlider = CreatePercentageSlider(
            "Output black",
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack);
        _outputWhiteSlider = CreatePercentageSlider(
            "Output white",
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite);
        _brightnessSlider = CreatePercentageSlider(
            "Brightness",
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness);
        _contrastSlider = CreatePercentageSlider(
            "Contrast",
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast);
        _saturationSlider = CreatePercentageSlider(
            "Saturation",
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation);
        _hueSlider = new ModernProfileSlider
        {
            AccessibleName = "Hue shift",
            DecimalPlaces = 1,
            Minimum = VisualProfileLimits.MinimumHueShift,
            Maximum = VisualProfileLimits.MaximumHueShift,
            SmallChange = 0.5f,
            Unit = "°",
        };

        Text = $"{ProductInfo.DisplayName} · Edit {profile.Name}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(940, 780);
        Size = new Size(1060, 860);
        ShowIcon = false;
        BackColor = AppTheme.WindowBackground;
        AccessibleName = $"Edit visual profile {profile.Name}";
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
            Padding = new Padding(24, 18, 24, 18),
            RowCount = 5,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 174));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));
        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreateOutputLimitsCard(), 0, 1);
        root.Controls.Add(CreatePreviewCard(), 0, 2);
        root.Controls.Add(CreateAdjustmentsCard(), 0, 3);
        root.Controls.Add(CreateActionBar(), 0, 4);
        return root;
    }

    private Control CreateHeader()
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
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 62));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 38));
        layout.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(18f, FontStyle.Bold),
            Text = _workingProfile.Name,
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.2f),
            Text = "Editing visual profile · changes apply to every application assigned to this profile.",
            TextAlign = ContentAlignment.TopLeft,
        }, 0, 1);
        return layout;
    }

    private Control CreateOutputLimitsCard()
    {
        var controls = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(10, 4, 10, 10),
            RowCount = 1,
        };
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 28));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 44));
        controls.Controls.Add(CreateSliderPanel(
            "Output black",
            PercentageRange(
                VisualProfileLimits.MinimumOutputBlack,
                VisualProfileLimits.MaximumOutputBlack,
                "minimum output level"),
            _outputBlackSlider), 0, 0);
        controls.Controls.Add(CreateSliderPanel(
            "Output white",
            PercentageRange(
                VisualProfileLimits.MinimumOutputWhite,
                VisualProfileLimits.MaximumOutputWhite,
                "maximum output level"),
            _outputWhiteSlider), 1, 0);
        controls.Controls.Add(CreateConversionSamplePanel(), 2, 0);

        return CreateSectionCard(
            "OUTPUT LIMITS AND CONVERSION SAMPLE",
            controls,
            new Padding(0, 0, 0, 14));
    }

    private Control CreateConversionSamplePanel()
    {
        var panel = new TableLayoutPanel
        {
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            Padding = new Padding(12, 8, 12, 10),
            RowCount = 2,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            Text = "Black-on-white conversion",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);
        panel.Controls.Add(_outputPreview, 0, 1);
        return panel;
    }

    private Control CreatePreviewCard()
    {
        var host = new Panel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Padding = new Padding(16, 8, 16, 14),
        };
        host.Controls.Add(_preview);
        return CreateSectionCard(
            "LIVE PROFILE PREVIEW",
            host,
            new Padding(0, 0, 0, 14));
    }

    private Control CreateAdjustmentsCard()
    {
        var grid = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(10, 4, 10, 10),
            RowCount = 2,
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.Controls.Add(CreateSliderPanel(
            "Brightness",
            PercentageRange(
                VisualProfileLimits.MinimumBrightness,
                VisualProfileLimits.MaximumBrightness,
                "moves the whole output range"),
            _brightnessSlider), 0, 0);
        grid.Controls.Add(CreateSliderPanel(
            "Contrast",
            PercentageRange(
                VisualProfileLimits.MinimumContrast,
                VisualProfileLimits.MaximumContrast,
                "expands or compresses differences"),
            _contrastSlider), 1, 0);
        grid.Controls.Add(CreateSliderPanel(
            "Saturation",
            PercentageRange(
                VisualProfileLimits.MinimumSaturation,
                VisualProfileLimits.MaximumSaturation,
                "grayscale to amplified color"),
            _saturationSlider), 0, 1);
        grid.Controls.Add(CreateSliderPanel(
            "Hue shift",
            Range(
                VisualProfileLimits.MinimumHueShift,
                VisualProfileLimits.MaximumHueShift,
                "°",
                "rotates the color spectrum"),
            _hueSlider), 1, 1);

        return CreateSectionCard(
            "COLOR ADJUSTMENTS",
            grid,
            Padding.Empty);
    }

    private static Control CreateSectionCard(
        string title,
        Control content,
        Padding margin)
    {
        var host = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        host.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        host.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        host.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        host.Controls.Add(new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            Padding = new Padding(16, 6, 0, 0),
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);
        host.Controls.Add(content, 0, 1);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = margin,
            Padding = new Padding(1),
        };
        card.Controls.Add(host);
        return card;
    }

    private static Control CreateSliderPanel(
        string title,
        string description,
        ModernProfileSlider slider)
    {
        var valueLabel = new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            ForeColor = AppTheme.AccentHover,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            Margin = Padding.Empty,
            TextAlign = ContentAlignment.MiddleRight,
        };

        void RefreshValueLabel()
        {
            valueLabel.Text = slider.FormattedValue;
        }

        slider.ValueChanged += (_, _) => RefreshValueLabel();
        RefreshValueLabel();

        var header = new TableLayoutPanel
        {
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 1,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);
        header.Controls.Add(valueLabel, 1, 0);

        var panel = new TableLayoutPanel
        {
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(6),
            Padding = new Padding(12, 8, 12, 8),
            RowCount = 3,
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(slider, 0, 1);
        panel.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.CreateUiFont(8.1f),
            Text = description,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 2);
        return panel;
    }

    private Control CreateActionBar()
    {
        var reset = CreateButton(
            "Reset soft profile",
            ModernButtonStyle.Secondary,
            160);
        reset.AccessibleDescription =
            "Restore the canonical Soft invert tuning values.";
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
            AccessibleName = text,
            Text = text,
            VisualStyle = style,
            MinimumSize = new Size(minimumWidth, 40),
            Margin = Padding.Empty,
        };
    }

    private static ModernProfileSlider CreatePercentageSlider(
        string accessibleName,
        float minimum,
        float maximum)
    {
        return new ModernProfileSlider
        {
            AccessibleName = accessibleName,
            DecimalPlaces = 2,
            Minimum = minimum * 100f,
            Maximum = maximum * 100f,
            SmallChange = 0.25f,
            Unit = "%",
        };
    }

    private void AttachChangeHandlers()
    {
        AttachPercentage(
            _outputBlackSlider,
            value => _workingProfile.OutputBlack = value);
        AttachPercentage(
            _outputWhiteSlider,
            value => _workingProfile.OutputWhite = value);
        AttachPercentage(
            _brightnessSlider,
            value => _workingProfile.Brightness = value);
        AttachPercentage(
            _contrastSlider,
            value => _workingProfile.Contrast = value);
        AttachPercentage(
            _saturationSlider,
            value => _workingProfile.Saturation = value);
        AttachSlider(
            _hueSlider,
            value => _workingProfile.HueShiftDegrees = value);
    }

    private void AttachPercentage(
        ModernProfileSlider slider,
        Action<float> setter)
    {
        AttachSlider(slider, value => setter(value / 100f));
    }

    private void AttachSlider(
        ModernProfileSlider slider,
        Action<float> setter)
    {
        slider.ValueChanged += (_, _) =>
        {
            if (_loadingValues)
            {
                return;
            }

            setter(slider.Value);
            InvalidatePreviews();
        };
    }

    private void LoadValues()
    {
        _loadingValues = true;
        try
        {
            _outputBlackSlider.Value = _workingProfile.OutputBlack * 100f;
            _outputWhiteSlider.Value = _workingProfile.OutputWhite * 100f;
            _brightnessSlider.Value = _workingProfile.Brightness * 100f;
            _contrastSlider.Value = _workingProfile.Contrast * 100f;
            _saturationSlider.Value = _workingProfile.Saturation * 100f;
            _hueSlider.Value = _workingProfile.HueShiftDegrees;
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
        InvalidatePreviews();
    }

    private void InvalidatePreviews()
    {
        _preview.Invalidate();
        _outputPreview.Invalidate();
    }

    private static string PercentageRange(
        float minimum,
        float maximum,
        string explanation)
    {
        return Range(minimum * 100f, maximum * 100f, "%", explanation);
    }

    private static string Range(
        float minimum,
        float maximum,
        string unit,
        string explanation)
    {
        return $"{minimum:0.##}–{maximum:0.##}{unit} · {explanation}";
    }
}

internal sealed class ModernProfileSlider : Control
{
    private float _minimum;
    private float _maximum = 100f;
    private float _value;
    private float _smallChange = 1f;
    private bool _hovered;

    public ModernProfileSlider()
    {
        AccessibleRole = AccessibleRole.Slider;
        BackColor = AppTheme.SurfaceRaised;
        Cursor = Cursors.Hand;
        MinimumSize = new Size(160, 34);
        TabStop = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable |
            ControlStyles.UserPaint,
            true);
    }

    public event EventHandler? ValueChanged;

    public float Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_maximum < _minimum)
            {
                _maximum = _minimum;
            }
            Value = _value;
            Invalidate();
        }
    }

    public float Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            Value = _value;
            Invalidate();
        }
    }

    public float SmallChange
    {
        get => _smallChange;
        set => _smallChange = value > 0f ? value : 1f;
    }

    public int DecimalPlaces { get; set; }

    public string Unit { get; set; } = string.Empty;

    public float Value
    {
        get => _value;
        set
        {
            var next = Math.Clamp(value, Minimum, Maximum);
            if (MathF.Abs(next - _value) < 0.0001f)
            {
                return;
            }

            _value = next;
            AccessibleDescription = $"Current value: {FormattedValue}";
            ValueChanged?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
    }

    public string FormattedValue =>
        _value.ToString(
            $"F{Math.Clamp(DecimalPlaces, 0, 4)}",
            CultureInfo.CurrentCulture) + Unit;

    protected override bool IsInputKey(Keys keyData)
    {
        return keyData is Keys.Left or Keys.Right or Keys.Up or Keys.Down or
            Keys.Home or Keys.End or Keys.PageUp or Keys.PageDown ||
            base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs eventArgs)
    {
        base.OnKeyDown(eventArgs);

        switch (eventArgs.KeyCode)
        {
            case Keys.Left:
            case Keys.Down:
                Value = Snap(Value - SmallChange);
                break;
            case Keys.Right:
            case Keys.Up:
                Value = Snap(Value + SmallChange);
                break;
            case Keys.PageDown:
                Value = Snap(Value - SmallChange * 10f);
                break;
            case Keys.PageUp:
                Value = Snap(Value + SmallChange * 10f);
                break;
            case Keys.Home:
                Value = Minimum;
                break;
            case Keys.End:
                Value = Maximum;
                break;
            default:
                return;
        }

        eventArgs.Handled = true;
        eventArgs.SuppressKeyPress = true;
    }

    protected override void OnMouseDown(MouseEventArgs eventArgs)
    {
        base.OnMouseDown(eventArgs);
        if (eventArgs.Button != MouseButtons.Left)
        {
            return;
        }

        Focus();
        Capture = true;
        SetValueFromPosition(eventArgs.X);
    }

    protected override void OnMouseMove(MouseEventArgs eventArgs)
    {
        base.OnMouseMove(eventArgs);
        if (Capture && (eventArgs.Button & MouseButtons.Left) != 0)
        {
            SetValueFromPosition(eventArgs.X);
        }
    }

    protected override void OnMouseUp(MouseEventArgs eventArgs)
    {
        base.OnMouseUp(eventArgs);
        if (eventArgs.Button == MouseButtons.Left)
        {
            Capture = false;
        }
    }

    protected override void OnMouseEnter(EventArgs eventArgs)
    {
        base.OnMouseEnter(eventArgs);
        _hovered = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs eventArgs)
    {
        base.OnMouseLeave(eventArgs);
        _hovered = false;
        Invalidate();
    }

    protected override void OnGotFocus(EventArgs eventArgs)
    {
        base.OnGotFocus(eventArgs);
        Invalidate();
    }

    protected override void OnLostFocus(EventArgs eventArgs)
    {
        base.OnLostFocus(eventArgs);
        Capture = false;
        Invalidate();
    }

    protected override void OnEnabledChanged(EventArgs eventArgs)
    {
        base.OnEnabledChanged(eventArgs);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Parent?.BackColor ?? BackColor);

        var track = new Rectangle(
            10,
            Math.Max(1, Height / 2 - 3),
            Math.Max(1, Width - 20),
            6);
        using (var path = DrawingHelpers.CreateRoundedRectangle(track, 3))
        using (var brush = new SolidBrush(
                   Enabled ? AppTheme.Border : AppTheme.Surface))
        {
            graphics.FillPath(brush, path);
        }

        var ratio = Maximum <= Minimum
            ? 0f
            : (Value - Minimum) / (Maximum - Minimum);
        var thumbX = track.Left +
            (int)MathF.Round(ratio * track.Width);
        var activeWidth = Math.Max(1, thumbX - track.Left);
        var active = new Rectangle(
            track.Left,
            track.Top,
            activeWidth,
            track.Height);
        using (var path = DrawingHelpers.CreateRoundedRectangle(active, 3))
        using (var brush = new SolidBrush(
                   Enabled ? AppTheme.Accent : AppTheme.TextMuted))
        {
            graphics.FillPath(brush, path);
        }

        const int thumbSize = 18;
        var thumb = new Rectangle(
            thumbX - thumbSize / 2,
            Height / 2 - thumbSize / 2,
            thumbSize,
            thumbSize);
        using var thumbBrush = new SolidBrush(
            Enabled
                ? _hovered || Focused
                    ? AppTheme.AccentHover
                    : AppTheme.TextPrimary
                : AppTheme.TextMuted);
        using var thumbBorder = new Pen(
            Enabled ? AppTheme.Accent : AppTheme.Border,
            2f);
        graphics.FillEllipse(thumbBrush, thumb);
        graphics.DrawEllipse(thumbBorder, thumb);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(
                graphics,
                Rectangle.Inflate(ClientRectangle, -2, -2),
                AppTheme.TextPrimary,
                Parent?.BackColor ?? BackColor);
        }
    }

    private void SetValueFromPosition(int x)
    {
        const int horizontalPadding = 10;
        var usableWidth = Math.Max(1, Width - horizontalPadding * 2);
        var ratio = Math.Clamp(
            (x - horizontalPadding) / (float)usableWidth,
            0f,
            1f);
        Value = Snap(Minimum + ratio * (Maximum - Minimum));
    }

    private float Snap(float value)
    {
        if (SmallChange <= 0f)
        {
            return value;
        }

        var steps = MathF.Round((value - Minimum) / SmallChange);
        return Math.Clamp(
            Minimum + steps * SmallChange,
            Minimum,
            Maximum);
    }
}

internal sealed class OutputLimitPreview : Control
{
    public OutputLimitPreview()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.SurfaceRaised;
        ForeColor = AppTheme.TextSecondary;
        Font = AppTheme.CreateUiFont(7.8f, FontStyle.Bold);
        MinimumSize = new Size(260, 90);
        AccessibleName = "Black and white conversion preview";
    }

    public VisualProfile? Profile { get; set; }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        eventArgs.Graphics.Clear(BackColor);

        if (Profile is null || Width < 180 || Height < 70)
        {
            return;
        }

        var transform = VisualTransformCatalog.Default.GetRequired(
            Profile.TransformId);
        var effect = transform.CreateColorEffect(Profile);
        var gap = 10;
        var top = 22;
        var sampleHeight = Math.Max(38, Height - top - 4);
        var sampleWidth = Math.Max(60, (Width - gap - 8) / 2);
        var sourceBounds = new Rectangle(4, top, sampleWidth, sampleHeight);
        var outputBounds = new Rectangle(
            sourceBounds.Right + gap,
            top,
            Math.Max(60, Width - sourceBounds.Right - gap - 4),
            sampleHeight);

        DrawCaption(eventArgs.Graphics, "SOURCE", sourceBounds.Left, sampleWidth);
        DrawCaption(eventArgs.Graphics, "OUTPUT", outputBounds.Left, outputBounds.Width);
        DrawSample(
            eventArgs.Graphics,
            sourceBounds,
            Color.White,
            Color.Black);
        DrawSample(
            eventArgs.Graphics,
            outputBounds,
            ColorEffectPreviewMath.Apply(Color.White, effect),
            ColorEffectPreviewMath.Apply(Color.Black, effect));
    }

    private void DrawCaption(
        Graphics graphics,
        string text,
        int left,
        int width)
    {
        TextRenderer.DrawText(
            graphics,
            text,
            Font,
            new Rectangle(left, 0, width, 20),
            ForeColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);
    }

    private static void DrawSample(
        Graphics graphics,
        Rectangle bounds,
        Color background,
        Color foreground)
    {
        using var path = DrawingHelpers.CreateRoundedRectangle(bounds, 7);
        using var backgroundBrush = new SolidBrush(background);
        using var borderPen = new Pen(AppTheme.Border);
        graphics.FillPath(backgroundBrush, path);
        graphics.DrawPath(borderPen, path);
        TextRenderer.DrawText(
            graphics,
            "SightAdapt Aa",
            AppTheme.CreateUiFont(10f, FontStyle.Bold),
            Rectangle.Inflate(bounds, -8, -4),
            foreground,
            TextFormatFlags.HorizontalCenter |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.EndEllipsis |
            TextFormatFlags.NoPadding);
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
        AccessibleName = "Live grayscale and color profile preview";
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

        var transform = VisualTransformCatalog.Default.GetRequired(
            Profile.TransformId);
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
                var ratio = stripWidth <= 1
                    ? 0f
                    : (float)x / (stripWidth - 1);
                var source = row < 2
                    ? ColorEffectPreviewMath.CreateGray(ratio)
                    : ColorEffectPreviewMath.CreateHue(ratio * 360f);
                var color = row is 1 or 3
                    ? ColorEffectPreviewMath.Apply(source, effect)
                    : source;
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
        eventArgs.Graphics.DrawRectangle(
            border,
            0,
            0,
            Width - 1,
            Height - 1);
    }
}

internal static class ColorEffectPreviewMath
{
    public static Color Apply(Color source, MagColorEffect effect)
    {
        var red = source.R / 255f;
        var green = source.G / 255f;
        var blue = source.B / 255f;
        return Color.FromArgb(
            ToByte(red * effect.M00 + green * effect.M10 + blue * effect.M20 + effect.M40),
            ToByte(red * effect.M01 + green * effect.M11 + blue * effect.M21 + effect.M41),
            ToByte(red * effect.M02 + green * effect.M12 + blue * effect.M22 + effect.M42));
    }

    public static Color CreateGray(float value)
    {
        var channel = ToByte(value);
        return Color.FromArgb(channel, channel, channel);
    }

    public static Color CreateHue(float degrees)
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
