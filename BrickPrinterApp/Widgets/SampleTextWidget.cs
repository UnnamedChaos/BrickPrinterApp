using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public class SampleTextWidget : IWidget
{
    private readonly ITextService _textService;

    public string Name => "Sample Text";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(10);

    public SampleTextWidget(ITextService textService)
    {
        _textService = textService;
    }

    public byte[] GetContent()
    {
        var lines = new[]
        {
            "BrickPrinter",
            "Status: OK",
            "Status: OK",
            "Status: OK",
            $"Zeit: {DateTime.Now:HH:mm:ss}",
            $"Datum: {DateTime.Now:dd.MM.yyyy}"
        };

        return _textService.ConvertTextToBinary(lines);
    }
}
