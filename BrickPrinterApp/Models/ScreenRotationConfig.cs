namespace BrickPrinterApp.Models;

public class ScreenRotationConfig
{
    public bool IsEnabled { get; set; } = false;
    public int RotationIntervalSeconds { get; set; } = 60;
    public List<string> WidgetNames { get; set; } = new();
}
