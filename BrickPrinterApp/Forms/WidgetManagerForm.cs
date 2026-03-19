using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Forms;

public class WidgetManagerForm : Form
{
    private readonly WidgetService _widgetService;
    private readonly List<ComboBox> _screenDropdowns = new();
    private readonly List<(string Name, object Widget, bool IsScript)> _allWidgets = new();
    private readonly int[] _originalSelections;

    public WidgetManagerForm(WidgetService widgetService)
    {
        _widgetService = widgetService;
        _originalSelections = new int[SettingService.NumScreens];
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
        Size = new Size(350, 200);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterScreen;

        var yOffset = 20;

        for (int i = 0; i < SettingService.NumScreens; i++)
        {
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

            dropdown.Items.Add("(Kein Widget)");

            foreach (var (name, _, _) in _allWidgets)
            {
                dropdown.Items.Add(name);
            }

            _screenDropdowns.Add(dropdown);
            Controls.Add(label);
            Controls.Add(dropdown);

            yOffset += 35;
        }
        
        Controls.Add(CreateButton(yOffset, "Anwenden", 20, 90, ApplyChanges));
        Controls.Add(CreateButton(yOffset, "Alle senden", 120, 90, ResendAll));
        Controls.Add(CreateButton(yOffset, "Schließen", 220, 90, Close));
    }

    private Button CreateButton(int yOffset, string text,  int size,  int width, Action applyChanges)
    {
        var btnApply = new Button
        {
            Text = text,
            Location = new Point(size, yOffset + 10),
            Width = width
        };
        btnApply.Click += (_, _) => applyChanges();
        return btnApply;
    }

    private void LoadCurrentAssignments()
    {
        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            var currentWidget = _widgetService.GetWidgetForScreen(i);
            var dropdown = _screenDropdowns[i];

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

            _originalSelections[i] = dropdown.SelectedIndex;
        }
    }

    private void ApplyChanges()
    {
        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            var dropdown = _screenDropdowns[i];
            var selectedIndex = dropdown.SelectedIndex;

            // Skip if selection hasn't changed
            if (selectedIndex == _originalSelections[i])
                continue;

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

            // Update original selection to new value
            _originalSelections[i] = selectedIndex;
        }
    }

    private void ResendAll()
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
