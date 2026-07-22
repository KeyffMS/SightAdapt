from pathlib import Path

root = Path(__file__).resolve().parents[1]
for relative in [
    "tests/SightAdapt.Tests/OverlayScopeTests.cs",
    "tests/SightAdapt.Tests/ConfigurationGridCommitRegressionTests.cs",
]:
    path = root / relative
    text = path.read_text(encoding="utf-8")
    text = text.replace('@"C:\\Apps\neader.exe"', '@"C:\\Apps\\reader.exe"')
    path.write_text(text, encoding="utf-8", newline="\n")
