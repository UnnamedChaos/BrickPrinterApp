namespace BrickPrinterApp.Interfaces;

public interface ITransferService
{
    Task<bool> SendBinaryDataAsync(byte[] binaryData, int screenId = 0);
    Task<bool> SendScriptAsync(string script, string language, int screenId = 0);
    Task<bool> StopScriptAsync(int screenId = 0);
    Task<bool> PingAsync();
    void StartKeepAlive(TimeSpan interval);
    void StopKeepAlive();
    bool IsConnected { get; }
}
