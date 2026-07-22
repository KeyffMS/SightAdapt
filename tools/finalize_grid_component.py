from pathlib import Path

root = Path(__file__).resolve().parents[1]

grid_path = root / "src/SightAdapt.Demo/ApplicationProfilesGrid.cs"
grid = grid_path.read_text(encoding="utf-8")
grid = grid.replace("\n    internal DataGridView Grid => _grid;\n", "")
grid = grid.replace(
    "        grid.SelectionChanged += (_, _) =>\n            SelectedApplicationChanged?.Invoke(this, EventArgs.Empty);\n",
    "        grid.SelectionChanged += (_, _) =>\n        {\n            if (!_binding)\n            {\n                SelectedApplicationChanged?.Invoke(this, EventArgs.Empty);\n            }\n        };\n")
grid_path.write_text(grid, encoding="utf-8")

selector_path = root / "src/SightAdapt.Demo/VisualProfileComboBoxColumn.cs"
selector = selector_path.read_text(encoding="utf-8")
selector = selector.replace(
    "            SelectOption(option, notifyGrid: true);\n",
    "            SelectOptionFromInput(option);\n",
    1)
selector = selector.replace(
    "        SelectOption(_options[nextIndex], notifyGrid: true);\n",
    "        SelectOptionFromInput(_options[nextIndex]);\n")
selector_path.write_text(selector, encoding="utf-8")

test_path = root / "tests/SightAdapt.Tests/ConfigurationGridCommitRegressionTests.cs"
test = test_path.read_text(encoding="utf-8")
test = test.replace(
    "            var grid = profilesGrid.Grid;\n",
    "            var grid = FindControl<DataGridView>(profilesGrid);\n")
test = test.replace(
    "            var grid = FindControl<ApplicationProfilesGrid>(form).Grid;\n",
    "            var grid = FindControl<DataGridView>(\n                FindControl<ApplicationProfilesGrid>(form));\n")
test_path.write_text(test, encoding="utf-8")
