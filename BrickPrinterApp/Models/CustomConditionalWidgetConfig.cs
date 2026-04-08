namespace BrickPrinterApp.Models;

/// <summary>
/// Serializable configuration for custom conditional widgets
/// </summary>
public class CustomConditionalWidgetConfig
{
    public ConditionalWidgetType ConditionType { get; set; }
    public string WidgetName { get; set; } = string.Empty;

    // For ProcessRunning type
    public string? ProcessName { get; set; }

    // For TimeRange type
    public TimeSpan? StartTime { get; set; }
    public TimeSpan? EndTime { get; set; }

    // For Weekday type
    public DayOfWeek[]? ActiveDays { get; set; }
}

public enum ConditionalWidgetType
{
    AlwaysActive,
    ProcessRunning,
    TimeRange,
    Weekday,
    BambuLab
}
