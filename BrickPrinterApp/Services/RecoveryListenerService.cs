using System.Net;
using System.Text;

namespace BrickPrinterApp.Services;

/// <summary>
/// HTTP listener that handles recovery requests from ESP32 when screens lose their widgets
/// </summary>
public class RecoveryListenerService : IDisposable
{
    private readonly WidgetService _widgetService;
    private readonly HttpListener _listener;
    private CancellationTokenSource? _cts;
    private Task? _listenerTask;
    private bool _isRunning;

    public const int RecoveryPort = 5225;

    public bool IsRunning => _isRunning;

    public RecoveryListenerService(WidgetService widgetService)
    {
        _widgetService = widgetService;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://+:{RecoveryPort}/");
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _listener.Start();
            _isRunning = true;
            _cts = new CancellationTokenSource();

            _listenerTask = Task.Run(() => ListenAsync(_cts.Token));
            Console.WriteLine($"Recovery listener started on port {RecoveryPort}");
        }
        catch (HttpListenerException ex)
        {
            Console.WriteLine($"Failed to start recovery listener: {ex.Message}");
            Console.WriteLine("Try running as administrator or use: netsh http add urlacl url=http://+:5225/ user=Everyone");
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _cts?.Cancel();
        _listener.Stop();
        _isRunning = false;

        Console.WriteLine("Recovery listener stopped");
    }

    private async Task ListenAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener.IsListening)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequestAsync(context);
            }
            catch (HttpListenerException) when (ct.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Recovery listener error: {ex.Message}");
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            string responseText;
            int statusCode;

            if (request.Url?.AbsolutePath == "/recovery")
            {
                var screenParam = request.QueryString["screen"];

                if (int.TryParse(screenParam, out int screenId))
                {
                    Console.WriteLine($"Recovery request from {request.RemoteEndPoint} for screen {screenId}");
                    var success = await _widgetService.ResendWidgetAsync(screenId);
                    statusCode = success ? 200 : 204;
                    responseText = success
                        ? $"{{\"message\":\"Widget resent\",\"screen\":{screenId}}}"
                        : $"{{\"message\":\"No widget assigned\",\"screen\":{screenId}}}";
                }
                else
                {
                    Console.WriteLine($"Recovery request from {request.RemoteEndPoint} for all screens");
                    await _widgetService.ResendAllWidgetsAsync();
                    statusCode = 200;
                    responseText = "{\"message\":\"All widgets resent\"}";
                }
            }
            else
            {
                statusCode = 404;
                responseText = "{\"error\":\"Not found\"}";
            }

            response.StatusCode = statusCode;
            response.ContentType = "application/json";
            var buffer = Encoding.UTF8.GetBytes(responseText);
            response.ContentLength64 = buffer.Length;
            await response.OutputStream.WriteAsync(buffer);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling recovery request: {ex.Message}");
            response.StatusCode = 500;
        }
        finally
        {
            response.Close();
        }
    }

    public void Dispose()
    {
        Stop();
        _cts?.Dispose();
        _listener.Close();
        GC.SuppressFinalize(this);
    }
}
