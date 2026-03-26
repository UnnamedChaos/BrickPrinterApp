using System.Diagnostics;
using System.Management;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Widgets;

public class GpuWidget : IWidget
{
    private readonly IDisplayService _displayService;
    private PerformanceCounter? _gpuMemoryCounter;
    private readonly Dictionary<string, PerformanceCounter> _engineCounters = new();
    private bool _countersWarmedUp = false;
    private string _gpuName = "GPU";
    private int _physicalGpu = 0;
    private DateTime _lastCounterRefresh = DateTime.MinValue;
    private bool _hasGpuEngine = false;
    private long _totalVramBytes = 0;
    private bool _useProcessMemory = false;

    public string Name => "GPU Monitor";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(2);

    public GpuWidget(IDisplayService displayService)
    {
        _displayService = displayService;
        InitializeCounters();
    }

    private void InitializeCounters()
    {
        try
        {
            // Check if GPU Engine category exists
            if (PerformanceCounterCategory.Exists("GPU Engine"))
            {
                _hasGpuEngine = true;
                var category = new PerformanceCounterCategory("GPU Engine");
                var instances = category.GetInstanceNames();

                if (instances.Length > 0)
                {
                    // Extract physical GPU index from first instance
                    var firstInstance = instances[0];
                    var parts = firstInstance.Split('_');

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        if (parts[i] == "phys" && int.TryParse(parts[i + 1], out int physIdx))
                        {
                            _physicalGpu = physIdx;
                            break;
                        }
                    }

                    Console.WriteLine($"GPU Widget: Found GPU Engine category with {instances.Length} instances (Physical GPU {_physicalGpu})");
                }
            }

            // Try memory counters - use GPU Process Memory to get total dedicated memory usage
            try
            {
                if (PerformanceCounterCategory.Exists("GPU Process Memory"))
                {
                    var category = new PerformanceCounterCategory("GPU Process Memory");
                    var instances = category.GetInstanceNames();

                    Console.WriteLine($"GPU Widget: GPU Process Memory has {instances.Length} instances");

                    // We'll sum up dedicated memory from all processes in DrawGpuStats
                    // Just verify the category exists
                    if (instances.Length > 0)
                    {
                        Console.WriteLine($"GPU Widget: Sample instance: {instances[0]}");
                    }
                }

                // Also try GPU Adapter Memory as fallback
                if (PerformanceCounterCategory.Exists("GPU Adapter Memory"))
                {
                    var category = new PerformanceCounterCategory("GPU Adapter Memory");
                    var instances = category.GetInstanceNames();

                    Console.WriteLine($"GPU Widget: GPU Adapter Memory instances: {string.Join(", ", instances.Take(3))}...");

                    foreach (var instance in instances)
                    {
                        try
                        {
                            // Try Local Usage which shows currently used memory
                            _gpuMemoryCounter = new PerformanceCounter("GPU Adapter Memory", "Local Usage", instance);
                            _gpuMemoryCounter.NextValue(); // Initialize
                            System.Threading.Thread.Sleep(50);
                            var testValue = _gpuMemoryCounter.NextValue();

                            if (testValue > 0)
                            {
                                Console.WriteLine($"GPU Widget: Using Local Usage from '{instance}': {testValue / (1024 * 1024):F0}MB");
                                break;
                            }
                        }
                        catch
                        {
                            // Try Dedicated Usage instead
                            try
                            {
                                _gpuMemoryCounter = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", instance);
                                _gpuMemoryCounter.NextValue();
                                System.Threading.Thread.Sleep(50);
                                var testValue = _gpuMemoryCounter.NextValue();

                                if (testValue > 0)
                                {
                                    Console.WriteLine($"GPU Widget: Using Dedicated Usage from '{instance}': {testValue / (1024 * 1024):F0}MB");
                                    break;
                                }
                            }
                            catch { }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU Widget: Memory counters not available: {ex.Message}");
            }

            // Get total VRAM - try registry first (supports >4GB), fall back to WMI
            try
            {
                // Try registry first - it has 64-bit values for modern GPUs
                _totalVramBytes = GetVramFromRegistry();

                if (_totalVramBytes > 0)
                {
                    Console.WriteLine($"GPU Widget: VRAM from registry: {_totalVramBytes / (1024 * 1024)}MB");
                }
                else
                {
                    // Fall back to WMI (limited to 4GB due to uint32)
                    using var searcher = new ManagementObjectSearcher("SELECT AdapterRAM, Name FROM Win32_VideoController");
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        var adapterRam = obj["AdapterRAM"];
                        var name = obj["Name"]?.ToString() ?? "Unknown GPU";

                        if (adapterRam != null)
                        {
                            long vram = Convert.ToInt64(adapterRam);
                            if (vram > _totalVramBytes)
                            {
                                _totalVramBytes = vram;
                                _gpuName = name;
                            }
                            Console.WriteLine($"GPU Widget: {name} - VRAM from WMI: {vram / (1024 * 1024)}MB");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GPU Widget: VRAM query failed: {ex.Message}");
            }

            if (!_hasGpuEngine && _gpuMemoryCounter == null)
            {
                Console.WriteLine("GPU Widget: No GPU counters available. Widget will show placeholder.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU Widget: Failed to initialize: {ex.Message}");
        }
    }

    private long GetVramFromRegistry()
    {
        // For GPUs with >4GB VRAM, we need to read from registry
        // Look for qwMemorySize in display adapter registry keys
        // Try multiple adapter indices (0000, 0001, etc.)
        long maxVram = 0;

        string[] registryPaths = new[]
        {
            @"SYSTEM\ControlSet001\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}",
            @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"
        };

        foreach (var basePath in registryPaths)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    string keyPath = $@"{basePath}\{i:D4}";
                    using var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(keyPath);

                    if (key == null) continue;

                    // Get adapter description for logging
                    var desc = key.GetValue("DriverDesc")?.ToString() ?? $"Adapter {i}";

                    // List of possible VRAM registry value names (different vendors use different names)
                    string[] vramValueNames = new[]
                    {
                        "HardwareInformation.qwMemorySize",      // Modern GPUs (64-bit)
                        "HardwareInformation.MemorySize",        // Older format
                        "Intel.Display.VideoMemorySize",         // Intel specific
                        "HardwareInformation.AdapterString"      // Sometimes contains memory info
                    };

                    foreach (var valueName in vramValueNames)
                    {
                        var value = key.GetValue(valueName);
                        if (value == null) continue;

                        long vram = 0;

                        if (value is long l)
                            vram = l;
                        else if (value is ulong ul)
                            vram = (long)ul;
                        else if (value is int intVal)
                            vram = intVal;
                        else if (value is uint uintVal)
                            vram = uintVal;
                        else if (value is byte[] bytes)
                        {
                            if (bytes.Length >= 8)
                                vram = BitConverter.ToInt64(bytes, 0);
                            else if (bytes.Length >= 4)
                                vram = BitConverter.ToUInt32(bytes, 0);
                        }
                        else
                        {
                            try { vram = Convert.ToInt64(value); } catch { }
                        }

                        if (vram > 1024 * 1024 * 100) // At least 100MB to be valid
                        {
                            Console.WriteLine($"GPU Widget: Registry[{i:D4}] {desc}: {valueName} = {vram / (1024 * 1024)}MB");
                            if (vram > maxVram) maxVram = vram;
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GPU Widget: Registry[{i:D4}] error: {ex.Message}");
                }
            }
        }

        return maxVram;
    }

    private float GetProcessGpuMemory()
    {
        // Aggregate dedicated GPU memory from all processes
        float totalMemoryBytes = 0;

        try
        {
            if (!PerformanceCounterCategory.Exists("GPU Process Memory"))
                return 0;

            var category = new PerformanceCounterCategory("GPU Process Memory");
            var instances = category.GetInstanceNames();

            // Filter to our physical GPU
            var relevantInstances = instances.Where(name =>
                name.Contains($"phys_{_physicalGpu}")).ToList();

            foreach (var instance in relevantInstances)
            {
                try
                {
                    using var counter = new PerformanceCounter("GPU Process Memory", "Dedicated Usage", instance);
                    float value = counter.NextValue();
                    totalMemoryBytes += value;
                }
                catch
                {
                    // Skip failed counters
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU Widget: Process memory aggregation failed: {ex.Message}");
        }

        return totalMemoryBytes / (1024 * 1024); // Return MB
    }

    private void RefreshEngineCounters()
    {
        if (!_hasGpuEngine) return;

        // Refresh counters every 10 seconds to handle process changes
        if ((DateTime.Now - _lastCounterRefresh).TotalSeconds < 10 && _engineCounters.Count > 0)
            return;

        _lastCounterRefresh = DateTime.Now;

        try
        {
            var category = new PerformanceCounterCategory("GPU Engine");
            var instances = category.GetInstanceNames();

            // Filter to relevant engine types for this physical GPU
            var relevantInstances = instances.Where(name =>
                name.Contains($"phys_{_physicalGpu}_") &&
                (name.Contains("engtype_3D") ||
                 name.Contains("engtype_3d") ||
                 name.Contains("engtype_Compute") ||
                 name.Contains("engtype_compute") ||
                 name.Contains("engtype_Copy") ||
                 name.Contains("engtype_copy") ||
                 name.Contains("engtype_VideoDecode") ||
                 name.Contains("engtype_videodecode") ||
                 name.Contains("engtype_VideoEncode") ||
                 name.Contains("engtype_videoencode"))
            ).ToHashSet();

            // Remove counters for instances that no longer exist
            var toRemove = _engineCounters.Keys.Where(k => !relevantInstances.Contains(k)).ToList();
            foreach (var key in toRemove)
            {
                _engineCounters[key].Dispose();
                _engineCounters.Remove(key);
            }

            // Add new counters for new instances
            foreach (var instance in relevantInstances)
            {
                if (!_engineCounters.ContainsKey(instance))
                {
                    try
                    {
                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        counter.NextValue(); // Initialize - first call returns 0
                        _engineCounters[instance] = counter;
                    }
                    catch
                    {
                        // Instance may have disappeared
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU Widget: Error refreshing counters: {ex.Message}");
        }
    }

    public byte[] GetContent()
    {
        var image = DrawGpuStats();
        return _displayService.ConvertImageToBinary(image);
    }

    private Bitmap DrawGpuStats()
    {
        var bitmap = new Bitmap(SettingService.ScreenWidth, SettingService.ScreenHeight);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(Color.Black);

        var font = new Font("Consolas", 8);
        var brush = new SolidBrush(Color.White);
        var pen = new Pen(Color.White, 1);

        // Refresh engine counters (handles process changes)
        RefreshEngineCounters();

        // First call warmup - counters need one read to initialize
        if (!_countersWarmedUp)
        {
            _countersWarmedUp = true;
            // Return placeholder on first call
            g.DrawString("GPU", font, brush, 2, 2);
            g.DrawString("Initializing...", font, brush, 2, 25);
            return bitmap;
        }

        // Get current values
        float usage = 0;
        float memoryMB = 0;
        float memoryTotalMB = _totalVramBytes / (1024f * 1024f);

        try
        {
            // Calculate total GPU usage from cached counters
            if (_engineCounters.Count > 0)
            {
                float totalUtilization = 0;

                foreach (var kvp in _engineCounters.ToList()) // ToList to avoid modification during iteration
                {
                    try
                    {
                        float value = kvp.Value.NextValue();
                        totalUtilization += value;
                    }
                    catch
                    {
                        // Counter may have become invalid, will be cleaned up on next refresh
                    }
                }

                usage = Math.Max(0, Math.Min(100, totalUtilization));
            }

            // Get memory usage - try adapter counter first, then aggregate process memory
            if (_gpuMemoryCounter != null && !_useProcessMemory)
            {
                try
                {
                    float rawValue = _gpuMemoryCounter.NextValue();
                    memoryMB = rawValue / (1024 * 1024);

                    // If still 0 after warmup, switch to process memory approach
                    if (memoryMB < 1 && _countersWarmedUp)
                    {
                        Console.WriteLine("GPU Widget: Adapter memory counter returns 0, switching to process memory aggregation");
                        _useProcessMemory = true;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GPU Widget: Memory counter read failed: {ex.Message}");
                    _useProcessMemory = true;
                }
            }

            // Alternative: Sum up dedicated memory from all GPU processes
            if (_useProcessMemory || _gpuMemoryCounter == null)
            {
                memoryMB = GetProcessGpuMemory();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU Widget: Error reading counters: {ex.Message}");
        }

        // Draw title
        g.DrawString("GPU", font, brush, 2, 2);

        // Draw usage bar
        int barY = 15;
        int barHeight = 20;
        int barWidth = SettingService.ScreenWidth - 4;

        // Border
        g.DrawRectangle(pen, 2, barY, barWidth - 1, barHeight - 1);

        // Fill bar based on usage
        int fillWidth = (int)((usage / 100f) * (barWidth - 2));
        if (fillWidth > 0)
        {
            g.FillRectangle(brush, 3, barY + 1, fillWidth, barHeight - 2);
        }

        // Usage percentage text
        string usageText = $"{usage:F0}%";
        var textSize = g.MeasureString(usageText, font);
        g.DrawString(usageText, font,
            fillWidth > textSize.Width ? new SolidBrush(Color.Black) : brush,
            5, barY + 5);

        // Memory info
        int yPos = barY + barHeight + 5;

        if (_gpuMemoryCounter != null || memoryTotalMB > 0)
        {
            // Format memory values (use GB if > 1024 MB)
            string usedStr = memoryMB >= 1024 ? $"{memoryMB / 1024:F1}G" : $"{memoryMB:F0}M";
            string totalStr = memoryTotalMB >= 1024 ? $"{memoryTotalMB / 1024:F0}G" : $"{memoryTotalMB:F0}M";

            if (memoryTotalMB > 0)
            {
                g.DrawString($"VRAM: {usedStr}/{totalStr}", font, brush, 2, yPos);
            }
            else
            {
                g.DrawString($"VRAM: {usedStr}", font, brush, 2, yPos);
            }
            yPos += 12;

            // Calculate VRAM percentage bar
            if (memoryTotalMB > 0)
            {
                float memPercent = (memoryMB / memoryTotalMB) * 100f;
                int memBarWidth = (int)((memPercent / 100f) * (barWidth - 2));

                g.DrawRectangle(pen, 2, yPos, barWidth - 1, 8);
                if (memBarWidth > 0)
                {
                    g.FillRectangle(brush, 3, yPos + 1, memBarWidth, 6);
                }
            }
        }

        // If no counters available, show message
        if (!_hasGpuEngine && _gpuMemoryCounter == null)
        {
            g.DrawString("No GPU data", font, brush, 2, 25);
            g.DrawString("available", font, brush, 2, 37);
        }

        return bitmap;
    }
}
