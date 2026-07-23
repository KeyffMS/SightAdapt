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

Write-NewFile 'src/SightAdapt/ShellLauncher.cs' @'
using System.ComponentModel;
using System.Diagnostics;

namespace SightAdapt;

internal static class ShellLauncher
{
    public static bool TryOpenUrl(
        IWin32Window owner,
        string url)
    {
        return TryOpenUrl(
            owner,
            url,
            startInfo =>
            {
                Process.Start(startInfo);
            },
            ShowError);
    }

    internal static bool TryOpenUrl(
        IWin32Window owner,
        string url,
        Action<ProcessStartInfo> start,
        Action<IWin32Window, string> showError)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(start);
        ArgumentNullException.ThrowIfNull(showError);

        if (!TryCreateStartInfo(url, out var startInfo))
        {
            showError(
                owner,
                "The link is not a supported web address.");
            return false;
        }

        try
        {
            start(startInfo);
            return true;
        }
        catch (Exception exception) when (
            exception is Win32Exception or
            InvalidOperationException)
        {
            Debug.WriteLine(
                $"SightAdapt could not open '{url}': {exception}");
            showError(
                owner,
                $"The link could not be opened.\n\n{exception.Message}");
            return false;
        }
    }

    internal static bool TryCreateStartInfo(
        string? url,
        out ProcessStartInfo startInfo)
    {
        startInfo = null!;
        if (!Uri.TryCreate(
                url?.Trim(),
                UriKind.Absolute,
                out var uri) ||
            uri.Scheme is not ("http" or "https"))
        {
            return false;
        }

        startInfo = new ProcessStartInfo(uri.AbsoluteUri)
        {
            UseShellExecute = true,
        };
        return true;
    }

    private static void ShowError(
        IWin32Window owner,
        string message)
    {
        MessageBox.Show(
            owner,
            message,
            ProductInfo.DisplayName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
    }
}
'@

Replace-Exact 'src/SightAdapt/AboutForm.cs' @'
using System.ComponentModel;
using System.Diagnostics;

'@ ''
Replace-Exact 'src/SightAdapt/AboutForm.cs' `
    '    private static LinkLabel CreateRepositoryLink()' `
    '    private LinkLabel CreateRepositoryLink()'
Replace-Exact 'src/SightAdapt/AboutForm.cs' `
    '        link.LinkClicked += (_, _) => OpenRepository();' `
    '        link.LinkClicked += (_, _) => ShellLauncher.TryOpenUrl(this, ProductInfo.RepositoryUrl);'
Replace-Exact 'src/SightAdapt/AboutForm.cs' @'

    private static void OpenRepository()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ProductInfo.RepositoryUrl)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            MessageBox.Show(
                $"The repository could not be opened.\n\n{exception.Message}",
                ProductInfo.ProductName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
'@ "`n"

Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' @'
using System.ComponentModel;
using System.Diagnostics;
'@ ''
Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' `
    '    private static Control CreateProjectInfoCard()' `
    '    private Control CreateProjectInfoCard()'
Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' `
    '        repository.LinkClicked += (_, _) => OpenRepository();' `
    '        repository.LinkClicked += (_, _) => ShellLauncher.TryOpenUrl(this, ProductInfo.RepositoryUrl);'
Replace-Exact 'src/SightAdapt/ConfigurationForm.cs' @'

    private static void OpenRepository()
    {
        try
        {
            Process.Start(new ProcessStartInfo(ProductInfo.RepositoryUrl)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception exception) when (
            exception is Win32Exception or InvalidOperationException)
        {
            Debug.WriteLine($"SightAdapt could not open the repository: {exception}");
            MessageBox.Show(
                $"The repository could not be opened.\n\n{exception.Message}",
                ProductInfo.DisplayName,
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }
'@ "`n"

Write-NewFile 'tests/SightAdapt.Tests/ShellLauncherTests.cs' @'
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
'@
