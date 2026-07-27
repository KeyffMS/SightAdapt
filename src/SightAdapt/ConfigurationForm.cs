
using System.Drawing;

namespace SightAdapt;

internal sealed class ConfigurationForm : Form
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly ConfigurationUseCases _useCases;
    private readonly Func<ApplicationIdentity?> _getCurrentApplication;
    private readonly Action<IWin32Window, SettingsCoordinator>
        _showVisualProfileManager;
    private readonly ToggleSwitch _automaticModeSwitch;
    private readonly Label _automaticModeStateLabel;
    private readonly Label _applicationCountLabel;
    private readonly ApplicationAssignmentsGrid _assignmentsGrid;
    private readonly ModernButton _editVisualProfileButton;
    private bool _refreshing;
    private bool _committingGridValue;

    public ConfigurationForm(
        SettingsCoordinator settingsCoordinator,
        Func<ApplicationIdentity?> getCurrentApplication,
        Action<IWin32Window, SettingsCoordinator>?
            showVisualProfileManager = null)
    {
        _settingsCoordinator = settingsCoordinator ??
            throw new ArgumentNullException(nameof(settingsCoordinator));
        _useCases = new ConfigurationUseCases(_settingsCoordinator);
        _getCurrentApplication = getCurrentApplication ??
            throw new ArgumentNullException(nameof(getCurrentApplication));
        _showVisualProfileManager = showVisualProfileManager ??
            VisualProfileManagerForm.ShowManager;

        Text = ProductInfo.WindowTitle;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1180, 680);
        Size = new Size(1400, 780);
        ShowIcon = false;
        BackColor = AppTheme.WindowBackground;
        AppTheme.ApplyTo(this);

        _automaticModeSwitch = new ToggleSwitch
        {
            AccessibleName = "Enable automatic mode",
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 16, 0),
        };
        _automaticModeSwitch.CheckedChanged += AutomaticModeCheckedChanged;
        _automaticModeStateLabel = CreateAutomaticModeStateLabel();
        _applicationCountLabel = FormPresentation.CreateCountLabel();
        _editVisualProfileButton = FormPresentation.CreateActionButton(
            "Edit color profile",
            ModernButtonStyle.Secondary,
            EditSelectedVisualProfile,
            160);
        _editVisualProfileButton.Enabled = false;
        _assignmentsGrid = new ApplicationAssignmentsGrid();
        _assignmentsGrid.AssignmentChanged +=
            AssignmentChanged;
        _assignmentsGrid.SelectedApplicationChanged += (_, _) =>
        {
            var snapshot = _useCases.Snapshot;
            UpdateSelectedProfileActions(snapshot);
        };

        Controls.Add(CreateRootLayout());
        _useCases.Changed += SettingsChanged;
        FormClosed += (_, _) => _useCases.Changed -= SettingsChanged;
        RefreshProfiles();
    }

    internal int RefreshGeneration { get; private set; }

    public void RefreshProfiles()
    {
        if (IsDisposed)
        {
            return;
        }

        var settings = _useCases.Snapshot;
        _refreshing = true;
        try
        {
            _automaticModeSwitch.Checked = settings.AutomaticMode;
            UpdateAutomaticModeState(settings.AutomaticMode);
            _assignmentsGrid.Bind(
                ApplicationAssignmentRowMapper.MapAll(
                    settings.Assignments),
                settings.VisualProfiles);

            var count = settings.Assignments.Count;
            _applicationCountLabel.Text = count == 1
                ? "1 APPLICATION"
                : $"{count} APPLICATIONS";
            UpdateSelectedProfileActions(settings);
        }
        finally
        {
            _refreshing = false;
        }

        RefreshGeneration++;
    }

    private Control CreateRootLayout()
    {
        var content = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 16),
            RowCount = 5,
        };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        content.Controls.Add(CreateAutomaticModeCard(), 0, 0);
        content.Controls.Add(CreateProfilesCard(), 0, 1);
        content.Controls.Add(CreateActionBar(), 0, 2);
        content.Controls.Add(CreateProjectInfoCard(), 0, 3);
        content.Controls.Add(new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.CreateUiFont(8.5f),
            Text = $"Settings are stored locally: {_useCases.SettingsPath}",
            TextAlign = ContentAlignment.MiddleLeft,
        }, 0, 4);

        var root = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(content, 0, 1);
        return root;
    }

    private static Control CreateHeader()
    {
        var text = new TableLayoutPanel
        {
            BackColor = AppTheme.HeaderBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 18, 24, 14),
            RowCount = 2,
        };
        text.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        text.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        text.Controls.Add(FormPresentation.CreateHeaderLabel(
            "Application and color profiles",
            20f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.BottomLeft), 0, 0);
        text.Controls.Add(FormPresentation.CreateHeaderLabel(
            "Assign visual correction profiles to application windows and native popup menus.",
            9.5f,
            FontStyle.Regular,
            AppTheme.TextSecondary,
            ContentAlignment.TopLeft), 0, 1);

        var header = new Panel
        {
            BackColor = AppTheme.HeaderBackground,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        header.Controls.Add(text);
        header.Controls.Add(new Panel
        {
            BackColor = AppTheme.Accent,
            Dock = DockStyle.Left,
            Width = 5,
        });
        return header;
    }

    private Control CreateAutomaticModeCard()
    {
        var description = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        description.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        description.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        description.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        description.Controls.Add(FormPresentation.CreateHeaderLabel(
            "Automatic mode",
            10.5f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.BottomLeft), 0, 0);
        description.Controls.Add(FormPresentation.CreateHeaderLabel(
            "Apply each application's window and native-menu profiles whenever it becomes active.",
            9f,
            FontStyle.Regular,
            AppTheme.TextSecondary,
            ContentAlignment.TopLeft), 0, 1);

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 3,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(18, 12, 18, 12),
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(_automaticModeSwitch, 0, 0);
        layout.Controls.Add(description, 1, 0);
        layout.Controls.Add(_automaticModeStateLabel, 2, 0);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 14),
        };
        card.Controls.Add(layout);
        return card;
    }

    private Control CreateProfilesCard()
    {
        var header = new TableLayoutPanel
        {
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 2,
            Dock = DockStyle.Top,
            Height = 52,
            Margin = Padding.Empty,
            RowCount = 1,
        };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(new Label
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(10.5f, FontStyle.Bold),
            Margin = new Padding(18, 0, 0, 0),
            Text = "Configured applications",
        }, 0, 0);
        header.Controls.Add(_applicationCountLabel, 1, 0);

        var host = new Panel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        host.Controls.Add(_assignmentsGrid);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(1),
        };
        card.Controls.Add(host);
        card.Controls.Add(header);
        return card;
    }

    private Control CreateActionBar()
    {
        var left = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            BackColor = AppTheme.WindowBackground,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        left.Controls.AddRange([
            FormPresentation.CreateActionButton("Add current app", ModernButtonStyle.Primary, AddCurrentApplication, 150),
            FormPresentation.CreateActionButton("Browse for .exe", ModernButtonStyle.Secondary, BrowseForApplication, 140),
            FormPresentation.CreateActionButton("Manage profiles", ModernButtonStyle.Secondary, ManageVisualProfiles, 145),
            _editVisualProfileButton,
            FormPresentation.CreateActionButton("Remove selected", ModernButtonStyle.Danger, RemoveSelectedProfile, 145),
        ]);

        var close = new ModernButton
        {
            DialogResult = DialogResult.Cancel,
            Text = "Close",
            VisualStyle = ModernButtonStyle.Ghost,
            MinimumSize = new Size(96, 40),
            Margin = Padding.Empty,
        };
        close.Click += (_, _) => Close();
        CancelButton = close;

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 8),
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        layout.Controls.Add(left, 0, 0);
        layout.Controls.Add(close, 1, 0);
        close.Anchor = AnchorStyles.Right;
        return layout;
    }

    private Control CreateProjectInfoCard()
    {
        var repository = new LinkLabel
        {
            ActiveLinkColor = AppTheme.AccentHover,
            AutoEllipsis = true,
            AutoSize = true,
            BackColor = AppTheme.Surface,
            Font = AppTheme.CreateUiFont(8.8f, FontStyle.Bold),
            LinkBehavior = LinkBehavior.HoverUnderline,
            LinkColor = AppTheme.AccentHover,
            Margin = Padding.Empty,
            Text = ProductInfo.RepositoryDisplay,
            VisitedLinkColor = AppTheme.Accent,
        };
        repository.LinkClicked += (_, _) => ShellLauncher.TryOpenUrl(this, ProductInfo.RepositoryUrl);

        var product = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        product.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        product.Controls.Add(FormPresentation.CreateHeaderLabel(
            ProductInfo.DisplayName,
            10.5f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.BottomLeft), 0, 0);
        product.Controls.Add(FormPresentation.CreateHeaderLabel(
            ProductInfo.Tagline,
            8.8f,
            FontStyle.Regular,
            AppTheme.TextSecondary,
            ContentAlignment.TopLeft), 0, 1);

        var metadata = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = AppTheme.Surface,
            FlowDirection = FlowDirection.TopDown,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        metadata.Controls.Add(CreateInfoLabel(ProductInfo.License, FontStyle.Bold));
        metadata.Controls.Add(CreateInfoLabel(ProductInfo.Author, FontStyle.Regular));
        metadata.Controls.Add(repository);

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(18, 12, 18, 12),
            RowCount = 1,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));
        layout.Controls.Add(product, 0, 0);
        layout.Controls.Add(metadata, 1, 0);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 10),
        };
        card.Controls.Add(layout);
        return card;
    }

    private static Label CreateAutomaticModeStateLabel()
    {
        return new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            Font = AppTheme.CreateUiFont(8.5f, FontStyle.Bold),
            Margin = new Padding(12, 0, 0, 0),
            Padding = new Padding(12, 6, 12, 6),
            TextAlign = ContentAlignment.MiddleCenter,
        };
    }

    private static Label CreateInfoLabel(string text, FontStyle style)
    {
        return new Label
        {
            AutoSize = true,
            BackColor = AppTheme.Surface,
            ForeColor = style == FontStyle.Bold ? AppTheme.TextPrimary : AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(8.8f, style),
            Margin = new Padding(0, 0, 0, 3),
            Text = text,
        };
    }

    private void AutomaticModeCheckedChanged(object? sender, EventArgs eventArgs)
    {
        if (_refreshing)
        {
            return;
        }

        var result = _useCases.SetAutomaticMode(
            _automaticModeSwitch.Checked);
        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            RefreshProfiles();
        }
    }

    private void UpdateAutomaticModeState(bool automaticMode)
    {
        _automaticModeStateLabel.Text = automaticMode
            ? "ACTIVE"
            : "PAUSED";
        _automaticModeStateLabel.BackColor = automaticMode
            ? AppTheme.SuccessSoft
            : AppTheme.SurfaceRaised;
        _automaticModeStateLabel.ForeColor = automaticMode
            ? AppTheme.Success
            : AppTheme.TextSecondary;
    }

    private void AssignmentChanged(
        ApplicationAssignmentChange change)
    {
        ArgumentNullException.ThrowIfNull(change);

        var displayedSettings = _useCases.Snapshot;
        var displayedAssignment =
            ProfileResolver.RequireAssignmentByExecutablePath(
                displayedSettings,
                change.ExecutablePath);
        var displayedRow =
            ApplicationAssignmentRowMapper.Map(
                displayedAssignment);
        SettingsCommitResult result;

        _committingGridValue = true;
        try
        {
            result = _useCases.Apply(change);
        }
        finally
        {
            _committingGridValue = false;
        }

        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            _assignmentsGrid.UpdateAssignment(displayedRow);
            return;
        }

        var committedSettings = _useCases.Snapshot;
        var committedAssignment =
            ProfileResolver.RequireAssignmentByExecutablePath(
                committedSettings,
                change.ExecutablePath);
        _assignmentsGrid.UpdateAssignment(
            ApplicationAssignmentRowMapper.Map(
                committedAssignment));
        UpdateSelectedProfileActions(committedSettings);
    }

    private void UpdateSelectedProfileActions(
        IReadOnlySightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var assignment =
            GetSelectedApplicationAssignment(settings);
        var visualProfile = assignment is null
            ? null
            : ProfileResolver.ResolveVisualProfile(
                settings,
                assignment);
        _editVisualProfileButton.Enabled = visualProfile?.SupportsTuning == true;
        _editVisualProfileButton.Text = visualProfile?.SupportsTuning == true
            ? $"Edit {visualProfile.Name}"
            : "Edit color profile";
    }

    private void EditSelectedVisualProfile()
    {
        var settings = _useCases.Snapshot;
        var assignment =
            GetSelectedApplicationAssignment(settings);
        if (assignment is null)
        {
            return;
        }

        var profile = ProfileResolver.ResolveVisualProfile(settings, assignment);
        if (!profile.SupportsTuning)
        {
            MessageBox.Show(
                this,
                "Select an editable profile in the VISUAL PROFILE column before editing color parameters.",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        var values = VisualProfileEditorForm.Edit(this, profile);
        if (values is null)
        {
            return;
        }

        var profileId = profile.Id;
        var result = _useCases.UpdateTuning(
            profileId,
            values);
        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            RefreshProfiles();
        }
    }

    internal void ManageVisualProfiles()
    {
        _showVisualProfileManager(this, _settingsCoordinator);
    }

    private ApplicationAssignment? GetSelectedApplicationAssignment(
        IReadOnlySightAdaptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var executablePath = _assignmentsGrid.SelectedExecutablePath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        return ProfileResolver.FindAssignmentByExecutablePath(
            settings,
            executablePath);
    }

    private void AddCurrentApplication()
    {
        var identity = _getCurrentApplication();
        if (identity is null)
        {
            MessageBox.Show(
                this,
                "No supported application window is available. Activate an application before opening this panel, or browse for its .exe file.",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        AddOrUpdateProfile(identity);
    }

    private void BrowseForApplication()
    {
        using var dialog = new OpenFileDialog
        {
            CheckFileExists = true,
            DereferenceLinks = true,
            Filter = "Windows applications (*.exe)|*.exe|All files (*.*)|*.*",
            Multiselect = false,
            RestoreDirectory = true,
            Title = "Select an application for SightAdapt",
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        try
        {
            AddOrUpdateProfile(ApplicationDiscovery.FromExecutablePath(dialog.FileName));
        }
        catch (Exception exception) when (
            exception is ArgumentException or IOException or UnauthorizedAccessException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void AddOrUpdateProfile(ApplicationIdentity identity)
    {
        var result = _useCases.AddOrEnable(identity);
        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            return;
        }

        MessageBox.Show(
            this,
            result.Value
                ? $"{identity.DisplayName} was added with the " +
                  $"{VisualProfilePolicy.NewAssignmentProfileName} visual profile."
                : $"{identity.DisplayName} is already configured and was enabled.",
            ProductInfo.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RemoveSelectedProfile()
    {
        var settings = _useCases.Snapshot;
        var profile =
            GetSelectedApplicationAssignment(settings);
        if (profile is null ||
            MessageBox.Show(
                this,
                $"Remove {profile.DisplayName} from SightAdapt?",
                ProductInfo.DisplayName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        var path = profile.ExecutablePath;
        var result = _useCases.Remove(path);
        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
        }
    }

    private void SettingsChanged(object? sender, EventArgs eventArgs)
    {
        if (_committingGridValue)
        {
            return;
        }

        RefreshProfiles();
    }

    private void ShowCommitError(string? message)
    {
        MessageBox.Show(
            this,
            message ?? "Settings could not be changed.",
            ProductInfo.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }

}
