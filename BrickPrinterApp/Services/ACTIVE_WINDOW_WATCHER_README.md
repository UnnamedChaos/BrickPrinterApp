# Active Window Watcher

## Overview

The Active Window Watcher monitors the currently active/foreground application in Windows and prints changes to the console in real-time.

## Features

- **Automatic Detection**: Monitors the foreground window every second (configurable)
- **Change Detection**: Only prints when the active window changes
- **YouTube Detection**: Specifically detects when YouTube is active
- **Console Output**: Displays process name, window title, and timestamp for each change
- **Tray Control**: Start/stop the watcher from the system tray menu

## Files

- **IActiveWindowService.cs** - Service interface for getting active window info
- **ActiveWindowService.cs** - Implementation using Windows API (P/Invoke)
- **ActiveWindowWatcherService.cs** - Monitoring service that watches for window changes
- **ActiveWindowServiceExample.cs** - Usage examples

## Usage

### Automatic Start

The watcher starts automatically when the application launches. Output appears in the console window.

### Manual Control

Right-click the tray icon and select:
- **"Window Watcher stoppen"** - Stops monitoring
- **"Window Watcher starten"** - Resumes monitoring

### Console Output Example

```
ActiveWindowWatcher started. Monitoring foreground applications...
----------------------------------------
[14:23:15] Window Changed:
  Process: chrome (PID: 12345)
  Title: YouTube - Google Chrome

  *** YOUTUBE DETECTED ***

[14:23:42] Window Changed:
  Process: Code (PID: 54321)
  Title: Visual Studio Code - BrickPrinterApp

[14:24:10] Window Changed:
  Process: chrome (PID: 12345)
  Title: Awesome Video - YouTube
  *** YOUTUBE DETECTED ***
```

## Configuration

You can adjust the check interval by modifying `ActiveWindowWatcherService`:

```csharp
// In BrickPrinter.cs constructor, before Start():
_windowWatcher.CheckIntervalMs = 500; // Check every 500ms instead of 1000ms
_windowWatcher.Start();
```

## API Reference

### IActiveWindowService

```csharp
// Get detailed info about the current active window
var info = activeWindowService.GetActiveWindow();
Console.WriteLine($"Process: {info.ProcessName}");
Console.WriteLine($"Title: {info.WindowTitle}");
Console.WriteLine($"Is YouTube: {info.IsYouTube}");

// Quick check if YouTube is active
if (activeWindowService.IsYouTubeActive())
{
    // Do something
}
```

### ActiveWindowWatcherService

```csharp
// Start monitoring
_windowWatcher.Start();

// Stop monitoring
_windowWatcher.Stop();

// Check if running
bool isRunning = _windowWatcher.IsRunning;

// Adjust interval
_windowWatcher.CheckIntervalMs = 1000; // milliseconds
```

## YouTube Detection

The service detects YouTube by checking if "youtube" appears in the window title. This works across all major browsers:
- Google Chrome
- Mozilla Firefox
- Microsoft Edge
- Opera
- Brave

## Integration Ideas

You could extend this to:
- Display a special widget on the ESP32 when YouTube is active
- Track time spent in different applications
- Automatically pause/resume certain widgets based on active app
- Log application usage statistics
- Change display brightness based on the active application

## Example: Create a YouTube-Aware Widget

```csharp
public class YouTubeWidget : IWidget
{
    private readonly IActiveWindowService _windowService;
    private readonly IDisplayService _displayService;

    public YouTubeWidget(IActiveWindowService windowService, IDisplayService displayService)
    {
        _windowService = windowService;
        _displayService = displayService;
    }

    public byte[] GetBinaryData()
    {
        var bitmap = new Bitmap(128, 64);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.Clear(Color.Black);

            if (_windowService.IsYouTubeActive())
            {
                // Show YouTube icon or message
                g.DrawString("Watching YouTube", new Font("Arial", 8), Brushes.White, 5, 28);
            }
            else
            {
                var info = _windowService.GetActiveWindow();
                g.DrawString(info.ProcessName, new Font("Arial", 8), Brushes.White, 5, 28);
            }
        }

        return _displayService.ConvertImageToBinary(bitmap);
    }
}
```
