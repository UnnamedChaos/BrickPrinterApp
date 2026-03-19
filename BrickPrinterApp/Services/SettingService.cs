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
    public string EndpointUrl => $"http://{EspIpAddress}/upload";
    public string PingUrl => $"http://{EspIpAddress}/ping";
    public const int ScreenHeight = 64;
    public const int ScreenWidth = 128;
    public const int NumScreens = 3;

    public SettingService()
    {
        Load();
    }

    public string GetEndpointUrl(int screenId) => $"http://{EspIpAddress}/upload?screen={screenId}";

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsFolder);
            var data = new SettingsData
            {
                EspIpAddress = EspIpAddress,
                SelectedScreen = SelectedScreen
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
                }
            }
        }
        catch
        {
            // Use defaults if we can't load settings
        }
    }

    private class SettingsData
    {
        public string? EspIpAddress { get; set; }
        public int SelectedScreen { get; set; }
    }
}
