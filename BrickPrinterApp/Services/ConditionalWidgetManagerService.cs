using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;

namespace BrickPrinterApp.Services;

public class ConditionalWidgetManagerService : IDisposable
{
    private readonly WidgetService _widgetService;
    private readonly SettingService _settingService;
    private readonly RotationManagerService _rotationManager;
    private readonly ActiveWindowWatcherService _windowWatcher;

    // Track which screens are currently showing a conditional widget
    private readonly Dictionary<int, string> _activeConditionalWidget = new();

    // Track the "base" widget for each screen (what to restore when condition ends)
    private readonly Dictionary<int, object?> _baseWidgets = new();

    private readonly object _lock = new();

    public ConditionalWidgetManagerService(
        WidgetService widgetService,
        SettingService settingService,
        RotationManagerService rotationManager,
        ActiveWindowWatcherService windowWatcher)
    {
        _widgetService = widgetService;
        _settingService = settingService;
        _rotationManager = rotationManager;
        _windowWatcher = windowWatcher;

        // Subscribe to window changes
        _windowWatcher.ActiveWindowChanged += OnActiveWindowChanged;
    }

    public void Initialize()
    {
        // Capture initial base widgets for all screens
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            CaptureBaseWidget(i);
        }
    }

    private void CaptureBaseWidget(int screenId)
    {
        var widget = _widgetService.GetWidgetForScreen(screenId);
        lock (_lock)
        {
            _baseWidgets[screenId] = widget;
        }
    }

    private void OnActiveWindowChanged(object? sender, ActiveWindowInfo windowInfo)
    {
        EvaluateConditions(windowInfo);
    }

    private void EvaluateConditions(ActiveWindowInfo windowInfo)
    {
        for (int screenId = 0; screenId < _settingService.NumScreens; screenId++)
        {
            if (!_settingService.ConditionalConfigs.TryGetValue(screenId, out var configs))
                continue;

            // Find first matching enabled condition
            ConditionalWidgetConfig? matchingConfig = null;
            foreach (var config in configs)
            {
                if (!config.IsEnabled) continue;

                if (EvaluateCondition(config, windowInfo))
                {
                    matchingConfig = config;
                    break;
                }
            }

            lock (_lock)
            {
                var currentConditional = _activeConditionalWidget.GetValueOrDefault(screenId);

                if (matchingConfig != null)
                {
                    // Condition matched - switch to conditional widget
                    if (currentConditional != matchingConfig.WidgetName)
                    {
                        // Store current widget as base if not already in conditional mode
                        if (currentConditional == null)
                        {
                            _baseWidgets[screenId] = _widgetService.GetWidgetForScreen(screenId);
                        }

                        SwitchToConditionalWidget(screenId, matchingConfig.WidgetName);
                        _activeConditionalWidget[screenId] = matchingConfig.WidgetName;
                    }
                }
                else if (currentConditional != null)
                {
                    // Condition no longer matched - restore base widget
                    RestoreBaseWidget(screenId);
                    _activeConditionalWidget.Remove(screenId);
                }
            }
        }
    }

    private bool EvaluateCondition(ConditionalWidgetConfig config, ActiveWindowInfo windowInfo)
    {
        bool hasProcessCondition = !string.IsNullOrEmpty(config.ProcessName);
        bool hasTitleCondition = !string.IsNullOrEmpty(config.WindowTitleContains);

        // No conditions set = never match
        if (!hasProcessCondition && !hasTitleCondition)
            return false;

        bool processMatch = !hasProcessCondition ||
            windowInfo.ProcessName.Contains(config.ProcessName!, StringComparison.OrdinalIgnoreCase);

        bool titleMatch = !hasTitleCondition ||
            windowInfo.WindowTitle.Contains(config.WindowTitleContains!, StringComparison.OrdinalIgnoreCase);

        if (config.MatchBothConditions)
        {
            // AND logic: both must match (if specified)
            return processMatch && titleMatch;
        }
        else
        {
            // OR logic: at least one must match
            if (hasProcessCondition && hasTitleCondition)
                return processMatch || titleMatch;
            else if (hasProcessCondition)
                return processMatch;
            else
                return titleMatch;
        }
    }

    private void SwitchToConditionalWidget(int screenId, string widgetName)
    {
        var widget = FindWidgetByName(widgetName);
        if (widget == null)
        {
            Console.WriteLine($"Conditional: Widget '{widgetName}' not found");
            return;
        }

        // Use internal methods to avoid triggering saves
        if (widget is IScriptWidget scriptWidget)
        {
            _widgetService.AssignScriptWidgetToScreenInternal(screenId, scriptWidget);
        }
        else if (widget is IWidget regularWidget)
        {
            _widgetService.AssignWidgetToScreenInternal(screenId, regularWidget);
        }

        Console.WriteLine($"Conditional: Screen {screenId} switched to '{widgetName}'");
    }

    private void RestoreBaseWidget(int screenId)
    {
        // Check if rotation is active for this screen
        if (_rotationManager.IsRotationEnabled(screenId))
        {
            // Let rotation manager handle it by getting the current rotation widget
            var rotatingWidget = _rotationManager.GetCurrentRotatingWidget(screenId);
            if (rotatingWidget is IScriptWidget scriptWidget)
            {
                _widgetService.AssignScriptWidgetToScreenInternal(screenId, scriptWidget);
            }
            else if (rotatingWidget is IWidget regularWidget)
            {
                _widgetService.AssignWidgetToScreenInternal(screenId, regularWidget);
            }
            Console.WriteLine($"Conditional: Screen {screenId} restored to rotating widget");
        }
        else
        {
            // Restore base widget
            var baseWidget = _baseWidgets.GetValueOrDefault(screenId);
            if (baseWidget is IScriptWidget scriptWidget)
            {
                _widgetService.AssignScriptWidgetToScreenInternal(screenId, scriptWidget);
            }
            else if (baseWidget is IWidget regularWidget)
            {
                _widgetService.AssignWidgetToScreenInternal(screenId, regularWidget);
            }
            Console.WriteLine($"Conditional: Screen {screenId} restored to base widget");
        }
    }

    private object? FindWidgetByName(string widgetName)
    {
        if (widgetName.StartsWith("[Lua] "))
        {
            var scriptName = widgetName.Substring(6);
            return _widgetService.AvailableScriptWidgets.FirstOrDefault(w => w.Name == scriptName);
        }
        else
        {
            return _widgetService.AvailableWidgets.FirstOrDefault(w => w.Name == widgetName);
        }
    }

    public bool IsConditionalActive(int screenId)
    {
        lock (_lock)
        {
            return _activeConditionalWidget.ContainsKey(screenId);
        }
    }

    public void Dispose()
    {
        _windowWatcher.ActiveWindowChanged -= OnActiveWindowChanged;
    }
}
