namespace SightAdapt;

internal sealed class VisualProfileManagerForm : Form
{
    private readonly VisualProfileUseCases _useCases;
    private readonly DataGridView _profilesGrid;
    private readonly Label _profileCountLabel;
    private readonly ModernButton _duplicateButton;
    private readonly ModernButton _renameButton;
    private readonly ModernButton _editButton;
    private readonly ModernButton _deleteButton;
    private bool _committingLocalChange;

    internal VisualProfileManagerForm(
        SettingsCoordinator settingsCoordinator)
        : this(new VisualProfileUseCases(
            settingsCoordinator ??
            throw new ArgumentNullException(
                nameof(settingsCoordinator))))
    {
    }

    internal VisualProfileManagerForm(
        VisualProfileUseCases useCases)
    {
        _useCases = useCases ??
            throw new ArgumentNullException(nameof(useCases));

        Text = $"{ProductInfo.DisplayName} · Visual profiles";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(820, 560);
        Size = new Size(940, 640);
        ShowIcon = false;
        BackColor = AppTheme.WindowBackground;
        AppTheme.ApplyTo(this);

        _profileCountLabel = FormPresentation.CreateCountLabel();
        _profilesGrid = CreateProfilesGrid();
        _duplicateButton = FormPresentation.CreateActionButton("Duplicate", ModernButtonStyle.Secondary, DuplicateSelectedProfile);
        _renameButton = FormPresentation.CreateActionButton("Rename", ModernButtonStyle.Secondary, RenameSelectedProfile);
        _editButton = FormPresentation.CreateActionButton("Edit parameters", ModernButtonStyle.Secondary, EditSelectedProfile, 145);
        _deleteButton = FormPresentation.CreateActionButton("Delete", ModernButtonStyle.Danger, DeleteSelectedProfile);

        Controls.Add(CreateRootLayout());
        _useCases.Changed += SettingsChanged;
        FormClosed += (_, _) => _useCases.Changed -= SettingsChanged;
        RefreshProfiles();
    }

    internal int RefreshGeneration { get; private set; }

    public static void ShowManager(IWin32Window owner, SettingsCoordinator settingsCoordinator)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(settingsCoordinator);
        using var manager = new VisualProfileManagerForm(settingsCoordinator);
        manager.ShowDialog(owner);
    }

    private Control CreateRootLayout()
    {
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
        return root;
    }

    private static Control CreateHeader()
    {
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
        layout.Controls.Add(FormPresentation.CreateHeaderLabel(
            "Visual profiles",
            18f,
            FontStyle.Bold,
            AppTheme.TextPrimary,
            ContentAlignment.BottomLeft), 0, 0);
        layout.Controls.Add(FormPresentation.CreateHeaderLabel(
            "Create independent editable profiles and assign them to different applications.",
            9.3f,
            FontStyle.Regular,
            AppTheme.TextSecondary,
            ContentAlignment.TopLeft), 0, 1);
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
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn("Name", "PROFILE", 240, fill: true));
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn("Type", "TYPE", 130));
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn("Transform", "TRANSFORM", 150));
        grid.Columns.Add(FormPresentation.CreateReadOnlyTextColumn("Assignments", "APPLICATIONS", 135));
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
            Text = "Available profiles",
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
        var create = FormPresentation.CreateActionButton(
            "Create profile",
            ModernButtonStyle.Primary,
            CreateProfile,
            135);
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
            create,
            _duplicateButton,
            _renameButton,
            _editButton,
            _deleteButton,
        ]);

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
        layout.Controls.Add(left, 0, 0);
        layout.Controls.Add(close, 1, 0);
        close.Anchor = AnchorStyles.Right;
        return layout;
    }

    private void RefreshProfiles(string? selectedProfileId = null)
    {
        if (IsDisposed)
        {
            return;
        }

        var settings = _useCases.Snapshot;
        selectedProfileId ??= GetSelectedProfile()?.Id;
        _profilesGrid.Rows.Clear();
        foreach (var profile in settings.VisualProfiles)
        {
            var index = _profilesGrid.Rows.Add(
                profile.Name,
                VisualProfileManagementService.IsBuiltIn(profile) ? "Built-in" : "User-defined",
                VisualProfileCatalog.Default.GetTransformDisplayName(profile.TransformId),
                VisualProfileManagementService.CountAssignments(settings, profile));
            var row = _profilesGrid.Rows[index];
            row.Tag = profile;
            if (string.Equals(profile.Id, selectedProfileId, StringComparison.OrdinalIgnoreCase))
            {
                row.Selected = true;
                _profilesGrid.CurrentCell = row.Cells["Name"];
            }
        }

        _profileCountLabel.Text = settings.VisualProfiles.Count == 1
            ? "1 PROFILE"
            : $"{settings.VisualProfiles.Count} PROFILES";
        if (_profilesGrid.CurrentRow is null && _profilesGrid.Rows.Count > 0)
        {
            _profilesGrid.Rows[0].Selected = true;
            _profilesGrid.CurrentCell = _profilesGrid.Rows[0].Cells["Name"];
        }

        UpdateActions();
        RefreshGeneration++;
    }

    private void UpdateActions()
    {
        var profile = GetSelectedProfile();
        var isBuiltIn = profile is not null && VisualProfileManagementService.IsBuiltIn(profile);
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
        var suggested = _useCases.CreateAvailableName(
            VisualProfilePolicy.CustomProfileBaseName);
        if (!VisualProfileNameDialog.TryGetName(
                this,
                "Create profile",
                "Enter a unique name for the new editable profile.",
                suggested,
                out var name))
        {
            return;
        }

        Commit(useCases => useCases.Create(name));
    }

    private void DuplicateSelectedProfile()
    {
        var source = GetSelectedProfile();
        if (source is null)
        {
            return;
        }

        var suggested = _useCases.CreateAvailableName(
            source.Name + " copy");
        if (!VisualProfileNameDialog.TryGetName(
                this,
                "Duplicate profile",
                $"Create an independent copy of '{source.Name}'.",
                suggested,
                out var name))
        {
            return;
        }

        var sourceId = source.Id;
        Commit(useCases =>
            useCases.Duplicate(sourceId, name));
    }

    private void RenameSelectedProfile()
    {
        var profile = GetSelectedProfile();
        if (profile is null ||
            !VisualProfileNameDialog.TryGetName(
                this,
                "Rename profile",
                "Enter a new unique profile name.",
                profile.Name,
                out var name))
        {
            return;
        }

        var profileId = profile.Id;
        Commit(useCases =>
            useCases.Rename(profileId, name));
    }

    private void EditSelectedProfile()
    {
        var profile = GetSelectedProfile();
        if (profile?.SupportsTuning != true)
        {
            return;
        }

        var values = VisualProfileEditorForm.Edit(this, profile);
        if (values is null)
        {
            return;
        }

        var profileId = profile.Id;
        Commit(useCases =>
            useCases.UpdateTuning(profileId, values));
    }

    private void DeleteSelectedProfile()
    {
        var profile = GetSelectedProfile();
        if (profile is null)
        {
            return;
        }

        var settings = _useCases.Snapshot;
        var fallback = ProfileResolver.FindVisualProfile(
            settings,
            VisualProfilePolicy.DeletionFallbackProfileId) ??
            throw new InvalidOperationException("The fallback profile is missing.");
        var assignments = VisualProfileManagementService.CountAssignments(settings, profile);
        var usage = assignments == 1
            ? "1 application uses this profile"
            : $"{assignments} applications use this profile";
        if (MessageBox.Show(
                this,
                $"Delete '{profile.Name}'?\n\n{usage}. Affected applications will be reassigned to '{fallback.Name}'.",
                ProductInfo.DisplayName,
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning,
                MessageBoxDefaultButton.Button2) != DialogResult.Yes)
        {
            return;
        }

        var profileId = profile.Id;
        var fallbackId = fallback.Id;
        Commit(useCases =>
            useCases.Delete(profileId, fallbackId));
    }

    internal void Commit(
        Func<VisualProfileUseCases,
            SettingsCommitResult<string>> command)
    {
        ArgumentNullException.ThrowIfNull(command);

        SettingsCommitResult<string> result;
        _committingLocalChange = true;
        try
        {
            result = command(_useCases);
        }
        finally
        {
            _committingLocalChange = false;
        }

        if (result.Succeeded)
        {
            RefreshProfiles(result.Value);
            return;
        }

        MessageBox.Show(
            this,
            result.ErrorMessage ?? "The visual profile operation failed.",
            ProductInfo.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        RefreshProfiles();
    }

    private void SettingsChanged(object? sender, EventArgs eventArgs)
    {
        if (!_committingLocalChange)
        {
            RefreshProfiles();
        }
    }

}
