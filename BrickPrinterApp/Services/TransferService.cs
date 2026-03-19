using System.Net.Http.Headers;
using BrickPrinterApp.Interfaces;
using Timer = System.Threading.Timer;

namespace BrickPrinterApp.Services;

public class TransferService : ITransferService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly SettingService _settings;
    private Timer? _keepAliveTimer;
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

    public async Task<bool> SendScriptAsync(string script, string language, int screenId = 0)
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

                    var url = _settings.GetScriptUrl(screenId);
                    Console.WriteLine("Sending script to " + url);
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
        try
        {
            await ThrottleAsync();
            try
            {
                var success = await ExecuteWithRetryAsync(async () =>
                {
                    var response = await _httpClient.GetAsync(_settings.PingUrl);
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

    public void StartKeepAlive(TimeSpan interval)
    {
        StopKeepAlive();
        _keepAliveTimer = new Timer(async _ => await PingAsync(), null, TimeSpan.Zero, interval);
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
