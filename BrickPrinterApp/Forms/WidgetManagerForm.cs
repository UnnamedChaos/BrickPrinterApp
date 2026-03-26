using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using BrickPrinterApp.Services;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp.Forms;

public class WidgetManagerForm : MaterialForm
{
    private readonly WidgetService _widgetService;
    private readonly SettingService _settingService;
    private readonly RotationManagerService _rotationManager;
    private readonly ActiveWindowWatcherService _windowWatcher;
    private readonly MaterialSkinManager _materialSkinManager;
    private readonly List<MaterialComboBox> _screenDropdowns = new();
    private readonly List<MaterialCheckbox> _rotationCheckboxes = new();
    private readonly List<TextBox> _intervalInputs = new();
    private readonly List<ListBox> _rotationLists = new();
    private readonly List<MaterialButton> _addButtons = new();
    private readonly List<MaterialButton> _removeButtons = new();
    private readonly List<Panel> _rotationPanels = new();
    private readonly List<GroupBox> _conditionalPanels = new();
    private readonly List<ListView> _conditionalLists = new();
    private readonly List<(string Name, object Widget, bool IsScript)> _allWidgets = new();
    private readonly int[] _originalSelections;

    public WidgetManagerForm(WidgetService widgetService, SettingService settingService, RotationManagerService rotationManager, ActiveWindowWatcherService windowWatcher)
    {
        _widgetService = widgetService;
        _settingService = settingService;
        _rotationManager = rotationManager;
        _windowWatcher = windowWatcher;
        _originalSelections = new int[_settingService.NumScreens];

        // Initialize Material Skin
        _materialSkinManager = MaterialSkinManager.Instance;
        _materialSkinManager.AddFormToManage(this);
        _materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
        _materialSkinManager.ColorScheme = new ColorScheme(
            Primary.BlueGrey800,
            Primary.BlueGrey900,
            Primary.BlueGrey500,
            Accent.LightBlue200,
            TextShade.WHITE
        );

        BuildWidgetList();
        InitializeComponents();
        LoadCurrentAssignments();
    }

    private void BuildWidgetList()
    {
        _allWidgets.Clear();

        // Add binary widgets
        foreach (var widget in _widgetService.AvailableWidgets)
        {
            _allWidgets.Add((widget.Name, widget, false));
        }

        // Add script widgets (marked with icon)
        foreach (var widget in _widgetService.AvailableScriptWidgets)
        {
            _allWidgets.Add(($"[Lua] {widget.Name}", widget, true));
        }
    }

    private void InitializeComponents()
    {
        Text = "Widget Manager";
        StartPosition = FormStartPosition.CenterScreen;
        Sizable = false;

        var yOffset = 80; // Account for MaterialForm title bar (64px) + padding

        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            var screenId = i;

            // Screen label
            var label = new MaterialLabel
            {
                Text = $"Screen {i}:",
                Location = new Point(20, yOffset + 8),
                AutoSize = true,
                FontType = MaterialSkinManager.fontType.Subtitle1
            };

            // Widget dropdown
            var dropdown = new MaterialComboBox
            {
                Location = new Point(120, yOffset),
                Width = 200
            };

            dropdown.Items.Add("(Kein Widget)");
            foreach (var (name, _, _) in _allWidgets)
            {
                dropdown.Items.Add(name);
            }

            // Rotation checkbox
            var rotationCheckbox = new MaterialCheckbox
            {
                Text = "Rotation",
                Location = new Point(330, yOffset + 4),
                AutoSize = true
            };
            rotationCheckbox.CheckedChanged += (s, e) => OnRotationCheckboxChanged(screenId);

            _screenDropdowns.Add(dropdown);
            _rotationCheckboxes.Add(rotationCheckbox);
            Controls.Add(label);
            Controls.Add(dropdown);
            Controls.Add(rotationCheckbox);

            yOffset += 50;

            // Rotation panel (initially hidden)
            var rotationPanel = new Panel
            {
                Location = new Point(20, yOffset),
                Size = new Size(400, 140),
                Visible = false
            };

            // Interval label and input
            var intervalLabel = new MaterialLabel
            {
                Text = "Interval (sec):",
                Location = new Point(0, 5),
                AutoSize = true
            };

            var intervalInput = new TextBox
            {
                Text = "60",
                Location = new Point(100, 0),
                Width = 60
            };

            // Rotation list
            var rotationList = new ListBox
            {
                Location = new Point(0, 40),
                Size = new Size(300, 80),
                BorderStyle = BorderStyle.FixedSingle
            };

            // Add widget button
            var addButton = new MaterialButton
            {
                Text = "+ Add",
                Location = new Point(310, 40),
                Width = 80,
                Height = 36,
                Type = MaterialButton.MaterialButtonType.Outlined
            };
            addButton.Click += (s, e) => ShowAddWidgetDialog(screenId);

            // Remove widget button
            var removeButton = new MaterialButton
            {
                Text = "- Remove",
                Location = new Point(310, 84),
                Width = 80,
                Height = 36,
                Type = MaterialButton.MaterialButtonType.Text
            };
            removeButton.Click += (s, e) => RemoveSelectedWidget(screenId);

            rotationPanel.Controls.Add(intervalLabel);
            rotationPanel.Controls.Add(intervalInput);
            rotationPanel.Controls.Add(rotationList);
            rotationPanel.Controls.Add(addButton);
            rotationPanel.Controls.Add(removeButton);

            _intervalInputs.Add(intervalInput);
            _rotationLists.Add(rotationList);
            _addButtons.Add(addButton);
            _removeButtons.Add(removeButton);
            _rotationPanels.Add(rotationPanel);
            Controls.Add(rotationPanel);

            yOffset += 150; // Space for rotation panel (even when hidden, for layout)
        }

        // Conditional Widgets Section - separate from per-screen controls
        var conditionalSectionY = yOffset;

        // Section header
        var conditionalHeader = new MaterialLabel
        {
            Text = "Conditional Widgets (override when process/title matches)",
            Location = new Point(20, conditionalSectionY),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Subtitle1
        };
        Controls.Add(conditionalHeader);
        conditionalSectionY += 30;

        // Create conditional panels for each screen
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            var screenId = i;

            var conditionalPanel = new GroupBox
            {
                Text = $"Screen {i}",
                Location = new Point(20, conditionalSectionY),
                Size = new Size(520, 130),
                ForeColor = Color.FromArgb(80, 80, 80)
            };

            // ListView for conditional widgets
            var conditionalList = new ListView
            {
                Location = new Point(10, 20),
                Size = new Size(400, 100),
                View = View.Details,
                FullRowSelect = true,
                GridLines = true
            };
            conditionalList.Columns.Add("Widget", 150);
            conditionalList.Columns.Add("Process", 100);
            conditionalList.Columns.Add("Title", 100);
            conditionalList.Columns.Add("Mode", 45);

            // Add button
            var addCondBtn = new MaterialButton
            {
                Text = "+ Add",
                Location = new Point(420, 20),
                Width = 90,
                Height = 30,
                Type = MaterialButton.MaterialButtonType.Outlined
            };
            addCondBtn.Click += (s, e) => ShowAddConditionalDialog(screenId);

            // Edit button
            var editCondBtn = new MaterialButton
            {
                Text = "Edit",
                Location = new Point(420, 55),
                Width = 90,
                Height = 30,
                Type = MaterialButton.MaterialButtonType.Text
            };
            editCondBtn.Click += (s, e) => ShowEditConditionalDialog(screenId);

            // Remove button
            var removeCondBtn = new MaterialButton
            {
                Text = "Remove",
                Location = new Point(420, 90),
                Width = 90,
                Height = 30,
                Type = MaterialButton.MaterialButtonType.Text
            };
            removeCondBtn.Click += (s, e) => RemoveConditionalWidget(screenId);

            conditionalPanel.Controls.Add(conditionalList);
            conditionalPanel.Controls.Add(addCondBtn);
            conditionalPanel.Controls.Add(editCondBtn);
            conditionalPanel.Controls.Add(removeCondBtn);

            _conditionalLists.Add(conditionalList);
            _conditionalPanels.Add(conditionalPanel);
            Controls.Add(conditionalPanel);

            conditionalSectionY += 140;
        }

        // Bottom buttons
        var buttonY = conditionalSectionY + 10;
        Controls.Add(CreateButton(buttonY, "Anwenden", 20, 110, ApplyChanges, MaterialButton.MaterialButtonType.Contained));
        Controls.Add(CreateButton(buttonY, "Alle senden", 140, 120, ResendAll, MaterialButton.MaterialButtonType.Outlined));
        Controls.Add(CreateButton(buttonY, "Schließen", 270, 100, Close, MaterialButton.MaterialButtonType.Text));

        // Set form size
        int formHeight = buttonY + 80;
        Size = new Size(560, formHeight);
        MaximizeBox = false;
    }

    private MaterialButton CreateButton(int yOffset, string text, int xPos, int width, Action action, MaterialButton.MaterialButtonType type)
    {
        var btn = new MaterialButton
        {
            Text = text,
            Location = new Point(xPos, yOffset + 10),
            Width = width,
            Height = 36,
            Type = type
        };
        btn.Click += (_, _) => action();
        return btn;
    }

    private void LoadCurrentAssignments()
    {
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            var dropdown = _screenDropdowns[i];
            var rotationConfig = _rotationManager.GetConfig(i);

            if (rotationConfig != null && rotationConfig.IsEnabled && rotationConfig.WidgetNames.Count > 0)
            {
                // Rotation is enabled
                _rotationCheckboxes[i].Checked = true;
                _intervalInputs[i].Text = rotationConfig.RotationIntervalSeconds.ToString();
                _rotationLists[i].Items.Clear();
                foreach (var widgetName in rotationConfig.WidgetNames)
                {
                    _rotationLists[i].Items.Add(widgetName);
                }
                _rotationPanels[i].Visible = true;

                // Set dropdown to first widget in rotation (or none)
                if (rotationConfig.WidgetNames.Count > 0)
                {
                    var firstWidget = rotationConfig.WidgetNames[0];
                    var index = _allWidgets.FindIndex(w => w.Name == firstWidget);
                    dropdown.SelectedIndex = index >= 0 ? index + 1 : 0;
                }
                else
                {
                    dropdown.SelectedIndex = 0;
                }
                dropdown.Enabled = false;
            }
            else
            {
                // Single widget mode
                var currentWidget = _widgetService.GetWidgetForScreen(i);

                if (currentWidget == null)
                {
                    dropdown.SelectedIndex = 0;
                }
                else
                {
                    var widgetName = currentWidget switch
                    {
                        IWidget w => w.Name,
                        IScriptWidget sw => $"[Lua] {sw.Name}",
                        _ => null
                    };

                    var index = _allWidgets.FindIndex(w => w.Name == widgetName);
                    dropdown.SelectedIndex = index >= 0 ? index + 1 : 0;
                }

                _rotationCheckboxes[i].Checked = false;
                _rotationPanels[i].Visible = false;
                dropdown.Enabled = true;
            }

            _originalSelections[i] = dropdown.SelectedIndex;

            // Load conditional config
            LoadConditionalConfig(i);
        }

        // Recalculate form height based on visible panels
        RecalculateFormHeight();
    }

    private void LoadConditionalConfig(int screenId)
    {
        _conditionalLists[screenId].Items.Clear();

        if (_settingService.ConditionalConfigs.TryGetValue(screenId, out var configs))
        {
            foreach (var config in configs)
            {
                if (!config.IsEnabled && string.IsNullOrEmpty(config.WidgetName))
                    continue;

                var item = new ListViewItem(config.WidgetName);
                item.SubItems.Add(config.ProcessName ?? "");
                item.SubItems.Add(config.WindowTitleContains ?? "");
                item.SubItems.Add(config.MatchBothConditions ? "AND" : "OR");
                item.Tag = config;
                item.ForeColor = config.IsEnabled ? Color.Black : Color.Gray;
                _conditionalLists[screenId].Items.Add(item);
            }
        }
    }

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

    private void RecalculateFormHeight()
    {
        var yOffset = 80;
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            // Move controls to correct position
            var labelY = yOffset + 8;
            var dropdownY = yOffset;

            // Find and move the screen label
            foreach (Control c in Controls)
            {
                if (c is MaterialLabel lbl && lbl.Text == $"Screen {i}:")
                {
                    lbl.Location = new Point(20, labelY);
                }
            }
            _screenDropdowns[i].Location = new Point(120, dropdownY);
            _rotationCheckboxes[i].Location = new Point(330, dropdownY + 4);

            yOffset += 50;

            // Position and show/hide rotation panel
            _rotationPanels[i].Location = new Point(20, yOffset);
            if (_rotationCheckboxes[i].Checked)
            {
                yOffset += 150;
            }
        }

        // Conditional section header
        foreach (Control c in Controls)
        {
            if (c is MaterialLabel lbl && lbl.Text.StartsWith("Conditional Widgets"))
            {
                lbl.Location = new Point(20, yOffset);
            }
        }
        yOffset += 30;

        // Conditional panels (always visible)
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            _conditionalPanels[i].Location = new Point(20, yOffset);
            yOffset += 140;
        }

        // Move buttons
        var buttonY = yOffset + 10;
        foreach (Control c in Controls)
        {
            if (c is MaterialButton btn)
            {
                if (btn.Text == "Anwenden" || btn.Text == "Alle senden" || btn.Text == "Schließen")
                {
                    btn.Location = new Point(btn.Location.X, buttonY);
                }
            }
        }

        // Resize form
        int formHeight = buttonY + 70;
        Size = new Size(560, formHeight);
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

    private void ApplyChanges()
    {
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            if (_rotationCheckboxes[i].Checked)
            {
                // Rotation mode
                ApplyRotationConfig(i);
            }
            else
            {
                // Single widget mode - disable any rotation first
                if (_rotationManager.IsRotationEnabled(i))
                {
                    _rotationManager.DisableRotation(i);
                }

                // Apply single widget
                ApplySingleWidget(i);
            }

            // Apply conditional config
            ApplyConditionalConfig(i);
        }
    }

    private void ApplyConditionalConfig(int screenId)
    {
        var configs = new List<ConditionalWidgetConfig>();

        foreach (ListViewItem item in _conditionalLists[screenId].Items)
        {
            if (item.Tag is ConditionalWidgetConfig config)
            {
                configs.Add(config);
            }
        }

        _settingService.ConditionalConfigs[screenId] = configs;
        _settingService.Save();
    }

    private void ShowAddConditionalDialog(int screenId)
    {
        var config = new ConditionalWidgetConfig { IsEnabled = true };
        if (ShowConditionalEditDialog(config, "Add Conditional Widget"))
        {
            var item = new ListViewItem(config.WidgetName);
            item.SubItems.Add(config.ProcessName ?? "");
            item.SubItems.Add(config.WindowTitleContains ?? "");
            item.SubItems.Add(config.MatchBothConditions ? "AND" : "OR");
            item.Tag = config;
            item.ForeColor = config.IsEnabled ? Color.Black : Color.Gray;
            _conditionalLists[screenId].Items.Add(item);
        }
    }

    private void ShowEditConditionalDialog(int screenId)
    {
        if (_conditionalLists[screenId].SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a conditional widget to edit.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _conditionalLists[screenId].SelectedItems[0];
        if (item.Tag is ConditionalWidgetConfig config)
        {
            if (ShowConditionalEditDialog(config, "Edit Conditional Widget"))
            {
                item.Text = config.WidgetName;
                item.SubItems[1].Text = config.ProcessName ?? "";
                item.SubItems[2].Text = config.WindowTitleContains ?? "";
                item.SubItems[3].Text = config.MatchBothConditions ? "AND" : "OR";
                item.ForeColor = config.IsEnabled ? Color.Black : Color.Gray;
            }
        }
    }

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

    private void RemoveConditionalWidget(int screenId)
    {
        if (_conditionalLists[screenId].SelectedItems.Count == 0)
        {
            MessageBox.Show("Please select a conditional widget to remove.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var item = _conditionalLists[screenId].SelectedItems[0];
        _conditionalLists[screenId].Items.Remove(item);
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

    private void ApplySingleWidget(int screenId)
    {
        var dropdown = _screenDropdowns[screenId];
        var selectedIndex = dropdown.SelectedIndex;

        // Skip if selection hasn't changed
        if (selectedIndex == _originalSelections[screenId])
            return;

        if (selectedIndex == 0)
        {
            _widgetService.RemoveWidgetFromScreen(screenId);
        }
        else
        {
            var (_, widget, isScript) = _allWidgets[selectedIndex - 1];

            if (isScript)
            {
                _widgetService.AssignScriptWidgetToScreen(screenId, (IScriptWidget)widget);
            }
            else
            {
                _widgetService.AssignWidgetToScreen(screenId, (IWidget)widget);
            }
        }

        // Update original selection to new value
        _originalSelections[screenId] = selectedIndex;
    }

    private void ResendAll()
    {
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            if (_rotationCheckboxes[i].Checked)
            {
                // Rotation mode - apply config (this will send first widget)
                ApplyRotationConfig(i);
            }
            else
            {
                // Single widget mode
                var dropdown = _screenDropdowns[i];
                var selectedIndex = dropdown.SelectedIndex;

                // Disable any rotation first
                if (_rotationManager.IsRotationEnabled(i))
                {
                    _rotationManager.DisableRotation(i);
                }

                if (selectedIndex == 0)
                {
                    _widgetService.RemoveWidgetFromScreen(i);
                }
                else
                {
                    var (_, widget, isScript) = _allWidgets[selectedIndex - 1];

                    if (isScript)
                    {
                        _widgetService.AssignScriptWidgetToScreen(i, (IScriptWidget)widget);
                    }
                    else
                    {
                        _widgetService.AssignWidgetToScreen(i, (IWidget)widget);
                    }
                }

                _originalSelections[i] = selectedIndex;
            }
        }
    }
}
