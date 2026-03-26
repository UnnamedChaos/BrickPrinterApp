using BrickPrinterApp.Models;
using Newtonsoft.Json;

namespace BrickPrinterApp.Services;

public class SettingService
{
    private static readonly string SettingsFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BrickPrinterApp");

    private static readonly string SettingsFile = Path.Combine(SettingsFolder, "settings.json");

    public string EspIpAddress { get; set; } = "localhost:5224";
    public int SelectedScreen { get; set; } = 0;
    public Dictionary<int, string?> WidgetAssignments { get; set; } = new();
    public Dictionary<int, ScreenRotationConfig> RotationConfigs { get; set; } = new();
    public Dictionary<int, List<ConditionalWidgetConfig>> ConditionalConfigs { get; set; } = new();
    public string LastComPort { get; set; } = string.Empty;
    public int LastBaudRate { get; set; } = 115200;
    public int TimeOffsetHours { get; set; } = 0;
    public int NumScreens { get; set; } = 3;
    public string EndpointUrl => $"http://{EspIpAddress}/upload";
    public string PingUrl => $"http://{EspIpAddress}/ping";
    public const int ScreenHeight = 64;
    public const int ScreenWidth = 128;
    public string GetEndpointUrl(int screenId) => $"http://{EspIpAddress}/upload?screen={screenId}";
    public string GetScriptUrl(int screenId) => $"http://{EspIpAddress}/lua?screen={screenId}";
    public string GetStopScriptUrl(int screenId) => $"http://{EspIpAddress}/lua/stop?screen={screenId}";
    public string GetStatusUrl(int screenId) => $"http://{EspIpAddress}/status?screen={screenId}";
    
    public SettingService()
    {
        Load();
    }
    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var data = new SettingsData
            {
                EspIpAddress = EspIpAddress,
                SelectedScreen = SelectedScreen,
                WidgetAssignments = WidgetAssignments,
                RotationConfigs = RotationConfigs,
                ConditionalConfigs = ConditionalConfigs,
                LastComPort = LastComPort,
                LastBaudRate = LastBaudRate,
                TimeOffsetHours = TimeOffsetHours,
                NumScreens = NumScreens
            };
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // Silently fail if we can't save settings
        }
    }

    private void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var data = JsonConvert.DeserializeObject<SettingsData>(json);
                if (data != null)
                {
                    EspIpAddress = data.EspIpAddress ?? EspIpAddress;
                    SelectedScreen = data.SelectedScreen;
                    WidgetAssignments = data.WidgetAssignments ?? new Dictionary<int, string?>();
                    RotationConfigs = data.RotationConfigs ?? new Dictionary<int, ScreenRotationConfig>();
                    ConditionalConfigs = data.ConditionalConfigs ?? new Dictionary<int, List<ConditionalWidgetConfig>>();
                    LastComPort = data.LastComPort ?? LastComPort;
                    LastBaudRate = data.LastBaudRate > 0 ? data.LastBaudRate : LastBaudRate;
                    TimeOffsetHours = data.TimeOffsetHours;
                    NumScreens = data.NumScreens > 0 && data.NumScreens <= 3 ? data.NumScreens : 3;
                }
            }
        }
        catch
        {
            // Use defaults if we can't load settings
        }
    }
}
