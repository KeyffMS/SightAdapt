using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class ConfigurationForm : Form
{
    private const string EnabledColumnName = "Enabled";
    private const string VisualProfileColumnName = "VisualProfile";

    private readonly SightAdaptSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly Func<ApplicationIdentity?> _getCurrentApplication;
    private readonly Action _settingsChanged;
    private readonly ToggleSwitch _automaticModeSwitch;
    private readonly Label _automaticModeStateLabel;
    private readonly Label _profileCountLabel;
    private readonly Label _emptyStateLabel;
    private readonly DataGridView _profilesGrid;
    private readonly ModernButton _editVisualProfileButton;
    private bool _refreshing;

    public ConfigurationForm(
        SightAdaptSettings settings,
        SettingsStore settingsStore,
        Func<ApplicationIdentity?> getCurrentApplication,
        Action settingsChanged)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsStore = settingsStore ?? throw new ArgumentNullException(nameof(settingsStore));
        _getCurrentApplication = getCurrentApplication
            ?? throw new ArgumentNullException(nameof(getCurrentApplication));
        _settingsChanged = settingsChanged
            ?? throw new ArgumentNullException(nameof(settingsChanged));

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

        _automaticModeStateLabel = new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            Font = AppTheme.CreateUiFont(8.5f, FontStyle.Bold),
            Margin = new Padding(12, 0, 0, 0),
            Padding = new Padding(12, 6, 12, 6),
            TextAlign = ContentAlignment.MiddleCenter,
        };

        _profileCountLabel = new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 18, 0),
            TextAlign = ContentAlignment.MiddleRight,
        };

        _editVisualProfileButton = new ModernButton
        {
            Text = "Edit color profile",
            VisualStyle = ModernButtonStyle.Secondary,
            MinimumSize = new Size(160, 40),
            Margin = new Padding(0, 0, 8, 0),
            Enabled = false,
        };
        _editVisualProfileButton.Click += (_, _) => EditSelectedVisualProfile();

        _profilesGrid = CreateProfilesGrid();
        _emptyStateLabel = new Label
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(10.5f),
            Padding = new Padding(32),
            Text = "No application profiles yet.\n\nAdd the currently active application or select an executable file.",
            TextAlign = ContentAlignment.MiddleCenter,
            Visible = false,
        };

        var settingsPathLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextMuted,
            Font = AppTheme.CreateUiFont(8.5f),
            Text = $"Settings are stored locally: {_settingsStore.SettingsPath}",
            TextAlign = ContentAlignment.MiddleLeft,
        };

        var contentLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 20, 24, 16),
            RowCount = 5,
        };
        contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 98));
        contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
        contentLayout.Controls.Add(CreateAutomaticModeCard(), 0, 0);
        contentLayout.Controls.Add(CreateProfilesCard(), 0, 1);
        contentLayout.Controls.Add(CreateActionBar(), 0, 2);
        contentLayout.Controls.Add(CreateProjectInfoCard(), 0, 3);
        contentLayout.Controls.Add(settingsPathLabel, 0, 4);

        var rootLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            RowCount = 2,
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        rootLayout.Controls.Add(CreateHeader(), 0, 0);
        rootLayout.Controls.Add(contentLayout, 0, 1);

        Controls.Add(rootLayout);
        RefreshProfiles();
    }

    public void RefreshProfiles()
    {
        _refreshing = true;

        try
        {
            var selectedPath = GetSelectedApplicationProfile()?.ExecutablePath;
            _automaticModeSwitch.Checked = _settings.AutomaticMode;
            UpdateAutomaticModeState();
            RefreshVisualProfileColumn();
            _profilesGrid.Rows.Clear();

            foreach (var profile in _settings.Applications)
            {
                var index = _profilesGrid.Rows.Add(
                    profile.Enabled,
                    profile.DisplayName,
                    profile.VisualProfileId,
                    profile.ExecutableName,
                    profile.ExecutablePath);

                var row = _profilesGrid.Rows[index];
                row.Tag = profile;

                if (!string.IsNullOrWhiteSpace(selectedPath) &&
                    string.Equals(
                        selectedPath,
                        profile.ExecutablePath,
                        StringComparison.OrdinalIgnoreCase))
                {
                    row.Selected = true;
                    _profilesGrid.CurrentCell = row.Cells["Application"];
                }
            }

            var count = _settings.Applications.Count;
            _profileCountLabel.Text = count == 1 ? "1 PROFILE" : $"{count} PROFILES";
            _emptyStateLabel.Visible = count == 0;
            _profilesGrid.Visible = count > 0;
            UpdateSelectedProfileActions();
        }
        finally
        {
            _refreshing = false;
        }
    }

    private static Panel CreateHeader()
    {
        var titleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(20f, FontStyle.Bold),
            Text = "Application and color profiles",
            TextAlign = ContentAlignment.BottomLeft,
        };

        var subtitleLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.5f),
            Text = "Assign and manage independent visual correction profiles for each Windows application.",
            TextAlign = ContentAlignment.TopLeft,
        };

        var textLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.HeaderBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(26, 18, 24, 14),
            RowCount = 2,
        };
        textLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        textLayout.Controls.Add(titleLabel, 0, 0);
        textLayout.Controls.Add(subtitleLabel, 0, 1);

        var accentStrip = new Panel
        {
            BackColor = AppTheme.Accent,
            Dock = DockStyle.Left,
            Width = 5,
        };

        var header = new Panel
        {
            BackColor = AppTheme.HeaderBackground,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        header.Controls.Add(textLayout);
        header.Controls.Add(accentStrip);
        return header;
    }

    private RoundedPanel CreateAutomaticModeCard()
    {
        var titleLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(10.5f, FontStyle.Bold),
            Text = "Automatic mode",
            TextAlign = ContentAlignment.BottomLeft,
        };

        var descriptionLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9f),
            Text = "Apply each application's assigned visual profile whenever its window becomes active.",
            TextAlign = ContentAlignment.TopLeft,
        };

        var descriptionLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        descriptionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 55));
        descriptionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 45));
        descriptionLayout.Controls.Add(titleLabel, 0, 0);
        descriptionLayout.Controls.Add(descriptionLabel, 0, 1);

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
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(_automaticModeSwitch, 0, 0);
        layout.Controls.Add(descriptionLayout, 1, 0);
        layout.Controls.Add(_automaticModeStateLabel, 2, 0);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 0, 0, 14),
        };
        card.Controls.Add(layout);
        return card;
    }

    private DataGridView CreateProfilesGrid()
    {
        var grid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            Dock = DockStyle.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter,
            MultiSelect = false,
            ReadOnly = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
        AppTheme.StyleGrid(grid);

        var enabledColumn = new DataGridViewCheckBoxColumn
        {
            Name = EnabledColumnName,
            HeaderText = "ACTIVE",
            Width = 92,
            MinimumWidth = 92,
            Resizable = DataGridViewTriState.False,
            FlatStyle = FlatStyle.Flat,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        };
        enabledColumn.HeaderCell.Style.Alignment = DataGridViewContentAlignment.MiddleCenter;
        enabledColumn.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        enabledColumn.DefaultCellStyle.Padding = Padding.Empty;

        grid.Columns.Add(enabledColumn);
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Application",
            HeaderText = "APPLICATION",
            ReadOnly = true,
            Width = 205,
            MinimumWidth = 165,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new DataGridViewComboBoxColumn
        {
            Name = VisualProfileColumnName,
            HeaderText = "VISUAL PROFILE",
            DisplayMember = nameof(VisualProfile.Name),
            ValueMember = nameof(VisualProfile.Id),
            DisplayStyle = DataGridViewComboBoxDisplayStyle.ComboBox,
            FlatStyle = FlatStyle.Flat,
            Width = 185,
            MinimumWidth = 160,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Executable",
            HeaderText = "EXECUTABLE",
            ReadOnly = true,
            Width = 155,
            MinimumWidth = 125,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Path",
            HeaderText = "FULL PATH",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 220,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });

        grid.CellValueChanged += ProfilesGridCellValueChanged;
        grid.CurrentCellDirtyStateChanged += ProfilesGridCurrentCellDirtyStateChanged;
        grid.SelectionChanged += (_, _) => UpdateSelectedProfileActions();
        grid.DataError += (_, eventArgs) => eventArgs.ThrowException = false;
        return grid;
    }

    private RoundedPanel CreateProfilesCard()
    {
        var titleLabel = new Label
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(10.5f, FontStyle.Bold),
            Margin = new Padding(18, 0, 0, 0),
            Text = "Configured applications",
        };

        var headerLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.SurfaceRaised,
            ColumnCount = 2,
            Dock = DockStyle.Top,
            Height = 52,
            Margin = Padding.Empty,
            RowCount = 1,
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        headerLayout.Controls.Add(titleLabel, 0, 0);
        headerLayout.Controls.Add(_profileCountLabel, 1, 0);

        var gridHost = new Panel
        {
            BackColor = AppTheme.Surface,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
        };
        gridHost.Controls.Add(_profilesGrid);
        gridHost.Controls.Add(_emptyStateLabel);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            Padding = new Padding(1),
        };
        card.Controls.Add(gridHost);
        card.Controls.Add(headerLayout);
        return card;
    }

    private Control CreateActionBar()
    {
        var addCurrentButton = new ModernButton
        {
            Text = "Add current app",
            VisualStyle = ModernButtonStyle.Primary,
            MinimumSize = new Size(150, 40),
            Margin = new Padding(0, 0, 8, 0),
        };
        addCurrentButton.Click += (_, _) => AddCurrentApplication();

        var browseButton = new ModernButton
        {
            Text = "Browse for .exe",
            VisualStyle = ModernButtonStyle.Secondary,
            MinimumSize = new Size(140, 40),
            Margin = new Padding(0, 0, 8, 0),
        };
        browseButton.Click += (_, _) => BrowseForApplication();

        var manageProfilesButton = new ModernButton
        {
            Text = "Manage profiles",
            VisualStyle = ModernButtonStyle.Secondary,
            MinimumSize = new Size(145, 40),
            Margin = new Padding(0, 0, 8, 0),
        };
        manageProfilesButton.Click += (_, _) => ManageVisualProfiles();

        var removeButton = new ModernButton
        {
            Text = "Remove selected",
            VisualStyle = ModernButtonStyle.Danger,
            MinimumSize = new Size(145, 40),
            Margin = Padding.Empty,
        };
        removeButton.Click += (_, _) => RemoveSelectedProfile();

        var closeButton = new ModernButton
        {
            DialogResult = DialogResult.Cancel,
            Text = "Close",
            VisualStyle = ModernButtonStyle.Ghost,
            MinimumSize = new Size(96, 40),
            Margin = Padding.Empty,
        };
        closeButton.Click += (_, _) => Close();
        CancelButton = closeButton;

        var leftButtons = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            BackColor = AppTheme.WindowBackground,
            FlowDirection = FlowDirection.LeftToRight,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        leftButtons.Controls.AddRange([
            addCurrentButton,
            browseButton,
            manageProfilesButton,
            _editVisualProfileButton,
            removeButton,
        ]);

        var actionLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 2,
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 12, 0, 8),
            RowCount = 1,
        };
        actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        actionLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        actionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        actionLayout.Controls.Add(leftButtons, 0, 0);
        actionLayout.Controls.Add(closeButton, 1, 0);
        closeButton.Anchor = AnchorStyles.Right;
        return actionLayout;
    }

    private static RoundedPanel CreateProjectInfoCard()
    {
        var productLabel = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(10.5f, FontStyle.Bold),
            Text = ProductInfo.DisplayName,
            TextAlign = ContentAlignment.BottomLeft,
        };

        var taglineLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(8.8f),
            Text = ProductInfo.Tagline,
            TextAlign = ContentAlignment.TopLeft,
        };

        var productLayout = new TableLayoutPanel
        {
            BackColor = AppTheme.Surface,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        productLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        productLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
        productLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 46));
        productLayout.Controls.Add(productLabel, 0, 0);
        productLayout.Controls.Add(taglineLabel, 0, 1);

        var repositoryLink = new LinkLabel
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
        repositoryLink.LinkClicked += (_, _) => OpenRepository();

        var metadataLayout = new FlowLayoutPanel
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            BackColor = AppTheme.Surface,
            FlowDirection = FlowDirection.TopDown,
            Margin = Padding.Empty,
            WrapContents = false,
        };
        metadataLayout.Controls.Add(CreateInfoLabel(ProductInfo.License, FontStyle.Bold));
        metadataLayout.Controls.Add(CreateInfoLabel(ProductInfo.Author, FontStyle.Regular));
        metadataLayout.Controls.Add(repositoryLink);

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
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(productLayout, 0, 0);
        layout.Controls.Add(metadataLayout, 1, 0);

        var card = new RoundedPanel
        {
            Dock = DockStyle.Fill,
            Margin = new Padding(0, 4, 0, 10),
        };
        card.Controls.Add(layout);
        return card;
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
        }
    }

    private void AutomaticModeCheckedChanged(object? sender, EventArgs eventArgs)
    {
        if (_refreshing)
        {
            return;
        }

        _settings.AutomaticMode = _automaticModeSwitch.Checked;
        UpdateAutomaticModeState();
        _settingsChanged();
    }

    private void UpdateAutomaticModeState()
    {
        _automaticModeStateLabel.Text = _settings.AutomaticMode ? "ACTIVE" : "PAUSED";
        _automaticModeStateLabel.BackColor = _settings.AutomaticMode
            ? AppTheme.SuccessSoft
            : AppTheme.SurfaceRaised;
        _automaticModeStateLabel.ForeColor = _settings.AutomaticMode
            ? AppTheme.Success
            : AppTheme.TextSecondary;
    }

    private void RefreshVisualProfileColumn()
    {
        if (_profilesGrid.Columns[VisualProfileColumnName] is not DataGridViewComboBoxColumn column)
        {
            return;
        }

        column.DataSource = null;
        column.DataSource = _settings.VisualProfiles.ToList();
    }

    private void ProfilesGridCurrentCellDirtyStateChanged(object? sender, EventArgs eventArgs)
    {
        if (_profilesGrid.IsCurrentCellDirty)
        {
            _profilesGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void ProfilesGridCellValueChanged(object? sender, DataGridViewCellEventArgs eventArgs)
    {
        if (_refreshing || eventArgs.RowIndex < 0 || eventArgs.ColumnIndex < 0)
        {
            return;
        }

        var row = _profilesGrid.Rows[eventArgs.RowIndex];
        if (row.Tag is not ApplicationProfile profile)
        {
            return;
        }

        var columnName = _profilesGrid.Columns[eventArgs.ColumnIndex].Name;
        if (columnName == EnabledColumnName)
        {
            profile.Enabled = row.Cells[eventArgs.ColumnIndex].Value is true;
        }
        else if (columnName == VisualProfileColumnName &&
                 row.Cells[eventArgs.ColumnIndex].Value is string visualProfileId &&
                 _settings.VisualProfiles.Any(candidate => string.Equals(
                     candidate.Id,
                     visualProfileId,
                     StringComparison.OrdinalIgnoreCase)))
        {
            profile.VisualProfileId = visualProfileId;
        }
        else
        {
            return;
        }

        _settingsChanged();
        UpdateSelectedProfileActions();
    }

    private void UpdateSelectedProfileActions()
    {
        var assignment = GetSelectedApplicationProfile();
        var visualProfile = assignment is null
            ? null
            : ProfileResolver.ResolveVisualProfile(_settings, assignment);

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

        var visualProfile = ProfileResolver.ResolveVisualProfile(_settings, assignment);
        if (!visualProfile.SupportsTuning)
        {
            MessageBox.Show(
                this,
                "Select Soft invert in the VISUAL PROFILE column before editing color parameters.",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        if (VisualProfileEditorForm.Edit(this, visualProfile))
        {
            _settingsChanged();
            RefreshProfiles();
        }
    }

    private void ManageVisualProfiles()
    {
        VisualProfileManagerForm.ShowManager(
            this,
            _settings,
            _settingsChanged);
        RefreshProfiles();
    }

    private ApplicationProfile? GetSelectedApplicationProfile()
    {
        return _profilesGrid.SelectedRows.Count > 0
            ? _profilesGrid.SelectedRows[0].Tag as ApplicationProfile
            : _profilesGrid.CurrentRow?.Tag as ApplicationProfile;
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
        var profile = _settings.Applications.FirstOrDefault(candidate => candidate.Matches(identity));
        var added = profile is null;

        if (profile is null)
        {
            profile = new ApplicationProfile
            {
                VisualProfileId = VisualProfile.DefaultSoftInvertId,
            };
            _settings.Applications.Add(profile);
        }

        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
        profile.Enabled = true;
        profile.LegacyEffect = null;
        _settings.AutomaticMode = true;

        _settingsChanged();
        RefreshProfiles();

        MessageBox.Show(
            this,
            added
                ? $"{identity.DisplayName} was added with the Soft invert visual profile."
                : $"{identity.DisplayName} is already configured and was enabled.",
            ProductInfo.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RemoveSelectedProfile()
    {
        var profile = GetSelectedApplicationProfile();
        if (profile is null)
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            $"Remove {profile.DisplayName} from SightAdapt?",
            ProductInfo.DisplayName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        _settings.Applications.Remove(profile);
        _settingsChanged();
        RefreshProfiles();
    }
}
