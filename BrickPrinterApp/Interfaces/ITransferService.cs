namespace BrickPrinterApp.Interfaces;

public record ScreenStatus(int Id, bool Active);

public interface ITransferService
{
    Task<bool> SendBinaryDataAsync(byte[] binaryData, int screenId = 0);
    Task<bool> SendScriptAsync(string script, string language, int screenId = 0);
    Task<bool> StopScriptAsync(int screenId = 0);
    Task<bool> PingAsync();
    Task<ScreenStatus[]?> PingWithStatusAsync();
    Task<bool> IsScriptRunningAsync(int screenId = 0);
    void StartKeepAlive(TimeSpan interval, Func<ScreenStatus[], Task>? onStatus = null);
    void StopKeepAlive();
    bool IsConnected { get; }
}
