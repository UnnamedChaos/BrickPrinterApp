using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Widgets;

public class YouTubeWidget : IWidget
{
    private readonly IDisplayService _displayService;
    private readonly IActiveWindowService _activeWindowService;

    // Scrolling state
    private int _scrollOffset = 0;
    private string _lastTitle = "";
    private float _textWidth = 0;
    private const int ScrollSpeed = 3;
    private const int PauseAtStart = 5; // Frames to pause at start
    private int _pauseCounter = 0;

    public string Name => "YouTube";
    public TimeSpan UpdateInterval => TimeSpan.FromMilliseconds(150); // Fast updates for smooth scrolling

    public YouTubeWidget(IDisplayService displayService, IActiveWindowService activeWindowService)
    {
        _displayService = displayService;
        _activeWindowService = activeWindowService;
    }

    public byte[] GetContent()
    {
        var windowInfo = _activeWindowService.GetActiveWindow();
        var videoTitle = ExtractVideoTitle(windowInfo.WindowTitle);

        // Reset scroll when title changes
        if (videoTitle != _lastTitle)
        {
            _scrollOffset = 0;
            _pauseCounter = PauseAtStart;
            _lastTitle = videoTitle;
        }

        var image = DrawYouTubeScreen(videoTitle);
        return _displayService.ConvertImageToBinary(image);
    }

    private string ExtractVideoTitle(string windowTitle)
    {
        if (string.IsNullOrEmpty(windowTitle))
            return "No Video";

        // Pattern: "Video Title - YouTube - Browser" or "Video Title - YouTube"
        var match = Regex.Match(windowTitle, @"^(.+?)\s*-\s*YouTube", RegexOptions.IgnoreCase);
        if (match.Success)
        {
            var title = match.Groups[1].Value.Trim();
            return string.IsNullOrEmpty(title) ? "YouTube" : title;
        }

        // Check if it's YouTube at all
        if (windowTitle.Contains("YouTube", StringComparison.OrdinalIgnoreCase))
            return "YouTube";

        return "No Video";
    }

    private Bitmap DrawYouTubeScreen(string videoTitle)
    {
        var bitmap = new Bitmap(SettingService.ScreenWidth, SettingService.ScreenHeight);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Black);

        // Draw YouTube play button icon (centered, top portion)
        DrawYouTubeIcon(g, 44, 4, 40, 28);

        // Draw scrolling video title
        DrawScrollingTitle(g, videoTitle, 38);

        return bitmap;
    }

    private void DrawYouTubeIcon(System.Drawing.Graphics g, int x, int y, int width, int height)
    {
        using var pen = new Pen(Color.White, 2);
        using var brush = new SolidBrush(Color.White);

        // Draw rounded rectangle (YouTube logo background shape)
        var rect = new Rectangle(x, y, width, height);
        var radius = 8;
        using var path = CreateRoundedRectangle(rect, radius);
        g.DrawPath(pen, path);

        // Draw play triangle in center
        var centerX = x + width / 2;
        var centerY = y + height / 2;
        var triangleSize = height / 3;

        var triangle = new Point[]
        {
            new Point(centerX - triangleSize / 2, centerY - triangleSize),
            new Point(centerX - triangleSize / 2, centerY + triangleSize),
            new Point(centerX + triangleSize, centerY)
        };
        g.FillPolygon(brush, triangle);
    }

    private GraphicsPath CreateRoundedRectangle(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
        path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
        path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private void DrawScrollingTitle(System.Drawing.Graphics g, string title, int y)
    {
        using var font = new Font("Arial", 10, FontStyle.Bold);
        var screenWidth = SettingService.ScreenWidth;

        // Measure text width
        _textWidth = g.MeasureString(title, font).Width;

        // If text fits on screen, just center it (no scrolling needed)
        if (_textWidth <= screenWidth)
        {
            var x = (screenWidth - _textWidth) / 2;
            g.DrawString(title, font, Brushes.White, x, y);
            return;
        }

        // Scrolling text with separator
        var separator = "   ●   ";
        var fullText = title + separator + title + separator;
        var fullWidth = g.MeasureString(title + separator, font).Width;

        // Handle pause at start
        if (_pauseCounter > 0)
        {
            _pauseCounter--;
            g.DrawString(fullText, font, Brushes.White, 0, y);
            return;
        }

        // Draw scrolling text
        var x_pos = -_scrollOffset;
        g.DrawString(fullText, font, Brushes.White, x_pos, y);

        // Update scroll position
        _scrollOffset += ScrollSpeed;

        // Reset when one full cycle is complete
        if (_scrollOffset >= fullWidth)
        {
            _scrollOffset = 0;
            _pauseCounter = PauseAtStart;
        }
    }
}
