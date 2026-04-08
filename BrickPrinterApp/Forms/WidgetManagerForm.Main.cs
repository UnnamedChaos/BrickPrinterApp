using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using BrickPrinterApp.Services;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp.Forms;

/// <summary>
/// Main form for managing widget assignments to displays.
/// This is the core partial class containing fields, constructor, and coordination logic.
/// </summary>
public partial class WidgetManagerForm : MaterialForm
{
    private readonly WidgetService _widgetService;
    private readonly SettingService _settingService;
    private readonly RotationManagerService _rotationManager;
    private readonly ActiveWindowWatcherService _windowWatcher;
    private readonly ConditionalWidgetMonitorService _conditionalMonitor;
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

    public WidgetManagerForm(WidgetService widgetService, SettingService settingService, RotationManagerService rotationManager, ActiveWindowWatcherService windowWatcher, ConditionalWidgetMonitorService conditionalMonitor)
    {
        _widgetService = widgetService;
        _settingService = settingService;
        _rotationManager = rotationManager;
        _windowWatcher = windowWatcher;
        _conditionalMonitor = conditionalMonitor;
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
                // Single widget mode - load the SAVED base widget assignment, not the currently active widget
                // (currently active might be a conditional override)
                var savedWidgetName = _settingService.WidgetAssignments.GetValueOrDefault(i);

                if (string.IsNullOrEmpty(savedWidgetName))
                {
                    dropdown.SelectedIndex = 0;
                }
                else
                {
                    var index = _allWidgets.FindIndex(w => w.Name == savedWidgetName);
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

        // Force conditional monitor to re-check immediately
        // If a conditional widget should override, it will switch now
        _conditionalMonitor.ForceCheck();
    }

    private async void ResendAll()
    {
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            // Simply resend whatever is currently showing on each screen
            // This preserves conditional widgets, rotation state, etc.
            await _widgetService.ResendCurrentWidgetAsync(i);
        }
    }
}
