using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using BrickPrinterApp.Widgets.Conditional;
using Timer = System.Threading.Timer;

namespace BrickPrinterApp.Services;

/// <summary>
/// Service that periodically checks conditional widgets and activates them based on priority
/// </summary>
public class ConditionalWidgetMonitorService : IDisposable
{
    private readonly WidgetService _widgetService;
    private readonly SettingService _settingService;
    private readonly RotationManagerService _rotationManager;

    // Track conditional widgets per screen (priority = list order)
    private readonly Dictionary<int, List<IConditionalWidget>> _conditionalWidgets = new();

    // Track which screens are currently showing a conditional widget
    private readonly Dictionary<int, IConditionalWidget?> _activeConditionalWidget = new();

    // Track the "base" widget for each screen (what to restore when condition ends)
    private readonly Dictionary<int, object?> _baseWidgets = new();

    private readonly object _lock = new();
    private Timer? _monitorTimer;
    private readonly TimeSpan _checkInterval = TimeSpan.FromSeconds(2); // Check every 2 seconds

    public ConditionalWidgetMonitorService(
        WidgetService widgetService,
        SettingService settingService,
        RotationManagerService rotationManager)
    {
        _widgetService = widgetService;
        _settingService = settingService;
        _rotationManager = rotationManager;

        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            _conditionalWidgets[i] = new List<IConditionalWidget>();
        }
    }

    /// <summary>
    /// Initialize and start monitoring
    /// </summary>
    public void Initialize()
    {
        Console.WriteLine("ConditionalMonitor: Initializing...");

        // Load saved custom conditional widgets
        LoadFromSettings();

        // Log what was loaded
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            var count = _conditionalWidgets[i].Count;
            if (count > 0)
            {
                Console.WriteLine($"ConditionalMonitor: Screen {i} has {count} conditional widget(s):");
                foreach (var cw in _conditionalWidgets[i])
                {
                    Console.WriteLine($"  - {cw.ConditionDescription}");
                }
            }
        }

        // Capture initial base widgets for all screens
        for (int i = 0; i < _settingService.NumScreens; i++)
        {
            CaptureBaseWidget(i);
            var baseWidget = _baseWidgets[i];
            var baseWidgetName = baseWidget switch
            {
                IWidget w => w.Name,
                IScriptWidget sw => $"[Lua] {sw.Name}",
                _ => "null"
            };
            Console.WriteLine($"ConditionalMonitor: Screen {i} base widget: {baseWidgetName}");
        }

        // Start monitoring timer
        Console.WriteLine($"ConditionalMonitor: Starting condition monitor (check every {_checkInterval.TotalSeconds}s)");

        // Do an immediate check on startup to ensure correct widget is showing
        Console.WriteLine("ConditionalMonitor: Running initial condition check...");
        CheckAllConditions();

        _monitorTimer = new Timer(_ => CheckAllConditions(), null, _checkInterval, _checkInterval);
    }

    /// <summary>
    /// Register a conditional widget for a specific screen
    /// </summary>
    public void RegisterConditionalWidget(int screenId, IConditionalWidget conditionalWidget)
    {
        lock (_lock)
        {
            if (!_conditionalWidgets[screenId].Contains(conditionalWidget))
            {
                _conditionalWidgets[screenId].Add(conditionalWidget);
            }
        }
    }

    /// <summary>
    /// Remove a conditional widget from a screen
    /// </summary>
    public void RemoveConditionalWidget(int screenId, IConditionalWidget conditionalWidget)
    {
        bool wasActive = false;

        lock (_lock)
        {
            _conditionalWidgets[screenId].Remove(conditionalWidget);

            // Check if this was the currently active conditional widget
            if (_activeConditionalWidget.TryGetValue(screenId, out var activeWidget) && activeWidget == conditionalWidget)
            {
                wasActive = true;
                Console.WriteLine($"ConditionalMonitor: Removed active conditional widget from screen {screenId}");
            }
        }

        // If we removed the currently active conditional widget, immediately restore base widget
        if (wasActive)
        {
            Console.WriteLine($"ConditionalMonitor: Restoring base widget for screen {screenId}");
            RestoreBaseWidget(screenId);

            lock (_lock)
            {
                _activeConditionalWidget.Remove(screenId);
            }
        }

        // Re-check all conditions in case another conditional should now activate
        EvaluateConditionsForScreen(screenId);
    }

    /// <summary>
    /// Clear all conditional widgets for a screen
    /// </summary>
    public void ClearConditionalWidgets(int screenId)
    {
        bool wasActive = false;

        lock (_lock)
        {
            _conditionalWidgets[screenId].Clear();

            // Check if a conditional was currently active
            if (_activeConditionalWidget.ContainsKey(screenId))
            {
                wasActive = true;
                Console.WriteLine($"ConditionalMonitor: Cleared all conditional widgets from screen {screenId}");
            }
        }

        // If a conditional was active, restore base widget
        if (wasActive)
        {
            Console.WriteLine($"ConditionalMonitor: Restoring base widget for screen {screenId}");
            RestoreBaseWidget(screenId);

            lock (_lock)
            {
                _activeConditionalWidget.Remove(screenId);
            }
        }
    }

    /// <summary>
    /// Get all conditional widgets for a screen (in priority order)
    /// </summary>
    public IReadOnlyList<IConditionalWidget> GetConditionalWidgets(int screenId)
    {
        lock (_lock)
        {
            return _conditionalWidgets[screenId].ToList().AsReadOnly();
        }
    }

    /// <summary>
    /// Set conditional widgets for a screen (replaces existing list)
    /// </summary>
    public void SetConditionalWidgets(int screenId, List<IConditionalWidget> widgets)
    {
        lock (_lock)
        {
            _conditionalWidgets[screenId] = widgets.ToList();
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

    /// <summary>
    /// Force an immediate condition check (useful after user manually changes widget assignment)
    /// </summary>
    public void ForceCheck()
    {
        Console.WriteLine("ConditionalMonitor: Force check triggered");
        CheckAllConditions();
    }

    private void CheckAllConditions()
    {
        try
        {
            for (int screenId = 0; screenId < _settingService.NumScreens; screenId++)
            {
                EvaluateConditionsForScreen(screenId);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in conditional monitor: {ex.Message}");
        }
    }

    private void EvaluateConditionsForScreen(int screenId)
    {
        List<IConditionalWidget> widgets;
        lock (_lock)
        {
            widgets = _conditionalWidgets[screenId].ToList();
        }

        if (widgets.Count == 0)
        {
            return; // No conditional widgets for this screen
        }

        // Find first matching condition (priority = list order)
        IConditionalWidget? matchingWidget = null;
        foreach (var conditionalWidget in widgets)
        {
            try
            {
                bool conditionMet = conditionalWidget.IsConditionMet();

                if (conditionMet)
                {
                    matchingWidget = conditionalWidget;
                    break; // First match wins (highest priority)
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConditionalMonitor: Error checking condition for screen {screenId} - {ex.Message}");
            }
        }

        lock (_lock)
        {
            var currentConditional = _activeConditionalWidget.GetValueOrDefault(screenId);

            if (matchingWidget != null)
            {
                // Condition matched - switch to conditional widget if different
                if (currentConditional != matchingWidget)
                {
                    Console.WriteLine($"ConditionalMonitor: Screen {screenId} switched to '{matchingWidget.ConditionDescription}'");

                    // Store current widget as base if not already in conditional mode
                    if (currentConditional == null)
                    {
                        _baseWidgets[screenId] = _widgetService.GetWidgetForScreen(screenId);
                    }

                    SwitchToConditionalWidget(screenId, matchingWidget);
                    _activeConditionalWidget[screenId] = matchingWidget;
                }
            }
            else if (currentConditional != null)
            {
                // Condition no longer matched - restore base widget
                Console.WriteLine($"ConditionalMonitor: Screen {screenId} restored to base widget");
                RestoreBaseWidget(screenId);
                _activeConditionalWidget.Remove(screenId);
            }
        }
    }

    private void SwitchToConditionalWidget(int screenId, IConditionalWidget conditionalWidget)
    {
        var widget = conditionalWidget.Widget;

        // Use internal methods to avoid triggering saves
        if (widget is IScriptWidget scriptWidget)
        {
            _widgetService.AssignScriptWidgetToScreenInternal(screenId, scriptWidget);
        }
        else if (widget is IWidget regularWidget)
        {
            _widgetService.AssignWidgetToScreenInternal(screenId, regularWidget);
        }
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
        }
    }

    public bool IsConditionalActive(int screenId)
    {
        lock (_lock)
        {
            return _activeConditionalWidget.ContainsKey(screenId);
        }
    }

    /// <summary>
    /// Save current conditional widgets to settings
    /// </summary>
    public void SaveToSettings()
    {
        lock (_lock)
        {
            _settingService.CustomConditionalConfigs.Clear();

            for (int screenId = 0; screenId < _settingService.NumScreens; screenId++)
            {
                var configs = new List<CustomConditionalWidgetConfig>();

                foreach (var conditionalWidget in _conditionalWidgets[screenId])
                {
                    var config = ConvertToConfig(conditionalWidget);
                    if (config != null)
                    {
                        configs.Add(config);
                    }
                }

                if (configs.Count > 0)
                {
                    _settingService.CustomConditionalConfigs[screenId] = configs;
                }
            }

            _settingService.Save();
        }
    }

    /// <summary>
    /// Load conditional widgets from settings
    /// </summary>
    private void LoadFromSettings()
    {
        lock (_lock)
        {
            foreach (var kvp in _settingService.CustomConditionalConfigs)
            {
                int screenId = kvp.Key;
                if (screenId >= _settingService.NumScreens) continue;

                foreach (var config in kvp.Value)
                {
                    var conditionalWidget = ConvertFromConfig(config);
                    if (conditionalWidget != null)
                    {
                        _conditionalWidgets[screenId].Add(conditionalWidget);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Convert IConditionalWidget to CustomConditionalWidgetConfig
    /// </summary>
    private CustomConditionalWidgetConfig? ConvertToConfig(IConditionalWidget conditionalWidget)
    {
        var widgetName = conditionalWidget.Widget switch
        {
            IWidget w => w.Name,
            IScriptWidget sw => $"[Lua] {sw.Name}",
            _ => null
        };

        if (widgetName == null) return null;

        var config = new CustomConditionalWidgetConfig
        {
            WidgetName = widgetName
        };

        switch (conditionalWidget)
        {
            case AlwaysTrueConditionalWidget:
                config.ConditionType = ConditionalWidgetType.AlwaysActive;
                break;

            case ProcessRunningConditionalWidget processWidget:
                config.ConditionType = ConditionalWidgetType.ProcessRunning;
                // Use reflection to get the private _processName field
                var processField = typeof(ProcessRunningConditionalWidget).GetField("_processName",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                config.ProcessName = processField?.GetValue(processWidget) as string;
                break;

            case TimeRangeConditionalWidget timeWidget:
                config.ConditionType = ConditionalWidgetType.TimeRange;
                var startField = typeof(TimeRangeConditionalWidget).GetField("_startTime",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var endField = typeof(TimeRangeConditionalWidget).GetField("_endTime",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                config.StartTime = (TimeSpan?)startField?.GetValue(timeWidget);
                config.EndTime = (TimeSpan?)endField?.GetValue(timeWidget);
                break;

            case WeekdayConditionalWidget weekdayWidget:
                config.ConditionType = ConditionalWidgetType.Weekday;
                var daysField = typeof(WeekdayConditionalWidget).GetField("_activeDays",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                config.ActiveDays = daysField?.GetValue(weekdayWidget) as DayOfWeek[];
                break;

            case BambuLabConditionalWidget:
                config.ConditionType = ConditionalWidgetType.BambuLab;
                break;

            default:
                return null; // Unknown type
        }

        return config;
    }

    /// <summary>
    /// Convert CustomConditionalWidgetConfig to IConditionalWidget
    /// </summary>
    private IConditionalWidget? ConvertFromConfig(CustomConditionalWidgetConfig config)
    {
        // Find the widget by name
        object? widget = null;

        if (config.WidgetName.StartsWith("[Lua] "))
        {
            var scriptWidgetName = config.WidgetName.Substring(6);
            widget = _widgetService.AvailableScriptWidgets.FirstOrDefault(w => w.Name == scriptWidgetName);
        }
        else
        {
            widget = _widgetService.AvailableWidgets.FirstOrDefault(w => w.Name == config.WidgetName);
        }

        if (widget == null) return null;

        return config.ConditionType switch
        {
            ConditionalWidgetType.AlwaysActive => new AlwaysTrueConditionalWidget(widget),
            ConditionalWidgetType.ProcessRunning when config.ProcessName != null =>
                new ProcessRunningConditionalWidget(widget, config.ProcessName),
            ConditionalWidgetType.TimeRange when config.StartTime != null && config.EndTime != null =>
                new TimeRangeConditionalWidget(widget, config.StartTime.Value, config.EndTime.Value),
            ConditionalWidgetType.Weekday when config.ActiveDays != null =>
                new WeekdayConditionalWidget(widget, config.ActiveDays),
            ConditionalWidgetType.BambuLab =>
                new BambuLabConditionalWidget(widget),
            _ => null
        };
    }

    public void Dispose()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }
}
