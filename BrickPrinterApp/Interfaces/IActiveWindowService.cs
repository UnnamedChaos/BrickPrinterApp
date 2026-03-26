namespace BrickPrinterApp.Interfaces;

public interface IActiveWindowService
{
    ActiveWindowInfo GetActiveWindow();
    bool IsYouTubeActive();
}

public class ActiveWindowInfo
{
    public string WindowTitle { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public bool IsYouTube { get; set; }
    public int ProcessId { get; set; }
}
