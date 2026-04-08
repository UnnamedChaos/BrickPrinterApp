using BrickPrinterApp.Models;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp.Forms;

/// <summary>
/// UI component initialization for WidgetManagerForm.
/// This partial class handles the creation and layout of all form controls.
/// </summary>
public partial class WidgetManagerForm
{
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
            Text = "Conditional Widgets (priority order: top = highest)",
            Location = new Point(20, conditionalSectionY),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Subtitle1
        };
        Controls.Add(conditionalHeader);
        conditionalSectionY += 30;

        // Create flow layout for conditional panels
        var conditionalFlowPanel = new FlowLayoutPanel
        {
            Location = new Point(20, conditionalSectionY),
            Width = 560,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Padding = new Padding(0)
        };

        // Create conditional panels for each screen
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            var screenId = i;

            // Table layout for GroupBox content
            var tableLayout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(10, 20, 10, 10),
                Location = new Point(0, 15),
                Width = 560
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 420F));
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120F));
            tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // ListView for conditional widgets
            var conditionalList = new ListView
            {
                Width = 410,
                Height = 130,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false
            };
            conditionalList.Columns.Add("#", 30);
            conditionalList.Columns.Add("Type", 60);
            conditionalList.Columns.Add("Condition", 200);
            conditionalList.Columns.Add("Widget", 110);

            // Button panel with flow layout
            var buttonPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(0)
            };

            // Up button
            var upCondBtn = new MaterialButton
            {
                Text = "↑ Up",
                Width = 110,
                Height = 24,
                Margin = new Padding(0, 0, 0, 2),
                Type = MaterialButton.MaterialButtonType.Outlined
            };
            upCondBtn.Click += (s, e) => MoveConditionalUp(screenId);

            // Down button
            var downCondBtn = new MaterialButton
            {
                Text = "↓ Down",
                Width = 110,
                Height = 24,
                Margin = new Padding(0, 0, 0, 3),
                Type = MaterialButton.MaterialButtonType.Outlined
            };
            downCondBtn.Click += (s, e) => MoveConditionalDown(screenId);

            // Add Custom button
            var addCustomBtn = new MaterialButton
            {
                Text = "+ Custom",
                Width = 110,
                Height = 24,
                Margin = new Padding(0, 0, 0, 2),
                Type = MaterialButton.MaterialButtonType.Text
            };
            addCustomBtn.Click += (s, e) => ShowAddCustomConditionalDialog(screenId);

            // Add Process button
            var addProcessBtn = new MaterialButton
            {
                Text = "+ Process",
                Width = 110,
                Height = 24,
                Margin = new Padding(0, 0, 0, 3),
                Type = MaterialButton.MaterialButtonType.Text
            };
            addProcessBtn.Click += (s, e) => ShowAddProcessConditionalDialog(screenId);

            // Remove button
            var removeCondBtn = new MaterialButton
            {
                Text = "Remove",
                Width = 110,
                Height = 24,
                Margin = new Padding(0, 0, 0, 0),
                Type = MaterialButton.MaterialButtonType.Text
            };
            removeCondBtn.Click += (s, e) => RemoveConditionalWidget(screenId);

            // Add buttons to flow panel
            buttonPanel.Controls.Add(upCondBtn);
            buttonPanel.Controls.Add(downCondBtn);
            buttonPanel.Controls.Add(addCustomBtn);
            buttonPanel.Controls.Add(addProcessBtn);
            buttonPanel.Controls.Add(removeCondBtn);

            // Add ListView and buttons to table layout
            tableLayout.Controls.Add(conditionalList, 0, 0);
            tableLayout.Controls.Add(buttonPanel, 1, 0);

            // GroupBox containing the table layout
            var conditionalPanel = new GroupBox
            {
                Text = $"Screen {i}",
                Width = 560,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0, 0, 0, 5),
                ForeColor = Color.FromArgb(80, 80, 80)
            };
            conditionalPanel.Controls.Add(tableLayout);

            _conditionalLists.Add(conditionalList);
            _conditionalPanels.Add(conditionalPanel);
            conditionalFlowPanel.Controls.Add(conditionalPanel);
        }

        Controls.Add(conditionalFlowPanel);
        conditionalSectionY += conditionalFlowPanel.Height + 10;

        // Bottom buttons
        var buttonY = conditionalSectionY + 10;
        Controls.Add(CreateButton(buttonY, "Anwenden", 20, 110, ApplyChanges, MaterialButton.MaterialButtonType.Contained));
        Controls.Add(CreateButton(buttonY, "Alle senden", 140, 120, ResendAll, MaterialButton.MaterialButtonType.Outlined));
        Controls.Add(CreateButton(buttonY, "Schließen", 270, 100, Close, MaterialButton.MaterialButtonType.Text));

        // Set form size
        int formHeight = buttonY + 80;
        Size = new Size(600, formHeight);
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
        FlowLayoutPanel? conditionalFlowPanel = null;
        foreach (Control c in Controls)
        {
            if (c is MaterialLabel lbl && lbl.Text.StartsWith("Conditional Widgets"))
            {
                lbl.Location = new Point(20, yOffset);
            }
            if (c is FlowLayoutPanel fp && fp.Controls.Count > 0 && fp.Controls[0] is GroupBox)
            {
                conditionalFlowPanel = fp;
            }
        }
        yOffset += 30;

        // Position conditional flow panel
        if (conditionalFlowPanel != null)
        {
            conditionalFlowPanel.Location = new Point(20, yOffset);
            yOffset += conditionalFlowPanel.Height;
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
        Size = new Size(600, formHeight);
    }
}
