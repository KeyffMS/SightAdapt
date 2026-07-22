from pathlib import Path
import subprocess

root = Path.cwd()
path = root / "src" / "SightAdapt.Demo" / "VisualProfileComboBoxColumn.cs"
source = path.read_text(encoding="utf-8")
old = "_selected?.Id ?? string.Empty"
count = source.count(old)
if count != 2:
    raise RuntimeError(f"Expected two ID formatted-value getters, found {count}")
source = source.replace(old, "_selected?.Name ?? string.Empty")
path.write_text(source, encoding="utf-8")

script = root / "tools" / "apply_profile_formatted_value_fix.py"
if script.exists():
    script.unlink()

original_build = subprocess.check_output(
    ["git", "show", "HEAD^:.github/workflows/build.yml"],
    text=True)
(root / ".github" / "workflows" / "build.yml").write_text(
    original_build,
    encoding="utf-8")
