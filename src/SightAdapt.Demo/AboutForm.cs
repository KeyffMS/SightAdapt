namespace SightAdapt.Demo;

internal sealed class AboutForm : Form
{
    private readonly Icon _windowIcon;
    private readonly Bitmap _logo;

    public AboutForm(Icon sourceIcon)
    {
        ArgumentNullException.ThrowIfNull(sourceIcon);

        _windowIcon = (Icon)sourceIcon.Clone();
        _logo = _windowIcon.ToBitmap();

        Text = $"About {ProductInfo.ProductName}";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ShowIcon = true;
        Icon = _windowIcon;
        ClientSize = new Size(620, 390);
        MinimumSize = new Size(620, 390);
        BackColor = AppTheme.WindowBackground;
        AccessibleName = $"About {ProductInfo.ProductName}";
        AccessibleDescription =
            "Product name, current milestone, version, author, and license information.";
        AppTheme.ApplyTo(this);

        Controls.Add(CreateRootLayout());
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _logo.Dispose();
            _windowIcon.Dispose();
        }

        base.Dispose(disposing);
    }

    private Control CreateRootLayout()
    {
        var root = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 24, 26, 20),
            RowCount = 3,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.Controls.Add(CreateIdentityCard(), 0, 0);
        root.Controls.Add(new Panel
        {
            BackColor = AppTheme.Border,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 14, 0, 0),
        }, 0, 1);
        root.Controls.Add(CreateActionBar(), 0, 2);
        return root;
    }

    private Control CreateIdentityCard()
    {
        var logo = new PictureBox
        {
            AccessibleName = "SightAdapt logo",
            Anchor = AnchorStyles.None,
            BackColor = AppTheme.SurfaceRaised,
            Image = _logo,
            Margin = new Padding(0),
            Size = new Size(132, 132),
            SizeMode = PictureBoxSizeMode.CenterImage,
            TabStop = false,
        };

        var identity = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 6,
        };
        identity.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 52));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        identity.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        identity.Controls.Add(CreateLabel(
            ProductInfo.ProductName,
            22f,
            FontStyle.Bold,
            AppTheme.TextPrimary), 0, 0);
        identity.Controls.Add(CreateLabel(
            ProductInfo.MilestoneLabel,
            11f,
            FontStyle.Bold,
            AppTheme.AccentHover), 0, 1);
        identity.Controls.Add(CreateLabel(
            ProductInfo.Tagline,
            9.5f,
            FontStyle.Regular,
            AppTheme.TextSecondary), 0, 2);
        identity.Controls.Add(CreateLabel(
            $"Version: {ProductInfo.VersionLabel}",
            9f,
            FontStyle.Regular,
            AppTheme.TextSecondary), 0, 3);
        identity.Controls.Add(CreateLabel(
            $"Author: {ProductInfo.Author}",
            9f,
            FontStyle.Regular,
            AppTheme.TextSecondary), 0, 4);
        identity.Controls.Add(CreateLabel(
            ProductInfo.License,
            9f,
            FontStyle.Bold,
            AppTheme.TextPrimary), 0, 5);

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(24),
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 164));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.Controls.Add(logo, 0, 0);
        layout.Controls.Add(identity, 1, 0);

        var card = new RoundedPanel
        {
            AccessibleName = "SightAdapt product information",
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(1),
        };
        card.Controls.Add(layout);
        return card;
    }

    private Control CreateActionBar()
    {
        var close = new ModernButton
        {
            AccessibleName = "Close About window",
            DialogResult = DialogResult.OK,
            MinimumSize = new Size(112, 40),
            Text = "Close",
            VisualStyle = ModernButtonStyle.Primary,
        };
        close.Click += (_, _) => Close();
        AcceptButton = close;
        CancelButton = close;

        var layout = new FlowLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Margin = Padding.Empty,
            Padding = new Padding(0, 14, 0, 0),
            WrapContents = false,
        };
        layout.Controls.Add(close);
        return layout;
    }

    private static Label CreateLabel(
        string text,
        float size,
        FontStyle style,
        Color color)
    {
        return new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = color,
            Font = AppTheme.CreateUiFont(size, style),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }
}
