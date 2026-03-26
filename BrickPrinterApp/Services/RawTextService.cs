using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Peripherals.Displays;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
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
            // Font naming: FontWIDTHxHEIGHT - use matching lineHeight
            var (font, lineHeight) = lines.Length switch
            {
                <= 3 => ((IFont)new Font12x20(), 21),
                4 => (new Font12x16(), 16),
                5 => (new Font8x12(), 12),
                <= 8 => (new Font4x8(), 8),  // Use Font4x8 for better rendering
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