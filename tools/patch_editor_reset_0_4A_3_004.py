from pathlib import Path
import subprocess

root = Path(__file__).resolve().parents[1]
path = root / "src/SightAdapt.Demo/VisualProfileEditorForm.cs"
text = path.read_text(encoding="utf-8")
old = """        var defaults = VisualProfile.CreateDefaultSoftInvert();
        _workingProfile.CopyTuningFrom(defaults);"""
new = """        VisualProfileDefaults.ApplyTuning(
            _workingProfile,
            VisualProfileDefaults.SoftInvertTuning);"""
if old not in text:
    raise RuntimeError("Visual profile editor reset anchor was not found")
path.write_text(text.replace(old, new, 1), encoding="utf-8", newline="\n")
Path(__file__).unlink()
subprocess.run(["git", "config", "user.name", "SightAdapt architecture automation"], cwd=root, check=True)
subprocess.run(["git", "config", "user.email", "actions@users.noreply.github.com"], cwd=root, check=True)
subprocess.run(["git", "add", "-A"], cwd=root, check=True)
subprocess.run(["git", "commit", "-m", "Complete canonical defaults integration [0.4A.3.004]"], cwd=root, check=True)
subprocess.run(["git", "push", "origin", "HEAD:agent/alpha-v0.4"], cwd=root, check=True)
