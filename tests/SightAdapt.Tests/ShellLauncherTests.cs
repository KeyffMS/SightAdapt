using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class ShellLauncherTests
{
    [TestMethod]
    public void ValidWebAddressUsesShellExecution()
    {
        var owner = new TestWindow();
        ProcessStartInfo? captured = null;
        var errors = new List<string>();

        var opened = ShellLauncher.TryOpenUrl(
            owner,
            " https://github.com/KeyffMS/SightAdapt ",
            value => captured = value,
            (_, message) => errors.Add(message));

        Assert.IsTrue(opened);
        Assert.IsNotNull(captured);
        Assert.IsTrue(captured.UseShellExecute);
        Assert.AreEqual(
            "https://github.com/KeyffMS/SightAdapt",
            captured.FileName.TrimEnd('/'));
        Assert.AreEqual(0, errors.Count);
    }

    [TestMethod]
    public void UnsupportedSchemeIsRejectedBeforeLaunch()
    {
        var owner = new TestWindow();
        var launches = 0;
        var errors = new List<string>();

        var opened = ShellLauncher.TryOpenUrl(
            owner,
            "file:///C:/Windows/System32",
            _ => launches++,
            (_, message) => errors.Add(message));

        Assert.IsFalse(opened);
        Assert.AreEqual(0, launches);
        Assert.AreEqual(1, errors.Count);
    }

    [TestMethod]
    public void ExpectedShellFailureUsesSharedErrorPath()
    {
        var owner = new TestWindow();
        var errors = new List<string>();

        var opened = ShellLauncher.TryOpenUrl(
            owner,
            "https://example.test/",
            _ => throw new Win32Exception("No browser"),
            (actualOwner, message) =>
            {
                Assert.AreSame(owner, actualOwner);
                errors.Add(message);
            });

        Assert.IsFalse(opened);
        Assert.AreEqual(1, errors.Count);
        StringAssert.Contains(errors[0], "No browser");
    }

    [TestMethod]
    public void StartInfoFactoryRejectsMissingAddress()
    {
        Assert.IsFalse(
            ShellLauncher.TryCreateStartInfo(
                null,
                out _));
        Assert.IsFalse(
            ShellLauncher.TryCreateStartInfo(
                "",
                out _));
    }

    private sealed class TestWindow : IWin32Window
    {
        public nint Handle => nint.Zero;
    }
}