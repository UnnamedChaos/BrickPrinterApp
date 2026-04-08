# Custom Conditional Widgets

This folder contains the new generic conditional widget system that allows you to display widgets based on custom logic.

## Overview

The conditional widget system allows you to automatically switch widgets on your displays based on custom conditions you define in code. Unlike the older window/process-based system, this system is completely generic and extensible.

## How It Works

1. **Interface**: All conditional widgets implement `IConditionalWidget`
2. **Priority**: Conditions are checked in list order - the first matching condition wins
3. **Monitoring**: A background service checks conditions every 2 seconds
4. **Restoration**: When conditions are no longer met, the original widget is restored

## Key Components

### IConditionalWidget Interface

```csharp
public interface IConditionalWidget
{
    object Widget { get; }                    // The widget to display
    string ConditionDescription { get; }      // Human-readable description shown in UI
    bool IsConditionMet();                    // Returns true when condition is met
}
```

### ConditionalWidgetMonitorService

Background service that:
- Periodically checks all registered conditions
- Switches to conditional widgets when conditions are met
- Restores base widgets when conditions no longer apply
- Respects priority order (first registered = highest priority)

### Integrated UI in Widget Manager

The conditional widgets are managed in the main Widget Manager form:
- View all conditional widgets per screen (both custom and process-based)
- Custom widgets shown with "Custom" type in blue
- Process/window widgets shown with "Process" type in black/gray
- Change priority order (move up/down) for custom widgets
- Delete conditional widgets
- Accessible via tray menu: "Widget Manager" → Conditional Widgets section

## Built-in Conditional Widgets

### AlwaysTrueConditionalWidget
Always active (for testing/demo purposes)

### ProcessRunningConditionalWidget
Activates when a specific process is running
```csharp
new ProcessRunningConditionalWidget(widget, "notepad")
```

### TimeRangeConditionalWidget
Activates during specific time range (supports overnight ranges)
```csharp
new TimeRangeConditionalWidget(widget, 
    new TimeSpan(9, 0, 0),   // 9:00 AM
    new TimeSpan(17, 0, 0))  // 5:00 PM
```

### WeekdayConditionalWidget
Activates on specific days of the week
```csharp
new WeekdayConditionalWidget(widget,
    DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
    DayOfWeek.Thursday, DayOfWeek.Friday)
```

## Creating Custom Conditional Widgets

1. Create a new class implementing `IConditionalWidget`
2. Implement the three required properties:
   - `Widget` - return the IWidget or IScriptWidget to display
   - `ConditionDescription` - return a human-readable description
   - `IsConditionMet()` - return true when your condition is met

Example:
```csharp
public class CpuTemperatureConditionalWidget : IConditionalWidget
{
    private readonly object _widget;
    private readonly float _thresholdCelsius;

    public CpuTemperatureConditionalWidget(object widget, float thresholdCelsius)
    {
        _widget = widget;
        _thresholdCelsius = thresholdCelsius;
    }

    public object Widget => _widget;

    public string ConditionDescription => $"CPU > {_thresholdCelsius}°C";

    public bool IsConditionMet()
    {
        // Your custom logic here
        var temp = GetCpuTemperature();
        return temp > _thresholdCelsius;
    }
}
```

## Registering Conditional Widgets

Conditional widgets must be registered in `Program.cs` after services are initialized:

```csharp
var conditionalMonitor = host.Services.GetRequiredService<ConditionalWidgetMonitorService>();
conditionalMonitor.Initialize();

// Register conditional widgets (priority = registration order)
// Screen 0: Show CPU widget when notepad is running
var notepadCondition = new ProcessRunningConditionalWidget(
    new CpuSimpleWidget(displayService), "notepad");
conditionalMonitor.RegisterConditionalWidget(0, notepadCondition);

// Screen 0: Show weather during work hours (lower priority than notepad)
var workHoursCondition = new TimeRangeConditionalWidget(
    new WeatherWidget(displayService),
    new TimeSpan(9, 0, 0),
    new TimeSpan(17, 0, 0));
conditionalMonitor.RegisterConditionalWidget(0, workHoursCondition);
```

## Priority System

Conditional widgets are evaluated in the order they are registered:
- **First registered = Highest priority**
- The first condition that returns `true` wins
- Lower priority conditions are only checked if higher ones are `false`

Example priority order:
1. Process "notepad" running → Show CPU widget (highest)
2. Time 9 AM - 5 PM → Show weather widget
3. Monday-Friday → Show stock widget (lowest)

If notepad is running during work hours on a weekday, the CPU widget is shown (highest priority).

## Management UI

Access the conditional widget manager via the tray menu → "Widget Manager" → scroll to "Conditional Widgets" section

Features:
- View both custom and process-based conditional widgets in one list
- Custom widgets displayed with blue color and "Custom" type
- Process/window widgets displayed with black/gray color and "Process" type
- See condition descriptions in plain language
- Reorder custom widget priorities (move up/down)
- Add new process/window conditions (via "+ Add" button)
- Delete conditional widgets
- Priority order: Custom widgets (top), then Process widgets (bottom)
- Changes take effect immediately

## Tips

1. **Keep conditions simple**: Complex conditions slow down the check loop
2. **Use priority wisely**: Most specific conditions should be highest priority
3. **Test your conditions**: Use the UI to verify widgets activate correctly
4. **Handle exceptions**: Wrap risky code in try-catch to prevent crashes
5. **Descriptive names**: Write clear `ConditionDescription` text for the UI

## Differences from Old System

| Old System (Process/Window) | New System (Custom Logic) |
|----------------------------|---------------------------|
| Process/window title matching only | Any custom logic you can code |
| Configured in UI | Registered in Program.cs |
| Saved to settings.json | In-memory only (not persisted) |
| Limited flexibility | Unlimited flexibility |
| No priority ordering | Explicit priority via list order |

Both systems work together - use the old system for simple process/window rules, and the new system for complex custom logic.
