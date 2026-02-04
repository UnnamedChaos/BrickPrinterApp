namespace BrickPrinterApp.Interfaces;

public interface IDisplayService
{
    Task<bool> SendImageAsync(Image image);
}