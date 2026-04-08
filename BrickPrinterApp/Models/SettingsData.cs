namespace BrickPrinterApp.Models;

public class SettingsData
{
    public string? EspIpAddress { get; set; }
    public int SelectedScreen { get; set; }
    public Dictionary<int, string?>? WidgetAssignments { get; set; }
    public Dictionary<int, ScreenRotationConfig>? RotationConfigs { get; set; }
    public Dictionary<int, List<ConditionalWidgetConfig>>? ConditionalConfigs { get; set; }
    public Dictionary<int, List<CustomConditionalWidgetConfig>>? CustomConditionalConfigs { get; set; }
    public string? LastComPort { get; set; }
    public int LastBaudRate { get; set; }
    public int TimeOffsetHours { get; set; }
    public int NumScreens { get; set; }
    public string? BambuLabIp { get; set; }
    public string? BambuLabAccessCode { get; set; }
    public string? BambuLabSerial { get; set; }
}
