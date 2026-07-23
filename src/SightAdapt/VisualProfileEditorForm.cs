namespace SightAdapt;

internal sealed class VisualProfileEditorForm : Form
{
    private readonly VisualProfile _workingProfile;
    private readonly ColorProfilePreview _preview;
    private readonly OutputLimitPreview _outputPreview;
    private readonly IReadOnlyDictionary<string, VisualAdjustmentBinding>
        _adjustments;
    private readonly ModernProfileSlider _outputBlackSlider;
    private readonly ModernProfileSlider _outputWhiteSlider;
    private readonly ModernProfileSlider _brightnessSlider;
    private readonly ModernProfileSlider _contrastSlider;
    private readonly ModernProfileSlider _saturationSlider;
    private readonly ModernProfileSlider _hueSlider;
    private bool _loadingValues;

    internal VisualProfileEditorForm(VisualProfile profile)
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
            TransformCatalog = VisualTransformCatalog.Default,
        };
        _outputPreview = new OutputLimitPreview
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Profile = _workingProfile,
            TransformCatalog = VisualTransformCatalog.Default,
        };
        _adjustments = VisualAdjustmentDefinitions.All
            .Select(definition => new VisualAdjustmentBinding(
                definition,
                definition.CreateSlider()))
            .ToDictionary(
                binding => binding.Definition.Id,
                StringComparer.Ordinal);
        _outputBlackSlider = SliderFor(
            VisualAdjustmentDefinitions.OutputBlack);
        _outputWhiteSlider = SliderFor(
            VisualAdjustmentDefinitions.OutputWhite);
        _brightnessSlider = SliderFor(
            VisualAdjustmentDefinitions.Brightness);
        _contrastSlider = SliderFor(
            VisualAdjustmentDefinitions.Contrast);
        _saturationSlider = SliderFor(
            VisualAdjustmentDefinitions.Saturation);
        _hueSlider = SliderFor(
            VisualAdjustmentDefinitions.HueShift);

        Text = $"{ProductInfo.DisplayName} · Edit {profile.Name}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(980, 820);
        Size = new Size(1120, 920);
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 210));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
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
            AutoEllipsis = false,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(18f, FontStyle.Bold),
            Text = _workingProfile.Name,
            TextAlign = ContentAlignment.BottomLeft,
        }, 0, 0);
        layout.Controls.Add(new Label
        {
            AutoEllipsis = false,
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
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
        controls.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        controls.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.OutputBlack), 0, 0);
        controls.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.OutputWhite), 1, 0);
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
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.Brightness), 0, 0);
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.Contrast), 1, 0);
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.Saturation), 0, 1);
        grid.Controls.Add(CreateAdjustmentPanel(
            VisualAdjustmentDefinitions.HueShift), 1, 1);

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
            AutoEllipsis = false,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            Text = title,
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 0);
        header.Controls.Add(slider.ValueEditor, 1, 0);

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
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        panel.Controls.Add(header, 0, 0);
        panel.Controls.Add(slider, 0, 1);
        panel.Controls.Add(new Label
        {
            AutoEllipsis = false,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.CreateUiFont(8.3f),
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

    private ModernProfileSlider SliderFor(
        VisualAdjustmentDefinition definition)
    {
        return _adjustments[definition.Id].Slider;
    }

    private Control CreateAdjustmentPanel(
        VisualAdjustmentDefinition definition)
    {
        return CreateSliderPanel(
            definition.Title,
            definition.RangeDescription,
            SliderFor(definition));
    }

    private void AttachChangeHandlers()
    {
        foreach (var binding in _adjustments.Values)
        {
            binding.Slider.ValueChanged += (_, _) =>
            {
                if (_loadingValues)
                {
                    return;
                }

                binding.Definition.WriteEditorValue(
                    _workingProfile,
                    binding.Slider.Value);
                InvalidatePreviews();
            };
        }
    }

    private void LoadValues()
    {
        _loadingValues = true;
        try
        {
            foreach (var binding in _adjustments.Values)
            {
                binding.Slider.Value =
                    binding.Definition.ReadEditorValue(
                        _workingProfile);
            }
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


}
