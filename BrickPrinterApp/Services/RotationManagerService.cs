using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using Timer = System.Threading.Timer;

namespace BrickPrinterApp.Services;

public class RotationManagerService : IDisposable
{
    private readonly WidgetService _widgetService;
    private readonly SettingService _settingService;
    private readonly Dictionary<int, ScreenRotationConfig> _rotationConfigs = new();
    private readonly Dictionary<int, int> _currentIndices = new();
    private readonly Dictionary<int, Timer?> _rotationTimers = new();
    private readonly object _lock = new();

    private const int MinRotationIntervalSeconds = 10;

    public RotationManagerService(WidgetService widgetService, SettingService settingService)
    {
        _widgetService = widgetService;
        _settingService = settingService;
    }

    public void EnableRotation(int screenId, ScreenRotationConfig config)
    {
        if (config.WidgetNames.Count == 0)
        {
            DisableRotation(screenId);
            return;
        }

        // Enforce minimum interval
        if (config.RotationIntervalSeconds < MinRotationIntervalSeconds)
        {
            config.RotationIntervalSeconds = MinRotationIntervalSeconds;
        }

        lock (_lock)
        {
            StopRotationTimer(screenId);

            _rotationConfigs[screenId] = config;
            _currentIndices[screenId] = 0;

            // Send first widget immediately
            SendCurrentWidget(screenId);

            // Start rotation timer
            StartRotationTimer(screenId);

            SaveConfigs();
        }
    }

    public void DisableRotation(int screenId)
    {
        lock (_lock)
        {
            StopRotationTimer(screenId);
            _rotationConfigs.Remove(screenId);
            _currentIndices.Remove(screenId);
            SaveConfigs();
        }
    }

    public bool IsRotationEnabled(int screenId)
    {
        lock (_lock)
        {
            return _rotationConfigs.ContainsKey(screenId) &&
                   _rotationConfigs[screenId].IsEnabled &&
                   _rotationConfigs[screenId].WidgetNames.Count > 0;
        }
    }

    public ScreenRotationConfig? GetConfig(int screenId)
    {
        lock (_lock)
        {
            return _rotationConfigs.GetValueOrDefault(screenId);
        }
    }

    public object? GetCurrentRotatingWidget(int screenId)
    {
        lock (_lock)
        {
            if (!_rotationConfigs.TryGetValue(screenId, out var config))
                return null;

            var index = _currentIndices.GetValueOrDefault(screenId, 0);
            if (index >= config.WidgetNames.Count)
                return null;

            var widgetName = config.WidgetNames[index];
            return FindWidgetByName(widgetName);
        }
    }

    public void RotateNow(int screenId)
    {
        lock (_lock)
        {
            RotateToNextWidget(screenId);

            // Reset the timer
            if (_rotationConfigs.TryGetValue(screenId, out var config))
            {
                StopRotationTimer(screenId);
                StartRotationTimer(screenId);
            }
        }
    }

    public void LoadSavedConfigs()
    {
        lock (_lock)
        {
            foreach (var (screenId, config) in _settingService.RotationConfigs)
            {
                if (config.IsEnabled && config.WidgetNames.Count > 0)
                {
                    _rotationConfigs[screenId] = config;
                    _currentIndices[screenId] = 0;

                    // Send first widget
                    SendCurrentWidget(screenId);

                    // Start timer
                    StartRotationTimer(screenId);
                }
            }
        }
    }

    private void RotateToNextWidget(int screenId)
    {
        if (!_rotationConfigs.TryGetValue(screenId, out var config))
            return;

        if (config.WidgetNames.Count == 0)
            return;

        // Move to next widget (wrap around)
        var currentIndex = _currentIndices.GetValueOrDefault(screenId, 0);
        currentIndex = (currentIndex + 1) % config.WidgetNames.Count;
        _currentIndices[screenId] = currentIndex;

        Console.WriteLine($"Rotation: Screen {screenId} rotating to widget {currentIndex + 1}/{config.WidgetNames.Count}: {config.WidgetNames[currentIndex]}");

        // Send only to THIS screen
        SendCurrentWidget(screenId);
    }

    private void SendCurrentWidget(int screenId)
    {
        if (!_rotationConfigs.TryGetValue(screenId, out var config))
            return;

        var index = _currentIndices.GetValueOrDefault(screenId, 0);
        if (index >= config.WidgetNames.Count)
            return;

        var widgetName = config.WidgetNames[index];
        var widget = FindWidgetByName(widgetName);

        if (widget == null)
        {
            Console.WriteLine($"Rotation: Widget '{widgetName}' not found for screen {screenId}");
            return;
        }

        // Use internal methods that don't trigger save
        if (widget is IScriptWidget scriptWidget)
        {
            _widgetService.AssignScriptWidgetToScreenInternal(screenId, scriptWidget);
        }
        else if (widget is IWidget regularWidget)
        {
            _widgetService.AssignWidgetToScreenInternal(screenId, regularWidget);
        }
    }

    private object? FindWidgetByName(string widgetName)
    {
        // Check if it's a script widget
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

    private void StartRotationTimer(int screenId)
    {
        if (!_rotationConfigs.TryGetValue(screenId, out var config))
            return;

        var interval = TimeSpan.FromSeconds(config.RotationIntervalSeconds);
        _rotationTimers[screenId] = new Timer(
            _ =>
            {
                lock (_lock)
                {
                    RotateToNextWidget(screenId);
                }
            },
            null,
            interval,
            interval);
    }

    private void StopRotationTimer(int screenId)
    {
        if (_rotationTimers.TryGetValue(screenId, out var timer) && timer != null)
        {
            timer.Dispose();
            _rotationTimers[screenId] = null;
        }
    }

    private void SaveConfigs()
    {
        _settingService.RotationConfigs.Clear();
        foreach (var (screenId, config) in _rotationConfigs)
        {
            _settingService.RotationConfigs[screenId] = config;
        }
        _settingService.Save();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var screenId in _rotationTimers.Keys.ToList())
            {
                StopRotationTimer(screenId);
            }
        }
    }
}
