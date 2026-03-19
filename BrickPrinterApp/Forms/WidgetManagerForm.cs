using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Forms;

public class WidgetManagerForm : Form
{
    private readonly WidgetService _widgetService;
    private readonly List<ComboBox> _screenDropdowns = new();

    public WidgetManagerForm(WidgetService widgetService)
    {
        _widgetService = widgetService;
        InitializeComponents();
        LoadCurrentAssignments();
    }

    private void InitializeComponents()
    {
        Text = "Widget Manager";
        Size = new Size(350, 200);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterScreen;

        var yOffset = 20;

        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            var screenId = i;

            var label = new Label
            {
                Text = $"Screen {i}:",
                Location = new Point(20, yOffset + 3),
                AutoSize = true
            };

            var dropdown = new ComboBox
            {
                Location = new Point(100, yOffset),
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            // Add "None" option
            dropdown.Items.Add("(Kein Widget)");

            // Add all available widgets
            foreach (var widget in _widgetService.AvailableWidgets)
            {
                dropdown.Items.Add(widget.Name);
            }

            dropdown.SelectedIndexChanged += (_, _) => OnWidgetSelectionChanged(screenId, dropdown);

            _screenDropdowns.Add(dropdown);
            Controls.Add(label);
            Controls.Add(dropdown);

            yOffset += 35;
        }

        var btnApply = new Button
        {
            Text = "Anwenden",
            Location = new Point(100, yOffset + 10),
            Width = 100
        };
        btnApply.Click += (_, _) => ApplyChanges();
        Controls.Add(btnApply);

        var btnClose = new Button
        {
            Text = "Schliessen",
            Location = new Point(210, yOffset + 10),
            Width = 90
        };
        btnClose.Click += (_, _) => Close();
        Controls.Add(btnClose);
    }

    private void LoadCurrentAssignments()
    {
        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            var currentWidget = _widgetService.GetWidgetForScreen(i);
            var dropdown = _screenDropdowns[i];

            if (currentWidget == null)
            {
                dropdown.SelectedIndex = 0; // "(Kein Widget)"
            }
            else
            {
                var index = _widgetService.AvailableWidgets
                    .ToList()
                    .FindIndex(w => w.Name == currentWidget.Name);

                dropdown.SelectedIndex = index + 1; // +1 because of "(Kein Widget)"
            }
        }
    }

    private void OnWidgetSelectionChanged(int screenId, ComboBox dropdown)
    {
        // Changes are applied on button click
    }

    private void ApplyChanges()
    {
        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            var dropdown = _screenDropdowns[i];
            var selectedIndex = dropdown.SelectedIndex;

            if (selectedIndex == 0)
            {
                _widgetService.RemoveWidgetFromScreen(i);
            }
            else
            {
                var widget = _widgetService.AvailableWidgets[selectedIndex - 1];
                _widgetService.AssignWidgetToScreen(i, widget);
            }
        }
    }
}
