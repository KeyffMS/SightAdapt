using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileEditingControlLifecycleTests
{
    [TestMethod]
    public void SelectorCloseQueuesCellEditCompletionWithoutRefocusingEditor()
    {
        var source = File.ReadAllText(Path.Combine(
            SourceDirectory,
            "VisualProfileComboBoxColumn.cs"));

        StringAssert.Contains(
            source,
            "_dropDown.Closed += (_, _) =>");
        StringAssert.Contains(
            source,
            "QueueGridEditCompletion();");
        StringAssert.Contains(
            source,
            "grid.BeginInvoke");
        StringAssert.Contains(
            source,
            "grid.EndEdit()");

        var commitStart = source.IndexOf(
            "private void CommitListSelection()",
            StringComparison.Ordinal);
        var commitEnd = source.IndexOf(
            "private void ListKeyDown",
            commitStart,
            StringComparison.Ordinal);

        Assert.IsTrue(commitStart >= 0);
        Assert.IsTrue(commitEnd > commitStart);
        var commitSource = source[commitStart..commitEnd];
        Assert.IsFalse(
            commitSource.Contains("Focus();", StringComparison.Ordinal),
            "Selecting a profile must not return focus to the editing control and keep the grid locked in edit mode.");
    }

    private static string SourceDirectory =>
        Path.Combine(RepositoryRoot, "src", "SightAdapt.Demo");

    private static string RepositoryRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (Directory.Exists(Path.Combine(
                        directory.FullName,
                        "src",
                        "SightAdapt.Demo")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException(
                "The SightAdapt repository root could not be located.");
        }
    }
}
