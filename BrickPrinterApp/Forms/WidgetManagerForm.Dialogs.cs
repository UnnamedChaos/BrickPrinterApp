using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp.Forms;

/// <summary>
/// Dialog creation methods for WidgetManagerForm.
/// This partial class handles the creation of complex configuration dialogs.
/// </summary>
public partial class WidgetManagerForm
{
    private bool ShowConditionalEditDialog(ConditionalWidgetConfig config, string title)
    {
        var dialog = new MaterialForm();
        dialog.Text = title;
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.MaximizeBox = false;
        dialog.MinimizeBox = false;
        dialog.Sizable = false;
        dialog.ShowInTaskbar = false;

        // Apply Material theme to dialog
        _materialSkinManager.AddFormToManage(dialog);

        var yOffset = 80; // Account for MaterialForm title bar

        // Widget dropdown
        var widgetLabel = new MaterialLabel
        {
            Text = "Widget:",
            Location = new Point(20, yOffset + 5),
            AutoSize = true
        };
        var widgetDropdown = new MaterialComboBox
        {
            Location = new Point(130, yOffset),
            Width = 320
        };
        foreach (var (name, _, _) in _allWidgets)
        {
            widgetDropdown.Items.Add(name);
        }
        var widgetIndex = _allWidgets.FindIndex(w => w.Name == config.WidgetName);
        widgetDropdown.SelectedIndex = widgetIndex >= 0 ? widgetIndex : 0;

        yOffset += 55;

        // Recent windows section
        var recentLabel = new MaterialLabel
        {
            Text = "Recent Windows (click to use):",
            Location = new Point(20, yOffset),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Subtitle2
        };
        yOffset += 25;

        var recentList = new ListView
        {
            Location = new Point(20, yOffset),
            Size = new Size(440, 100),
            View = View.Details,
            FullRowSelect = true,
            GridLines = true
        };
        recentList.Columns.Add("Process", 120);
        recentList.Columns.Add("Window Title", 310);

        // Populate with recent windows from watcher
        foreach (var window in _windowWatcher.RecentWindows)
        {
            var item = new ListViewItem(window.ProcessName);
            item.SubItems.Add(window.WindowTitle);
            item.Tag = window;
            recentList.Items.Add(item);
        }

        yOffset += 110;

        // Process input with combo for suggestions
        var processLabel = new MaterialLabel
        {
            Text = "Process:",
            Location = new Point(20, yOffset + 5),
            AutoSize = true
        };
        var processInput = new ComboBox
        {
            Location = new Point(130, yOffset),
            Width = 320,
            DropDownStyle = ComboBoxStyle.DropDown  // Allow text input
        };
        // Add unique process names as suggestions
        var processNames = _windowWatcher.RecentWindows
            .Select(w => w.ProcessName)
            .Distinct()
            .ToList();
        foreach (var name in processNames)
        {
            processInput.Items.Add(name);
        }
        // Set current value
        if (!string.IsNullOrEmpty(config.ProcessName))
        {
            processInput.Text = config.ProcessName;
        }

        yOffset += 55;

        // Title input
        var titleLabel = new MaterialLabel
        {
            Text = "Window Title:",
            Location = new Point(20, yOffset + 5),
            AutoSize = true
        };
        var titleInput = new TextBox
        {
            Location = new Point(130, yOffset),
            Width = 320,
            Text = config.WindowTitleContains ?? "",
            PlaceholderText = "e.g. YouTube, Visual Studio"
        };

        yOffset += 60;

        // Match mode
        var matchLabel = new MaterialLabel
        {
            Text = "Match Mode:",
            Location = new Point(20, yOffset + 5),
            AutoSize = true
        };
        var matchDropdown = new MaterialComboBox
        {
            Location = new Point(130, yOffset),
            Width = 150
        };
        matchDropdown.Items.AddRange(new object[] { "OR (either)", "AND (both)" });
        matchDropdown.SelectedIndex = config.MatchBothConditions ? 1 : 0;

        yOffset += 55;

        // Enabled checkbox
        var enabledCheckbox = new MaterialCheckbox
        {
            Text = "Enabled",
            Location = new Point(130, yOffset),
            Checked = config.IsEnabled,
            AutoSize = true
        };

        yOffset += 45;

        // Buttons
        var okBtn = new MaterialButton
        {
            Text = "Save",
            Location = new Point(130, yOffset),
            Width = 100,
            Type = MaterialButton.MaterialButtonType.Contained,
            DialogResult = DialogResult.OK
        };

        var cancelBtn = new MaterialButton
        {
            Text = "Cancel",
            Location = new Point(240, yOffset),
            Width = 100,
            Type = MaterialButton.MaterialButtonType.Outlined,
            DialogResult = DialogResult.Cancel
        };

        // Handle recent list double-click to populate fields
        recentList.DoubleClick += (s, e) =>
        {
            if (recentList.SelectedItems.Count > 0 && recentList.SelectedItems[0].Tag is ActiveWindowInfo window)
            {
                // Set process name directly
                processInput.Text = window.ProcessName;

                // Set title as suggestion (first 50 chars if long)
                var titleHint = window.WindowTitle.Length > 50
                    ? window.WindowTitle.Substring(0, 50)
                    : window.WindowTitle;
                titleInput.Text = titleHint;
            }
        };

        // Also handle single click for convenience
        recentList.Click += (s, e) =>
        {
            if (recentList.SelectedItems.Count > 0 && recentList.SelectedItems[0].Tag is ActiveWindowInfo window)
            {
                processInput.Text = window.ProcessName;

                var titleHint = window.WindowTitle.Length > 50
                    ? window.WindowTitle.Substring(0, 50)
                    : window.WindowTitle;
                titleInput.Text = titleHint;
            }
        };

        dialog.Controls.AddRange(new Control[] {
            widgetLabel, widgetDropdown,
            recentLabel, recentList,
            processLabel, processInput,
            titleLabel, titleInput,
            matchLabel, matchDropdown,
            enabledCheckbox,
            okBtn, cancelBtn
        });
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;

        // Set form size after adding all controls
        dialog.Size = new Size(500, yOffset + 80);

        try
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                var process = processInput.Text?.Trim() ?? "";
                var windowTitle = titleInput.Text.Trim();

                if (string.IsNullOrEmpty(process) && string.IsNullOrEmpty(windowTitle))
                {
                    MessageBox.Show("Please enter a process name or window title.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                config.WidgetName = widgetDropdown.SelectedItem?.ToString() ?? "";
                config.ProcessName = string.IsNullOrEmpty(process) ? null : process;
                config.WindowTitleContains = string.IsNullOrEmpty(windowTitle) ? null : windowTitle;
                config.MatchBothConditions = matchDropdown.SelectedIndex == 1;
                config.IsEnabled = enabledCheckbox.Checked;
                return true;
            }

            return false;
        }
        finally
        {
            _materialSkinManager.RemoveFormToManage(dialog);
            dialog.Dispose();
        }
    }

    private bool ShowCustomConditionalDialog(int screenId, out IConditionalWidget? conditionalWidget)
    {
        conditionalWidget = null;

        var dialog = new MaterialForm();
        dialog.Text = "Add Custom Conditional Widget";
        dialog.StartPosition = FormStartPosition.CenterParent;
        dialog.MaximizeBox = false;
        dialog.MinimizeBox = false;
        dialog.Sizable = false;
        dialog.ShowInTaskbar = false;

        _materialSkinManager.AddFormToManage(dialog);

        var yOffset = 80;

        // Widget selection
        var widgetLabel = new MaterialLabel
        {
            Text = "Widget:",
            Location = new Point(20, yOffset + 5),
            AutoSize = true
        };
        var widgetDropdown = new MaterialComboBox
        {
            Location = new Point(150, yOffset),
            Width = 320
        };
        foreach (var (name, _, _) in _allWidgets)
        {
            widgetDropdown.Items.Add(name);
        }
        if (widgetDropdown.Items.Count > 0)
            widgetDropdown.SelectedIndex = 0;

        // Handler to check if BambuLab widget is selected
        bool IsBambuLabWidgetSelected()
        {
            if (widgetDropdown.SelectedIndex < 0) return false;
            var (name, _, _) = _allWidgets[widgetDropdown.SelectedIndex];
            return name == "BambuLab";
        }

        yOffset += 55;

        // Condition type selection
        var conditionLabel = new MaterialLabel
        {
            Text = "Condition Type:",
            Location = new Point(20, yOffset + 5),
            AutoSize = true
        };
        var conditionDropdown = new MaterialComboBox
        {
            Location = new Point(150, yOffset),
            Width = 320
        };
        conditionDropdown.Items.AddRange(new object[]
        {
            "Always Active (for testing)",
            "When Process is Running",
            "Time Range",
            "Weekdays Only"
        });
        conditionDropdown.SelectedIndex = 0;

        yOffset += 55;

        // BambuLab settings panel (visible only for BambuLab widget)
        var bambuLabPanel = new Panel
        {
            Location = new Point(20, yOffset),
            Size = new Size(470, 120),
            BorderStyle = BorderStyle.FixedSingle,
            Visible = false
        };

        var bambuIpLabel = new Label
        {
            Text = "IP Address:",
            Location = new Point(10, 10),
            AutoSize = true
        };
        var bambuIpInput = new TextBox
        {
            Location = new Point(130, 7),
            Width = 320,
            Text = _settingService.BambuLabIp ?? "",
            PlaceholderText = "e.g. 192.168.1.100"
        };

        var bambuPassLabel = new Label
        {
            Text = "Access Code:",
            Location = new Point(10, 45),
            AutoSize = true
        };
        var bambuPassInput = new TextBox
        {
            Location = new Point(130, 42),
            Width = 320,
            Text = _settingService.BambuLabAccessCode ?? "",
            PlaceholderText = "8-character code"
        };

        var bambuSerialLabel = new Label
        {
            Text = "Serial Number:",
            Location = new Point(10, 80),
            AutoSize = true
        };
        var bambuSerialInput = new TextBox
        {
            Location = new Point(130, 77),
            Width = 320,
            Text = _settingService.BambuLabSerial ?? "",
            PlaceholderText = "e.g. AC12345678910123"
        };

        bambuLabPanel.Controls.AddRange(new Control[]
        {
            bambuIpLabel, bambuIpInput,
            bambuPassLabel, bambuPassInput,
            bambuSerialLabel, bambuSerialInput
        });

        // Parameter panel (changes based on condition type)
        var paramPanel = new Panel
        {
            Location = new Point(20, yOffset),
            Size = new Size(470, 100),
            BorderStyle = BorderStyle.FixedSingle
        };

        // Process name input (for "Process Running")
        var processLabel = new Label
        {
            Text = "Process Name:",
            Location = new Point(10, 10),
            AutoSize = true,
            Visible = false
        };
        var processInput = new TextBox
        {
            Location = new Point(130, 7),
            Width = 320,
            PlaceholderText = "e.g. notepad, chrome",
            Visible = false
        };

        // Time range inputs
        var startTimeLabel = new Label
        {
            Text = "Start Time:",
            Location = new Point(10, 10),
            AutoSize = true,
            Visible = false
        };
        var startTimeInput = new DateTimePicker
        {
            Location = new Point(130, 7),
            Width = 150,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(9),
            Visible = false
        };
        var endTimeLabel = new Label
        {
            Text = "End Time:",
            Location = new Point(10, 45),
            AutoSize = true,
            Visible = false
        };
        var endTimeInput = new DateTimePicker
        {
            Location = new Point(130, 42),
            Width = 150,
            Format = DateTimePickerFormat.Time,
            ShowUpDown = true,
            Value = DateTime.Today.AddHours(17),
            Visible = false
        };

        // Weekday checkboxes
        var weekdayLabel = new Label
        {
            Text = "Active Days:",
            Location = new Point(10, 10),
            AutoSize = true,
            Visible = false
        };
        var dayCheckboxes = new Dictionary<DayOfWeek, CheckBox>();
        int dayX = 130;
        foreach (DayOfWeek day in Enum.GetValues(typeof(DayOfWeek)))
        {
            var cb = new CheckBox
            {
                Text = day.ToString().Substring(0, 3),
                Location = new Point(dayX, 10),
                Width = 50,
                Checked = day != DayOfWeek.Saturday && day != DayOfWeek.Sunday,
                Visible = false
            };
            dayCheckboxes[day] = cb;
            paramPanel.Controls.Add(cb);
            dayX += 55;
        }

        paramPanel.Controls.AddRange(new Control[]
        {
            processLabel, processInput,
            startTimeLabel, startTimeInput,
            endTimeLabel, endTimeInput,
            weekdayLabel
        });

        // Update visibility when widget selection changes
        widgetDropdown.SelectedIndexChanged += (s, e) =>
        {
            bool isBambuLab = IsBambuLabWidgetSelected();

            // Hide/show appropriate panels
            conditionLabel.Visible = !isBambuLab;
            conditionDropdown.Visible = !isBambuLab;
            paramPanel.Visible = !isBambuLab;
            bambuLabPanel.Visible = isBambuLab;

            // Adjust dialog height
            if (isBambuLab)
            {
                bambuLabPanel.Location = new Point(20, conditionLabel.Location.Y + 55);
            }
        };

        // Update parameter panel when condition type changes
        conditionDropdown.SelectedIndexChanged += (s, e) =>
        {
            // Hide all
            processLabel.Visible = processInput.Visible = false;
            startTimeLabel.Visible = startTimeInput.Visible = false;
            endTimeLabel.Visible = endTimeInput.Visible = false;
            weekdayLabel.Visible = false;
            foreach (var cb in dayCheckboxes.Values) cb.Visible = false;

            // Show relevant controls
            switch (conditionDropdown.SelectedIndex)
            {
                case 0: // Always Active
                    break;
                case 1: // Process Running
                    processLabel.Visible = processInput.Visible = true;
                    break;
                case 2: // Time Range
                    startTimeLabel.Visible = startTimeInput.Visible = true;
                    endTimeLabel.Visible = endTimeInput.Visible = true;
                    break;
                case 3: // Weekdays
                    weekdayLabel.Visible = true;
                    foreach (var cb in dayCheckboxes.Values) cb.Visible = true;
                    break;
            }
        };

        yOffset += 110;

        // Buttons
        var okBtn = new MaterialButton
        {
            Text = "Create",
            Location = new Point(150, yOffset),
            Width = 100,
            Type = MaterialButton.MaterialButtonType.Contained,
            DialogResult = DialogResult.OK
        };

        var cancelBtn = new MaterialButton
        {
            Text = "Cancel",
            Location = new Point(260, yOffset),
            Width = 100,
            Type = MaterialButton.MaterialButtonType.Outlined,
            DialogResult = DialogResult.Cancel
        };

        dialog.Controls.AddRange(new Control[]
        {
            widgetLabel, widgetDropdown,
            conditionLabel, conditionDropdown,
            paramPanel,
            bambuLabPanel,
            okBtn, cancelBtn
        });
        dialog.AcceptButton = okBtn;
        dialog.CancelButton = cancelBtn;
        dialog.Size = new Size(520, yOffset + 80);

        try
        {
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                if (widgetDropdown.SelectedIndex < 0)
                {
                    MessageBox.Show("Please select a widget.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }

                var (_, widget, _) = _allWidgets[widgetDropdown.SelectedIndex];

                // Check if BambuLab widget is selected
                if (IsBambuLabWidgetSelected())
                {
                    // Validate BambuLab settings
                    if (string.IsNullOrWhiteSpace(bambuIpInput.Text) ||
                        string.IsNullOrWhiteSpace(bambuPassInput.Text) ||
                        string.IsNullOrWhiteSpace(bambuSerialInput.Text))
                    {
                        MessageBox.Show("Please enter IP, Access Code, and Serial Number for BambuLab printer.",
                            "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return false;
                    }

                    // Save BambuLab settings
                    _settingService.BambuLabIp = bambuIpInput.Text.Trim();
                    _settingService.BambuLabAccessCode = bambuPassInput.Text.Trim();
                    _settingService.BambuLabSerial = bambuSerialInput.Text.Trim();
                    _settingService.Save();

                    // Create BambuLab conditional widget (always active)
                    conditionalWidget = new BrickPrinterApp.Widgets.Conditional.BambuLabConditionalWidget(widget);
                }
                else
                {
                    // Create appropriate conditional widget based on type
                    switch (conditionDropdown.SelectedIndex)
                    {
                        case 0: // Always Active
                            conditionalWidget = new BrickPrinterApp.Widgets.Conditional.AlwaysTrueConditionalWidget(widget);
                            break;

                        case 1: // Process Running
                            if (string.IsNullOrWhiteSpace(processInput.Text))
                            {
                                MessageBox.Show("Please enter a process name.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            }
                            conditionalWidget = new BrickPrinterApp.Widgets.Conditional.ProcessRunningConditionalWidget(
                                widget, processInput.Text.Trim());
                            break;

                        case 2: // Time Range
                            conditionalWidget = new BrickPrinterApp.Widgets.Conditional.TimeRangeConditionalWidget(
                                widget,
                                startTimeInput.Value.TimeOfDay,
                                endTimeInput.Value.TimeOfDay);
                            break;

                        case 3: // Weekdays
                            var selectedDays = dayCheckboxes.Where(kvp => kvp.Value.Checked)
                                                            .Select(kvp => kvp.Key)
                                                            .ToArray();
                            if (selectedDays.Length == 0)
                            {
                                MessageBox.Show("Please select at least one day.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return false;
                            }
                            conditionalWidget = new BrickPrinterApp.Widgets.Conditional.WeekdayConditionalWidget(
                                widget, selectedDays);
                            break;
                    }
                }

                return true;
            }

            return false;
        }
        finally
        {
            _materialSkinManager.RemoveFormToManage(dialog);
            dialog.Dispose();
        }
    }
}
