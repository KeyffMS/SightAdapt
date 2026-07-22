from pathlib import Path

root = Path(__file__).resolve().parents[1]

configuration_path = root / "src/SightAdapt.Demo/ConfigurationForm.cs"
configuration = configuration_path.read_text(encoding="utf-8")
configuration = configuration.replace(
    "    private static Label CreateInfoLabel(    private static Label CreateInfoLabel(string text, FontStyle style)\n",
    "    private static Label CreateInfoLabel(string text, FontStyle style)\n")
configuration = configuration.replace(
    "    private static ModernButton CreateButton(\n    private static ModernButton CreateButton(\n",
    "    private static ModernButton CreateButton(\n")
configuration = configuration.replace(
    "    private void UpdateSelectedProfileActions()\n    private void UpdateSelectedProfileActions()\n",
    "    private void UpdateSelectedProfileActions()\n")
configuration = configuration.replace(
    "    private void AddCurrentApplication()\n    private void AddCurrentApplication()\n",
    "    private void AddCurrentApplication()\n")
configuration_path.write_text(configuration, encoding="utf-8")

selector_path = root / "src/SightAdapt.Demo/VisualProfileComboBoxColumn.cs"
selector = selector_path.read_text(encoding="utf-8")
selector = selector.replace("        clone._attachedGrid = null;\n", "")
selector = selector.replace(
    "internal sealed class ModernVisualProfileComboBoxCellinternal sealed class ModernVisualProfileComboBoxCell :",
    "internal sealed class ModernVisualProfileComboBoxCell :")
selector_path.write_text(selector, encoding="utf-8")
