namespace BrickPrinterApp.Interfaces;

public interface IWidget
{
    string Name { get; }
    TimeSpan UpdateInterval { get; }
    byte[] GetContent();
}
