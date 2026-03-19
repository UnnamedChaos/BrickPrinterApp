using System.Reflection;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Widgets;

public class LogoWidget : IWidget
{
    private readonly IDisplayService _displayService;
    private readonly byte[] _cachedContent;

    public string Name => "Logo";
    public TimeSpan UpdateInterval => TimeSpan.FromHours(1);

    public LogoWidget(IDisplayService displayService)
    {
        _displayService = displayService;
        _cachedContent = LoadAndConvertLogo();
    }

    public byte[] GetContent()
    {
        return _cachedContent;
    }

    private byte[] LoadAndConvertLogo()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream("BrickPrinterApp.Resources.logo.png");
        if (stream == null)
            throw new InvalidOperationException("Could not find embedded resource: logo.png");

        using var image = Image.FromStream(stream);
        return _displayService.ConvertImageToBinary(image);
    }
}
