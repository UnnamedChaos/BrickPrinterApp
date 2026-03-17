namespace BrickPrinterApp.Interfaces;

public interface IWotDisplayService
{
    Task<byte[]> CreateDisplayDataAsync(string playerId);
}
