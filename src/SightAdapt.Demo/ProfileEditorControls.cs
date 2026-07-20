using System.Drawing.Drawing2D;
using System.Globalization;

namespace SightAdapt.Demo;

internal sealed class ModernProfileSlider : UserControl
{
    private readonly ProfileSliderTrack _track;
    private readonly TextBox _valueInput;
    private readonly Label _unitLabel;
    private readonly TableLayoutPanel _valueEditor;
    private float _minimum;
    private float _maximum = 100f;
    private float _value;
    private float _smallChange = 1f;
    private bool _synchronizingInput;

    public ModernProfileSlider()
    {
        AccessibleRole = AccessibleRole.Slider;
        BackColor = AppTheme.SurfaceRaised;
        MinimumSize = new Size(160, 36);
        TabStop = false;

        _track = new ProfileSliderTrack(this)
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        Controls.Add(_track);

        _valueInput = new TextBox
        {
            AccessibleName = "Numeric slider value",
            Anchor = AnchorStyles.Right,
            BackColor = AppTheme.Surface,
            BorderStyle = BorderStyle.FixedSingle,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            ForeColor = AppTheme.TextPrimary,
            Margin = Padding.Empty,
            Size = new Size(78, 28),
            TextAlign = HorizontalAlignment.Right,
        };
        _unitLabel = new Label
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            BackColor = AppTheme.SurfaceRaised,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            Margin = new Padding(7, 0, 0, 0),
            TextAlign = ContentAlignment.MiddleLeft,
        };
        _valueEditor = new TableLayoutPanel
        {
            AccessibleName = "Slider numeric value editor",
            AutoSize = true,
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 2,
            Margin = Padding.Empty,
            RowCount = 1,
        };
        _valueEditor.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
        _valueEditor.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _valueEditor.Controls.Add(_valueInput, 0, 0);
        _valueEditor.Controls.Add(_unitLabel, 1, 0);

        _valueInput.Enter += (_, _) => _valueInput.SelectAll();
        _valueInput.Validating += (_, _) => CommitInput();
        _valueInput.KeyDown += ValueInputKeyDown;

        SynchronizeInput();
    }

    public event EventHandler? ValueChanged;

    internal Control ValueEditor => _valueEditor;

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

            SetValue(_value, raiseEvent: false);
        }
    }

    public float Maximum
    {
        get => _maximum;
        set
        {
            _maximum = Math.Max(value, _minimum);
            SetValue(_value, raiseEvent: false);
        }
    }

    public float SmallChange
    {
        get => _smallChange;
        set => _smallChange = value > 0f ? value : 1f;
    }

    public int DecimalPlaces { get; set; }

    public string Unit
    {
        get => _unitLabel.Text;
        set => _unitLabel.Text = value ?? string.Empty;
    }

    public float Value
    {
        get => _value;
        set => SetValue(value, raiseEvent: true);
    }

    public string FormattedValue =>
        _value.ToString(
            $"F{Math.Clamp(DecimalPlaces, 0, 4)}",
            CultureInfo.CurrentCulture) + Unit;

    internal float NormalizedValue =>
        Maximum <= Minimum
            ? 0f
            : Math.Clamp((Value - Minimum) / (Maximum - Minimum), 0f, 1f);

    internal void SetValueFromRatio(float ratio)
    {
        var raw = Minimum + Math.Clamp(ratio, 0f, 1f) * (Maximum - Minimum);
        Value = Snap(raw);
    }

    internal void Nudge(float direction)
    {
        Value = Snap(Value + SmallChange * direction);
    }

    internal void Page(float direction)
    {
        Value = Snap(Value + SmallChange * 10f * direction);
    }

    internal void SetBoundary(bool maximum)
    {
        Value = maximum ? Maximum : Minimum;
    }

    protected override void OnHandleCreated(EventArgs eventArgs)
    {
        base.OnHandleCreated(eventArgs);
        _track.AccessibleName = AccessibleName;
        _valueInput.AccessibleName = string.IsNullOrWhiteSpace(AccessibleName)
            ? "Numeric slider value"
            : $"{AccessibleName} numeric value";
        _valueEditor.AccessibleName = _valueInput.AccessibleName;
    }

    private void SetValue(float value, bool raiseEvent)
    {
        var next = Math.Clamp(value, Minimum, Maximum);
        if (MathF.Abs(next - _value) < 0.0001f)
        {
            SynchronizeInput();
            _track.Invalidate();
            return;
        }

        _value = next;
        AccessibleDescription = $"Current value: {FormattedValue}";
        SynchronizeInput();
        _track.Invalidate();

        if (raiseEvent)
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private float Snap(float value)
    {
        if (SmallChange <= 0f)
        {
            return Math.Clamp(value, Minimum, Maximum);
        }

        var steps = MathF.Round((value - Minimum) / SmallChange);
        return Math.Clamp(Minimum + steps * SmallChange, Minimum, Maximum);
    }

    private void SynchronizeInput()
    {
        if (_synchronizingInput)
        {
            return;
        }

        _synchronizingInput = true;
        try
        {
            _valueInput.Text = _value.ToString(
                $"F{Math.Clamp(DecimalPlaces, 0, 4)}",
                CultureInfo.CurrentCulture);
        }
        finally
        {
            _synchronizingInput = false;
        }
    }

    private void CommitInput()
    {
        if (_synchronizingInput)
        {
            return;
        }

        var text = NormalizeDecimalSeparator(_valueInput.Text.Trim());
        if (float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.CurrentCulture,
                out var value))
        {
            Value = Snap(value);
        }
        else
        {
            SynchronizeInput();
        }
    }

    private void ValueInputKeyDown(object? sender, KeyEventArgs eventArgs)
    {
        if (eventArgs.KeyCode == Keys.Enter)
        {
            CommitInput();
            _track.Focus();
            eventArgs.Handled = true;
            eventArgs.SuppressKeyPress = true;
        }
        else if (eventArgs.KeyCode == Keys.Escape)
        {
            SynchronizeInput();
            _track.Focus();
            eventArgs.Handled = true;
            eventArgs.SuppressKeyPress = true;
        }
    }

    private static string NormalizeDecimalSeparator(string text)
    {
        var separator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        return separator == ","
            ? text.Replace('.', ',')
            : text.Replace(',', '.');
    }
}

internal sealed class ProfileSliderTrack : Control
{
    private readonly ModernProfileSlider _owner;
    private bool _hovered;

    public ProfileSliderTrack(ModernProfileSlider owner)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        AccessibleRole = AccessibleRole.Slider;
        BackColor = AppTheme.SurfaceRaised;
        Cursor = Cursors.Hand;
        MinimumSize = new Size(150, 34);
        TabStop = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable |
            ControlStyles.UserPaint,
            true);
    }

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
                _owner.Nudge(-1f);
                break;
            case Keys.Right:
            case Keys.Up:
                _owner.Nudge(1f);
                break;
            case Keys.PageDown:
                _owner.Page(-1f);
                break;
            case Keys.PageUp:
                _owner.Page(1f);
                break;
            case Keys.Home:
                _owner.SetBoundary(maximum: false);
                break;
            case Keys.End:
                _owner.SetBoundary(maximum: true);
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
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        var track = new Rectangle(10, Height / 2 - 3, Math.Max(1, Width - 20), 6);
        using var trackPath = DrawingHelpers.CreateRoundedRectangle(track, 3);
        using var baseBrush = new SolidBrush(AppTheme.Border);
        graphics.FillPath(baseBrush, trackPath);

        var fillWidth = (int)MathF.Round(track.Width * _owner.NormalizedValue);
        if (fillWidth > 0)
        {
            var fill = new Rectangle(track.Left, track.Top, fillWidth, track.Height);
            using var fillPath = DrawingHelpers.CreateRoundedRectangle(fill, 3);
            using var fillBrush = new SolidBrush(AppTheme.Accent);
            graphics.FillPath(fillBrush, fillPath);
        }

        const int thumbSize = 18;
        var centerX = Math.Clamp(track.Left + fillWidth, track.Left, track.Right);
        var thumb = new Rectangle(
            centerX - thumbSize / 2,
            Height / 2 - thumbSize / 2,
            thumbSize,
            thumbSize);
        using var thumbBrush = new SolidBrush(
            _hovered || Focused ? AppTheme.TextPrimary : AppTheme.AccentHover);
        using var thumbBorder = new Pen(AppTheme.Accent, 2f);
        graphics.FillEllipse(thumbBrush, thumb);
        graphics.DrawEllipse(thumbBorder, thumb);

        if (Focused && ShowFocusCues)
        {
            ControlPaint.DrawFocusRectangle(
                graphics,
                Rectangle.Inflate(ClientRectangle, -2, -2),
                AppTheme.TextPrimary,
                BackColor);
        }
    }

    private void SetValueFromPosition(int x)
    {
        var width = Math.Max(1, Width - 20);
        _owner.SetValueFromRatio((float)(x - 10) / width);
    }
}

internal sealed class OutputLimitPreview : Control
{
    public OutputLimitPreview()
    {
        DoubleBuffered = true;
        BackColor = AppTheme.SurfaceRaised;
        MinimumSize = new Size(220, 84);
    }

    public VisualProfile? Profile { get; set; }

    public VisualTransformCatalog TransformCatalog { get; set; } =
        VisualTransformCatalog.Default;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        var graphics = eventArgs.Graphics;
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(BackColor);

        if (Profile is null || Width < 160 || Height < 60)
        {
            return;
        }

        var effect = TransformCatalog
            .GetRequired(Profile.TransformId)
            .CreateColorEffect(Profile);
        var sourceForeground = Color.Black;
        var sourceBackground = Color.White;
        var outputForeground = ColorEffectPreviewMath.Apply(sourceForeground, effect);
        var outputBackground = ColorEffectPreviewMath.Apply(sourceBackground, effect);

        const int gap = 10;
        var sampleWidth = Math.Max(60, (Width - gap * 3) / 2);
        var sampleHeight = Height - 14;
        DrawSample(
            graphics,
            new Rectangle(gap, 7, sampleWidth, sampleHeight),
            "SOURCE",
            sourceForeground,
            sourceBackground);
        DrawSample(
            graphics,
            new Rectangle(gap * 2 + sampleWidth, 7, sampleWidth, sampleHeight),
            "OUTPUT",
            outputForeground,
            outputBackground);
    }

    private static void DrawSample(
        Graphics graphics,
        Rectangle bounds,
        string caption,
        Color foreground,
        Color background)
    {
        using var backgroundBrush = new SolidBrush(background);
        using var borderPen = new Pen(AppTheme.Border);
        graphics.FillRectangle(backgroundBrush, bounds);
        graphics.DrawRectangle(borderPen, bounds);

        var captionColor = ColorEffectPreviewMath.ContrastText(background);
        TextRenderer.DrawText(
            graphics,
            caption,
            AppTheme.CreateUiFont(7.5f, FontStyle.Bold),
            new Rectangle(bounds.Left + 8, bounds.Top + 5, bounds.Width - 16, 18),
            captionColor,
            TextFormatFlags.Left |
            TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding);
        TextRenderer.DrawText(
            graphics,
            "SightAdapt",
            AppTheme.CreateUiFont(13f, FontStyle.Bold),
            new Rectangle(bounds.Left + 8, bounds.Top + 24, bounds.Width - 16, bounds.Height - 30),
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
    }

    public VisualProfile? Profile { get; set; }

    public VisualTransformCatalog TransformCatalog { get; set; } =
        VisualTransformCatalog.Default;

    protected override void OnPaint(PaintEventArgs eventArgs)
    {
        base.OnPaint(eventArgs);
        eventArgs.Graphics.SmoothingMode = SmoothingMode.None;
        eventArgs.Graphics.Clear(BackColor);

        if (Profile is null || Width < 120 || Height < 80)
        {
            return;
        }

        var effect = TransformCatalog
            .GetRequired(Profile.TransformId)
            .CreateColorEffect(Profile);
        const int labelWidth = 96;
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
        eventArgs.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
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

    public static Color ContrastText(Color background)
    {
        var luminance =
            0.2126f * background.R +
            0.7152f * background.G +
            0.0722f * background.B;
        return luminance > 145f ? Color.Black : Color.White;
    }

    private static byte ToByte(float value)
    {
        return (byte)Math.Clamp(
            (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f),
            0,
            255);
    }
}
