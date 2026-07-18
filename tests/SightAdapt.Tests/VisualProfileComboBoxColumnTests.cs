using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileComboBoxColumnTests
{
    [TestMethod]
    public void RebindingRestoresNameAndIdMembers()
    {
        var column = new SightAdapt.Demo.DataGridViewComboBoxColumn
        {
            DisplayMember = string.Empty,
            ValueMember = string.Empty,
        };

        column.DataSource = null;
        column.DataSource = new List<VisualProfile>
        {
            VisualProfile.CreateDefaultInvert(),
            VisualProfile.CreateDefaultSoftInvert(),
        };

        Assert.AreEqual(nameof(VisualProfile.Name), column.DisplayMember);
        Assert.AreEqual(nameof(VisualProfile.Id), column.ValueMember);
    }
}
