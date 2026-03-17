using BrickPrinterApp.Interfaces;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Peripherals.Displays;
using Newtonsoft.Json.Linq;
using Color = Meadow.Color;

namespace BrickPrinterApp.Services;

public class WotDisplayService : IWotDisplayService
{
    private readonly IWotService _wotService;
    private readonly HttpClient _httpClient;
    private readonly Buffer1bpp _buffer;
    private readonly MicroGraphics _graphics;
    private readonly DisplayBufferSimulator _displaySimulator;

    public WotDisplayService(IWotService wotService, HttpClient httpClient)
    {
        _wotService = wotService;
        _httpClient = httpClient;

        _buffer = new Buffer1bpp(128, 64);
        _displaySimulator = new DisplayBufferSimulator(_buffer);
        _graphics = new MicroGraphics(_displaySimulator)
        {
            CurrentFont = new Font4x8()
        };
    }

    public async Task<byte[]> CreateDisplayDataAsync(string playerId)
    {
        // Hole Player-Statistiken (Overall + Today)
        var playerData = await _wotService.GetPlayerDataAsync(playerId);
        if (playerData == null)
        {
            return CreateErrorDisplay("API Error");
        }

        // Hole letztes Battle
        var battlesJson = await _wotService.GetPlayerSessionsAsync(playerId);
        if (battlesJson == null)
        {
            return CreateErrorDisplay("No Battles");
        }

        var battles = battlesJson["data"] as JArray;
        if (battles == null || battles.Count == 0)
        {
            return CreateErrorDisplay("No Battles");
        }

        // Statistiken aus API holen (nicht berechnen!)
        var todayWnx = playerData["days"]?["1"]?["wn"]?.Value<double>() ?? 0;
        var battlesToday = playerData["days"]?["1"]?["battles"]?.Value<int>() ?? 0;

        // Letztes Battle
        var lastBattle = battles[1];
        var tankName = lastBattle["short_name"]?.Value<string>() ?? "Unknown";
        var battleWnx = lastBattle["wnx"]?.Value<double>() ?? 0;
        var moe = lastBattle["moe"]?.Value<string>() ?? "0";
        var credits = lastBattle["credits_recieved"]?.Value<int>() ?? 0;
        var contourUrl = lastBattle["contour_icon"]?.Value<string>();

        // Erstelle Display
        return await CreateDisplayAsync(
            todayWnx, battlesToday,
            tankName, battleWnx, moe, credits, contourUrl);
    }

    private async Task<byte[]> CreateDisplayAsync(
        double todayWnx, int battlesToday,
        string tankName, double battleWnx, string moe, int credits, string? contourUrl)
    {
        _graphics.Clear(true);

        // === HEADER (Oberste 10 Pixel) ===
        // Header-Text: "OVR:1234 TDY:1234 B:12"
        var headerText = $"TDY:{(int)todayWnx} B:{battlesToday}";
        _graphics.DrawText(1, 1, headerText, Color.White);

        // Trennlinie bei Pixel 10
        _graphics.DrawLine(0, 10, 127, 10, Color.White);

        // === BATTLE INFO (Rest: 54 Pixel) ===
        int yOffset = 12;

        // Lade Contour Image
        byte[]? contourBuffer = null;
        int contourWidth = 0;
        int contourHeight = 0;

        if (!string.IsNullOrEmpty(contourUrl))
        {
            var imageData = await DownloadAndConvertImageAsync(contourUrl);
            if (imageData != null)
            {
                contourBuffer = imageData.Item1;
                contourWidth = imageData.Item2;
                contourHeight = imageData.Item3;
            }
        }

        // Wenn Contour-Bild verfügbar, links platzieren
        if (contourBuffer != null)
        {
            // Zeichne Contour-Bild (links)
            DrawImage(contourBuffer, contourWidth, contourHeight, 1, yOffset);

            // Text rechts vom Bild (ab x=54)
            int textX = 54;

            // Tank Name
            _graphics.DrawText(textX, yOffset, tankName, Color.White);

            // WNx
            _graphics.DrawText(textX, yOffset + 10, $"WN:{(int)battleWnx}", Color.White);

            // MoE
            _graphics.DrawText(textX, yOffset + 20, $"MoE:{moe}", Color.White);

            // Credits (mit K für Tausend)
            var creditsText = credits >= 1000 ? $"{credits / 1000}K" : credits.ToString();
            _graphics.DrawText(textX, yOffset + 30, $"CR:{creditsText}", Color.White);
        }
        else
        {
            // Kein Bild, zentrierter Text
            _graphics.DrawText(2, yOffset, tankName, Color.White);
            _graphics.DrawText(2, yOffset + 10, $"WN:{(int)battleWnx}", Color.White);
            _graphics.DrawText(2, yOffset + 20, $"MoE:{moe}", Color.White);
            var creditsText = credits >= 1000 ? $"{credits / 1000}K" : credits.ToString();
            _graphics.DrawText(2, yOffset + 30, $"CR:{creditsText}", Color.White);
        }

        _graphics.Show();

        // Konvertiere zu ESP32 Page Mode
        return ConvertToPageMode();
    }

    private void DrawImage(byte[] imageBuffer, int width, int height, int x, int y)
    {
        // Zeichne Bild Pixel für Pixel
        int idx = 0;
        for (int row = 0; row < height; row++)
        {
            for (int col = 0; col < width; col += 8)
            {
                if (idx >= imageBuffer.Length) return;

                byte dataByte = imageBuffer[idx++];

                for (int bit = 0; bit < 8 && col + bit < width; bit++)
                {
                    bool isSet = (dataByte & (1 << (7 - bit))) != 0;
                    if (isSet)
                    {
                        _buffer.SetPixel(x + col + bit, y + row, Color.White);
                    }
                }
            }
        }
    }

    private async Task<Tuple<byte[], int, int>?> DownloadAndConvertImageAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var image = System.Drawing.Image.FromStream(stream);
            using var bitmap = new System.Drawing.Bitmap(image);

            // KEINE Skalierung - verwende Originalgröße
            int width = bitmap.Width;
            int height = bitmap.Height;

            // Konvertiere zu 1-Bit (horizontal packed)
            int bytesPerRow = (width + 7) / 8;
            var buffer = new byte[bytesPerRow * height];
            int bufferIdx = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x += 8)
                {
                    byte dataByte = 0;
                    for (int bit = 0; bit < 8 && x + bit < width; bit++)
                    {
                        var pixel = bitmap.GetPixel(x + bit, y);
                        var brightness = (pixel.R + pixel.G + pixel.B) / 3;
                        if (brightness > 128)
                        {
                            dataByte |= (byte)(1 << (7 - bit));
                        }
                    }
                    buffer[bufferIdx++] = dataByte;
                }
            }

            return Tuple.Create(buffer, width, height);
        }
        catch
        {
            return null;
        }
    }

    private byte[] ConvertToPageMode()
    {
        var output = new byte[1024];
        var idx = 0;

        // ESP32 Format: 8 vertikale Pixel pro Byte, column-major
        for (var page = 0; page < 8; page++)
        {
            for (var x = 0; x < 128; x++)
            {
                byte colByte = 0;
                for (var bit = 0; bit < 8; bit++)
                {
                    var y = page * 8 + bit;
                    if (_buffer.GetPixel(x, y) == Color.White)
                    {
                        colByte |= (byte)(1 << bit);
                    }
                }
                output[idx++] = colByte;
            }
        }

        return output;
    }

    private byte[] CreateErrorDisplay(string message)
    {
        _graphics.Clear(true);
        _graphics.DrawText(10, 28, $"Error: {message}", Color.White);
        _graphics.Show();
        return ConvertToPageMode();
    }
}
