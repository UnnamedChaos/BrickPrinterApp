using System.Text.Json.Serialization;

namespace BrickPrinterApp.Models;

public class WeatherData
{
    public CurrentWeather? current { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

public class CurrentWeather
{
    public double temperature_2m { get; set; }
    public int weathercode { get; set; }
    public double windspeed_10m { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

public class ForecastData
{
    public DailyForecast? daily { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

public class DailyForecast
{
    public string[] time { get; set; } = Array.Empty<string>();
    public double[] temperature_2m_max { get; set; } = Array.Empty<double>();
    public double[] temperature_2m_min { get; set; } = Array.Empty<double>();
    public int[] weathercode { get; set; } = Array.Empty<int>();

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
