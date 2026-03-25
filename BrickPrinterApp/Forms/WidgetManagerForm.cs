using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp.Forms;

public class WidgetManagerForm : MaterialForm
{
    private readonly WidgetService _widgetService;
    private readonly SettingService _settingService;
    private readonly MaterialSkinManager _materialSkinManager;
    private readonly List<MaterialComboBox> _screenDropdowns = new();
    private readonly List<(string Name, object Widget, bool IsScript)> _allWidgets = new();
    private readonly int[] _originalSelections;

    public WidgetManagerForm(WidgetService widgetService, SettingService settingService)
    {
        _widgetService = widgetService;
        _settingService = settingService;
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
            var label = new MaterialLabel
            {
                Text = $"Screen {i}:",
                Location = new Point(20, yOffset + 8),
                AutoSize = true,
                FontType = MaterialSkinManager.fontType.Subtitle1
            };

            var dropdown = new MaterialComboBox
            {
                Location = new Point(120, yOffset),
                Width = 250
            };

            dropdown.Items.Add("(Kein Widget)");

            foreach (var (name, _, _) in _allWidgets)
            {
                dropdown.Items.Add(name);
            }

            _screenDropdowns.Add(dropdown);
            Controls.Add(label);
            Controls.Add(dropdown);

            yOffset += 50;
        }

        Controls.Add(CreateButton(yOffset, "Anwenden", 20, 110, ApplyChanges, MaterialButton.MaterialButtonType.Contained));
        Controls.Add(CreateButton(yOffset, "Alle senden", 140, 120, ResendAll, MaterialButton.MaterialButtonType.Outlined));
        Controls.Add(CreateButton(yOffset, "Schließen", 270, 100, Close, MaterialButton.MaterialButtonType.Text));

        // Set form height dynamically based on number of screens
        // Add extra height for MaterialForm title bar (64px)
        int formHeight = yOffset + 120;
        Size = new Size(420, formHeight);
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
        for (int i = 0; i < _settingService.NumScreens; i++)
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
        for (int i = 0; i < _settingService.NumScreens; i++)
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
