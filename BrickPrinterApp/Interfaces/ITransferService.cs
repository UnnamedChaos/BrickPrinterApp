namespace BrickPrinterApp.Interfaces;

public interface ITransferService
{
    Task<bool> SendBinaryDataAsync(byte[] binaryData);
}
