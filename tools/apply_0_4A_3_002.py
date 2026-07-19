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
        raise RuntimeError(f"Missing anchor in {path}: {old[:100]!r}")
    write(path, content.replace(old, new, 1))

run("git", "config", "user.name", "SightAdapt architecture automation")
run("git", "config", "user.email", "actions@users.noreply.github.com")

replace("src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        "    private readonly VisualProfile _sourceProfile;\n", "")
replace("src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        """        _sourceProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (!profile.SupportsTuning)""",
        """        ArgumentNullException.ThrowIfNull(profile);
        if (!profile.SupportsTuning)""")
replace("src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        """    public static bool Edit(IWin32Window owner, VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(profile);

        using var editor = new VisualProfileEditorForm(profile);
        return editor.ShowDialog(owner) == DialogResult.OK;
    }""",
        """    public static VisualProfile? Edit(IWin32Window owner, VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(profile);

        using var editor = new VisualProfileEditorForm(profile);
        return editor.ShowDialog(owner) == DialogResult.OK
            ? editor._workingProfile.CreateWorkingCopy()
            : null;
    }""")
replace("src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        "        saveButton.Click += (_, _) => _sourceProfile.CopyTuningFrom(_workingProfile);\n",
        "")

anchor = """    public static int Delete(
        SightAdaptSettings settings,"""
method = """    public static void UpdateTuning(
        SightAdaptSettings settings,
        VisualProfile profile,
        VisualProfile values)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(values);
        EnsureCollections(settings);
        EnsureMember(settings, profile);

        if (!profile.SupportsTuning)
        {
            throw new InvalidOperationException(
                "Only editable Soft Invert profiles can be tuned.");
        }

        profile.OutputBlack = VisualProfileLimits.ClampFinite(
            values.OutputBlack,
            VisualProfileLimits.MinimumOutputBlack,
            VisualProfileLimits.MaximumOutputBlack,
            0.08f);
        profile.OutputWhite = VisualProfileLimits.ClampFinite(
            values.OutputWhite,
            VisualProfileLimits.MinimumOutputWhite,
            VisualProfileLimits.MaximumOutputWhite,
            0.92f);
        profile.Brightness = VisualProfileLimits.ClampFinite(
            values.Brightness,
            VisualProfileLimits.MinimumBrightness,
            VisualProfileLimits.MaximumBrightness,
            0.0f);
        profile.Contrast = VisualProfileLimits.ClampFinite(
            values.Contrast,
            VisualProfileLimits.MinimumContrast,
            VisualProfileLimits.MaximumContrast,
            1.0f);
        profile.Saturation = VisualProfileLimits.ClampFinite(
            values.Saturation,
            VisualProfileLimits.MinimumSaturation,
            VisualProfileLimits.MaximumSaturation,
            1.0f);
        profile.HueShiftDegrees = VisualProfileLimits.ClampFinite(
            values.HueShiftDegrees,
            VisualProfileLimits.MinimumHueShift,
            VisualProfileLimits.MaximumHueShift,
            0.0f);
    }

"""
replace("src/SightAdapt.Demo/VisualProfileManagementService.cs", anchor, method + anchor)

replace("src/SightAdapt.Demo/ConfigurationForm.cs",
        """        if (VisualProfileEditorForm.Edit(this, visualProfile))
        {
            _settingsChanged();
            RefreshProfiles();
        }""",
        """        var values = VisualProfileEditorForm.Edit(this, visualProfile);
        if (values is not null)
        {
            VisualProfileManagementService.UpdateTuning(
                _settings,
                visualProfile,
                values);
            _settingsChanged();
            RefreshProfiles();
        }""")

replace("src/SightAdapt.Demo/VisualProfileManagerForm.cs",
        """        if (VisualProfileEditorForm.Edit(this, profile))
        {
            SaveAndRefresh(profile.Id);
        }""",
        """        var values = VisualProfileEditorForm.Edit(this, profile);
        if (values is not null)
        {
            RunProfileOperation(() =>
            {
                VisualProfileManagementService.UpdateTuning(
                    _settings,
                    profile,
                    values);
                SaveAndRefresh(profile.Id);
            });
        }""")

write("tests/SightAdapt.Tests/VisualProfileTuningAuthorityTests.cs", """using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileTuningAuthorityTests
{
    [TestMethod]
    public void UpdateTuningClampsValuesBeforePersisting()
    {
        var settings = new SightAdaptSettings();
        var profile = VisualProfileManagementService.Create(settings, "Reader");
        var values = profile.CreateWorkingCopy();
        values.OutputBlack = -2.0f;
        values.OutputWhite = 4.0f;
        values.Brightness = float.NaN;
        values.Contrast = 8.0f;
        values.Saturation = -3.0f;
        values.HueShiftDegrees = 900.0f;

        VisualProfileManagementService.UpdateTuning(settings, profile, values);

        Assert.AreEqual(0.0f, profile.OutputBlack);
        Assert.AreEqual(1.0f, profile.OutputWhite);
        Assert.AreEqual(0.0f, profile.Brightness);
        Assert.AreEqual(2.0f, profile.Contrast);
        Assert.AreEqual(0.0f, profile.Saturation);
        Assert.AreEqual(180.0f, profile.HueShiftDegrees);
    }

    [TestMethod]
    public void UpdateTuningRejectsDetachedAndExactInvertProfiles()
    {
        var settings = new SightAdaptSettings();
        var detached = VisualProfile.CreateDefaultSoftInvert();
        var exact = settings.VisualProfiles.Single(
            profile => profile.Id == VisualProfile.DefaultInvertId);

        Assert.ThrowsException<InvalidOperationException>(() =>
            VisualProfileManagementService.UpdateTuning(
                settings,
                detached,
                detached.CreateWorkingCopy()));
        Assert.ThrowsException<InvalidOperationException>(() =>
            VisualProfileManagementService.UpdateTuning(
                settings,
                exact,
                exact.CreateWorkingCopy()));
    }

    [TestMethod]
    public void WorkingCopyDoesNotMutatePersistedProfile()
    {
        var source = VisualProfile.CreateDefaultSoftInvert();
        var working = source.CreateWorkingCopy();

        working.Brightness = 0.25f;

        Assert.AreEqual(0.0f, source.Brightness);
        Assert.AreEqual(0.25f, working.Brightness);
    }
}
""")

Path(__file__).unlink()
run("git", "add", "-A")
run("git", "commit", "-m", "Centralize visual profile tuning mutations [0.4A.3.002]")
run("git", "push", "origin", "HEAD:agent/alpha-v0.4")
