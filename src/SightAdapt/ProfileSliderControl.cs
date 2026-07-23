using System.Drawing.Drawing2D;
using System.Globalization;

namespace SightAdapt;

internal sealed class ModernProfileSlider : UserControl
{
    private const float NeutralMagnetRatio = 0.03f;

    private readonly ProfileSliderTrack _track;
    private readonly TextBox _valueInput;
    private readonly Label _unitLabel;
    private readonly TableLayoutPanel _valueEditor;
    private float _minimum;
    private float _maximum = 100f;
    private float _value;
    private float _smallChange = 1f;
    private float? _neutralValue;
    private bool _synchronizingInput;

    public ModernProfileSlider()
    {
        AccessibleRole = AccessibleRole.Slider;
        BackColor = AppTheme.SurfaceRaised;
        Dock = DockStyle.Fill;
        Margin = Padding.Empty;
        MinimumSize = new Size(220, 38);
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

    public float? NeutralValue
    {
        get => _neutralValue;
        set
        {
            _neutralValue = value is not null &&
                value > Minimum &&
                value < Maximum
                    ? value
                    : null;
            _track.Invalidate();
        }
    }

    public string FormattedValue =>
        _value.ToString(
            $"F{Math.Clamp(DecimalPlaces, 0, 4)}",
            CultureInfo.CurrentCulture) + Unit;

    internal bool HasNeutralPoint =>
        NeutralValue is { } neutral &&
        neutral > Minimum &&
        neutral < Maximum;

    internal float NormalizedValue => ValueToRatio(Value);

    internal float NeutralRatio =>
        HasNeutralPoint && NeutralValue is { } neutral
            ? ValueToRatio(neutral)
            : 0f;

    internal void SetValueFromRatio(float ratio)
    {
        var boundedRatio = Math.Clamp(ratio, 0f, 1f);
        if (HasNeutralPoint &&
            MathF.Abs(boundedRatio - NeutralRatio) <= NeutralMagnetRatio &&
            NeutralValue is { } neutral)
        {
            Value = neutral;
            return;
        }

        Value = SnapToStep(RatioToValue(boundedRatio));
    }

    internal void Nudge(float direction)
    {
        Value = SnapToStep(Value + SmallChange * direction);
    }

    internal void Page(float direction)
    {
        Value = SnapToStep(Value + SmallChange * 10f * direction);
    }

    internal void SetBoundary(bool maximum)
    {
        Value = maximum ? Maximum : Minimum;
    }

    internal float ValueToRatio(float value)
    {
        var bounded = Math.Clamp(value, Minimum, Maximum);
        if (!HasNeutralPoint || NeutralValue is not { } neutral)
        {
            return Maximum <= Minimum
                ? 0f
                : (bounded - Minimum) / (Maximum - Minimum);
        }

        if (bounded <= neutral)
        {
            var lowerRange = neutral - Minimum;
            return lowerRange <= 0f
                ? 0.5f
                : 0.5f * (bounded - Minimum) / lowerRange;
        }

        var upperRange = Maximum - neutral;
        return upperRange <= 0f
            ? 0.5f
            : 0.5f + 0.5f * (bounded - neutral) / upperRange;
    }

    internal float RatioToValue(float ratio)
    {
        var bounded = Math.Clamp(ratio, 0f, 1f);
        if (!HasNeutralPoint || NeutralValue is not { } neutral)
        {
            return Minimum + bounded * (Maximum - Minimum);
        }

        return bounded <= 0.5f
            ? Minimum + bounded * 2f * (neutral - Minimum)
            : neutral + (bounded - 0.5f) * 2f * (Maximum - neutral);
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

    private float SnapToStep(float value)
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
            Value = SnapToStep(value);
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
        MinimumSize = new Size(180, 36);
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

        var track = new Rectangle(
            5,
            Height / 2 - 4,
            Math.Max(1, Width - 10),
            8);
        using var trackPath = DrawingHelpers.CreateRoundedRectangle(track, 4);
        using var baseBrush = new SolidBrush(AppTheme.SurfaceHover);
        using var basePen = new Pen(AppTheme.Border);
        graphics.FillPath(baseBrush, trackPath);
        graphics.DrawPath(basePen, trackPath);

        var valueX = RatioToX(track, _owner.NormalizedValue);
        var fillStartX = track.Left;
        if (_owner.HasNeutralPoint)
        {
            fillStartX = RatioToX(track, _owner.NeutralRatio);
            DrawNeutralMarker(graphics, track, fillStartX);
        }

        var fillLeft = Math.Min(fillStartX, valueX);
        var fillWidth = Math.Abs(valueX - fillStartX);
        if (fillWidth > 0)
        {
            var fill = new Rectangle(fillLeft, track.Top, fillWidth, track.Height);
            using var fillPath = DrawingHelpers.CreateRoundedRectangle(fill, 4);
            using var fillBrush = new SolidBrush(AppTheme.Accent);
            graphics.FillPath(fillBrush, fillPath);
        }

        const int thumbSize = 18;
        var thumb = new Rectangle(
            valueX - thumbSize / 2,
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

    private static int RatioToX(Rectangle track, float ratio)
    {
        return track.Left +
            (int)MathF.Round(track.Width * Math.Clamp(ratio, 0f, 1f));
    }

    private static void DrawNeutralMarker(
        Graphics graphics,
        Rectangle track,
        int centerX)
    {
        const int markerSize = 8;
        var marker = new Rectangle(
            centerX - markerSize / 2,
            track.Top + track.Height / 2 - markerSize / 2,
            markerSize,
            markerSize);
        using var markerBrush = new SolidBrush(AppTheme.TextSecondary);
        using var markerPen = new Pen(AppTheme.Surface, 1.5f);
        graphics.FillEllipse(markerBrush, marker);
        graphics.DrawEllipse(markerPen, marker);
    }

    private void SetValueFromPosition(int x)
    {
        const int horizontalInset = 5;
        var width = Math.Max(1, Width - horizontalInset * 2);
        _owner.SetValueFromRatio(
            (float)(x - horizontalInset) / width);
    }
}
