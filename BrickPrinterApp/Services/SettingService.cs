namespace BrickPrinterApp.Services;

public class SettingService
{
    public string EspIpAddress { get; set; } = "localhost:5224"; // Default
    public string EndpointUrl => $"http://{EspIpAddress}/upload";
    public const int ScreenHeight = 64;
    public const int ScreenWidth = 128;
}