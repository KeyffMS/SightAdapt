namespace SightAdapt.Demo;

internal sealed class VisualProfileNameDialog : Form
{
    private readonly TextBox _nameInput;

    private VisualProfileNameDialog(
        string title,
        string prompt,
        string initialName)
    {
        Text = $"{ProductInfo.DisplayName} · {title}";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(480, 230);
        Size = new Size(540, 250);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowIcon = false;
        BackColor = AppTheme.WindowBackground;
        AppTheme.ApplyTo(this);

        var promptLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.5f),
            Text = prompt,
            TextAlign = ContentAlignment.MiddleLeft,
        };

        _nameInput = new TextBox
        {
            AccessibleName = "Visual profile name",
            BackColor = AppTheme.SurfaceRaised,
            BorderStyle = BorderStyle.FixedSingle,
            Dock = DockStyle.Fill,
            Font = AppTheme.CreateUiFont(11f),
            ForeColor = AppTheme.TextPrimary,
            MaxLength = 80,
            Text = initialName,
        };

        var cancelButton = new ModernButton
        {
            DialogResult = DialogResult.Cancel,
            Text = "Cancel",
            VisualStyle = ModernButtonStyle.Ghost,
            MinimumSize = new Size(100, 40),
            Margin = new Padding(0, 0, 8, 0),
        };
        CancelButton = cancelButton;

        var confirmButton = new ModernButton
        {
            DialogResult = DialogResult.OK,
            Text = "Confirm",
            VisualStyle = ModernButtonStyle.Primary,
            MinimumSize = new Size(110, 40),
            Margin = Padding.Empty,
        };
        AcceptButton = confirmButton;

        var buttons = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = AppTheme.WindowBackground,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(confirmButton);

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            RowCount = 3,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(promptLabel, 0, 0);
        layout.Controls.Add(_nameInput, 0, 1);
        layout.Controls.Add(buttons, 0, 2);
        buttons.Anchor = AnchorStyles.Right | AnchorStyles.Bottom;

        Controls.Add(layout);
        Shown += (_, _) =>
        {
            _nameInput.SelectAll();
            _nameInput.Focus();
        };
    }

    public static bool TryGetName(
        IWin32Window owner,
        string title,
        string prompt,
        string initialName,
        out string name)
    {
        ArgumentNullException.ThrowIfNull(owner);

        using var dialog = new VisualProfileNameDialog(
            title,
            prompt,
            initialName);

        if (dialog.ShowDialog(owner) != DialogResult.OK)
        {
            name = string.Empty;
            return false;
        }

        name = dialog._nameInput.Text;
        return true;
    }
}
