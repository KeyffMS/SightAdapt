
namespace SightAdapt;

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
        ClientSize = new Size(720, 470);
        MinimumSize = new Size(720, 470);
        BackColor = AppTheme.WindowBackground;
        AccessibleName = $"About {ProductInfo.ProductName}";
        AccessibleDescription =
            "Product name, milestone, version, author, license, and repository information.";
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
            Margin = Padding.Empty,
            Size = new Size(144, 144),
            SizeMode = PictureBoxSizeMode.CenterImage,
            TabStop = false,
        };

        var identity = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 7,
        };
        identity.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        identity.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        identity.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        identity.Controls.Add(CreateLabel(
            ProductInfo.ProductName,
            23f,
            FontStyle.Bold,
            AppTheme.TextPrimary), 0, 0);
        identity.Controls.Add(CreateLabel(
            ProductInfo.MilestoneLabel,
            11.5f,
            FontStyle.Bold,
            AppTheme.AccentHover), 0, 1);
        identity.Controls.Add(CreateWrappingLabel(
            ProductInfo.Tagline,
            9.6f,
            FontStyle.Regular,
            AppTheme.TextSecondary), 0, 2);
        identity.Controls.Add(CreateWrappingLabel(
            $"Version: {ProductInfo.VersionLabel}",
            9.2f,
            FontStyle.Regular,
            AppTheme.TextSecondary), 0, 3);
        identity.Controls.Add(CreateLabel(
            $"Author: {ProductInfo.Author}",
            9.2f,
            FontStyle.Regular,
            AppTheme.TextSecondary), 0, 4);
        identity.Controls.Add(CreateRepositoryLink(), 0, 5);
        identity.Controls.Add(CreateLabel(
            ProductInfo.License,
            9.2f,
            FontStyle.Bold,
            AppTheme.TextPrimary), 0, 6);

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(26),
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 178));
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

    private LinkLabel CreateRepositoryLink()
    {
        var link = new LinkLabel
        {
            AccessibleName = "Open SightAdapt repository on GitHub",
            ActiveLinkColor = AppTheme.TextPrimary,
            AutoEllipsis = false,
            AutoSize = true,
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Font = AppTheme.CreateUiFont(9.2f, FontStyle.Bold),
            LinkBehavior = LinkBehavior.HoverUnderline,
            LinkColor = AppTheme.AccentHover,
            TabStop = true,
            Text = ProductInfo.RepositoryDisplay,
            TextAlign = ContentAlignment.MiddleLeft,
            VisitedLinkColor = AppTheme.AccentHover,
        };
        link.LinkClicked += (_, _) => ShellLauncher.TryOpenUrl(this, ProductInfo.RepositoryUrl);
        return link;
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
            AutoEllipsis = false,
            Dock = DockStyle.Fill,
            ForeColor = color,
            Font = AppTheme.CreateUiFont(size, style),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }

    private static Label CreateWrappingLabel(
        string text,
        float size,
        FontStyle style,
        Color color)
    {
        return new Label
        {
            AutoEllipsis = false,
            AutoSize = false,
            Dock = DockStyle.Fill,
            ForeColor = color,
            Font = AppTheme.CreateUiFont(size, style),
            Text = text,
            TextAlign = ContentAlignment.MiddleLeft,
        };
    }


}
