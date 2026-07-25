from pathlib import Path

path = Path("tests/SightAdapt.Tests/NoneVisualProfileTests.cs")
content = path.read_text(encoding="utf-8")
old = r'"C:\Apps\reader.exe"'
new = r'@"C:\Apps\reader.exe"'
if old not in content:
    if new in content:
        raise SystemExit(0)
    raise RuntimeError("The generated None profile test path was not found.")
with path.open("w", encoding="utf-8", newline="\n") as target:
    target.write(content.replace(old, new, 1))
