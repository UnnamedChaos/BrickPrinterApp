using BrickPrinterApp.Interfaces;
using Timer = System.Threading.Timer;

namespace BrickPrinterApp.Services;

public class WidgetService : IDisposable
{
    private readonly ITransferService _transferService;
    private readonly SettingService _settingService;
    private readonly Dictionary<int, object?> _screenWidgets = new();
    private readonly Dictionary<int, Timer?> _screenTimers = new();
    private readonly Dictionary<int, CancellationTokenSource?> _screenCts = new();
    private readonly Dictionary<int, bool> _screenUpdating = new(); // Track if screen is currently updating
    private readonly List<IWidget> _availableWidgets = new();
    private readonly List<IScriptWidget> _availableScriptWidgets = new();
    private readonly object _lock = new();
    private bool _isLoading = false;

    public IReadOnlyList<IWidget> AvailableWidgets => _availableWidgets;
    public IReadOnlyList<IScriptWidget> AvailableScriptWidgets => _availableScriptWidgets;

    public WidgetService(ITransferService transferService, SettingService settingService)
    {
        _transferService = transferService;
        _settingService = settingService;

        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            _screenWidgets[i] = null;
            _screenTimers[i] = null;
            _screenCts[i] = null;
            _screenUpdating[i] = false;
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
            _ = SendWidgetAndStartTimer(screenId, widget, cts);
        }
        else
        {
            // Just stop any running script
            _ = _transferService.StopScriptAsync(screenId);
        }

        SaveCurrentAssignments();
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

        SaveCurrentAssignments();
    }

    public void RemoveWidgetFromScreen(int screenId)
    {
        lock (_lock)
        {
            StopScreenOperations(screenId);
            _screenWidgets[screenId] = null;
        }

        _ = _transferService.StopScriptAsync(screenId);
        SaveCurrentAssignments();
    }

    /// <summary>
    /// Check screen status and resend widgets for screens that should be active but aren't
    /// </summary>
    public async Task RecoverScreensAsync(ScreenStatus[] screenStatus)
    {
        foreach (var status in screenStatus)
        {
            if (status.Active) continue; // Screen is already active

            object? widget;
            bool isUpdating;
            lock (_lock)
            {
                widget = _screenWidgets.GetValueOrDefault(status.Id);
                isUpdating = _screenUpdating.GetValueOrDefault(status.Id, false);
            }

            if (widget == null) continue; // No widget assigned, nothing to recover
            if (isUpdating)
            {
                Console.WriteLine($"Recovery: Screen {status.Id} is already updating, skipping");
                continue;
            }

            try
            {
                if (widget is IScriptWidget scriptWidget)
                {
                    Console.WriteLine($"Recovery: Resending {scriptWidget.Name} script to screen {status.Id}");
                    var script = scriptWidget.GetScript();
                    var result = await _transferService.SendScriptAsync(script, scriptWidget.ScriptLanguage, status.Id);
                    Console.WriteLine($"Recovery result for screen {status.Id}: {(result ? "success" : "failed")}");
                }
                else if (widget is IWidget regularWidget)
                {
                    Console.WriteLine($"Recovery: Resending {regularWidget.Name} widget to screen {status.Id}");
                    await SendWidgetContentWithLock(status.Id, regularWidget);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Recovery error for screen {status.Id}: {ex.Message}");
            }
        }
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

        // Clear updating flag
        _screenUpdating[screenId] = false;
    }

    private async Task SendWidgetAndStartTimer(int screenId, IWidget widget, CancellationTokenSource cts)
    {
        try
        {
            // Check if a script is currently running on the ESP32
            var scriptRunning = await _transferService.IsScriptRunningAsync(screenId);

            if (scriptRunning)
            {
                await _transferService.StopScriptAsync(screenId);
                if (cts.Token.IsCancellationRequested) return;
            }

            // Send the initial widget content
            await SendWidgetContent(screenId, widget, cts.Token);

            if (cts.Token.IsCancellationRequested) return;

            // Start the timer after first content is sent
            lock (_lock)
            {
                // Verify the widget and CTS haven't been replaced
                if (_screenCts.GetValueOrDefault(screenId) == cts)
                {
                    var timer = new Timer(
                        _ => SendWidgetContentIfNotCanceled(screenId, widget, cts.Token),
                        null,
                        widget.UpdateInterval,
                        widget.UpdateInterval);

                    _screenTimers[screenId] = timer;
                }
            }
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
        // Check if already updating
        lock (_lock)
        {
            if (_screenUpdating.GetValueOrDefault(screenId, false))
            {
                Console.WriteLine($"Screen {screenId} is already updating, skipping");
                return;
            }
            _screenUpdating[screenId] = true;
        }

        try
        {
            if (ct.IsCancellationRequested) return;

            var content = widget.GetContent();

            if (ct.IsCancellationRequested) return;

            await _transferService.SendBinaryDataAsync(content, screenId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending widget content to screen {screenId}: {ex.Message}");
        }
        finally
        {
            // Always clear the updating flag
            lock (_lock)
            {
                _screenUpdating[screenId] = false;
            }
        }
    }

    private async Task SendWidgetContentWithLock(int screenId, IWidget widget)
    {
        // Check if already updating
        lock (_lock)
        {
            if (_screenUpdating.GetValueOrDefault(screenId, false))
            {
                Console.WriteLine($"Screen {screenId} is already updating, skipping");
                return;
            }
            _screenUpdating[screenId] = true;
        }

        try
        {
            var content = widget.GetContent();
            await _transferService.SendBinaryDataAsync(content, screenId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending widget content to screen {screenId}: {ex.Message}");
        }
        finally
        {
            // Always clear the updating flag
            lock (_lock)
            {
                _screenUpdating[screenId] = false;
            }
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

    private void SaveCurrentAssignments()
    {
        if (_isLoading) return;

        lock (_lock)
        {
            _settingService.WidgetAssignments.Clear();

            foreach (var (screenId, widget) in _screenWidgets)
            {
                if (widget == null)
                {
                    _settingService.WidgetAssignments[screenId] = null;
                }
                else if (widget is IWidget w)
                {
                    _settingService.WidgetAssignments[screenId] = w.Name;
                }
                else if (widget is IScriptWidget sw)
                {
                    _settingService.WidgetAssignments[screenId] = $"[Lua] {sw.Name}";
                }
            }

            _settingService.Save();
        }
    }

    public void LoadSavedAssignments()
    {
        _isLoading = true;

        try
        {
            foreach (var (screenId, widgetName) in _settingService.WidgetAssignments)
            {
                if (string.IsNullOrEmpty(widgetName))
                {
                    continue;
                }

                // Check if it's a script widget
                if (widgetName.StartsWith("[Lua] "))
                {
                    var scriptName = widgetName.Substring(6);
                    var scriptWidget = _availableScriptWidgets.FirstOrDefault(w => w.Name == scriptName);
                    if (scriptWidget != null)
                    {
                        AssignScriptWidgetToScreen(screenId, scriptWidget);
                    }
                }
                else
                {
                    var widget = _availableWidgets.FirstOrDefault(w => w.Name == widgetName);
                    if (widget != null)
                    {
                        AssignWidgetToScreen(screenId, widget);
                    }
                }
            }
        }
        finally
        {
            _isLoading = false;
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
