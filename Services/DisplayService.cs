using System.Net.Http.Headers;
using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

public class DisplayService(HttpClient httpClient, SettingService settings) : IDisplayService
{
    private const int ScreenHeight = 64;
    private const int ScreenWidth = 128;

    public async Task<bool> SendImageAsync(Image image)
    {
        if (image.Width != ScreenWidth || image.Height != ScreenHeight)
            throw new ArgumentException("Bild muss exakt 128x64 Pixel groß sein.");

        var binaryData = ConvertTo1BitRaw(new Bitmap(image));

        using var content = new ByteArrayContent(binaryData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        var response = await httpClient.PostAsync(settings.EndpointUrl, content);
        return response.IsSuccessStatusCode;
    }

    private static byte[] ConvertTo1BitRaw(Bitmap bmp)
    {
        var buffer = new byte[1024];
        var idx = 0;

        for (var y = 0; y < ScreenHeight; y += 8)
        for (var x = 0; x < ScreenWidth; x++)
        {
            byte colByte = 0;
            for (var bit = 0; bit < 8; bit++)
            {
                var pixel = bmp.GetPixel(x, y + bit);
                if (pixel.R > ScreenWidth)
                    colByte |= (byte)(1 << bit);
            }

            buffer[idx++] = colByte;
        }

        return buffer;
    }
}