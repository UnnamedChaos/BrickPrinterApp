using BrickPrinterApp.Models;
using MaterialSkin.Controls;

namespace BrickPrinterApp.Forms;

/// <summary>
/// Rotation management logic for WidgetManagerForm.
/// This partial class handles widget rotation functionality.
/// </summary>
public partial class WidgetManagerForm
{
    private void OnRotationCheckboxChanged(int screenId)
    {
        var isChecked = _rotationCheckboxes[screenId].Checked;
        _rotationPanels[screenId].Visible = isChecked;
        _screenDropdowns[screenId].Enabled = !isChecked;

        if (isChecked && _rotationLists[screenId].Items.Count == 0)
        {
            // Pre-populate with current selection if any
            var currentSelection = _screenDropdowns[screenId].SelectedIndex;
            if (currentSelection > 0)
            {
                var widgetName = _allWidgets[currentSelection - 1].Name;
                _rotationLists[screenId].Items.Add(widgetName);
            }
        }

        RecalculateFormHeight();
    }

    private void ShowAddWidgetDialog(int screenId)
    {
        // Get widgets not already in the rotation
        var existingWidgets = _rotationLists[screenId].Items.Cast<string>().ToHashSet();
        var availableWidgets = _allWidgets.Where(w => !existingWidgets.Contains(w.Name)).ToList();

        if (availableWidgets.Count == 0)
        {
            MessageBox.Show("All widgets are already in the rotation.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        // Simple picker dialog
        using var dialog = new Form
        {
            Text = "Add Widget",
            Size = new Size(300, 200),
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false
        };

        var listBox = new ListBox
        {
            Location = new Point(10, 10),
            Size = new Size(260, 100)
        };

        foreach (var widget in availableWidgets)
        {
            listBox.Items.Add(widget.Name);
        }

        var addBtn = new Button
        {
            Text = "Add",
            Location = new Point(10, 120),
            DialogResult = DialogResult.OK
        };

        var cancelBtn = new Button
        {
            Text = "Cancel",
            Location = new Point(100, 120),
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.Add(listBox);
        dialog.Controls.Add(addBtn);
        dialog.Controls.Add(cancelBtn);
        dialog.AcceptButton = addBtn;
        dialog.CancelButton = cancelBtn;

        if (dialog.ShowDialog() == DialogResult.OK && listBox.SelectedItem != null)
        {
            var selectedName = listBox.SelectedItem.ToString();
            if (!string.IsNullOrEmpty(selectedName))
            {
                _rotationLists[screenId].Items.Add(selectedName);
            }
        }
    }

    private void RemoveSelectedWidget(int screenId)
    {
        var selectedIndex = _rotationLists[screenId].SelectedIndex;
        if (selectedIndex >= 0)
        {
            _rotationLists[screenId].Items.RemoveAt(selectedIndex);
        }
    }

    private void ApplyRotationConfig(int screenId)
    {
        var widgetNames = _rotationLists[screenId].Items.Cast<string>().ToList();

        if (widgetNames.Count == 0)
        {
            MessageBox.Show($"Screen {screenId}: Add at least one widget to the rotation.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!int.TryParse(_intervalInputs[screenId].Text, out var intervalSeconds) || intervalSeconds < 10)
        {
            intervalSeconds = 60;
            _intervalInputs[screenId].Text = "60";
        }

        var config = new ScreenRotationConfig
        {
            IsEnabled = true,
            RotationIntervalSeconds = intervalSeconds,
            WidgetNames = widgetNames
        };

        _rotationManager.EnableRotation(screenId, config);
    }
}
