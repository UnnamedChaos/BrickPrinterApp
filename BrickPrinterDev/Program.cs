using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();

// Ensure output directory exists
var outputDir = Path.Combine(Directory.GetCurrentDirectory(), "output");
Directory.CreateDirectory(outputDir);

app.MapPost("/upload", async (HttpRequest request) =>
{
    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    var binaryData = ms.ToArray();

    if (binaryData.Length != 1024)
    {
        return Results.BadRequest($"Expected 1024 bytes, received {binaryData.Length}");
    }

    // Convert binary data back to image
    var bitmap = ConvertBinaryToImage(binaryData);

    // Save to output folder with timestamp
    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

    // Speichere Original (128x64)
    var filename = Path.Combine(outputDir, $"screen.png");
    bitmap.Save(filename, ImageFormat.Png);

    // Speichere vergrößerte Version (4x Scale = 512x256) ohne Interpolation
    //var scaledFilename = Path.Combine(outputDir, $"screen_{timestamp}_scaled.png");
     //SaveScaledImage(bitmap, scaledFilename, 4);

    // Speichere als Text-Datei (ASCII-Art)
    var textFilename = Path.Combine(outputDir, $"screen.txt");
    SaveAsTextFile(binaryData, textFilename);

    Console.WriteLine($"Saved image to {filename}");
    Console.WriteLine($"Saved text file to {textFilename}");
    return Results.Ok(new { message = "Image saved", filename, textFilename });
});

app.Run();

static Bitmap ConvertBinaryToImage(byte[] binaryData)
{
    const int screenWidth = 128;
    const int screenHeight = 64;

    // Erstelle Bitmap im 1-Bit Format für pixelgenaue Darstellung
    var bitmap = new Bitmap(screenWidth, screenHeight, PixelFormat.Format1bppIndexed);

    // Setze Schwarz-Weiß-Palette
    var palette = bitmap.Palette;
    palette.Entries[0] = Color.Black; // Index 0 = Schwarz
    palette.Entries[1] = Color.White; // Index 1 = Weiß
    bitmap.Palette = palette;

    // Lock bitmap für direkten Pixel-Zugriff
    var bitmapData = bitmap.LockBits(
        new Rectangle(0, 0, screenWidth, screenHeight),
        ImageLockMode.WriteOnly,
        PixelFormat.Format1bppIndexed);

    try
    {
        var idx = 0;
        var stride = bitmapData.Stride;
        var scan0 = bitmapData.Scan0;

        // Reverse the encoding process from DisplayService
        for (var y = 0; y < screenHeight; y += 8)
        for (var x = 0; x < screenWidth; x++)
        {
            var colByte = binaryData[idx++];

            // Unpack 8 vertical pixels from the byte
            for (var bit = 0; bit < 8; bit++)
            {
                var yPos = y + bit;
                var isSet = (colByte & (1 << bit)) != 0;

                // Berechne Byte-Position in der Bitmap
                var byteIndex = yPos * stride + (x / 8);
                var bitPosition = 7 - (x % 8);

                unsafe
                {
                    var ptr = (byte*)scan0.ToPointer();
                    if (isSet)
                        ptr[byteIndex] |= (byte)(1 << bitPosition);
                    else
                        ptr[byteIndex] &= (byte)~(1 << bitPosition);
                }
            }
        }
    }
    finally
    {
        bitmap.UnlockBits(bitmapData);
    }

    return bitmap;
}

static void SaveScaledImage(Bitmap original, string filename, int scale)
{
    var scaledWidth = original.Width * scale;
    var scaledHeight = original.Height * scale;

    using var scaled = new Bitmap(scaledWidth, scaledHeight);
    using var graphics = Graphics.FromImage(scaled);

    // Wichtig: Keine Interpolation für pixelgenaue Skalierung
    graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
    graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;

    graphics.DrawImage(original, 0, 0, scaledWidth, scaledHeight);
    scaled.Save(filename, ImageFormat.Png);
}

static void SaveAsTextFile(byte[] binaryData, string filename)
{
    const int screenWidth = 128;
    const int screenHeight = 64;

    using var writer = new StreamWriter(filename);
    var idx = 0;

    // Reverse the encoding process from DisplayService
    for (var y = 0; y < screenHeight; y++)
    {
        for (var x = 0; x < screenWidth; x++)
        {
            // Finde das richtige Byte und Bit
            var byteIndex = (y / 8) * screenWidth + x;
            var bitPosition = y % 8;

            var colByte = binaryData[byteIndex];
            var isSet = (colByte & (1 << bitPosition)) != 0;

            writer.Write(isSet ? "X" : " ");
        }
        writer.WriteLine(); // Neue Zeile nach jeder Bildzeile
    }
}