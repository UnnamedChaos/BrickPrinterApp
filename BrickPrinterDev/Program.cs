using System.Drawing;
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

// Track screen activity for recovery simulation
var screenActive = new bool[3];

// Keep-alive with screen status for smart recovery
app.MapGet("/ping", () => Results.Json(new
{
    screens = new[]
    {
        new { id = 0, active = screenActive[0] },
        new { id = 1, active = screenActive[1] },
        new { id = 2, active = screenActive[2] }
    }
}));

// Status endpoint
app.MapGet("/status", () => Results.Ok(new
{
    status = "ok",
    numDisplays = 3,
    screens = new[] {
        new { id = 0, valid = true },
        new { id = 1, valid = true },
        new { id = 2, valid = true }
    }
}));

// Clear endpoint
app.MapPost("/clear", (HttpRequest request) =>
{
    if (request.Query.TryGetValue("screen", out var screenParam) && int.TryParse(screenParam, out var screenId))
    {
        Console.WriteLine($"Screen {screenId} cleared");
        if (screenId >= 0 && screenId < screenActive.Length)
            screenActive[screenId] = false;
        return Results.Ok(new { message = $"Display {screenId} cleared" });
    }
    Console.WriteLine("All screens cleared");
    for (int i = 0; i < screenActive.Length; i++)
        screenActive[i] = false;
    return Results.Ok(new { message = "All displays cleared" });
});

app.MapPost("/upload", async (HttpRequest request) =>
{
    // Get screen ID from query parameter (default to 0)
    var screenId = 0;
    if (request.Query.TryGetValue("screen", out var screenParam) && int.TryParse(screenParam, out var parsedId))
    {
        screenId = parsedId;
    }

    using var ms = new MemoryStream();
    await request.Body.CopyToAsync(ms);
    var binaryData = ms.ToArray();

    if (binaryData.Length != 1024)
    {
        return Results.BadRequest($"Expected 1024 bytes, received {binaryData.Length}");
    }

    // Convert binary data back to image
    var bitmap = ConvertBinaryToImage(binaryData);

    // Save to output folder with screen ID
    var filename = Path.Combine(outputDir, $"screen_{screenId}.png");
    bitmap.Save(filename, ImageFormat.Png);

    // Save as text file (ASCII-Art)
    var textFilename = Path.Combine(outputDir, $"screen_{screenId}.txt");
    SaveAsTextFile(binaryData, textFilename);

    Console.WriteLine($"Screen {screenId}: Saved image to {filename}");
    Console.WriteLine($"Screen {screenId}: Saved text file to {textFilename}");

    // Mark screen as active
    if (screenId >= 0 && screenId < screenActive.Length)
        screenActive[screenId] = true;

    return Results.Ok(new { message = "Image saved", screen = screenId, filename, textFilename });
});

// Lua script endpoint
app.MapPost("/lua", (HttpRequest request) =>
{
    var screenId = 0;
    if (request.Query.TryGetValue("screen", out var screenParam) && int.TryParse(screenParam, out var parsedId))
        screenId = parsedId;

    Console.WriteLine($"Screen {screenId}: Lua script received");

    // Mark screen as active
    if (screenId >= 0 && screenId < screenActive.Length)
        screenActive[screenId] = true;

    return Results.Ok("ok");
});

// Stop lua script endpoint
app.MapPost("/lua/stop", (HttpRequest request) =>
{
    var screenId = 0;
    if (request.Query.TryGetValue("screen", out var screenParam) && int.TryParse(screenParam, out var parsedId))
        screenId = parsedId;

    Console.WriteLine($"Screen {screenId}: Lua script stopped");

    // Mark screen as inactive
    if (screenId >= 0 && screenId < screenActive.Length)
        screenActive[screenId] = false;

    return Results.Ok("ok");
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

static void SaveAsTextFile(byte[] binaryData, string filename)
{
    const int screenWidth = 128;
    const int screenHeight = 64;

    using var writer = new StreamWriter(filename);

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