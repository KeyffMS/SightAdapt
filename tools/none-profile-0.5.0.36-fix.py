from pathlib import Path


def replace_required(path: str, old: str, new: str, label: str) -> None:
    file_path = Path(path)
    content = file_path.read_text(encoding="utf-8")
    if old not in content:
        if new in content:
            return
        raise RuntimeError(f"Required block not found: {label} ({path})")
    with file_path.open("w", encoding="utf-8", newline="\n") as target:
        target.write(content.replace(old, new, 1))


replace_required(
    "tests/SightAdapt.Tests/NoneVisualProfileTests.cs",
    r'"C:\Apps\reader.exe"',
    r'@"C:\Apps\reader.exe"',
    "None profile test path",
)
replace_required(
    "tests/SightAdapt.Tests/MutationPersistenceRegressionTests.cs",
    "Assert.AreEqual(2, settings.VisualProfiles.Count);",
    "Assert.AreEqual(3, settings.VisualProfiles.Count);",
    "remaining built-in profile count",
)
replace_required(
    "tests/SightAdapt.Tests/SettingsCoordinatorTests.cs",
    "Assert.AreEqual(2, current.VisualProfiles.Count);",
    "Assert.AreEqual(3, current.VisualProfiles.Count);",
    "defensive snapshot built-in profile count",
)
