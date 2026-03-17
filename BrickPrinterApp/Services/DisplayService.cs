using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

public class DisplayService : IDisplayService
{

    public byte[] ConvertImageToBinary(Image image)
    {
        if (image.Width != SettingService.ScreenWidth || image.Height != SettingService.ScreenHeight)
            throw new ArgumentException("Bild muss exakt 128x64 Pixel groß sein.");

        return ConvertTo1BitRaw(new Bitmap(image));
    }

    private static byte[] ConvertTo1BitRaw(Bitmap bmp)
    {
        var buffer = new byte[1024];
        var idx = 0;

        for (var y = 0; y < SettingService.ScreenHeight; y += 8)
        for (var x = 0; x < SettingService.ScreenWidth; x++)
        {
            byte colByte = 0;
            for (var bit = 0; bit < 8; bit++)
            {
                var pixel = bmp.GetPixel(x, y + bit);
                // Berechne Helligkeit: Durchschnitt von R, G, B
                var brightness = (pixel.R + pixel.G + pixel.B) / 3;
                // Pixel ist "gesetzt" (weiß) wenn Helligkeit über 128
                if (brightness > 128)
                    colByte |= (byte)(1 << bit);
            }

            buffer[idx++] = colByte;
        }

        return buffer;
    }
}