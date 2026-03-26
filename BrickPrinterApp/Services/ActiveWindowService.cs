using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

public class ActiveWindowService : IActiveWindowService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    public ActiveWindowInfo GetActiveWindow()
    {
        var info = new ActiveWindowInfo();

        try
        {
            IntPtr handle = GetForegroundWindow();

            if (handle == IntPtr.Zero)
            {
                return info;
            }

            // Get window title
            int length = GetWindowTextLength(handle);
            if (length > 0)
            {
                StringBuilder builder = new StringBuilder(length + 1);
                GetWindowText(handle, builder, builder.Capacity);
                info.WindowTitle = builder.ToString();
            }

            // Get process information
            GetWindowThreadProcessId(handle, out uint processId);
            info.ProcessId = (int)processId;

            try
            {
                using Process process = Process.GetProcessById((int)processId);
                info.ProcessName = process.ProcessName;
            }
            catch
            {
                // Process might have closed or access denied
                info.ProcessName = "Unknown";
            }

            // Check if YouTube is active
            info.IsYouTube = IsYouTubeWindow(info.WindowTitle, info.ProcessName);
        }
        catch (Exception)
        {
            // Return empty info on any error
        }

        return info;
    }

    public bool IsYouTubeActive()
    {
        var info = GetActiveWindow();
        return info.IsYouTube;
    }

    private bool IsYouTubeWindow(string windowTitle, string processName)
    {
        if (string.IsNullOrEmpty(windowTitle))
            return false;

        // Check if window title contains YouTube indicators
        var titleLower = windowTitle.ToLowerInvariant();

        // Common YouTube patterns in window titles:
        // - "YouTube" in the title
        // - "- YouTube" suffix (common in browsers)
        // - YouTube app window
        if (titleLower.Contains("youtube"))
        {
            // Additional check: exclude YouTube Studio, YouTube Music if you want only video watching
            // For now, we'll include all YouTube variants
            return true;
        }

        // Check for YouTube in common browsers
        var browserProcesses = new[] { "chrome", "firefox", "msedge", "opera", "brave" };
        if (browserProcesses.Contains(processName.ToLowerInvariant()) && titleLower.Contains("youtube"))
        {
            return true;
        }

        return false;
    }
}
