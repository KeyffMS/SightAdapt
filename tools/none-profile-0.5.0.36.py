from pathlib import Path


def read(path: str) -> str:
    return Path(path).read_text(encoding="utf-8")


def write(path: str, content: str) -> None:
    with Path(path).open("w", encoding="utf-8", newline="\n") as target:
        target.write(content)


def replace_required(path: str, old: str, new: str, label: str) -> None:
    content = read(path)
    if old not in content:
        if new in content:
            return
        raise RuntimeError(f"Required source block not found: {label} ({path})")
    write(path, content.replace(old, new, 1))


def replace_count(path: str, old: str, new: str, count: int, label: str) -> None:
    content = read(path)
    actual = content.count(old)
    if actual != count:
        if content.count(new) >= count and actual == 0:
            return
        raise RuntimeError(
            f"Expected {count} occurrence(s) for {label} in {path}, found {actual}"
        )
    write(path, content.replace(old, new))


replace_required(
    "src/SightAdapt/VisualTransforms.cs",
    """internal interface IVisualTransform
{
    string Id { get; }

    MagColorEffect CreateColorEffect(VisualProfile profile);
}

internal sealed class InvertVisualTransform : IVisualTransform
""",
    """internal interface IVisualTransform
{
    string Id { get; }

    MagColorEffect CreateColorEffect(VisualProfile profile);
}

internal sealed class NoneVisualTransform : IVisualTransform
{
    public const string TransformId = "none";

    public string Id => TransformId;

    public MagColorEffect CreateColorEffect(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return ColorAffineMatrix
            .CreateScaleOffset(1.0f, 0.0f)
            .ToMagColorEffect();
    }
}

internal sealed class InvertVisualTransform : IVisualTransform
""",
    "None transform",
)

replace_required(
    "src/SightAdapt/VisualTransforms.cs",
    """        [
            new(
                InvertVisualTransform.TransformId,
""",
    """        [
            new(
                NoneVisualTransform.TransformId,
                VisualProfileDefaults.NoneName,
                SupportsTuning: false,
                Transform: new NoneVisualTransform()),
            new(
                InvertVisualTransform.TransformId,
""",
    "None transform catalog entry",
)

replace_required(
    "src/SightAdapt/VisualProfileDefaults.cs",
    """internal static class VisualProfileDefaults
{
    public const string ExactInvertName = "Exact invert";
    public const string SoftInvertName = "Soft invert";
""",
    """internal static class VisualProfileDefaults
{
    public const string NoneName = "None";
    public const string ExactInvertName = "Exact invert";
    public const string SoftInvertName = "Soft invert";
""",
    "None profile display name",
)

replace_required(
    "src/SightAdapt/VisualProfileDefaults.cs",
    """    public static VisualProfile CreateExactInvert()
    {
""",
    """    public static VisualProfile CreateNone()
    {
        var profile = new VisualProfile
        {
            Id = VisualProfile.DefaultNoneId,
            Name = NoneName,
            TransformId = NoneVisualTransform.TransformId,
        };
        ApplyTuning(profile, ExactInvertTuning);
        return profile;
    }

    public static VisualProfile CreateExactInvert()
    {
""",
    "None profile factory",
)

replace_required(
    "src/SightAdapt/VisualProfileDefaults.cs",
    """    public static bool CanonicalizeExactInvert(VisualProfile profile)
    {
""",
    """    public static bool CanonicalizeNone(VisualProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        var changed = !string.Equals(
                profile.Id,
                VisualProfile.DefaultNoneId,
                StringComparison.Ordinal) ||
            !string.Equals(
                profile.Name,
                NoneName,
                StringComparison.Ordinal) ||
            !string.Equals(
                profile.TransformId,
                NoneVisualTransform.TransformId,
                StringComparison.Ordinal);

        profile.Id = VisualProfile.DefaultNoneId;
        profile.Name = NoneName;
        profile.TransformId = NoneVisualTransform.TransformId;
        return ApplyTuningIfChanged(profile, ExactInvertTuning) || changed;
    }

    public static bool CanonicalizeExactInvert(VisualProfile profile)
    {
""",
    "None profile canonicalization",
)

replace_required(
    "src/SightAdapt/VisualProfileDefaults.cs",
    """        var tuning = string.Equals(
            profile.TransformId,
            InvertVisualTransform.TransformId,
            StringComparison.OrdinalIgnoreCase)
                ? ExactInvertTuning
                : NormalizeSoftInvertTuning(profile);
""",
    """        var tuning = string.Equals(
            profile.TransformId,
            SoftInvertVisualTransform.TransformId,
            StringComparison.OrdinalIgnoreCase)
                ? NormalizeSoftInvertTuning(profile)
                : ExactInvertTuning;
""",
    "None transform tuning normalization",
)

replace_required(
    "src/SightAdapt/ApplicationProfile.cs",
    """    public List<VisualProfile> VisualProfiles { get; set; } =
    [
        VisualProfile.CreateDefaultInvert(),
        VisualProfile.CreateDefaultSoftInvert(),
    ];
""",
    """    public List<VisualProfile> VisualProfiles { get; set; } =
    [
        VisualProfile.CreateDefaultInvert(),
        VisualProfile.CreateDefaultSoftInvert(),
        VisualProfile.CreateDefaultNone(),
    ];
""",
    "default None profile",
)

replace_required(
    "src/SightAdapt/ApplicationProfile.cs",
    """internal sealed class VisualProfile
{
    public const string DefaultInvertId = "default-invert";
    public const string DefaultSoftInvertId = "default-soft-invert";
""",
    """internal sealed class VisualProfile
{
    public const string DefaultNoneId = "default-none";
    public const string DefaultInvertId = "default-invert";
    public const string DefaultSoftInvertId = "default-soft-invert";
""",
    "None profile identifier",
)

replace_required(
    "src/SightAdapt/ApplicationProfile.cs",
    """    public static VisualProfile CreateDefaultInvert()
    {
        return VisualProfileDefaults.CreateExactInvert();
    }
""",
    """    public static VisualProfile CreateDefaultNone()
    {
        return VisualProfileDefaults.CreateNone();
    }

    public static VisualProfile CreateDefaultInvert()
    {
        return VisualProfileDefaults.CreateExactInvert();
    }
""",
    "None profile convenience factory",
)

replace_required(
    "src/SightAdapt/VisualProfilePolicy.cs",
    """        return string.Equals(
                   profileId,
                   VisualProfile.DefaultInvertId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   profileId,
                   VisualProfile.DefaultSoftInvertId,
                   StringComparison.OrdinalIgnoreCase);
""",
    """        return string.Equals(
                   profileId,
                   VisualProfile.DefaultNoneId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   profileId,
                   VisualProfile.DefaultInvertId,
                   StringComparison.OrdinalIgnoreCase) ||
               string.Equals(
                   profileId,
                   VisualProfile.DefaultSoftInvertId,
                   StringComparison.OrdinalIgnoreCase);
""",
    "None built-in policy",
)

replace_required(
    "src/SightAdapt/SettingsNormalizer.cs",
    """        context.AddProfile(softInvert);
    }
""",
    """        context.AddProfile(softInvert);

        var none = TakeProfile(
            context.RemainingProfiles,
            VisualProfile.DefaultNoneId);

        if (none is null)
        {
            none = VisualProfile.CreateDefaultNone();
            context.MarkChanged();
        }

        if (VisualProfileDefaults.CanonicalizeNone(none))
        {
            context.MarkChanged();
        }

        context.AddProfile(none);
    }
""",
    "None built-in normalization",
)

replace_count(
    "src/SightAdapt/SightAdapt.csproj",
    "0.5.0.35",
    "0.5.0.36",
    3,
    "0.5.0.36 version",
)

replace_required(
    "tests/SightAdapt.Tests/VisualTransformCatalogTests.cs",
    """        var catalog = VisualTransformCatalog.Default;

        Assert.IsTrue(catalog.IsSupported(InvertVisualTransform.TransformId));
""",
    """        var catalog = VisualTransformCatalog.Default;

        Assert.IsTrue(catalog.IsSupported(NoneVisualTransform.TransformId));
        Assert.IsFalse(catalog.SupportsTuning(NoneVisualTransform.TransformId));
        Assert.AreEqual(
            VisualProfileDefaults.NoneName,
            catalog.GetDisplayName(NoneVisualTransform.TransformId));
        Assert.AreEqual(
            NoneVisualTransform.TransformId,
            catalog.GetRequired(NoneVisualTransform.TransformId).Id);

        Assert.IsTrue(catalog.IsSupported(InvertVisualTransform.TransformId));
""",
    "None transform catalog test",
)

replace_required(
    "tests/SightAdapt.Tests/RuntimeMenuProfileTests.cs",
    """    private sealed class RuntimeTestContext : IDisposable
""",
    """    [TestMethod]
    public void NonePrimaryWithExplicitMenuProfileFlowsToRuntimeOverlay()
    {
        using var context = new RuntimeTestContext();
        context.AddMenuOnlyAssignment();

        context.Coordinator
            .HandleForegroundWindowChanged(
                context.Target);

        Assert.IsTrue(context.Overlay.IsActive);
        Assert.AreEqual(
            VisualProfile.DefaultNoneId,
            context.Overlay.PrimaryVisualProfileId);
        Assert.AreEqual(
            VisualProfile.DefaultInvertId,
            context.Overlay.MenuVisualProfileId);
    }

    private sealed class RuntimeTestContext : IDisposable
""",
    "menu-only runtime test",
)

replace_required(
    "tests/SightAdapt.Tests/RuntimeMenuProfileTests.cs",
    """            Assert.IsTrue(result.Succeeded);
            return result.Value;
        }

        public void Dispose()
""",
    """            Assert.IsTrue(result.Succeeded);
            return result.Value;
        }

        public void AddMenuOnlyAssignment()
        {
            var result = Settings.Commit(settings =>
            {
                var assignment =
                    ApplicationProfileManagementService
                        .AddOrEnable(
                            settings,
                            _identity)
                        .Profile;
                ApplicationProfileManagementService
                    .AssignVisualProfile(
                        settings,
                        assignment,
                        VisualProfile.DefaultNoneId);
                ApplicationProfileManagementService
                    .AssignMenuVisualProfile(
                        settings,
                        assignment,
                        VisualProfile.DefaultInvertId);
            });

            Assert.IsTrue(result.Succeeded);
        }

        public void Dispose()
""",
    "menu-only runtime setup",
)

replace_required(
    "tests/SightAdapt.Tests/VisualProfileManagementTests.cs",
    "Assert.AreEqual(3, settings.VisualProfiles.Count);",
    "Assert.AreEqual(4, settings.VisualProfiles.Count);",
    "built-in profile count after create",
)

replace_required(
    "tests/SightAdapt.Tests/SettingsStoreTests.cs",
    "Assert.AreEqual(3, settings.VisualProfiles.Count);",
    "Assert.AreEqual(4, settings.VisualProfiles.Count);",
    "recovered settings profile count",
)
replace_required(
    "tests/SightAdapt.Tests/SettingsStoreTests.cs",
    "Assert.AreEqual(5, settings.VisualProfiles.Count);",
    "Assert.AreEqual(6, settings.VisualProfiles.Count);",
    "normalized duplicate profile count",
)
replace_required(
    "tests/SightAdapt.Tests/SettingsStoreTests.cs",
    "Assert.AreEqual(5, settings.VisualProfiles\n            .Select(profile => profile.Id)",
    "Assert.AreEqual(6, settings.VisualProfiles\n            .Select(profile => profile.Id)",
    "normalized distinct profile count",
)
replace_required(
    "tests/SightAdapt.Tests/SettingsStoreTests.cs",
    "Assert.AreEqual(4, reloaded.VisualProfiles.Count);",
    "Assert.AreEqual(5, reloaded.VisualProfiles.Count);",
    "round-trip profile count",
)

write(
    "tests/SightAdapt.Tests/NoneVisualProfileTests.cs",
    """using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace SightAdapt.Tests;

[TestClass]
public sealed class NoneVisualProfileTests
{
    [TestMethod]
    public void DefaultSettingsExposeProtectedNoneProfile()
    {
        var settings = new SightAdaptSettings();
        var none = settings.VisualProfiles.Single(profile =>
            profile.Id == VisualProfile.DefaultNoneId);

        Assert.AreEqual(VisualProfileDefaults.NoneName, none.Name);
        Assert.AreEqual(NoneVisualTransform.TransformId, none.TransformId);
        Assert.IsFalse(none.SupportsTuning);
        Assert.IsTrue(VisualProfileManagementService.IsBuiltIn(none));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Rename(
                settings,
                none,
                "Changed"));
        Assert.ThrowsException<SettingsValidationException>(() =>
            VisualProfileManagementService.Delete(settings, none));
    }

    [TestMethod]
    public void NoneTransformProducesIdentityColorEffect()
    {
        var profile = VisualProfile.CreateDefaultNone();
        var transform = VisualTransformCatalog.Default.GetRequired(
            profile.TransformId);

        var effect = transform.CreateColorEffect(profile);

        Assert.AreEqual(1.0f, effect.M00);
        Assert.AreEqual(1.0f, effect.M11);
        Assert.AreEqual(1.0f, effect.M22);
        Assert.AreEqual(1.0f, effect.M33);
        Assert.AreEqual(1.0f, effect.M44);
        Assert.AreEqual(0.0f, effect.M01);
        Assert.AreEqual(0.0f, effect.M02);
        Assert.AreEqual(0.0f, effect.M10);
        Assert.AreEqual(0.0f, effect.M12);
        Assert.AreEqual(0.0f, effect.M20);
        Assert.AreEqual(0.0f, effect.M21);
        Assert.AreEqual(0.0f, effect.M40);
        Assert.AreEqual(0.0f, effect.M41);
        Assert.AreEqual(0.0f, effect.M42);
    }

    [TestMethod]
    public void ExplicitMenuProfileOverridesNoneApplicationProfile()
    {
        var settings = new SightAdaptSettings();
        var assignment = ApplicationProfileManagementService
            .AddOrEnable(
                settings,
                new ApplicationIdentity(
                    "Reader",
                    "reader.exe",
                    "C:\\Apps\\reader.exe"))
            .Profile;
        ApplicationProfileManagementService.AssignVisualProfile(
            settings,
            assignment,
            VisualProfile.DefaultNoneId);
        ApplicationProfileManagementService.AssignMenuVisualProfile(
            settings,
            assignment,
            VisualProfile.DefaultInvertId);

        var primary = ProfileResolver.ResolveVisualProfile(
            settings,
            assignment);
        var menu = ProfileResolver.ResolveMenuVisualProfile(
            settings,
            assignment);

        Assert.AreEqual(VisualProfile.DefaultNoneId, primary.Id);
        Assert.AreEqual(VisualProfile.DefaultInvertId, menu.Id);
    }

    [TestMethod]
    public void ExistingSchemaFiveSettingsGainNoneProfileOnce()
    {
        var settings = new SightAdaptSettings
        {
            SchemaVersion = 5,
            VisualProfiles =
            [
                VisualProfile.CreateDefaultInvert(),
                VisualProfile.CreateDefaultSoftInvert(),
            ],
        };

        Assert.IsTrue(SettingsStore.Normalize(settings));
        Assert.IsTrue(settings.VisualProfiles.Any(profile =>
            profile.Id == VisualProfile.DefaultNoneId));
        Assert.IsFalse(SettingsStore.Normalize(settings));
    }
}
""",
)

replace_required(
    "README.md",
    "- fixed `Exact invert` and editable `Soft invert` profiles;",
    "- built-in `None` (no correction), fixed `Exact invert`, and editable `Soft invert` profiles;",
    "README None profile bullet",
)
replace_required(
    "README.md",
    """New application assignments use Soft Invert, inherit that profile for native popup menus, and use client-area scope by default.
""",
    """New application assignments use Soft Invert, inherit that profile for native popup menus, and use client-area scope by default. Selecting `None` for the application and an explicit menu profile leaves the application unchanged while correcting only its native popup menus.
""",
    "README menu-only behavior",
)

replace_required(
    "docs/FEATURES.md",
    """## Visual profiles

### Exact invert
""",
    """## Visual profiles

### None

`None` is a fixed built-in no-correction profile. It uses an identity color matrix and cannot be edited, renamed, or deleted. Selecting `None` as the application profile while selecting an explicit native-menu profile keeps the application visually unchanged and applies correction only to supported popup menus.

### Exact invert
""",
    "FEATURES None section",
)
replace_required(
    "docs/FEATURES.md",
    """Each application assignment can optionally select a second visual profile for native Win32 popup-menu windows. Leaving the selector at `Same as application` stores no duplicate profile identifier and resolves the current application profile at runtime.
""",
    """Each application assignment can optionally select a second visual profile for native Win32 popup-menu windows. Leaving the selector at `Same as application` stores no duplicate profile identifier and resolves the current application profile at runtime. Choosing `None` for the application and a different menu profile provides a menu-only correction mode.
""",
    "FEATURES menu-only mode",
)

replace_required(
    "docs/ARCHITECTURE.md",
    """- with an active application overlay, it retargets the same instance;
- `Win32MenuWindowTracker` combines out-of-context WinEvent menu notifications with 75 ms `EnumWindows` verification;
""",
    """- with an active application overlay, it retargets the same instance;
- the built-in `None` profile retains the application session with an identity color effect, allowing an explicit menu profile to correct only native popup menus;
- `Win32MenuWindowTracker` combines out-of-context WinEvent menu notifications with 75 ms `EnumWindows` verification;
""",
    "ARCHITECTURE menu-only identity session",
)

print("Applied None visual profile and version 0.5.0.36 changes.")
