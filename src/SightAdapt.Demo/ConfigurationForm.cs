using System.Drawing;

namespace SightAdapt.Demo;

internal sealed class ConfigurationForm : Form
{
    private readonly SightAdaptSettings _settings;
    private readonly SettingsStore _settingsStore;
    private readonly Func<ApplicationIdentity?> _getCurrentApplication;
    private readonly Action _settingsChanged;
    private readonly CheckBox _automaticModeCheckBox;
    private readonly DataGridView _profilesGrid;
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

        Text = "SightAdapt Demo 0.2 - Application profiles";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 420);
        Size = new Size(920, 520);
        ShowIcon = false;

        _automaticModeCheckBox = new CheckBox
        {
            AutoSize = true,
            Text = "Enable automatic mode",
            Margin = new Padding(12, 13, 12, 8),
        };
        _automaticModeCheckBox.CheckedChanged += AutomaticModeCheckedChanged;

        var settingsPathLabel = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            Text = $"Settings: {_settingsStore.SettingsPath}",
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 8, 0),
        };

        var headerPanel = new TableLayoutPanel
        {
            ColumnCount = 2,
            Dock = DockStyle.Top,
            Height = 48,
        };
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerPanel.Controls.Add(_automaticModeCheckBox, 0, 0);
        headerPanel.Controls.Add(settingsPathLabel, 1, 0);

        _profilesGrid = new DataGridView
        {
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AutoGenerateColumns = false,
            BackgroundColor = SystemColors.Window,
            BorderStyle = BorderStyle.Fixed3D,
            Dock = DockStyle.Fill,
            EditMode = DataGridViewEditMode.EditOnEnter,
            MultiSelect = false,
            ReadOnly = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
        _profilesGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            Name = "Enabled",
            HeaderText = "Enabled",
            Width = 70,
        });
        _profilesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Application",
            HeaderText = "Application",
            ReadOnly = true,
            Width = 190,
        });
        _profilesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Executable",
            HeaderText = "Executable",
            ReadOnly = true,
            Width = 140,
        });
        _profilesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Path",
            HeaderText = "Full path",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            ReadOnly = true,
        });
        _profilesGrid.CellValueChanged += ProfilesGridCellValueChanged;
        _profilesGrid.CurrentCellDirtyStateChanged += ProfilesGridCurrentCellDirtyStateChanged;

        var addCurrentButton = new Button
        {
            AutoSize = true,
            Text = "Add current application",
        };
        addCurrentButton.Click += (_, _) => AddCurrentApplication();

        var browseButton = new Button
        {
            AutoSize = true,
            Text = "Browse for .exe...",
        };
        browseButton.Click += (_, _) => BrowseForApplication();

        var removeButton = new Button
        {
            AutoSize = true,
            Text = "Remove selected",
        };
        removeButton.Click += (_, _) => RemoveSelectedProfile();

        var closeButton = new Button
        {
            AutoSize = true,
            DialogResult = DialogResult.Cancel,
            Text = "Close",
        };
        closeButton.Click += (_, _) => Close();

        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            Padding = new Padding(8),
            WrapContents = false,
        };
        buttonPanel.Controls.AddRange([
            addCurrentButton,
            browseButton,
            removeButton,
            closeButton,
        ]);

        Controls.Add(_profilesGrid);
        Controls.Add(buttonPanel);
        Controls.Add(headerPanel);

        CancelButton = closeButton;
        RefreshProfiles();
    }

    public void RefreshProfiles()
    {
        _refreshing = true;

        try
        {
            _automaticModeCheckBox.Checked = _settings.AutomaticMode;
            _profilesGrid.Rows.Clear();

            foreach (var profile in _settings.Applications)
            {
                var index = _profilesGrid.Rows.Add(
                    profile.Enabled,
                    profile.DisplayName,
                    profile.ExecutableName,
                    profile.ExecutablePath);

                _profilesGrid.Rows[index].Tag = profile;
            }
        }
        finally
        {
            _refreshing = false;
        }
    }

    private void AutomaticModeCheckedChanged(object? sender, EventArgs eventArgs)
    {
        if (_refreshing)
        {
            return;
        }

        _settings.AutomaticMode = _automaticModeCheckBox.Checked;
        _settingsChanged();
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
        if (_refreshing || eventArgs.RowIndex < 0 || eventArgs.ColumnIndex != 0)
        {
            return;
        }

        var row = _profilesGrid.Rows[eventArgs.RowIndex];
        if (row.Tag is not ApplicationProfile profile)
        {
            return;
        }

        profile.Enabled = row.Cells[eventArgs.ColumnIndex].Value is true;
        _settingsChanged();
    }

    private void AddCurrentApplication()
    {
        var identity = _getCurrentApplication();
        if (identity is null)
        {
            MessageBox.Show(
                this,
                "No supported application window is available. Activate an application before opening this panel, or browse for its .exe file.",
                "SightAdapt Demo 0.2",
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
                "SightAdapt Demo 0.2",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private void AddOrUpdateProfile(ApplicationIdentity identity)
    {
        var profile = _settings.Applications.FirstOrDefault(
            candidate => string.Equals(
                candidate.ExecutablePath,
                identity.ExecutablePath,
                StringComparison.OrdinalIgnoreCase));

        var added = profile is null;
        if (profile is null)
        {
            profile = new ApplicationProfile();
            _settings.Applications.Add(profile);
        }

        profile.DisplayName = identity.DisplayName;
        profile.ExecutableName = identity.ExecutableName;
        profile.ExecutablePath = identity.ExecutablePath;
        profile.Enabled = true;
        profile.Effect = "invert";
        _settings.AutomaticMode = true;

        _settingsChanged();
        RefreshProfiles();

        MessageBox.Show(
            this,
            added
                ? $"{identity.DisplayName} was added to automatic inversion."
                : $"{identity.DisplayName} is already configured and was enabled.",
            "SightAdapt Demo 0.2",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void RemoveSelectedProfile()
    {
        if (_profilesGrid.SelectedRows.Count == 0 ||
            _profilesGrid.SelectedRows[0].Tag is not ApplicationProfile profile)
        {
            return;
        }

        var answer = MessageBox.Show(
            this,
            $"Remove {profile.DisplayName} from SightAdapt?",
            "SightAdapt Demo 0.2",
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
