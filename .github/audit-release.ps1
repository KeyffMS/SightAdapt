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

Replace-Exact 'src/SightAdapt/ModernTheme.cs' @'
namespace SightAdapt;

internal static class AppTheme
'@ @'
namespace SightAdapt;

internal enum MenuItemRole
{
    Default,
    Status,
    Danger,
}

internal static class AppTheme
'@

Replace-Exact 'src/SightAdapt/ModernTheme.cs' @'
        Color? foreground = null,
        FontStyle fontStyle = FontStyle.Regular,
        string? role = null)
'@ @'
        Color? foreground = null,
        FontStyle fontStyle = FontStyle.Regular,
        MenuItemRole role = MenuItemRole.Default)
'@

Replace-Exact 'src/SightAdapt/ModernTheme.cs' @'
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs eventArgs)
    {
        var itemText = eventArgs.Item.Text ?? string.Empty;
        var isChecked = eventArgs.Item is ToolStripMenuItem menuItem && menuItem.Checked;
        var textColor = !eventArgs.Item.Enabled
            ? AppTheme.TextSecondary
            : itemText.StartsWith("Emergency", StringComparison.OrdinalIgnoreCase)
                ? AppTheme.Danger
                : isChecked
                    ? AppTheme.AccentHover
                    : AppTheme.TextPrimary;

        TextRenderer.DrawText(
'@ @'
    protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs eventArgs)
    {
        var textColor = ResolveItemTextColor(eventArgs.Item);

        TextRenderer.DrawText(
'@

Replace-Exact 'src/SightAdapt/ModernTheme.cs' @'
    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs eventArgs)
'@ @'
    internal static Color ResolveItemTextColor(
        ToolStripItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (!item.Enabled)
        {
            return AppTheme.TextSecondary;
        }

        if (item.Tag is MenuItemRole role &&
            role == MenuItemRole.Danger)
        {
            return AppTheme.Danger;
        }

        return item is ToolStripMenuItem { Checked: true }
            ? AppTheme.AccentHover
            : AppTheme.TextPrimary;
    }

    protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs eventArgs)
'@

Replace-Exact 'src/SightAdapt/TrayPresenter.cs' `
    '            "status");' `
    '            MenuItemRole.Status);'
Replace-Exact 'src/SightAdapt/TrayPresenter.cs' `
    '            "danger");' `
    '            MenuItemRole.Danger);'

Write-NewFile 'tests/SightAdapt.Tests/MenuRendererRoleTests.cs' @'
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class MenuRendererRoleTests
{
    [TestMethod]
    public void DangerColorComesFromSemanticRole()
    {
        using var item =
            new ToolStripMenuItem("Stop every overlay");
        AppTheme.StyleMenuItem(
            item,
            AppTheme.Danger,
            FontStyle.Bold,
            MenuItemRole.Danger);

        Assert.AreEqual(
            AppTheme.Danger,
            DarkMenuRenderer.ResolveItemTextColor(item));

        item.Text = "Natychmiast wyłącz wszystkie nakładki";
        Assert.AreEqual(
            AppTheme.Danger,
            DarkMenuRenderer.ResolveItemTextColor(item));
    }

    [TestMethod]
    public void EmergencyTextWithoutDangerRoleIsNotSpecial()
    {
        using var item =
            new ToolStripMenuItem("Emergency wording only");
        AppTheme.StyleMenuItem(item);

        Assert.AreEqual(
            AppTheme.TextPrimary,
            DarkMenuRenderer.ResolveItemTextColor(item));
    }

    [TestMethod]
    public void DisabledAndCheckedPrioritiesRemainStable()
    {
        using var disabled =
            new ToolStripMenuItem("Disabled")
            {
                Enabled = false,
            };
        AppTheme.StyleMenuItem(
            disabled,
            role: MenuItemRole.Danger);
        Assert.AreEqual(
            AppTheme.TextSecondary,
            DarkMenuRenderer.ResolveItemTextColor(disabled));

        using var checkedItem =
            new ToolStripMenuItem("Automatic")
            {
                Checked = true,
            };
        AppTheme.StyleMenuItem(checkedItem);
        Assert.AreEqual(
            AppTheme.AccentHover,
            DarkMenuRenderer.ResolveItemTextColor(checkedItem));
    }
}
'@
