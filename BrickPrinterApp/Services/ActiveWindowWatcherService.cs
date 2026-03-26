using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

public class ActiveWindowWatcherService
{
    private readonly IActiveWindowService _activeWindowService;
    private System.Windows.Forms.Timer? _timer;
    private string _lastWindowTitle = string.Empty;
    private string _lastProcessName = string.Empty;
    private bool _isRunning = false;

    public int CheckIntervalMs { get; set; } = 1000; // Check every second by default

    public ActiveWindowWatcherService(IActiveWindowService activeWindowService)
    {
        _activeWindowService = activeWindowService;
    }

    public void Start()
    {
        if (_isRunning)
        {
            Console.WriteLine("ActiveWindowWatcher is already running.");
            return;
        }

        Console.WriteLine("ActiveWindowWatcher started. Monitoring foreground applications...");
        Console.WriteLine("----------------------------------------");

        _timer = new System.Windows.Forms.Timer();
        _timer.Interval = CheckIntervalMs;
        _timer.Tick += OnTimerTick;
        _timer.Start();
        _isRunning = true;

        // Print initial window immediately
        CheckAndPrintActiveWindow();
    }

    public void Stop()
    {
        if (!_isRunning)
        {
            return;
        }

        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;

        Console.WriteLine("----------------------------------------");
        Console.WriteLine("ActiveWindowWatcher stopped.");
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        CheckAndPrintActiveWindow();
    }

    private void CheckAndPrintActiveWindow()
    {
        var windowInfo = _activeWindowService.GetActiveWindow();

        // Only print if the window has changed
        if (windowInfo.WindowTitle != _lastWindowTitle || windowInfo.ProcessName != _lastProcessName)
        {
            _lastWindowTitle = windowInfo.WindowTitle;
            _lastProcessName = windowInfo.ProcessName;

            PrintWindowInfo(windowInfo);
        }
    }

    private void PrintWindowInfo(ActiveWindowInfo info)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");

        Console.WriteLine($"[{timestamp}] Window Changed:");
        Console.WriteLine($"  Process: {info.ProcessName} (PID: {info.ProcessId})");
        Console.WriteLine($"  Title: {info.WindowTitle}");

        if (info.IsYouTube)
        {
            Console.WriteLine($"  *** YOUTUBE DETECTED ***");
        }

        Console.WriteLine();
    }

    public bool IsRunning => _isRunning;
}
