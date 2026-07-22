from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def replace_between(text: str, start: str, end: str, replacement: str) -> str:
    start_index = text.index(start)
    end_index = text.index(end, start_index)
    return text[:start_index] + replacement + text[end_index:]


configuration_path = ROOT / "src/SightAdapt.Demo/ConfigurationForm.cs"
configuration = configuration_path.read_text(encoding="utf-8")

configuration = configuration.replace(
    '    private const string EnabledColumnName = "Enabled";\n'
    '    private const string VisualProfileColumnName = "VisualProfile";\n'
    '    private const string ApplicationColumnName = "Application";\n\n',
    '')
configuration = configuration.replace(
    '    private readonly Label _profileCountLabel;\n'
    '    private readonly Label _emptyStateLabel;\n'
    '    private readonly DataGridView _profilesGrid;\n',
    '    private readonly Label _profileCountLabel;\n'
    '    private readonly ApplicationProfilesGrid _profilesGrid;\n')
configuration = configuration.replace(
    '        _profilesGrid = CreateProfilesGrid();\n'
    '        _emptyStateLabel = CreateEmptyStateLabel();\n',
    '        _profilesGrid = new ApplicationProfilesGrid();\n'
    '        _profilesGrid.ValueChanged += ProfilesGridValueChanged;\n'
    '        _profilesGrid.SelectedApplicationChanged += (_, _) =>\n'
    '            UpdateSelectedProfileActions();\n')

configuration = replace_between(
    configuration,
    '    public void RefreshProfiles()\n',
    '    private Control CreateRootLayout()\n',
    '''    public void RefreshProfiles()\n    {\n        if (IsDisposed)\n        {\n            return;\n        }\n\n        _refreshing = true;\n        try\n        {\n            _automaticModeSwitch.Checked = Settings.AutomaticMode;\n            UpdateAutomaticModeState();\n            _profilesGrid.Bind(\n                Settings.Applications,\n                Settings.VisualProfiles);\n\n            var count = Settings.Applications.Count;\n            _profileCountLabel.Text = count == 1\n                ? "1 PROFILE"\n                : $"{count} PROFILES";\n            UpdateSelectedProfileActions();\n        }\n        finally\n        {\n            _refreshing = false;\n        }\n    }\n\n''')

configuration = replace_between(
    configuration,
    '    private DataGridView CreateProfilesGrid()\n',
    '    private Control CreateProfilesCard()\n',
    '')
configuration = configuration.replace(
    '        host.Controls.Add(_profilesGrid);\n'
    '        host.Controls.Add(_emptyStateLabel);\n',
    '        host.Controls.Add(_profilesGrid);\n')
configuration = replace_between(
    configuration,
    '    private static Label CreateEmptyStateLabel()\n',
    '    private static Label CreateInfoLabel(',
    '    private static Label CreateInfoLabel(')
configuration = replace_between(
    configuration,
    '    private static DataGridViewTextBoxColumn CreateTextColumn(\n',
    '    private static ModernButton CreateButton(\n',
    '    private static ModernButton CreateButton(\n')

configuration = replace_between(
    configuration,
    '    private void RefreshVisualProfileColumn()\n',
    '    private void UpdateSelectedProfileActions()\n',
    '''    private void ProfilesGridValueChanged(\n        object? sender,\n        ApplicationProfileGridValueChangedEventArgs eventArgs)\n    {\n        var displayedProfile = FindAssignment(\n            Settings,\n            eventArgs.ExecutablePath);\n        SettingsCommitResult result;\n\n        _committingGridValue = true;\n        try\n        {\n            result = eventArgs.Column switch\n            {\n                ApplicationProfileGridColumn.Enabled\n                    when eventArgs.Value is bool enabled =>\n                    _settingsCoordinator.Commit(settings =>\n                        ApplicationProfileManagementService.SetEnabled(\n                            settings,\n                            FindAssignment(settings, eventArgs.ExecutablePath),\n                            enabled)),\n                ApplicationProfileGridColumn.VisualProfile\n                    when eventArgs.Value is string visualProfileId =>\n                    _settingsCoordinator.Commit(settings =>\n                        ApplicationProfileManagementService.AssignVisualProfile(\n                            settings,\n                            FindAssignment(settings, eventArgs.ExecutablePath),\n                            visualProfileId)),\n                _ => SettingsCommitResult.Failure(\n                    "The edited application-profile value is not supported."),\n            };\n        }\n        finally\n        {\n            _committingGridValue = false;\n        }\n\n        if (!result.Succeeded)\n        {\n            ShowCommitError(result.ErrorMessage);\n            _profilesGrid.RestoreValue(\n                eventArgs.ExecutablePath,\n                eventArgs.Column,\n                eventArgs.Column == ApplicationProfileGridColumn.Enabled\n                    ? displayedProfile.Enabled\n                    : displayedProfile.VisualProfileId);\n            return;\n        }\n\n        _profilesGrid.UpdateApplication(FindAssignment(\n            Settings,\n            eventArgs.ExecutablePath));\n        UpdateSelectedProfileActions();\n    }\n\n    private void UpdateSelectedProfileActions()\n''')

configuration = replace_between(
    configuration,
    '    private ApplicationProfile? GetSelectedApplicationProfile()\n',
    '    private void AddCurrentApplication()\n',
    '''    private ApplicationProfile? GetSelectedApplicationProfile()\n    {\n        var executablePath = _profilesGrid.SelectedExecutablePath;\n        if (string.IsNullOrWhiteSpace(executablePath))\n        {\n            return null;\n        }\n\n        return Settings.Applications.FirstOrDefault(profile =>\n            string.Equals(\n                profile.ExecutablePath,\n                executablePath,\n                StringComparison.OrdinalIgnoreCase));\n    }\n\n    private void AddCurrentApplication()\n''')

configuration_path.write_text(configuration, encoding="utf-8")

selector_path = ROOT / "src/SightAdapt.Demo/VisualProfileComboBoxColumn.cs"
selector = selector_path.read_text(encoding="utf-8")
selector = selector.replace('using System.Diagnostics;\n', '')
selector = selector.replace('    private DataGridView? _attachedGrid;\n', '')
selector = replace_between(
    selector,
    '    protected override void OnDataGridViewChanged()\n',
    'internal sealed class ModernVisualProfileComboBoxCell',
    '}\n\ninternal sealed class ModernVisualProfileComboBoxCell')
selector = selector.replace(
    '    private void SelectByValue(string? value)\n',
    '''    internal void SelectOptionFromInput(VisualProfileOption option)\n    {\n        ArgumentNullException.ThrowIfNull(option);\n        SelectOption(option, notifyGrid: true);\n    }\n\n    private void SelectByValue(string? value)\n''')
selector_path.write_text(selector, encoding="utf-8")

test_path = ROOT / "tests/SightAdapt.Tests/ConfigurationGridCommitRegressionTests.cs"
test_path.write_text('''using System.Windows.Forms;\nusing Microsoft.VisualStudio.TestTools.UnitTesting;\n\nnamespace SightAdapt.Demo.Tests;\n\n[TestClass]\npublic sealed class ConfigurationGridCommitRegressionTests\n{\n    [TestMethod]\n    public void ProfileSelectionCommitsWithoutRebuildingActiveGrid()\n    {\n        RunOnSta(RunProfileSelectionScenario);\n    }\n\n    [TestMethod]\n    public void ExternalSettingsChangeRebindsGridFromCurrentSettings()\n    {\n        RunOnSta(RunExternalChangeScenario);\n    }\n\n    private static void RunOnSta(Action scenario)\n    {\n        Exception? failure = null;\n        var thread = new Thread(() =>\n        {\n            try\n            {\n                scenario();\n            }\n            catch (Exception exception)\n            {\n                failure = exception;\n            }\n        });\n        thread.SetApartmentState(ApartmentState.STA);\n        thread.Start();\n\n        Assert.IsTrue(\n            thread.Join(TimeSpan.FromSeconds(10)),\n            "The configuration-grid regression did not finish in time.");\n\n        if (failure is not null)\n        {\n            Assert.Fail(failure.ToString());\n        }\n    }\n\n    private static void RunProfileSelectionScenario()\n    {\n        var directory = CreateTemporaryDirectory();\n        try\n        {\n            var coordinator = CreateCoordinator(directory);\n            var identity = new ApplicationIdentity(\n                "Reader",\n                "reader.exe",\n                @"C:\\Apps\\reader.exe");\n            Assert.IsTrue(coordinator.Commit(settings =>\n            {\n                ApplicationProfileManagementService.AddOrEnable(\n                    settings,\n                    identity);\n            }).Succeeded);\n\n            using var form = new ConfigurationForm(coordinator, () => null);\n            form.Show();\n            Application.DoEvents();\n\n            var profilesGrid = FindControl<ApplicationProfilesGrid>(form);\n            var grid = profilesGrid.Grid;\n            Assert.AreEqual(1, grid.Rows.Count);\n            Assert.AreEqual(identity.ExecutablePath, grid.Rows[0].Tag);\n\n            var profileCell = grid.Rows[0].Cells["VisualProfile"];\n            grid.CurrentCell = profileCell;\n            grid.Focus();\n            Assert.IsTrue(grid.BeginEdit(true));\n            Assert.IsInstanceOfType<ModernVisualProfileEditingControl>(\n                grid.EditingControl);\n\n            var editor =\n                (ModernVisualProfileEditingControl)grid.EditingControl;\n            var option = ((DataGridViewComboBoxCell)profileCell)\n                .Items\n                .Cast<object>()\n                .OfType<VisualProfileOption>()\n                .Single(candidate =>\n                    candidate.Id == VisualProfile.DefaultInvertId);\n\n            editor.SelectOptionFromInput(option);\n            WaitFor(() =>\n                coordinator.Current.Applications.Single().VisualProfileId ==\n                VisualProfile.DefaultInvertId);\n\n            Assert.AreEqual(1, grid.Rows.Count);\n            Assert.AreEqual(\n                VisualProfile.DefaultInvertId,\n                grid.Rows[0].Cells["VisualProfile"].Value);\n            Assert.AreEqual(identity.ExecutablePath, grid.Rows[0].Tag);\n\n            grid.CurrentCell = grid.Rows[0].Cells["Application"];\n            Application.DoEvents();\n            Assert.IsFalse(grid.IsCurrentCellInEditMode);\n            Assert.AreEqual(1, grid.Rows.Count);\n            form.Close();\n        }\n        finally\n        {\n            DeleteTemporaryDirectory(directory);\n        }\n    }\n\n    private static void RunExternalChangeScenario()\n    {\n        var directory = CreateTemporaryDirectory();\n        try\n        {\n            var coordinator = CreateCoordinator(directory);\n            Assert.IsTrue(coordinator.Commit(settings =>\n            {\n                ApplicationProfileManagementService.AddOrEnable(\n                    settings,\n                    new ApplicationIdentity(\n                        "Reader",\n                        "reader.exe",\n                        @"C:\\Apps\\reader.exe"));\n            }).Succeeded);\n\n            using var form = new ConfigurationForm(coordinator, () => null);\n            form.Show();\n            Application.DoEvents();\n\n            var grid = FindControl<ApplicationProfilesGrid>(form).Grid;\n            Assert.AreEqual(1, grid.Rows.Count);\n\n            Assert.IsTrue(coordinator.Commit(settings =>\n            {\n                ApplicationProfileManagementService.AddOrEnable(\n                    settings,\n                    new ApplicationIdentity(\n                        "Writer",\n                        "writer.exe",\n                        @"C:\\Apps\\writer.exe"));\n            }).Succeeded);\n            Application.DoEvents();\n\n            Assert.AreEqual(2, grid.Rows.Count);\n            CollectionAssert.AreEquivalent(\n                new[]\n                {\n                    @"C:\\Apps\\reader.exe",\n                    @"C:\\Apps\\writer.exe",\n                },\n                grid.Rows\n                    .Cast<DataGridViewRow>()\n                    .Select(row => (string)row.Tag!)\n                    .ToArray());\n            form.Close();\n        }\n        finally\n        {\n            DeleteTemporaryDirectory(directory);\n        }\n    }\n\n    private static SettingsCoordinator CreateCoordinator(string directory)\n    {\n        return new SettingsCoordinator(\n            new SettingsStore(Path.Combine(directory, "settings.json")));\n    }\n\n    private static T FindControl<T>(Control root)\n        where T : Control\n    {\n        if (root is T match)\n        {\n            return match;\n        }\n\n        foreach (Control child in root.Controls)\n        {\n            try\n            {\n                return FindControl<T>(child);\n            }\n            catch (InvalidOperationException)\n            {\n            }\n        }\n\n        throw new InvalidOperationException(\n            $"Control {typeof(T).Name} was not found.");\n    }\n\n    private static string CreateTemporaryDirectory()\n    {\n        var directory = Path.Combine(\n            Path.GetTempPath(),\n            "SightAdapt.Tests",\n            Guid.NewGuid().ToString("N"));\n        Directory.CreateDirectory(directory);\n        return directory;\n    }\n\n    private static void DeleteTemporaryDirectory(string directory)\n    {\n        if (Directory.Exists(directory))\n        {\n            Directory.Delete(directory, true);\n        }\n    }\n\n    private static void WaitFor(Func<bool> condition)\n    {\n        var deadline = DateTime.UtcNow.AddSeconds(2);\n        while (!condition() && DateTime.UtcNow < deadline)\n        {\n            Application.DoEvents();\n            Thread.Sleep(1);\n        }\n    }\n}\n''', encoding="utf-8")
