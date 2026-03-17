namespace BrickPrinterApp.Interfaces;

public interface ITextService
{
    byte[] ConvertTextToBinary(string[] lines);
}
