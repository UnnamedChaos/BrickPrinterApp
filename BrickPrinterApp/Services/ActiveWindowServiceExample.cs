using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

/// <summary>
/// Example usage of IActiveWindowService
/// </summary>
public class ActiveWindowServiceExample
{
    private readonly IActiveWindowService _activeWindowService;

    public ActiveWindowServiceExample(IActiveWindowService activeWindowService)
    {
        _activeWindowService = activeWindowService;
    }

    public void CheckCurrentWindow()
    {
        // Get current active window information
        var windowInfo = _activeWindowService.GetActiveWindow();

        Console.WriteLine($"Window Title: {windowInfo.WindowTitle}");
        Console.WriteLine($"Process Name: {windowInfo.ProcessName}");
        Console.WriteLine($"Process ID: {windowInfo.ProcessId}");
        Console.WriteLine($"Is YouTube: {windowInfo.IsYouTube}");
    }

    public void CheckIfWatchingYouTube()
    {
        // Quick check if YouTube is currently active
        bool isOnYouTube = _activeWindowService.IsYouTubeActive();

        if (isOnYouTube)
        {
            Console.WriteLine("User is currently on YouTube!");
        }
        else
        {
            Console.WriteLine("User is not on YouTube");
        }
    }

    public void MonitorYouTubeUsage()
    {
        // Example: Monitor YouTube usage every 5 seconds
        var timer = new System.Windows.Forms.Timer();
        timer.Interval = 5000; // 5 seconds
        timer.Tick += (sender, e) =>
        {
            if (_activeWindowService.IsYouTubeActive())
            {
                var info = _activeWindowService.GetActiveWindow();
                Console.WriteLine($"YouTube detected: {info.WindowTitle}");
                // You could trigger actions here, like:
                // - Show a specific widget on the display
                // - Log YouTube watch time
                // - Change display brightness
                // etc.
            }
        };
        timer.Start();
    }
}
