Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$Utf8NoBom = [System.Text.UTF8Encoding]::new($false)

function Normalize-Newlines([string]$Value) {
    return $Value.Replace("`r`n", "`n")
}

function Replace-Exact(
    [string]$Path,
    [string]$Old,
    [string]$New,
    [int]$ExpectedCount = 1) {
    $content = Normalize-Newlines (Get-Content -Raw $Path)
    $oldValue = Normalize-Newlines $Old
    $newValue = Normalize-Newlines $New
    $count = [regex]::Matches(
        $content,
        [regex]::Escape($oldValue)).Count
    if ($count -ne $ExpectedCount) {
        throw "Expected $ExpectedCount occurrence(s) in '$Path', found $count."
    }

    $content = $content.Replace($oldValue, $newValue)
    [System.IO.File]::WriteAllText($Path, $content, $Utf8NoBom)
}

function Write-NewFile([string]$Path, [string]$Content) {
    if (Test-Path $Path) {
        throw "File '$Path' already exists."
    }

    [System.IO.File]::WriteAllText(
        $Path,
        (Normalize-Newlines $Content),
        $Utf8NoBom)
}

Replace-Exact 'src/SightAdapt/VisualProfileManagerForm.cs' @'
    private readonly ModernButton _deleteButton;

    private VisualProfileManagerForm(SettingsCoordinator settingsCoordinator)
'@ @'
    private readonly ModernButton _deleteButton;
    private bool _committingLocalChange;

    internal VisualProfileManagerForm(SettingsCoordinator settingsCoordinator)
'@

Replace-Exact 'src/SightAdapt/VisualProfileManagerForm.cs' @'
    private SightAdaptSettings Settings => _settingsCoordinator.Current;

    public static void ShowManager(IWin32Window owner, SettingsCoordinator settingsCoordinator)
'@ @'
    private SightAdaptSettings Settings => _settingsCoordinator.Current;

    internal int RefreshGeneration { get; private set; }

    public static void ShowManager(IWin32Window owner, SettingsCoordinator settingsCoordinator)
'@

Replace-Exact 'src/SightAdapt/VisualProfileManagerForm.cs' @'
        if (_profilesGrid.CurrentRow is null && _profilesGrid.Rows.Count > 0)
        {
            _profilesGrid.Rows[0].Selected = true;
            _profilesGrid.CurrentCell = _profilesGrid.Rows[0].Cells["Name"];
        }
        UpdateActions();
'@ @'
        if (_profilesGrid.CurrentRow is null && _profilesGrid.Rows.Count > 0)
        {
            _profilesGrid.Rows[0].Selected = true;
            _profilesGrid.CurrentCell = _profilesGrid.Rows[0].Cells["Name"];
        }

        UpdateActions();
        RefreshGeneration++;
'@

Replace-Exact 'src/SightAdapt/VisualProfileManagerForm.cs' @'
    private void Commit(Func<SightAdaptSettings, string> mutation)
    {
        var result = _settingsCoordinator.Commit(mutation);
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
        RefreshProfiles();
    }
'@ @'
    internal void Commit(Func<SightAdaptSettings, string> mutation)
    {
        ArgumentNullException.ThrowIfNull(mutation);

        SettingsCommitResult<string> result;
        _committingLocalChange = true;
        try
        {
            result = _settingsCoordinator.Commit(mutation);
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
'@

Write-NewFile 'tests/SightAdapt.Tests/VisualProfileManagerRefreshTests.cs' @'
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class VisualProfileManagerRefreshTests
{
    [TestMethod]
    public void LocalCommitRefreshesGridExactlyOnce()
    {
        RunOnSta(() =>
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                var coordinator = CreateCoordinator(directory);
                using var manager =
                    new VisualProfileManagerForm(coordinator);
                var generation = manager.RefreshGeneration;

                manager.Commit(settings =>
                    VisualProfileManagementService.Create(
                        settings,
                        "Reader").Id);

                Assert.AreEqual(
                    generation + 1,
                    manager.RefreshGeneration);
                Assert.IsTrue(coordinator.Current.VisualProfiles.Any(
                    profile => profile.Name == "Reader"));
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        });
    }

    [TestMethod]
    public void ExternalSettingsChangeRefreshesGridExactlyOnce()
    {
        RunOnSta(() =>
        {
            var directory = CreateTemporaryDirectory();
            try
            {
                var coordinator = CreateCoordinator(directory);
                using var manager =
                    new VisualProfileManagerForm(coordinator);
                var generation = manager.RefreshGeneration;

                var result = coordinator.Commit(settings =>
                    VisualProfileManagementService.Create(
                        settings,
                        "Writer").Id);

                Assert.IsTrue(result.Succeeded);
                Assert.AreEqual(
                    generation + 1,
                    manager.RefreshGeneration);
            }
            finally
            {
                DeleteTemporaryDirectory(directory);
            }
        });
    }

    private static SettingsCoordinator CreateCoordinator(
        string directory)
    {
        return new SettingsCoordinator(
            new SettingsStore(
                Path.Combine(directory, "settings.json")));
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.IsTrue(
            thread.Join(TimeSpan.FromSeconds(10)),
            "The profile-manager refresh test did not finish in time.");
        if (failure is not null)
        {
            Assert.Fail(failure.ToString());
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "SightAdapt.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void DeleteTemporaryDirectory(
        string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
'@
