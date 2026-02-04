using BrickPrinterApp.Interfaces;

namespace BrickPrinterApp.Services;

public class DisplayService : IDisplayService
{
    private const int ScreenHeight = 64;
    private const int ScreenWidth = 128;

    public byte[] ConvertImageToBinary(Image image)
    {
        if (image.Width != ScreenWidth || image.Height != ScreenHeight)
            throw new ArgumentException("Bild muss exakt 128x64 Pixel groß sein.");

        return ConvertTo1BitRaw(new Bitmap(image));
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