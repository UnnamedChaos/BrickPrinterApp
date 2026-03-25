using System.Diagnostics;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Widgets;

public class CpuSimpleWidget : IWidget
{
    private readonly IDisplayService _displayService;
    private readonly List<PerformanceCounter> _cpuCounters;
    private readonly int _coreCount;
    private bool _initialized = false;

    public string Name => "CPU Simple";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(2);

    public CpuSimpleWidget(IDisplayService displayService)
    {
        _displayService = displayService;
        _cpuCounters = new List<PerformanceCounter>();

        // Detect CPU core count
        _coreCount = Environment.ProcessorCount;

        // Initialize performance counters for each core
        try
        {
            for (int i = 0; i < _coreCount; i++)
            {
                var counter = new PerformanceCounter("Processor", "% Processor Time", i.ToString());
                _cpuCounters.Add(counter);
            }
            Console.WriteLine($"CPU Simple: Initialized {_coreCount} core counters");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CPU Simple: Failed to initialize counters: {ex.Message}");
        }
    }

    public byte[] GetContent()
    {
        var image = DrawCpuGrid();
        return _displayService.ConvertImageToBinary(image);
    }

    private Bitmap DrawCpuGrid()
    {
        var bitmap = new Bitmap(SettingService.ScreenWidth, SettingService.ScreenHeight);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(Color.Black);

        if (_cpuCounters.Count == 0)
        {
            return bitmap;
        }

        // First call to NextValue() returns 0, so we need to initialize
        if (!_initialized)
        {
            foreach (var counter in _cpuCounters)
            {
                counter.NextValue();
            }
            _initialized = true;
            return bitmap;
        }

        // Get CPU usage for each core
        var cpuUsages = new float[_cpuCounters.Count];
        for (int i = 0; i < _cpuCounters.Count; i++)
        {
            try
            {
                cpuUsages[i] = _cpuCounters[i].NextValue();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to read CPU {i}: {ex.Message}");
                cpuUsages[i] = 0;
            }
        }

        // Show all CPU cores dynamically
        int displayCores = _coreCount;

        // Calculate grid layout - aim for square-ish grid
        int cols = (int)Math.Ceiling(Math.Sqrt(displayCores));
        int rows = (displayCores + cols - 1) / cols;

        // Spacing between blocks
        int spacing = 2;

        // Calculate block dimensions to fill screen
        int blockWidth = (SettingService.ScreenWidth - spacing * (cols - 1)) / cols;
        int blockHeight = (SettingService.ScreenHeight - spacing * (rows - 1)) / rows;

        // Draw each core
        for (int i = 0; i < displayCores; i++)
        {
            int row = i / cols;
            int col = i % cols;

            int x = col * (blockWidth + spacing);
            int y = row * (blockHeight + spacing);

            float usage = i < cpuUsages.Length ? cpuUsages[i] : 0;
            DrawCpuCore(g, x, y, blockWidth, blockHeight, usage);
        }

        return bitmap;
    }

    private void DrawCpuCore(System.Drawing.Graphics g, int x, int y, int width, int height, float usage)
    {
        var pen = new Pen(Color.White, 1);
        var brush = new SolidBrush(Color.White);

        // Draw border
        g.DrawRectangle(pen, x, y, width - 1, height - 1);

        // Draw horizontal progress bar based on CPU usage
        // Calculate fill width (leave 1 pixel border inside)
        int innerWidth = width - 2;
        int innerHeight = height - 2;

        // Clamp usage to 0-100
        usage = Math.Max(0, Math.Min(100, usage));

        // Calculate bar width based on percentage
        int barWidth = (int)((usage / 100f) * innerWidth);

        if (barWidth > 0)
        {
            g.FillRectangle(brush, x + 1, y + 1, barWidth, innerHeight);
        }
    }
}
