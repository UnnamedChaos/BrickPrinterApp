using System.Text.Json.Serialization;

namespace BrickPrinterApp.Models;

public class StockData
{
    public string Ticker { get; set; } = "";
    public double CurrentPrice { get; set; }
    public double PreviousClose { get; set; }
    public string Currency { get; set; } = "USD";
    public double[] IntradayPrices { get; set; } = Array.Empty<double>();
}

public class YahooChartResponse
{
    public ChartData? chart { get; set; }
}

public class ChartData
{
    public ChartResult[]? result { get; set; }
}

public class ChartResult
{
    public ChartMeta? meta { get; set; }
    public ChartIndicators? indicators { get; set; }
}

public class ChartMeta
{
    public string? currency { get; set; }
    public string? symbol { get; set; }
    public double regularMarketPrice { get; set; }
    public double previousClose { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

public class ChartIndicators
{
    public QuoteData[]? quote { get; set; }
}

public class QuoteData
{
    public double?[]? close { get; set; }
    public double?[]? open { get; set; }
    public double?[]? high { get; set; }
    public double?[]? low { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
