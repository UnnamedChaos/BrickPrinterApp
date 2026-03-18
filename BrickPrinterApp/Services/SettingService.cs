namespace BrickPrinterApp.Services;

public class SettingService
{
    public string EspIpAddress { get; set; } = "localhost:5224"; // Default
    public int SelectedScreen { get; set; } = 0; // Default screen
    public string EndpointUrl => $"http://{EspIpAddress}/upload";
    public string PingUrl => $"http://{EspIpAddress}/ping";
    public const int ScreenHeight = 64;
    public const int ScreenWidth = 128;
    public const int NumScreens = 3;

    public string GetEndpointUrl(int screenId) => $"http://{EspIpAddress}/upload?screen={screenId}";
}