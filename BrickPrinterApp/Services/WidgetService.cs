using BrickPrinterApp.Interfaces;
using Timer = System.Threading.Timer;

namespace BrickPrinterApp.Services;

public class WidgetService : IDisposable
{
    private readonly ITransferService _transferService;
    private readonly Dictionary<int, object?> _screenWidgets = new();
    private readonly Dictionary<int, Timer?> _screenTimers = new();
    private readonly Dictionary<int, CancellationTokenSource?> _screenCts = new();
    private readonly List<IWidget> _availableWidgets = new();
    private readonly List<IScriptWidget> _availableScriptWidgets = new();
    private readonly object _lock = new();

    public IReadOnlyList<IWidget> AvailableWidgets => _availableWidgets;
    public IReadOnlyList<IScriptWidget> AvailableScriptWidgets => _availableScriptWidgets;

    public WidgetService(ITransferService transferService)
    {
        _transferService = transferService;

        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            _screenWidgets[i] = null;
            _screenTimers[i] = null;
            _screenCts[i] = null;
        }
    }

    public void RegisterWidget(IWidget widget)
    {
        if (!_availableWidgets.Contains(widget))
        {
            _availableWidgets.Add(widget);
        }
    }

    public void RegisterScriptWidget(IScriptWidget widget)
    {
        if (!_availableScriptWidgets.Contains(widget))
        {
            _availableScriptWidgets.Add(widget);
        }
    }

    public object? GetWidgetForScreen(int screenId)
    {
        lock (_lock)
        {
            return _screenWidgets.GetValueOrDefault(screenId);
        }
    }

    public void AssignWidgetToScreen(int screenId, IWidget? widget)
    {
        CancellationTokenSource? cts = null;

        lock (_lock)
        {
            StopScreenOperations(screenId);
            _screenWidgets[screenId] = widget;

            if (widget != null)
            {
                cts = new CancellationTokenSource();
                _screenCts[screenId] = cts;
            }
        }

        if (widget != null && cts != null)
        {
            // Run sequentially: stop script first, then send content
            _ = StopScriptThenSendWidget(screenId, widget, cts.Token);

            lock (_lock)
            {
                var timer = new Timer(
                    _ => SendWidgetContentIfNotCanceled(screenId, widget, cts.Token),
                    null,
                    widget.UpdateInterval,
                    widget.UpdateInterval);

                _screenTimers[screenId] = timer;
            }
        }
        else
        {
            // Just stop any running script
            _ = _transferService.StopScriptAsync(screenId);
        }
    }

    public void AssignScriptWidgetToScreen(int screenId, IScriptWidget? widget)
    {
        CancellationTokenSource? cts = null;

        lock (_lock)
        {
            StopScreenOperations(screenId);
            _screenWidgets[screenId] = widget;

            if (widget != null)
            {
                cts = new CancellationTokenSource();
                _screenCts[screenId] = cts;
            }
        }

        if (widget != null && cts != null)
        {
            // Send script (no need to stop first, loading a new script replaces old one)
            _ = SendScriptContent(screenId, widget, cts.Token);
        }
        else
        {
            // Stop script when widget is removed
            _ = _transferService.StopScriptAsync(screenId);
        }
    }

    public void RemoveWidgetFromScreen(int screenId)
    {
        lock (_lock)
        {
            StopScreenOperations(screenId);
            _screenWidgets[screenId] = null;
        }

        _ = _transferService.StopScriptAsync(screenId);
    }

    private void StopScreenOperations(int screenId)
    {
        // Cancel any pending async operations
        if (_screenCts.TryGetValue(screenId, out var cts) && cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            _screenCts[screenId] = null;
        }

        // Stop the timer
        if (_screenTimers.TryGetValue(screenId, out var timer) && timer != null)
        {
            timer.Dispose();
            _screenTimers[screenId] = null;
        }
    }

    private async Task StopScriptThenSendWidget(int screenId, IWidget widget, CancellationToken ct)
    {
        try
        {
            // First stop any running script
            await _transferService.StopScriptAsync(screenId);

            if (ct.IsCancellationRequested) return;

            // Then send the widget content
            await SendWidgetContent(screenId, widget, ct);
        }
        catch
        {
            // Silently handle errors
        }
    }

    private void SendWidgetContentIfNotCanceled(int screenId, IWidget widget, CancellationToken ct)
    {
        if (ct.IsCancellationRequested) return;
        _ = SendWidgetContent(screenId, widget, ct);
    }

    private async Task SendWidgetContent(int screenId, IWidget widget, CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested) return;

            var content = widget.GetContent();

            if (ct.IsCancellationRequested) return;

            await _transferService.SendBinaryDataAsync(content, screenId);
        }
        catch
        {
            // Silently handle errors
        }
    }

    private async Task SendScriptContent(int screenId, IScriptWidget widget, CancellationToken ct)
    {
        try
        {
            if (ct.IsCancellationRequested) return;

            var script = widget.GetScript();

            if (ct.IsCancellationRequested) return;

            await _transferService.SendScriptAsync(script, widget.ScriptLanguage, screenId);
        }
        catch
        {
            // Silently handle errors
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            foreach (var screenId in _screenTimers.Keys.ToList())
            {
                StopScreenOperations(screenId);
            }
        }
    }
}
