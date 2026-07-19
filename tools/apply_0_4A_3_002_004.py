from __future__ import annotations

import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]

def run(*args: str) -> str:
    result = subprocess.run(
        list(args),
        cwd=ROOT,
        check=True,
        text=True,
        capture_output=True,
    )
    return result.stdout.strip()


def read(relative: str) -> str:
    return (ROOT / relative).read_text(encoding="utf-8")


def write(relative: str, content: str) -> None:
    path = ROOT / relative
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content, encoding="utf-8", newline="\n")


def replace_exact(relative: str, old: str, new: str, count: int = 1) -> None:
    content = read(relative)
    actual = content.count(old)
    if actual < count:
        raise RuntimeError(
            f"{relative}: expected at least {count} occurrence(s), found {actual}: {old[:100]!r}"
        )
    write(relative, content.replace(old, new, count))


def replace_regex(relative: str, pattern: str, replacement: str, count: int = 1) -> None:
    content = read(relative)
    updated, actual = re.subn(pattern, replacement, content, count=count, flags=re.MULTILINE | re.DOTALL)
    if actual != count:
        raise RuntimeError(f"{relative}: regex expected {count}, replaced {actual}: {pattern}")
    write(relative, updated)


def commit(message: str) -> None:
    run("git", "add", "-A")
    status = run("git", "status", "--porcelain")
    if not status:
        raise RuntimeError(f"No changes staged for commit: {message}")
    run("git", "commit", "-m", message)


def stage_002() -> None:
    replace_exact(
        "src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        "    private readonly VisualProfile _sourceProfile;\n",
        "",
    )
    replace_exact(
        "src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        """        _sourceProfile = profile ?? throw new ArgumentNullException(nameof(profile));
        if (!profile.SupportsTuning)""",
        """        ArgumentNullException.ThrowIfNull(profile);
        if (!profile.SupportsTuning)""",
    )
    replace_exact(
        "src/SightAdapt.Demo/VisualProfileEditorForm.cs",
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
    }""",
    )
    replace_exact(
        "src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        "        saveButton.Click += (_, _) => _sourceProfile.CopyTuningFrom(_workingProfile);\n",
        "",
    )

    insert_anchor = """    public static int Delete(
        SightAdaptSettings settings,"""
    update_method = """    public static void UpdateTuning(
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
    replace_exact(
        "src/SightAdapt.Demo/VisualProfileManagementService.cs",
        insert_anchor,
        update_method + insert_anchor,
    )

    replace_exact(
        "src/SightAdapt.Demo/ConfigurationForm.cs",
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
        }""",
    )
    replace_exact(
        "src/SightAdapt.Demo/VisualProfileManagerForm.cs",
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
        }""",
    )

    write(
        "tests/SightAdapt.Tests/VisualProfileTuningAuthorityTests.cs",
        """using Microsoft.VisualStudio.TestTools.UnitTesting;

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
""",
    )
    commit("Centralize visual profile tuning mutations [0.4A.3.002]")


def stage_003() -> None:
    write(
        "src/SightAdapt.Demo/AutomaticModeManagementService.cs",
        """namespace SightAdapt.Demo;

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
""",
    )

    replace_exact(
        "src/SightAdapt.Demo/ConfigurationForm.cs",
        "        _settings.AutomaticMode = _automaticModeSwitch.Checked;\n",
        """        AutomaticModeManagementService.Set(
            _settings,
            _automaticModeSwitch.Checked);
""",
    )
    replace_exact(
        "src/SightAdapt.Demo/ConfigurationForm.cs",
        "        _settings.AutomaticMode = true;\n",
        "        AutomaticModeManagementService.Enable(_settings);\n",
    )
    replace_exact(
        "src/SightAdapt.Demo/SightAdaptContext.cs",
        "            _settings.AutomaticMode = true;\n",
        "            AutomaticModeManagementService.Enable(_settings);\n",
    )
    replace_exact(
        "src/SightAdapt.Demo/SightAdaptContext.cs",
        "        _settings.AutomaticMode = _automaticModeItem.Checked;\n",
        """        AutomaticModeManagementService.Set(
            _settings,
            _automaticModeItem.Checked);
""",
    )
    replace_exact(
        "src/SightAdapt.Demo/SightAdaptContext.cs",
        "        _settings.AutomaticMode = false;\n",
        "        AutomaticModeManagementService.Disable(_settings);\n",
    )
    replace_exact(
        "src/SightAdapt.Demo/SightAdaptContext.cs",
        """        if (!_settings.AutomaticMode ||
            currentState is ApplicationRunState.ManualActive or ApplicationRunState.Emergency ||
            !IsSupportedTarget(target))""",
        """        if (!_settings.AutomaticMode ||
            !_stateController.AllowsAutomaticActivation ||
            currentState == ApplicationRunState.ManualActive ||
            !IsSupportedTarget(target))""",
    )

    write(
        "tests/SightAdapt.Tests/AutomaticModeManagementTests.cs",
        """using Microsoft.VisualStudio.TestTools.UnitTesting;

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
    public void EnableAndDisableShareTheSameMutationBoundary()
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
""",
    )
    commit("Centralize persisted automatic mode mutations [0.4A.3.003]")


def stage_004() -> None:
    write(
        "src/SightAdapt.Demo/VisualProfileDefaults.cs",
        """namespace SightAdapt.Demo;

internal readonly record struct VisualProfileTuning(
    float OutputBlack,
    float OutputWhite,
    float Brightness,
    float Contrast,
    float Saturation,
    float HueShiftDegrees);

internal static class VisualProfileDefaults
{
    public const string ExactInvertName = "Exact invert";
    public const string SoftInvertName = "Soft invert";

    public const float ExactOutputBlack = 0.0f;
    public const float ExactOutputWhite = 1.0f;
    public const float ExactBrightness = 0.0f;
    public const float ExactContrast = 1.0f;
    public const float ExactSaturation = 1.0f;
    public const float ExactHueShiftDegrees = 0.0f;

    public const float SoftOutputBlack = 0.08f;
    public const float SoftOutputWhite = 0.92f;
    public const float SoftBrightness = 0.0f;
    public const float SoftContrast = 1.0f;
    public const float SoftSaturation = 1.0f;
    public const float SoftHueShiftDegrees = 0.0f;

    public static VisualProfileTuning ExactInvertTuning { get; } = new(
        ExactOutputBlack,
        ExactOutputWhite,
        ExactBrightness,
        ExactContrast,
        ExactSaturation,
        ExactHueShiftDegrees);

    public static VisualProfileTuning SoftInvertTuning { get; } = new(
        SoftOutputBlack,
        SoftOutputWhite,
        SoftBrightness,
        SoftContrast,
        SoftSaturation,
        SoftHueShiftDegrees);

    public static VisualProfile CreateExactInvert()
    {
        var profile = new VisualProfile
        {
            Id = VisualProfile.DefaultInvertId,
            Name = ExactInvertName,
            TransformId = InvertVisualTransform.TransformId,
        };
        ApplyTuning(profile, ExactInvertTuning);
        return profile;
    }

    public static VisualProfile CreateSoftInvert()
    {
        var profile = new VisualProfile
        {
            Id = VisualProfile.DefaultSoftInvertId,
            Name = SoftInvertName,
            TransformId = SoftInvertVisualTransform.TransformId,
        };
        ApplyTuning(profile, SoftInvertTuning);
        return profile;
    }

    public static bool CanonicalizeExactInvert(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var changed = !string.Equals(
                profile.Id,
                VisualProfile.DefaultInvertId,
                StringComparison.Ordinal) ||
            !string.Equals(profile.Name, ExactInvertName, StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                InvertVisualTransform.TransformId,
                StringComparison.Ordinal);

        profile.Id = VisualProfile.DefaultInvertId;
        profile.Name = ExactInvertName;
        profile.TransformId = InvertVisualTransform.TransformId;
        return ApplyTuningIfChanged(profile, ExactInvertTuning) || changed;
    }

    public static bool CanonicalizeSoftInvert(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var changed = !string.Equals(
                profile.Id,
                VisualProfile.DefaultSoftInvertId,
                StringComparison.Ordinal) ||
            !string.Equals(profile.Name, SoftInvertName, StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                SoftInvertVisualTransform.TransformId,
                StringComparison.Ordinal);

        profile.Id = VisualProfile.DefaultSoftInvertId;
        profile.Name = SoftInvertName;
        profile.TransformId = SoftInvertVisualTransform.TransformId;
        return ApplyTuningIfChanged(
            profile,
            NormalizeSoftInvertTuning(profile)) || changed;
    }

    public static bool NormalizeTuningForTransform(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var tuning = string.Equals(
            profile.TransformId,
            InvertVisualTransform.TransformId,
            StringComparison.OrdinalIgnoreCase)
                ? ExactInvertTuning
                : NormalizeSoftInvertTuning(profile);

        return ApplyTuningIfChanged(profile, tuning);
    }

    public static VisualProfileTuning NormalizeSoftInvertTuning(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        return new VisualProfileTuning(
            VisualProfileLimits.ClampFinite(
                profile.OutputBlack,
                VisualProfileLimits.MinimumOutputBlack,
                VisualProfileLimits.MaximumOutputBlack,
                SoftOutputBlack),
            VisualProfileLimits.ClampFinite(
                profile.OutputWhite,
                VisualProfileLimits.MinimumOutputWhite,
                VisualProfileLimits.MaximumOutputWhite,
                SoftOutputWhite),
            VisualProfileLimits.ClampFinite(
                profile.Brightness,
                VisualProfileLimits.MinimumBrightness,
                VisualProfileLimits.MaximumBrightness,
                SoftBrightness),
            VisualProfileLimits.ClampFinite(
                profile.Contrast,
                VisualProfileLimits.MinimumContrast,
                VisualProfileLimits.MaximumContrast,
                SoftContrast),
            VisualProfileLimits.ClampFinite(
                profile.Saturation,
                VisualProfileLimits.MinimumSaturation,
                VisualProfileLimits.MaximumSaturation,
                SoftSaturation),
            VisualProfileLimits.ClampFinite(
                profile.HueShiftDegrees,
                VisualProfileLimits.MinimumHueShift,
                VisualProfileLimits.MaximumHueShift,
                SoftHueShiftDegrees));
    }

    public static void ApplyTuning(
        VisualProfile profile,
        VisualProfileTuning tuning)
    {
        ArgumentNullException.ThrowIfNull(profile);

        profile.OutputBlack = tuning.OutputBlack;
        profile.OutputWhite = tuning.OutputWhite;
        profile.Brightness = tuning.Brightness;
        profile.Contrast = tuning.Contrast;
        profile.Saturation = tuning.Saturation;
        profile.HueShiftDegrees = tuning.HueShiftDegrees;
    }

    private static bool ApplyTuningIfChanged(
        VisualProfile profile,
        VisualProfileTuning tuning)
    {
        var changed = profile.OutputBlack != tuning.OutputBlack ||
            profile.OutputWhite != tuning.OutputWhite ||
            profile.Brightness != tuning.Brightness ||
            profile.Contrast != tuning.Contrast ||
            profile.Saturation != tuning.Saturation ||
            profile.HueShiftDegrees != tuning.HueShiftDegrees;

        ApplyTuning(profile, tuning);
        return changed;
    }
}
""",
    )

    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        "    public string VisualProfileId { get; set; } = VisualProfile.DefaultSoftInvertId;",
        "    public string VisualProfileId { get; set; } = VisualProfilePolicy.NewAssignmentProfileId;",
    )
    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        '    public string Name { get; set; } = "Soft invert";',
        "    public string Name { get; set; } = VisualProfileDefaults.SoftInvertName;",
    )
    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        "    public float OutputBlack { get; set; } = 0.08f;",
        "    public float OutputBlack { get; set; } = VisualProfileDefaults.SoftOutputBlack;",
    )
    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        "    public float OutputWhite { get; set; } = 0.92f;",
        "    public float OutputWhite { get; set; } = VisualProfileDefaults.SoftOutputWhite;",
    )
    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        "    public float Brightness { get; set; }",
        "    public float Brightness { get; set; } = VisualProfileDefaults.SoftBrightness;",
    )
    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        "    public float Contrast { get; set; } = 1.0f;",
        "    public float Contrast { get; set; } = VisualProfileDefaults.SoftContrast;",
    )
    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        "    public float Saturation { get; set; } = 1.0f;",
        "    public float Saturation { get; set; } = VisualProfileDefaults.SoftSaturation;",
    )
    replace_exact(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        "    public float HueShiftDegrees { get; set; }",
        "    public float HueShiftDegrees { get; set; } = VisualProfileDefaults.SoftHueShiftDegrees;",
    )
    replace_regex(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        r"""    public void CopyTuningFrom\(VisualProfile source\)\n    \{.*?\n    \}\n\n""",
        "",
    )
    replace_regex(
        "src/SightAdapt.Demo/ApplicationProfile.cs",
        r"""    public static VisualProfile CreateDefaultInvert\(\)\n    \{.*?\n    \}\n\n    public static VisualProfile CreateDefaultSoftInvert\(\)\n    \{.*?\n    \}""",
        """    public static VisualProfile CreateDefaultInvert()
    {
        return VisualProfileDefaults.CreateExactInvert();
    }

    public static VisualProfile CreateDefaultSoftInvert()
    {
        return VisualProfileDefaults.CreateSoftInvert();
    }""",
    )

    replace_regex(
        "src/SightAdapt.Demo/VisualProfileManagementService.cs",
        r"""        profile\.OutputBlack = VisualProfileLimits\.ClampFinite\(.*?        profile\.HueShiftDegrees = VisualProfileLimits\.ClampFinite\(\n            values\.HueShiftDegrees,\n            VisualProfileLimits\.MinimumHueShift,\n            VisualProfileLimits\.MaximumHueShift,\n            0\.0f\);""",
        """        VisualProfileDefaults.ApplyTuning(
            profile,
            VisualProfileDefaults.NormalizeSoftInvertTuning(values));""",
    )

    replace_exact(
        "src/SightAdapt.Demo/VisualProfileEditorForm.cs",
        """        var defaults = VisualProfile.CreateDefaultSoftInvert();
        _workingProfile.CopyTuningFrom(defaults);""",
        """        VisualProfileDefaults.ApplyTuning(
            _workingProfile,
            VisualProfileDefaults.SoftInvertTuning);""",
    )

    replace_regex(
        "src/SightAdapt.Demo/VisualTransforms.cs",
        r"""        var outputBlack = VisualProfileLimits\.ClampFinite\(.*?        var hueShift = VisualProfileLimits\.ClampFinite\(\n            profile\.HueShiftDegrees,\n            VisualProfileLimits\.MinimumHueShift,\n            VisualProfileLimits\.MaximumHueShift,\n            0\.0f\);""",
        """        var tuning = VisualProfileDefaults.NormalizeSoftInvertTuning(profile);
        var outputBlack = tuning.OutputBlack;
        var outputWhite = tuning.OutputWhite;
        var brightness = tuning.Brightness;
        var contrast = tuning.Contrast;
        var saturation = tuning.Saturation;
        var hueShift = tuning.HueShiftDegrees;""",
    )

    write(
        "tests/SightAdapt.Tests/VisualProfileDefaultsTests.cs",
        """using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Demo.Tests;

[TestClass]
public sealed class VisualProfileDefaultsTests
{
    [TestMethod]
    public void FactoriesUseCanonicalNamesAndTuning()
    {
        var exact = VisualProfile.CreateDefaultInvert();
        var soft = VisualProfile.CreateDefaultSoftInvert();

        Assert.AreEqual(VisualProfileDefaults.ExactInvertName, exact.Name);
        Assert.AreEqual(VisualProfileDefaults.ExactOutputBlack, exact.OutputBlack);
        Assert.AreEqual(VisualProfileDefaults.ExactOutputWhite, exact.OutputWhite);
        Assert.AreEqual(VisualProfileDefaults.SoftInvertName, soft.Name);
        Assert.AreEqual(VisualProfileDefaults.SoftOutputBlack, soft.OutputBlack);
        Assert.AreEqual(VisualProfileDefaults.SoftOutputWhite, soft.OutputWhite);
    }

    [TestMethod]
    public void CanonicalExactInvertRestoresIdentityAndTuning()
    {
        var profile = VisualProfile.CreateDefaultInvert();
        profile.Name = "Broken";
        profile.TransformId = SoftInvertVisualTransform.TransformId;
        profile.OutputBlack = 0.2f;

        var changed = VisualProfileDefaults.CanonicalizeExactInvert(profile);

        Assert.IsTrue(changed);
        Assert.AreEqual(VisualProfileDefaults.ExactInvertName, profile.Name);
        Assert.AreEqual(InvertVisualTransform.TransformId, profile.TransformId);
        Assert.AreEqual(VisualProfileDefaults.ExactOutputBlack, profile.OutputBlack);
    }

    [TestMethod]
    public void SoftTuningNormalizationUsesCanonicalFallbacks()
    {
        var profile = VisualProfile.CreateDefaultSoftInvert();
        profile.OutputBlack = float.NaN;
        profile.OutputWhite = float.PositiveInfinity;

        var tuning = VisualProfileDefaults.NormalizeSoftInvertTuning(profile);

        Assert.AreEqual(VisualProfileDefaults.SoftOutputBlack, tuning.OutputBlack);
        Assert.AreEqual(VisualProfileDefaults.SoftOutputWhite, tuning.OutputWhite);
    }
}
""",
    )
    commit("Centralize canonical visual profile defaults [0.4A.3.004]")


def main() -> None:
    run("git", "config", "user.name", "SightAdapt architecture automation")
    run("git", "config", "user.email", "actions@users.noreply.github.com")
    stage_002()
    stage_003()
    stage_004()
    Path(__file__).unlink()
    run("git", "add", "-A")
    run("git", "commit", "-m", "Remove temporary 0.4A.3.002-004 migration helper")
    run("git", "push", "origin", "HEAD:agent/alpha-v0.4")

if __name__ == "__main__":
    main()
