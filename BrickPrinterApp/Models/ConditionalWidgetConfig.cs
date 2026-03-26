namespace BrickPrinterApp.Models;

public class ConditionalWidgetConfig
{
    public bool IsEnabled { get; set; } = false;
    public string WidgetName { get; set; } = string.Empty;
    public string? ProcessName { get; set; }
    public string? WindowTitleContains { get; set; }
    public bool MatchBothConditions { get; set; } = false;
}
