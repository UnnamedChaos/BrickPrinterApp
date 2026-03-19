using System.Drawing;
using System.Net.Http.Json;
using System.Text.Json;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Widgets;

public class WeatherWidget : IWidget
{
    private readonly IDisplayService _displayService;
    private readonly HttpClient _httpClient;
    private WeatherData? _currentWeather;
    private ForecastData? _forecast;
    private bool _showToday = true;
    private DateTime _lastFetch = DateTime.MinValue;

    // Default location (can be made configurable)
    private const double Latitude = 52.52;  // Berlin
    private const double Longitude = 13.41;

    public string Name => "Weather";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(25);

    public WeatherWidget(IDisplayService displayService)
    {
        _displayService = displayService;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    public byte[] GetContent()
    {
        // Fetch new weather data every 10 minutes
        if ((DateTime.Now - _lastFetch).TotalMinutes > 10)
        {
            try
            {
                FetchWeatherData();
                _lastFetch = DateTime.Now;
            }
            catch
            {
                // Keep using old data if fetch fails
            }
        }

        // Alternate between today and forecast screens
        var image = _showToday ? DrawTodayScreen() : DrawForecastScreen();
        _showToday = !_showToday;

        return _displayService.ConvertImageToBinary(image);
    }

    private void FetchWeatherData()
    {
        // Fetch current weather
        var currentUrl = $"https://api.open-meteo.com/v1/forecast?latitude={Latitude}&longitude={Longitude}&current=temperature_2m,weathercode,windspeed_10m&timezone=auto";
        var currentResponse = _httpClient.GetStringAsync(currentUrl).Result;
        _currentWeather = JsonSerializer.Deserialize<WeatherData>(currentResponse);

        // Fetch 3-day forecast
        var forecastUrl = $"https://api.open-meteo.com/v1/forecast?latitude={Latitude}&longitude={Longitude}&daily=temperature_2m_max,temperature_2m_min,weathercode&timezone=auto&forecast_days=4";
        var forecastResponse = _httpClient.GetStringAsync(forecastUrl).Result;
        _forecast = JsonSerializer.Deserialize<ForecastData>(forecastResponse);
    }

    private Bitmap DrawTodayScreen()
    {
        var bitmap = new Bitmap(SettingService.ScreenWidth, SettingService.ScreenHeight);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(Color.Black);

        if (_currentWeather?.current == null)
        {
            DrawCenteredText(g, "Loading...", 32);
            return bitmap;
        }

        // Draw title
        DrawText(g, "TODAY", 2, 2, new Font("Arial", 8, FontStyle.Bold));

        // Draw weather icon (centered top)
        DrawWeatherIcon(g, _currentWeather.current.weathercode, 48, 12, 32);

        // Draw temperature (large, centered)
        var temp = $"{Math.Round(_currentWeather.current.temperature_2m)}°C";
        var font = new Font("Arial", 14, FontStyle.Bold);
        var size = g.MeasureString(temp, font);
        DrawText(g, temp, (int)((128 - size.Width) / 2), 48, font);

        return bitmap;
    }

    private Bitmap DrawForecastScreen()
    {
        var bitmap = new Bitmap(SettingService.ScreenWidth, SettingService.ScreenHeight);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(Color.Black);

        if (_forecast?.daily == null)
        {
            DrawCenteredText(g, "Loading...", 32);
            return bitmap;
        }

        // Draw title
        DrawText(g, "3-DAY FORECAST", 2, 2, new Font("Arial", 7, FontStyle.Bold));

        // Draw 3 days (skip today, show next 3)
        for (int i = 1; i <= 3; i++)
        {
            if (i >= _forecast.daily.time.Length)
                break;

            int x = 4 + (i - 1) * 40;
            int y = 15;

            // Day name
            var date = DateTime.Parse(_forecast.daily.time[i]);
            var dayName = date.ToString("ddd").Substring(0, 2);
            DrawText(g, dayName, x + 8, y, new Font("Arial", 6));

            // Weather icon (small)
            DrawWeatherIcon(g, _forecast.daily.weathercode[i], x + 4, y + 10, 16);

            // Temperature
            var maxTemp = Math.Round(_forecast.daily.temperature_2m_max[i]);
            var minTemp = Math.Round(_forecast.daily.temperature_2m_min[i]);
            DrawText(g, $"{maxTemp}°", x, y + 30, new Font("Arial", 7, FontStyle.Bold));
            DrawText(g, $"{minTemp}°", x, y + 40, new Font("Arial", 6));
        }

        return bitmap;
    }

    private void DrawWeatherIcon(System.Drawing.Graphics g, int weatherCode, int x, int y, int size)
    {
        var pen = new Pen(Color.White, 1);
        var brush = new SolidBrush(Color.White);

        // Weather codes from Open-Meteo
        // 0: Clear, 1-3: Partly cloudy, 45-48: Fog, 51-67: Rain, 71-86: Snow, 95-99: Thunderstorm
        switch (weatherCode)
        {
            case 0: // Clear - Sun
                g.DrawEllipse(pen, x, y, size, size);
                // Sun rays
                for (int i = 0; i < 8; i++)
                {
                    var angle = i * Math.PI / 4;
                    var x1 = x + size / 2 + (int)(Math.Cos(angle) * size * 0.4);
                    var y1 = y + size / 2 + (int)(Math.Sin(angle) * size * 0.4);
                    var x2 = x + size / 2 + (int)(Math.Cos(angle) * size * 0.65);
                    var y2 = y + size / 2 + (int)(Math.Sin(angle) * size * 0.65);
                    g.DrawLine(pen, x1, y1, x2, y2);
                }
                break;

            case >= 1 and <= 3: // Partly cloudy - Cloud with sun
                // Sun (smaller, top right)
                g.DrawEllipse(pen, x + size / 2, y, size / 2, size / 2);
                // Cloud
                g.DrawEllipse(pen, x, y + size / 2, size / 3, size / 3);
                g.DrawEllipse(pen, x + size / 4, y + size / 3, size / 2, size / 2);
                g.DrawEllipse(pen, x + size / 2, y + size / 2, size / 3, size / 3);
                break;

            case >= 45 and <= 48: // Fog - Horizontal lines
                for (int i = 0; i < 4; i++)
                {
                    g.DrawLine(pen, x + 2, y + size / 4 + i * size / 5, x + size - 2, y + size / 4 + i * size / 5);
                }
                break;

            case >= 51 and <= 67: // Rain - Cloud with drops
                // Cloud
                g.DrawEllipse(pen, x + size / 6, y, size / 3, size / 3);
                g.DrawEllipse(pen, x + size / 3, y - 2, size / 2, size / 2);
                g.DrawEllipse(pen, x + size / 2, y, size / 3, size / 3);
                // Rain drops
                for (int i = 0; i < 3; i++)
                {
                    g.DrawLine(pen, x + size / 4 + i * size / 4, y + size / 2, x + size / 4 + i * size / 4, y + size - 2);
                }
                break;

            case >= 71 and <= 86: // Snow - Cloud with snowflakes
                // Cloud
                g.DrawEllipse(pen, x + size / 6, y, size / 3, size / 3);
                g.DrawEllipse(pen, x + size / 3, y - 2, size / 2, size / 2);
                g.DrawEllipse(pen, x + size / 2, y, size / 3, size / 3);
                // Snowflakes (asterisks)
                for (int i = 0; i < 3; i++)
                {
                    var sx = x + size / 4 + i * size / 4;
                    var sy = y + size * 2 / 3;
                    g.DrawLine(pen, sx - 2, sy, sx + 2, sy);
                    g.DrawLine(pen, sx, sy - 2, sx, sy + 2);
                }
                break;

            case >= 95: // Thunderstorm - Cloud with lightning
                // Cloud
                g.DrawEllipse(pen, x + size / 6, y, size / 3, size / 3);
                g.DrawEllipse(pen, x + size / 3, y - 2, size / 2, size / 2);
                g.DrawEllipse(pen, x + size / 2, y, size / 3, size / 3);
                // Lightning bolt
                var points = new Point[]
                {
                    new Point(x + size / 2, y + size / 2),
                    new Point(x + size / 3, y + size * 2 / 3),
                    new Point(x + size / 2 + 2, y + size * 2 / 3),
                    new Point(x + size / 3, y + size - 2)
                };
                g.DrawLines(pen, points);
                break;

            default: // Default - Simple cloud
                g.DrawEllipse(pen, x, y + size / 3, size / 3, size / 3);
                g.DrawEllipse(pen, x + size / 4, y, size / 2, size / 2);
                g.DrawEllipse(pen, x + size / 2, y + size / 3, size / 3, size / 3);
                break;
        }
    }

    private void DrawText(System.Drawing.Graphics g, string text, int x, int y, Font? font = null)
    {
        font ??= new Font("Arial", 8);
        g.DrawString(text, font, Brushes.White, x, y);
    }

    private void DrawCenteredText(System.Drawing.Graphics g, string text, int y)
    {
        var font = new Font("Arial", 8);
        var size = g.MeasureString(text, font);
        DrawText(g, text, (int)((128 - size.Width) / 2), y, font);
    }

    private class WeatherData
    {
        public CurrentWeather? current { get; set; }
    }

    private class CurrentWeather
    {
        public double temperature_2m { get; set; }
        public int weathercode { get; set; }
        public double windspeed_10m { get; set; }
    }

    private class ForecastData
    {
        public DailyForecast? daily { get; set; }
    }

    private class DailyForecast
    {
        public string[] time { get; set; } = Array.Empty<string>();
        public double[] temperature_2m_max { get; set; } = Array.Empty<double>();
        public double[] temperature_2m_min { get; set; } = Array.Empty<double>();
        public int[] weathercode { get; set; } = Array.Empty<int>();
    }
}
