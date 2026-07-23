using System.Drawing;
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