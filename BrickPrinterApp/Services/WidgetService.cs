using BrickPrinterApp.Interfaces;
using Timer = System.Threading.Timer;

namespace BrickPrinterApp.Services;

public class WidgetService : IDisposable
{
    private readonly ITransferService _transferService;
    private readonly Dictionary<int, IWidget?> _screenWidgets = new();
    private readonly Dictionary<int, Timer?> _screenTimers = new();
    private readonly List<IWidget> _availableWidgets = new();
    private readonly object _lock = new();

    public IReadOnlyList<IWidget> AvailableWidgets => _availableWidgets;

    public WidgetService(ITransferService transferService)
    {
        _transferService = transferService;

        // Initialize all screens with no widget
        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            _screenWidgets[i] = null;
            _screenTimers[i] = null;
        }
    }

    public void RegisterWidget(IWidget widget)
    {
        if (!_availableWidgets.Contains(widget))
        {
            _availableWidgets.Add(widget);
        }
    }

    public IWidget? GetWidgetForScreen(int screenId)
    {
        lock (_lock)
        {
            return _screenWidgets.GetValueOrDefault(screenId);
        }
    }

    public void AssignWidgetToScreen(int screenId, IWidget? widget)
    {
        lock (_lock)
        {
            // Stop existing timer for this screen
            StopTimerForScreen(screenId);

            _screenWidgets[screenId] = widget;

            if (widget != null)
            {
                // Send initial content
                _ = SendWidgetContent(screenId, widget);

                // Start update timer
                var timer = new Timer(
                    async _ => await SendWidgetContent(screenId, widget),
                    null,
                    widget.UpdateInterval,
                    widget.UpdateInterval);

                _screenTimers[screenId] = timer;
            }
        }
    }

    public void RemoveWidgetFromScreen(int screenId)
    {
        AssignWidgetToScreen(screenId, null);
    }

    private void StopTimerForScreen(int screenId)
    {
        if (_screenTimers.TryGetValue(screenId, out var timer) && timer != null)
        {
            timer.Dispose();
            _screenTimers[screenId] = null;
        }
    }

    private async Task SendWidgetContent(int screenId, IWidget widget)
    {
        try
        {
            var content = widget.GetContent();
            await _transferService.SendBinaryDataAsync(content, screenId);
        }
        catch
        {
            // Silently handle errors to prevent timer from crashing
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var screenId in _screenTimers.Keys.ToList())
            {
                StopTimerForScreen(screenId);
            }
        }
    }
}
