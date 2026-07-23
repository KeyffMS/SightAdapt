using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ThemeTokenTests
{
    [TestMethod]
    public void DangerButtonStatesUseSemanticThemeTokens()
    {
        var resting = ModernButton.ResolveColors(
  ModernButtonStyle.Danger,
  enabled: true,
  hovered: false,
  pressed: false);
        var hovered = ModernButton.ResolveColors(
  ModernButtonStyle.Danger,
  enabled: true,
  hovered: true,
  pressed: false);
        var pressed = ModernButton.ResolveColors(
  ModernButtonStyle.Danger,
  enabled: true,
  hovered: false,
  pressed: true);

        Assert.AreEqual(AppTheme.DangerSoft, resting.Background);
        Assert.AreEqual(AppTheme.DangerBorder, resting.Border);
        Assert.AreEqual(AppTheme.DangerHover, hovered.Background);
        Assert.AreEqual(AppTheme.Danger, hovered.Border);
        Assert.AreEqual(AppTheme.DangerPressed, pressed.Background);
    }

    [TestMethod]
    public void GridUsesAlternateSurfaceThemeToken()
    {
        RunOnSta(() =>
        {
  using var grid = new DataGridView();
  AppTheme.StyleGrid(grid);
  Assert.AreEqual(
      AppTheme.SurfaceAlternate,
      grid.AlternatingRowsDefaultCellStyle.BackColor);
        });
    }

    [TestMethod]
    public void SelectorPaintFontResolutionReusesExistingFonts()
    {
        using var cellStyleFont = AppTheme.CreateUiFont(9.5f);
        using var gridFont = AppTheme.CreateUiFont(10f);

        Assert.AreSame(
  cellStyleFont,
  ModernSelectorComboBoxCell.ResolvePaintFont(
      cellStyleFont,
      gridFont));
        Assert.AreSame(
  gridFont,
  ModernSelectorComboBoxCell.ResolvePaintFont(null, gridFont));
        Assert.AreSame(
  Control.DefaultFont,
  ModernSelectorComboBoxCell.ResolvePaintFont(null, null));
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
  "The theme test did not finish in time.");
        if (failure is not null)
        {
  Assert.Fail(failure.ToString());
        }
    }
}
