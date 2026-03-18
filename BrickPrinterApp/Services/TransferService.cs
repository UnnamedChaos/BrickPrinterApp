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

    public async Task<bool> SendBinaryDataAsync(byte[] binaryData, int screenId = 0)
    {
        try
        {
            using var content = new ByteArrayContent(binaryData);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var url = _settings.GetEndpointUrl(screenId);
            var response = await _httpClient.PostAsync(url, content);
            IsConnected = response.IsSuccessStatusCode;
            return response.IsSuccessStatusCode;
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
            var response = await _httpClient.GetAsync(_settings.PingUrl);
            IsConnected = response.IsSuccessStatusCode;
            return response.IsSuccessStatusCode;
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
        GC.SuppressFinalize(this);
    }
}
