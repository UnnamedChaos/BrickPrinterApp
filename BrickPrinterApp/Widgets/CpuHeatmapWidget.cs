using System.Diagnostics;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Widgets;

public class CpuHeatmapWidget : IWidget
{
    private readonly IDisplayService _displayService;
    private readonly List<PerformanceCounter> _cpuCounters;
    private readonly int _coreCount;
    private bool _initialized = false;

    public string Name => "CPU Heatmap";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(2);

    public CpuHeatmapWidget(IDisplayService displayService)
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
            Console.WriteLine($"CPU Heatmap: Initialized {_coreCount} core counters");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"CPU Heatmap: Failed to initialize counters: {ex.Message}");
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
        // Find best column count that creates a roughly square grid
        int cols = (int)Math.Ceiling(Math.Sqrt(displayCores));
        int rows = (displayCores + cols - 1) / cols;

        // Use minimal spacing
        int spacing = 1;

        // Calculate block dimensions to fill screen width completely
        int blockWidth = (SettingService.ScreenWidth - spacing * (cols - 1)) / cols;
        int blockHeight = (SettingService.ScreenHeight - spacing * (rows - 1)) / rows;

        // Draw each core with rectangular blocks
        for (int i = 0; i < displayCores; i++)
        {
            int row = i / cols;
            int col = i % cols;

            int x = col * (blockWidth + spacing);
            int y = row * (blockHeight + spacing);

            float usage = i < cpuUsages.Length ? cpuUsages[i] : 0;
            DrawCpuCore(g, x, y, blockWidth, blockHeight, i, usage);
        }

        return bitmap;
    }

    private void DrawCpuCore(System.Drawing.Graphics g, int x, int y, int width, int height, int coreNum, float usage)
    {
        var pen = new Pen(Color.White, 1);
        var brush = new SolidBrush(Color.White);

        // Draw block border
        g.DrawRectangle(pen, x, y, width, height);

        // Fill based on usage level
        if (usage < 5)
        {
            // Empty (0-5%)
        }
        else if (usage < 30)
        {
            // Light load - sparse dots
            DrawSparsePattern(g, x, y, width, height);
        }
        else if (usage < 60)
        {
            // Medium load - checkerboard pattern
            DrawCheckerboard(g, x, y, width, height);
        }
        else if (usage < 85)
        {
            // High load - dense pattern
            DrawDensePattern(g, x, y, width, height);
        }
        else
        {
            // Critical load - solid fill
            g.FillRectangle(brush, x + 1, y + 1, width - 1, height - 1);
        }
    }

    private void DrawSparsePattern(System.Drawing.Graphics g, int x, int y, int width, int height)
    {
        // Draw sparse dots (every 4 pixels)
        for (int py = 2; py < height; py += 4)
        {
            for (int px = 2; px < width; px += 4)
            {
                g.FillRectangle(Brushes.White, x + px, y + py, 1, 1);
            }
        }
    }

    private void DrawCheckerboard(System.Drawing.Graphics g, int x, int y, int width, int height)
    {
        // Classic checkerboard (2x2 blocks)
        for (int py = 1; py < height; py += 2)
        {
            for (int px = 1; px < width; px += 2)
            {
                if ((px + py) % 4 == 0)
                {
                    g.FillRectangle(Brushes.White, x + px, y + py, 2, 2);
                }
            }
        }
    }

    private void DrawDensePattern(System.Drawing.Graphics g, int x, int y, int width, int height)
    {
        // Dense pattern - inverted checkerboard (more white than black)
        for (int py = 1; py < height; py += 2)
        {
            for (int px = 1; px < width; px += 2)
            {
                if ((px + py) % 4 != 0)
                {
                    g.FillRectangle(Brushes.White, x + px, y + py, 2, 2);
                }
            }
        }
    }
}
