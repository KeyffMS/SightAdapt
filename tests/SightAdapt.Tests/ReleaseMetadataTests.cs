using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ReleaseMetadataTests
{
    [TestMethod]
    public void AssemblyAndDocumentationUseCanonicalReleaseMetadata()
    {
        var properties = XDocument.Load(Path.Combine(
            RepositoryLayout.Root,
            "Directory.Build.props"));
        var group = properties.Root!.Element("PropertyGroup")!;
        var productVersion = group.Element(
            "SightAdaptProductVersion")!.Value;
        var fileVersion = group.Element(
            "SightAdaptFileVersion")!.Value;
        var milestone = group.Element(
            "SightAdaptMilestone")!.Value;
        var schema = int.Parse(group.Element(
            "SightAdaptSettingsSchema")!.Value,
            System.Globalization.CultureInfo.InvariantCulture);
        var readme = File.ReadAllText(Path.Combine(
            RepositoryLayout.Root,
            "README.md"));

        Assert.AreEqual(productVersion, ProductInfo.VersionLabel);
        Assert.AreEqual(milestone, ProductInfo.MilestoneLabel);
        Assert.AreEqual(schema, ProductInfo.SettingsSchemaVersion);
        Assert.AreEqual(schema, SightAdaptSettings.CurrentSchemaVersion);
        StringAssert.Contains(readme, $"Product version: {productVersion}");
        StringAssert.Contains(readme, $"File version:    {fileVersion}");
        StringAssert.Contains(readme, $"Milestone:       {milestone}");
        StringAssert.Contains(readme, $"Settings schema: {schema}");
    }

    [TestMethod]
    public void BuildWorkflowUsesCanonicalArtifactOutput()
    {
        var workflow = File.ReadAllText(Path.Combine(
            RepositoryLayout.Root,
            ".github",
            "workflows",
            "build.yml"));

        StringAssert.Contains(
            workflow,
            "verify-release-metadata.ps1 -WriteGitHubOutput");
        StringAssert.Contains(
            workflow,
            "steps.release.outputs.artifact_name");
        Assert.IsFalse(workflow.Contains(
            "SightAdapt-0.5-Alpha-win-x64",
            StringComparison.Ordinal));

        var buildDocumentation = File.ReadAllText(Path.Combine(
            RepositoryLayout.Root,
            "docs",
            "BUILD.md"));
        StringAssert.Contains(
            buildDocumentation,
            "Directory.Build.props");
        Assert.IsFalse(
            buildDocumentation.Contains(
                "0.5.0-alpha.1+<commit>",
                StringComparison.Ordinal));
        Assert.IsFalse(
            buildDocumentation.Contains(
                "FileVersion:    0.5.0.0",
                StringComparison.Ordinal));
    }
}
