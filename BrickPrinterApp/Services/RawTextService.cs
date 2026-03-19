using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Peripherals.Displays;
using BrickPrinterApp.Interfaces;
using Color = Meadow.Color;
namespace BrickPrinterApp.Services;

public class RawTextService : ITextService
{
    private readonly Buffer1bpp _buffer;
    private readonly MicroGraphics _graphics;
    private readonly int _height;
    private readonly int _width;
    private readonly object _lock = new();

    public RawTextService()
    {
        _height = 64;
        _width = 128;
        _buffer = new Buffer1bpp(_width, _height);

        // Wir erstellen einen Simulator, der das IPixelDisplay Interface erfüllt
        var displaySimulator = new DisplayBufferSimulator(_buffer);

        _graphics = new MicroGraphics(displaySimulator)
        {
            CurrentFont = new Font4x8()
        };
    }

    public byte[] ConvertTextToBinary(string[] lines)
    {
        lock (_lock)
        {
            _buffer.Clear();
            _graphics.Clear(true);

            // Adjust font based on line count
            var (font, lineHeight) = lines.Length switch
            {
                <= 3 => ((IFont)new Font12x20(), 20),
                4 => (new Font12x16(), 16),
                5 => (new Font8x12(), 12),
                <= 8 => (new Font6x8(), 8),
                _ => (new Font4x6(), 6)
            };

            _graphics.CurrentFont = font;

            for (int i = 0; i < lines.Length; i++)
            {
                _graphics.DrawText(1, i * lineHeight, lines[i], Color.White);
            }

            _graphics.Show();

            // Return a copy to avoid reference issues
            var result = new byte[_buffer.Buffer.Length];
            Array.Copy(_buffer.Buffer, result, result.Length);
            return result;
        }
    }
}

public class DisplayBufferSimulator : IPixelDisplay
{
    // Falls ColorType nicht gefunden wird, probiere ColorMode
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