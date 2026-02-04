namespace BrickPrinterApp.Interfaces;

public interface IDisplayService
{
    byte[] ConvertImageToBinary(Image image);
}