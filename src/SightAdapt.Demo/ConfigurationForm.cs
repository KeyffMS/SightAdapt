using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class ConfigurationForm : Form
{
    private readonly SettingsCoordinator _settingsCoordinator;
    private readonly Func<ApplicationIdentity?> _getCurrentApplication;
    private readonly ToggleSwitch _automaticModeSwitch;
    private readonly Label _automaticModeStateLabel;
    private readonly Label _profileCountLabel;
    private readonly ApplicationProfilesGrid _profilesGrid;
    private readonly ModernButton _editVisualProfileButton;
    private bool _refreshing;
    private bool _committingGridValue;

    public ConfigurationForm(
        SettingsCoordinator settingsCoordinator,
        Func<ApplicationIdentity?> getCurrentApplication)
    {
        _settingsCoordinator = settingsCoordinator ??
            throw new ArgumentNullException(nameof(settingsCoordinator));
        _getCurrentApplication = getCurrentApplication ??
            throw new ArgumentNullException(nameof(getCurrentApplication));

        Text = ProductInfo.WindowTitle;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(980, 680);
        Size = new Size(1180, 780);
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
        _profileCountLabel = CreateProfileCountLabel();
        _editVisualProfileButton = CreateButton(
            "Edit color profile",
            ModernButtonStyle.Secondary,
            160,
            EditSelectedVisualProfile);
        _editVisualProfileButton.Enabled = false;
        _profilesGrid = new ApplicationProfilesGrid();
        _profilesGrid.ValueChanged += ProfilesGridValueChanged;
        _profilesGrid.SelectedApplicationChanged += (_, _) =>
            UpdateSelectedProfileActions();

        Controls.Add(CreateRootLayout());
        _settingsCoordinator.Changed += SettingsChanged;
        FormClosed += (_, _) => _settingsCoordinator.Changed -= SettingsChanged;
        RefreshProfiles();
    }

    private SightAdaptSettings Settings => _settingsCoordinator.Current;

    public void RefreshProfiles()
    {
        if (IsDisposed)
        {
            return;
        }

        _refreshing = true;
        try
        {
            _automaticModeSwitch.Checked = Settings.AutomaticMode;
            UpdateAutomaticModeState();
            _profilesGrid.Bind(
                Settings.Applications,
                Settings.VisualProfiles);

            var count = Settings.Applications.Count;
            _profileCountLabel.Text = count == 1
                ? "1 PROFILE"
                : $"{count} PROFILES";
            UpdateSelectedProfileActions();
        }
        finally
        {
            _refreshing = false;
        }
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
            Text = $"Settings are stored locally: {_settingsCoordinator.SettingsPath}",
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
        text.Controls.Add(CreateHeaderLabel(
            "Application and color profiles",
            20f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.BottomLeft), 0, 0);
        text.Controls.Add(CreateHeaderLabel(
            "Assign and manage independent visual correction profiles for each Windows application.",
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
        description.Controls.Add(CreateHeaderLabel(
            "Automatic mode",
            10.5f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.BottomLeft), 0, 0);
        description.Controls.Add(CreateHeaderLabel(
            "Apply each application's assigned visual profile whenever its window becomes active.",
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
        header.Controls.Add(_profileCountLabel, 1, 0);

        var host = new Panel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        host.Controls.Add(_profilesGrid);

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
            CreateButton("Add current app", ModernButtonStyle.Primary, 150, AddCurrentApplication),
            CreateButton("Browse for .exe", ModernButtonStyle.Secondary, 140, BrowseForApplication),
            CreateButton("Manage profiles", ModernButtonStyle.Secondary, 145, ManageVisualProfiles),
            _editVisualProfileButton,
            CreateButton("Remove selected", ModernButtonStyle.Danger, 145, RemoveSelectedProfile),
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

    private static Control CreateProjectInfoCard()
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
        repository.LinkClicked += (_, _) => OpenRepository();

        var product = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        product.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        product.Controls.Add(CreateHeaderLabel(
            ProductInfo.DisplayName,
            10.5f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.BottomLeft), 0, 0);
        product.Controls.Add(CreateHeaderLabel(
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

    private static Label CreateHeaderLabel(
        string text,
        float size,
        FontStyle style,
        Color color,
        ContentAlignment alignment)
    {
        return new Label
        {
            AutoEllipsis = true,
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = color,
            Font = AppTheme.CreateUiFont(size, style),
            Text = text,
            TextAlign = alignment,
        };
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

    private static Label CreateProfileCountLabel()
    {
        return new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 18, 0),
            TextAlign = ContentAlignment.MiddleRight,
        };
    }

    private static Label CreateInfoLabel(    private static Label CreateInfoLabel(string text, FontStyle style)
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

    private static ModernButton CreateButton(
    private static ModernButton CreateButton(
        string text,
        ModernButtonStyle style,
        int minimumWidth,
        Action action)
    {
        var button = new ModernButton
        {
            Text = text,
            VisualStyle = style,
            MinimumSize = new Size(minimumWidth, 40),
            Margin = new Padding(0, 0, 8, 0),
        };
        button.Click += (_, _) => action();
        return button;
    }

    private static void OpenRepository()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ProductInfo.RepositoryUrl)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            Debug.WriteLine($"SightAdapt could not open the repository: {exception}");
            MessageBox.Show(
                $"The repository could not be opened.\n\n{exception.Message}",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void AutomaticModeCheckedChanged(object? sender, EventArgs eventArgs)
    {
        if (_refreshing)
        {
            return;
        }

        var result = _settingsCoordinator.Commit(settings =>
            AutomaticModeManagementService.Set(settings, _automaticModeSwitch.Checked));
        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            RefreshProfiles();
        }
    }

    private void UpdateAutomaticModeState()
    {
        _automaticModeStateLabel.Text = Settings.AutomaticMode ? "ACTIVE" : "PAUSED";
        _automaticModeStateLabel.BackColor = Settings.AutomaticMode
            ? AppTheme.SuccessSoft
            : AppTheme.SurfaceRaised;
        _automaticModeStateLabel.ForeColor = Settings.AutomaticMode
            ? AppTheme.Success
            : AppTheme.TextSecondary;
    }

    private void ProfilesGridValueChanged(
        object? sender,
        ApplicationProfileGridValueChangedEventArgs eventArgs)
    {
        var displayedProfile = FindAssignment(
            Settings,
            eventArgs.ExecutablePath);
        SettingsCommitResult result;

        _committingGridValue = true;
        try
        {
            result = eventArgs.Column switch
            {
                ApplicationProfileGridColumn.Enabled
                    when eventArgs.Value is bool enabled =>
                    _settingsCoordinator.Commit(settings =>
                        ApplicationProfileManagementService.SetEnabled(
                            settings,
                            FindAssignment(settings, eventArgs.ExecutablePath),
                            enabled)),
                ApplicationProfileGridColumn.VisualProfile
                    when eventArgs.Value is string visualProfileId =>
                    _settingsCoordinator.Commit(settings =>
                        ApplicationProfileManagementService.AssignVisualProfile(
                            settings,
                            FindAssignment(settings, eventArgs.ExecutablePath),
                            visualProfileId)),
                _ => SettingsCommitResult.Failure(
                    "The edited application-profile value is not supported."),
            };
        }
        finally
        {
            _committingGridValue = false;
        }

        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            _profilesGrid.RestoreValue(
                eventArgs.ExecutablePath,
                eventArgs.Column,
                eventArgs.Column == ApplicationProfileGridColumn.Enabled
                    ? displayedProfile.Enabled
                    : displayedProfile.VisualProfileId);
            return;
        }

        _profilesGrid.UpdateApplication(FindAssignment(
            Settings,
            eventArgs.ExecutablePath));
        UpdateSelectedProfileActions();
    }

    private void UpdateSelectedProfileActions()
    private void UpdateSelectedProfileActions()
    {
        var assignment = GetSelectedApplicationProfile();
        var visualProfile = assignment is null
            ? null
            : ProfileResolver.ResolveVisualProfile(Settings, assignment);
        _editVisualProfileButton.Enabled = visualProfile?.SupportsTuning == true;
        _editVisualProfileButton.Text = visualProfile?.SupportsTuning == true
            ? $"Edit {visualProfile.Name}"
            : "Edit color profile";
    }

    private void EditSelectedVisualProfile()
    {
        var assignment = GetSelectedApplicationProfile();
        if (assignment is null)
        {
            return;
        }

        var profile = ProfileResolver.ResolveVisualProfile(Settings, assignment);
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
        var result = _settingsCoordinator.Commit(settings =>
            VisualProfileManagementService.UpdateTuning(
                settings,
                ProfileResolver.FindVisualProfile(settings, profileId) ??
                    throw new InvalidOperationException("The selected visual profile no longer exists."),
                values));
        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            RefreshProfiles();
        }
    }

    private void ManageVisualProfiles()
    {
        VisualProfileManagerForm.ShowManager(this, _settingsCoordinator);
        RefreshProfiles();
    }

    private ApplicationProfile? GetSelectedApplicationProfile()
    {
        var executablePath = _profilesGrid.SelectedExecutablePath;
        if (string.IsNullOrWhiteSpace(executablePath))
        {
            return null;
        }

        return Settings.Applications.FirstOrDefault(profile =>
            string.Equals(
                profile.ExecutablePath,
                executablePath,
                StringComparison.OrdinalIgnoreCase));
    }

    private void AddCurrentApplication()
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
        var result = _settingsCoordinator.Commit(settings =>
        {
            var assignment = ApplicationProfileManagementService.AddOrEnable(settings, identity);
            AutomaticModeManagementService.Enable(settings);
            return assignment.WasCreated;
        });
        if (!result.Succeeded)
        {
            ShowCommitError(result.ErrorMessage);
            return;
        }

        MessageBox.Show(
            this,
            result.Value
                ? $"{identity.DisplayName} was added with the Soft invert visual profile."
                : $"{identity.DisplayName} is already configured and was enabled.",
            ProductInfo.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RemoveSelectedProfile()
    {
        var profile = GetSelectedApplicationProfile();
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
        var result = _settingsCoordinator.Commit(settings =>
            ApplicationProfileManagementService.Remove(settings, FindAssignment(settings, path)));
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

    private static ApplicationProfile FindAssignment(SightAdaptSettings settings, string executablePath)
    {
        return settings.Applications.FirstOrDefault(profile => string.Equals(
                profile.ExecutablePath,
                executablePath,
                StringComparison.OrdinalIgnoreCase)) ??
            throw new InvalidOperationException("The selected application assignment no longer exists.");
    }
}
