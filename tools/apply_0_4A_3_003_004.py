from pathlib import Path
import subprocess

ROOT = Path(__file__).resolve().parents[1]

def run(*args):
    return subprocess.run(args, cwd=ROOT, check=True, text=True, capture_output=True).stdout.strip()

def read(path):
    return (ROOT / path).read_text(encoding="utf-8")

def write(path, content):
    target = ROOT / path
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text(content, encoding="utf-8", newline="\n")

def replace(path, old, new):
    content = read(path)
    if old not in content:
        raise RuntimeError(f"Missing anchor in {path}: {old[:120]!r}")
    write(path, content.replace(old, new, 1))

run("git", "config", "user.name", "SightAdapt architecture automation")
run("git", "config", "user.email", "actions@users.noreply.github.com")

write("src/SightAdapt.Demo/AutomaticModeManagementService.cs", """namespace SightAdapt.Demo;

internal static class AutomaticModeManagementService
{
    public static bool Set(SightAdaptSettings settings, bool enabled)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.AutomaticMode == enabled)
        {
            return false;
        }

        settings.AutomaticMode = enabled;
        return true;
    }

    public static bool Enable(SightAdaptSettings settings)
    {
        return Set(settings, true);
    }

    public static bool Disable(SightAdaptSettings settings)
    {
        return Set(settings, false);
    }
}
""")
replace("src/SightAdapt.Demo/ConfigurationForm.cs",
        "        _settings.AutomaticMode = _automaticModeSwitch.Checked;\n",
        """        AutomaticModeManagementService.Set(
            _settings,
            _automaticModeSwitch.Checked);
""")
replace("src/SightAdapt.Demo/ConfigurationForm.cs",
        "        _settings.AutomaticMode = true;\n",
        "        AutomaticModeManagementService.Enable(_settings);\n")
replace("src/SightAdapt.Demo/SightAdaptContext.cs",
        "            _settings.AutomaticMode = true;\n",
        "            AutomaticModeManagementService.Enable(_settings);\n")
replace("src/SightAdapt.Demo/SightAdaptContext.cs",
        "        _settings.AutomaticMode = _automaticModeItem.Checked;\n",
        """        AutomaticModeManagementService.Set(
            _settings,
            _automaticModeItem.Checked);
""")
replace("src/SightAdapt.Demo/SightAdaptContext.cs",
        "        _settings.AutomaticMode = false;\n",
        "        AutomaticModeManagementService.Disable(_settings);\n")
replace("src/SightAdapt.Demo/SightAdaptContext.cs",
        """        if (!_settings.AutomaticMode ||
            currentState is ApplicationRunState.ManualActive or ApplicationRunState.Emergency ||
            !IsSupportedTarget(target))""",
        """        if (!_settings.AutomaticMode ||
            !_stateController.AllowsAutomaticActivation ||
            currentState == ApplicationRunState.ManualActive ||
            !IsSupportedTarget(target))""")
write("tests/SightAdapt.Tests/AutomaticModeManagementTests.cs", """using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class AutomaticModeManagementTests
{
    [TestMethod]
    public void SetReportsWhetherPersistedModeChanged()
    {
        var settings = new SightAdaptSettings
        {
            AutomaticMode = true,
        };

        Assert.IsFalse(AutomaticModeManagementService.Enable(settings));
        Assert.IsTrue(AutomaticModeManagementService.Disable(settings));
        Assert.IsFalse(settings.AutomaticMode);
        Assert.IsFalse(AutomaticModeManagementService.Disable(settings));
    }

    [TestMethod]
    public void EnableAndDisableShareOneMutationBoundary()
    {
        var settings = new SightAdaptSettings
        {
            AutomaticMode = false,
        };

        AutomaticModeManagementService.Enable(settings);
        Assert.IsTrue(settings.AutomaticMode);

        AutomaticModeManagementService.Disable(settings);
        Assert.IsFalse(settings.AutomaticMode);
    }
}
""")
Path(__file__).unlink()
run("git", "add", "-A")
run("git", "commit", "-m", "Centralize persisted automatic mode mutations [0.4A.3.003]")
run("git", "push", "origin", "HEAD:agent/alpha-v0.4")
