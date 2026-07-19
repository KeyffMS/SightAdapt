namespace SightAdapt.Demo;

internal sealed class VisualProfileManagerForm : Form
{
    private readonly SightAdaptSettings _settings;
    private readonly Action _settingsChanged;
    private readonly DataGridView _profilesGrid;
    private readonly Label _profileCountLabel;
    private readonly ModernButton _duplicateButton;
    private readonly ModernButton _renameButton;
    private readonly ModernButton _editButton;
    private readonly ModernButton _deleteButton;

    private VisualProfileManagerForm(
        SightAdaptSettings settings,
        Action settingsChanged)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _settingsChanged = settingsChanged
            ?? throw new ArgumentNullException(nameof(settingsChanged));

        Text = $"{ProductInfo.DisplayName} · Visual profiles";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 560);
        Size = new Size(940, 640);
        ShowIcon = false;
        BackColor = AppTheme.WindowBackground;
        AppTheme.ApplyTo(this);

        _profileCountLabel = new Label
        {
            Anchor = AnchorStyles.Right,
            AutoSize = true,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9f, FontStyle.Bold),
            Margin = new Padding(0, 0, 18, 0),
            TextAlign = ContentAlignment.MiddleRight,
        };

        _profilesGrid = CreateProfilesGrid();

        _duplicateButton = CreateActionButton(
            "Duplicate",
            ModernButtonStyle.Secondary,
            DuplicateSelectedProfile);
        _renameButton = CreateActionButton(
            "Rename",
            ModernButtonStyle.Secondary,
            RenameSelectedProfile);
        _editButton = CreateActionButton(
            "Edit parameters",
            ModernButtonStyle.Secondary,
            EditSelectedProfile,
            145);
        _deleteButton = CreateActionButton(
            "Delete",
            ModernButtonStyle.Danger,
            DeleteSelectedProfile);

        var root = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Padding = new Padding(24, 18, 24, 18),
            RowCount = 3,
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
        root.Controls.Add(CreateHeader(), 0, 0);
        root.Controls.Add(CreateProfilesCard(), 0, 1);
        root.Controls.Add(CreateActionBar(), 0, 2);

        Controls.Add(root);
        RefreshProfiles();
    }

    public static void ShowManager(
        IWin32Window owner,
        SightAdaptSettings settings,
        Action settingsChanged)
    {
        ArgumentNullException.ThrowIfNull(owner);

        using var manager = new VisualProfileManagerForm(settings, settingsChanged);
        manager.ShowDialog(owner);
    }

    private Control CreateHeader()
    {
        var title = new Label
        {
            AutoSize = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(18f, FontStyle.Bold),
            Text = "Visual profiles",
            TextAlign = ContentAlignment.BottomLeft,
        };

        var subtitle = new Label
        {
            AutoEllipsis = true,
            Dock = DockStyle.Fill,
            ForeColor = AppTheme.TextSecondary,
            Font = AppTheme.CreateUiFont(9.3f),
            Text = "Create independent Soft Invert profiles and assign them to different applications.",
            TextAlign = ContentAlignment.TopLeft,
        };

        var layout = new TableLayoutPanel
        {
            BackColor = AppTheme.WindowBackground,
            ColumnCount = 1,
            Dock = DockStyle.Fill,
            Margin = Padding.Empty,
            RowCount = 2,
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        layout.Controls.Add(title, 0, 0);
        layout.Controls.Add(subtitle, 0, 1);
        return layout;
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
            MultiSelect = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        };
        AppTheme.StyleGrid(grid);

        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Name",
            HeaderText = "PROFILE",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
            MinimumWidth = 240,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Type",
            HeaderText = "TYPE",
            Width = 130,
            MinimumWidth = 120,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Transform",
            HeaderText = "TRANSFORM",
            Width = 150,
            MinimumWidth = 130,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Assignments",
            HeaderText = "APPLICATIONS",
            Width = 135,
            MinimumWidth = 120,
            SortMode = DataGridViewColumnSortMode.NotSortable,
        });

        grid.SelectionChanged += (_, _) => UpdateActions();
        grid.CellDoubleClick += (_, eventArgs) =>
        {
            if (eventArgs.RowIndex >= 0)
            {
                EditSelectedProfile();
            }
        };
        return grid;
    }

    private Control CreateProfilesCard()
    {
        var title = new Label
        {
            Anchor = AnchorStyles.Left,
            AutoSize = true,
            ForeColor = AppTheme.TextPrimary,
            Font = AppTheme.CreateUiFont(10.5f, FontStyle.Bold),
            Margin = new Padding(18, 0, 0, 0),
            Text = "Available profiles",
        };

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
        header.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        header.Controls.Add(title, 0, 0);
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
        var createButton = CreateActionButton(
            "Create profile",
            ModernButtonStyle.Primary,
            CreateProfile,
            135);

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
        leftButtons.Controls.Add(createButton);
        leftButtons.Controls.Add(_duplicateButton);
        leftButtons.Controls.Add(_renameButton);
        leftButtons.Controls.Add(_editButton);
        leftButtons.Controls.Add(_deleteButton);

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
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(leftButtons, 0, 0);
        layout.Controls.Add(closeButton, 1, 0);
        closeButton.Anchor = AnchorStyles.Right;
        return layout;
    }

    private static ModernButton CreateActionButton(
        string text,
        ModernButtonStyle style,
        Action action,
        int minimumWidth = 110)
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

    private void RefreshProfiles(string? selectedProfileId = null)
    {
        selectedProfileId ??= GetSelectedProfile()?.Id;
        _profilesGrid.Rows.Clear();

        foreach (var profile in _settings.VisualProfiles)
        {
            var assignmentCount = VisualProfileManagementService.CountAssignments(
                _settings,
                profile);
            var index = _profilesGrid.Rows.Add(
                profile.Name,
                VisualProfileManagementService.IsBuiltIn(profile)
                    ? "Built-in"
                    : "User-defined",
                profile.SupportsTuning ? "Soft invert" : "Exact invert",
                assignmentCount);

            var row = _profilesGrid.Rows[index];
            row.Tag = profile;

            if (!string.IsNullOrWhiteSpace(selectedProfileId) &&
                string.Equals(
                    profile.Id,
                    selectedProfileId,
                    StringComparison.OrdinalIgnoreCase))
            {
                row.Selected = true;
                _profilesGrid.CurrentCell = row.Cells["Name"];
            }
        }

        _profileCountLabel.Text = _settings.VisualProfiles.Count == 1
            ? "1 PROFILE"
            : $"{_settings.VisualProfiles.Count} PROFILES";

        if (_profilesGrid.CurrentRow is null && _profilesGrid.Rows.Count > 0)
        {
            _profilesGrid.Rows[0].Selected = true;
            _profilesGrid.CurrentCell = _profilesGrid.Rows[0].Cells["Name"];
        }

        UpdateActions();
    }

    private void UpdateActions()
    {
        var profile = GetSelectedProfile();
        var isBuiltIn = profile is not null &&
            VisualProfileManagementService.IsBuiltIn(profile);

        _duplicateButton.Enabled = profile?.SupportsTuning == true;
        _renameButton.Enabled = profile is not null && !isBuiltIn;
        _editButton.Enabled = profile?.SupportsTuning == true;
        _deleteButton.Enabled = profile is not null && !isBuiltIn;
    }

    private VisualProfile? GetSelectedProfile()
    {
        return _profilesGrid.SelectedRows.Count > 0
            ? _profilesGrid.SelectedRows[0].Tag as VisualProfile
            : _profilesGrid.CurrentRow?.Tag as VisualProfile;
    }

    private void CreateProfile()
    {
        var suggestedName = VisualProfileManagementService.CreateAvailableName(
            _settings,
            "Custom Soft Invert");

        if (!VisualProfileNameDialog.TryGetName(
                this,
                "Create profile",
                "Enter a unique name for the new Soft Invert profile.",
                suggestedName,
                out var name))
        {
            return;
        }

        RunProfileOperation(() =>
        {
            var profile = VisualProfileManagementService.Create(_settings, name);
            SaveAndRefresh(profile.Id);
        });
    }

    private void DuplicateSelectedProfile()
    {
        var source = GetSelectedProfile();
        if (source is null)
        {
            return;
        }

        var suggestedName = VisualProfileManagementService.CreateAvailableName(
            _settings,
            source.Name + " copy");

        if (!VisualProfileNameDialog.TryGetName(
                this,
                "Duplicate profile",
                $"Create an independent copy of '{source.Name}'.",
                suggestedName,
                out var name))
        {
            return;
        }

        RunProfileOperation(() =>
        {
            var profile = VisualProfileManagementService.Duplicate(
                _settings,
                source,
                name);
            SaveAndRefresh(profile.Id);
        });
    }

    private void RenameSelectedProfile()
    {
        var profile = GetSelectedProfile();
        if (profile is null)
        {
            return;
        }

        if (!VisualProfileNameDialog.TryGetName(
                this,
                "Rename profile",
                "Enter a new unique profile name.",
                profile.Name,
                out var name))
        {
            return;
        }

        RunProfileOperation(() =>
        {
            VisualProfileManagementService.Rename(_settings, profile, name);
            SaveAndRefresh(profile.Id);
        });
    }

    private void EditSelectedProfile()
    {
        var profile = GetSelectedProfile();
        if (profile?.SupportsTuning != true)
        {
            return;
        }

        if (VisualProfileEditorForm.Edit(this, profile))
        {
            SaveAndRefresh(profile.Id);
        }
    }

    private void DeleteSelectedProfile()
    {
        var profile = GetSelectedProfile();
        if (profile is null)
        {
            return;
        }

        var assignments = VisualProfileManagementService.CountAssignments(
            _settings,
            profile);
        var fallback = _settings.VisualProfiles.First(candidate => string.Equals(
            candidate.Id,
            VisualProfile.DefaultSoftInvertId,
            StringComparison.OrdinalIgnoreCase));

        var assignmentText = assignments == 1
            ? "1 application uses this profile"
            : $"{assignments} applications use this profile";
        var answer = MessageBox.Show(
            this,
            $"Delete '{profile.Name}'?\n\n{assignmentText}. " +
            $"Affected applications will be reassigned to '{fallback.Name}'.",
            ProductInfo.DisplayName,
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (answer != DialogResult.Yes)
        {
            return;
        }

        RunProfileOperation(() =>
        {
            VisualProfileManagementService.Delete(
                _settings,
                profile,
                fallback.Id);
            SaveAndRefresh(fallback.Id);
        });
    }

    private void SaveAndRefresh(string selectedProfileId)
    {
        _settingsChanged();
        RefreshProfiles(selectedProfileId);
    }

    private void RunProfileOperation(Action action)
    {
        try
        {
            action();
        }
        catch (Exception exception) when (
            exception is ArgumentException or InvalidOperationException)
        {
            MessageBox.Show(
                this,
                exception.Message,
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
}
