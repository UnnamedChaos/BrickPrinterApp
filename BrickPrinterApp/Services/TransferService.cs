using System.Net.Http.Headers;
using System.Text.Json;
using BrickPrinterApp.Interfaces;
using Timer = System.Threading.Timer;

namespace BrickPrinterApp.Services;

public class TransferService : ITransferService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SettingService _settings;
    private Timer? _keepAliveTimer;
    private Func<ScreenStatus[], Task>? _onStatusCallback;
    private bool _isConnected;
    private readonly object _lock = new();

    // Rate limiting - increased to 250ms to give ESP32 more breathing room
    private readonly SemaphoreSlim _requestSemaphore = new(1, 1);
    private DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly TimeSpan MinRequestInterval = TimeSpan.FromMilliseconds(250);

    // Retry settings
    private const int MaxRetries = 2;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(500);

    public bool IsConnected
    {
        get { lock (_lock) return _isConnected; }
        private set { lock (_lock) _isConnected = value; }
    }

    public TransferService(HttpClient httpClient, SettingService settings)
    {
        _httpClient = httpClient;
        _settings = settings;
    }

    private async Task ThrottleAsync(CancellationToken ct = default)
    {
        await _requestSemaphore.WaitAsync(ct);
        try
        {
            var elapsed = DateTime.UtcNow - _lastRequestTime;
            if (elapsed < MinRequestInterval)
            {
                await Task.Delay(MinRequestInterval - elapsed, ct);
            }
        }
        catch (OperationCanceledException)
        {
            _requestSemaphore.Release();
            throw;
        }
    }

    private void CompleteRequest()
    {
        _lastRequestTime = DateTime.UtcNow;
        _requestSemaphore.Release();
    }

    private async Task<bool> ExecuteWithRetryAsync(Func<Task<bool>> action)
    {
        for (int attempt = 0; attempt <= MaxRetries; attempt++)
        {
            try
            {
                var result = await action();
                if (result) return true;
            }
            catch (HttpRequestException)
            {
                // Connection error, will retry
            }
            catch (TaskCanceledException)
            {
                // Timeout, will retry
            }

            if (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelay);
            }
        }

        return false;
    }

    public async Task<bool> SendBinaryDataAsync(byte[] binaryData, int screenId = 0)
    {
        try
        {
            await ThrottleAsync();
            try
            {
                var success = await ExecuteWithRetryAsync(async () =>
                {
                    using var content = new ByteArrayContent(binaryData);
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

                    var url = _settings.GetEndpointUrl(screenId);
                    var response = await _httpClient.PostAsync(url, content);
                    return response.IsSuccessStatusCode;
                });

                IsConnected = success;
                return success;
            }
            finally
            {
                CompleteRequest();
            }
        }
        catch
        {
            IsConnected = false;
            return false;
        }
    }

    public async Task<bool> SendScriptAsync(string script, string language, int screenId = 0, int intervalMs = 1000)
    {
        try
        {
            await ThrottleAsync();
            try
            {
                var success = await ExecuteWithRetryAsync(async () =>
                {
                    var formData = new Dictionary<string, string>
                    {
                        { "script", script }
                    };
                    using var content = new FormUrlEncodedContent(formData);

                    var url = $"{_settings.GetScriptUrl(screenId)}&interval={intervalMs}";
                    Console.WriteLine($"Sending script to {url} (interval: {intervalMs}ms)");
                    var response = await _httpClient.PostAsync(url, content);
                    return response.IsSuccessStatusCode;
                });

                IsConnected = success;
                return success;
            }
            finally
            {
                CompleteRequest();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"SendScript error: {ex.Message}");
            IsConnected = false;
            return false;
        }
    }

    public async Task<bool> StopScriptAsync(int screenId = 0)
    {
        try
        {
            await ThrottleAsync();
            try
            {
                var success = await ExecuteWithRetryAsync(async () =>
                {
                    var url = _settings.GetStopScriptUrl(screenId);
                    var response = await _httpClient.PostAsync(url, null);
                    return response.IsSuccessStatusCode;
                });

                IsConnected = success;
                return success;
            }
            finally
            {
                CompleteRequest();
            }
        }
        catch
        {
            IsConnected = false;
            return false;
        }
    }

    public async Task<bool> PingAsync()
    {
        var status = await PingWithStatusAsync();
        return status != null;
    }

    public async Task<ScreenStatus[]?> PingWithStatusAsync()
    {
        try
        {
            await ThrottleAsync();
            try
            {
                var response = await _httpClient.GetAsync(_settings.PingUrl);
                if (!response.IsSuccessStatusCode)
                {
                    IsConnected = false;
                    return null;
                }

                IsConnected = true;
                var json = await response.Content.ReadAsStringAsync();

                // Parse JSON: {"screens":[{"id":0,"active":true},{"id":1,"active":false},...]}
                using var doc = JsonDocument.Parse(json);
                var screens = doc.RootElement.GetProperty("screens");
                var result = new List<ScreenStatus>();

                foreach (var screen in screens.EnumerateArray())
                {
                    var id = screen.GetProperty("id").GetInt32();
                    var active = screen.GetProperty("active").GetBoolean();
                    result.Add(new ScreenStatus(id, active));
                }

                return result.ToArray();
            }
            finally
            {
                CompleteRequest();
            }
        }
        catch
        {
            IsConnected = false;
            return null;
        }
    }

    public async Task<bool> IsScriptRunningAsync(int screenId = 0)
    {
        try
        {
            await ThrottleAsync();
            try
            {
                var url = _settings.GetStatusUrl(screenId);
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                // Parse JSON to check if script is running
                // Expected format: {"script_running": true/false}
                return json.Contains("\"script_running\":true") || json.Contains("\"script_running\": true");
            }
            finally
            {
                CompleteRequest();
            }
        }
        catch
        {
            return false;
        }
    }

    public void StartKeepAlive(TimeSpan interval, Func<ScreenStatus[], Task>? onStatus = null)
    {
        StopKeepAlive();
        _onStatusCallback = onStatus;
        _keepAliveTimer = new Timer(KeepAliveCallback, null, TimeSpan.Zero, interval);
    }

    private async void KeepAliveCallback(object? state)
    {
        var status = await PingWithStatusAsync();
        if (status != null && _onStatusCallback != null)
        {
            try
            {
                await _onStatusCallback(status);
            }
            catch
            {
                // Ignore callback errors
            }
        }
    }

    public void StopKeepAlive()
    {
        _keepAliveTimer?.Dispose();
        _keepAliveTimer = null;
    }

    public void Dispose()
    {
        StopKeepAlive();
        _requestSemaphore.Dispose();
        GC.SuppressFinalize(this);
    }
}
