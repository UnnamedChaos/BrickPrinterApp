namespace BrickPrinterApp.Interfaces;

public interface ITransferService
{
    Task<bool> SendBinaryDataAsync(byte[] binaryData, int screenId = 0);
    Task<bool> PingAsync();
    void StartKeepAlive(TimeSpan interval);
    void StopKeepAlive();
    bool IsConnected { get; }
}
