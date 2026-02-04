namespace BrickPrinterApp.Services;

public class SettingService
{
    public string EspIpAddress { get; set; } = "192.168.178.50"; // Default
    public string EndpointUrl => $"http://{EspIpAddress}/upload";
}