using Meadow.Foundation.Graphics.Buffers;
using Meadow.Peripherals.Displays;
using Color = Meadow.Color;

namespace BrickPrinterApp.Models;

public class DisplayBufferSimulator : IPixelDisplay
{
    public ColorMode SupportedColorModes => ColorMode.Format1bpp;
    public ColorMode NativeColorMode => ColorMode.Format1bpp;

    public int Width => PixelBuffer.Width;
    public int Height => PixelBuffer.Height;
    public IPixelBuffer PixelBuffer { get; }

    public DisplayBufferSimulator(IPixelBuffer buffer)
    {
        PixelBuffer = buffer;
    }

    public void Show() { /* Nichts zu tun auf dem PC */ }
    public void Show(int left, int top, int right, int bottom) { }

    public void Clear(bool updateDisplay = false) => PixelBuffer.Clear();
    public void Fill(Color fillColor, bool updateDisplay = false) => PixelBuffer.Fill(fillColor);
    public void Fill(int x, int y, int width, int height, Color fillColor) => PixelBuffer.Fill(x, y, width, height, fillColor);

    public void WriteBuffer(int x, int y, IPixelBuffer displayBuffer) => PixelBuffer.WriteBuffer(x, y, displayBuffer);
    public ColorMode ColorMode { get; }
    public void DrawPixel(int x, int y, Color color) => PixelBuffer.SetPixel(x, y, color);
    public void DrawPixel(int x, int y, bool enabled) => PixelBuffer.SetPixel(x, y, enabled ? Color.Black : Color.White);
    public void InvertPixel(int x, int y) => PixelBuffer.InvertPixel(x, y);
}
