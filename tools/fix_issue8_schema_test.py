from pathlib import Path

path = Path(__file__).resolve().parents[1] / "tests/SightAdapt.Tests/SettingsStoreTests.cs"
text = path.read_text(encoding="utf-8")
text = text.replace(
    'StringAssert.Contains(json, "\\\"schemaVersion\\\": 3");',
    'StringAssert.Contains(json, "\\\"schemaVersion\\\": 4");',
    1)
text = text.replace(
    'StringAssert.Contains(json, "\\\"visualProfileId\\\": \\\"default-soft-invert\\\"");',
    'StringAssert.Contains(json, "\\\"visualProfileId\\\": \\\"default-soft-invert\\\"");\n        StringAssert.Contains(json, "\\\"overlayScope\\\": \\\"client-area\\\"");',
    1)
path.write_text(text, encoding="utf-8", newline="\n")
