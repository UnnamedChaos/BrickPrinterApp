using System.Text.Json;
using System.Text.Json.Serialization;
using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using BrickPrinterApp.Services;

namespace BrickPrinterApp.Widgets;

public class StockWidget : IWidget
{
    private readonly IDisplayService _displayService;
    private readonly HttpClient _httpClient;
    private int _currentStockIndex = 0;
    private StockData? _currentStockData;
    private DateTime _lastFetch = DateTime.MinValue;

    // Stock definitions with ISIN and Yahoo ticker symbols
    private readonly StockDefinition[] _stocks =
    {
        new("AMD", "AMD", "US0079031078"),
        new("NVIDIA", "NVDA", "US67066G1040"),
        new("TESLA", "TSLA", "US88160R1014"),
        new("RIO TINTO", "RIO", "GB0007188757"),
        new("ISHS SDIV", "ISPA.DE", "DE000A0F5UH1"),  // iShares STOXX Global Select Dividend 100
        new("VW", "VOW3.DE", "DE0007664039"),
        new("BMW", "BMW.DE", "DE0005190003"),
        new("DHL", "DHL.DE", "DE0005552004")  // Deutsche Post (now DHL Group)
    };

    public string Name => "Stocks";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(30);

    public StockWidget(IDisplayService displayService)
    {
        _displayService = displayService;
        _httpClient = new HttpClient();
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
    }

    public byte[] GetContent()
    {
        var stock = _stocks[_currentStockIndex];

        // Fetch data for current stock
        try
        {
            FetchStockData(stock.Ticker);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Stock fetch failed for {stock.Ticker}: {ex.Message}");
        }

        var image = DrawStockScreen(stock);

        // Move to next stock for next update
        _currentStockIndex = (_currentStockIndex + 1) % _stocks.Length;

        return _displayService.ConvertImageToBinary(image);
    }

    private void FetchStockData(string ticker)
    {
        // Use Yahoo Finance chart API for intraday data
        // Range: 1d gives today's data, interval: 5m for 5-minute intervals
        var url = $"https://query1.finance.yahoo.com/v8/finance/chart/{ticker}?range=1d&interval=5m";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        using var response = _httpClient.Send(request);

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Yahoo Finance API returned {response.StatusCode} for {ticker}");
            return;
        }

        var json = response.Content.ReadAsStringAsync().Result;
        var chartResponse = JsonSerializer.Deserialize<YahooChartResponse>(json);

        if (chartResponse?.chart?.result?.Length > 0)
        {
            var result = chartResponse.chart.result[0];
            var meta = result.meta;
            var quotes = result.indicators?.quote?.FirstOrDefault();

            if (quotes?.close != null && meta != null)
            {
                var closePrices = quotes.close.Where(p => p.HasValue).Select(p => p!.Value).ToArray();

                _currentStockData = new StockData
                {
                    Ticker = ticker,
                    CurrentPrice = meta.regularMarketPrice,
                    PreviousClose = meta.previousClose,
                    Currency = meta.currency ?? "USD",
                    IntradayPrices = closePrices
                };
            }
        }
    }

    private Bitmap DrawStockScreen(StockDefinition stock)
    {
        var bitmap = new Bitmap(SettingService.ScreenWidth, SettingService.ScreenHeight);
        using var g = System.Drawing.Graphics.FromImage(bitmap);
        g.Clear(Color.Black);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        var pen = new Pen(Color.White, 1);
        var brush = new SolidBrush(Color.White);

        // Draw ticker name (top left)
        var tickerFont = new Font("Arial", 10, FontStyle.Bold);
        g.DrawString(stock.DisplayName, tickerFont, brush, 2, 2);

        if (_currentStockData == null || _currentStockData.IntradayPrices.Length == 0)
        {
            // Loading state
            var loadingFont = new Font("Arial", 8);
            g.DrawString("Loading...", loadingFont, brush, 40, 30);
            return bitmap;
        }

        // Draw current price
        var priceFont = new Font("Arial", 12, FontStyle.Bold);
        var priceText = FormatPrice(_currentStockData.CurrentPrice, _currentStockData.Currency);
        g.DrawString(priceText, priceFont, brush, 2, 16);

        // Calculate and draw change
        var change = _currentStockData.CurrentPrice - _currentStockData.PreviousClose;
        var changePercent = (_currentStockData.PreviousClose > 0)
            ? (change / _currentStockData.PreviousClose) * 100
            : 0;

        var changeFont = new Font("Arial", 8);
        var arrow = change >= 0 ? "▲" : "▼";
        var changeText = $"{arrow} {Math.Abs(changePercent):F2}%";
        g.DrawString(changeText, changeFont, brush, 2, 32);

        // Draw bar chart (bottom portion)
        DrawBarChart(g, _currentStockData.IntradayPrices, 2, 44, 124, 18);

        return bitmap;
    }

    private void DrawBarChart(System.Drawing.Graphics g, double[] prices, int x, int y, int width, int height)
    {
        if (prices.Length == 0) return;

        var pen = new Pen(Color.White, 1);
        var brush = new SolidBrush(Color.White);

        // Find min/max for scaling
        var minPrice = prices.Min();
        var maxPrice = prices.Max();
        var range = maxPrice - minPrice;

        // If no range (flat), add some padding
        if (range < 0.001)
        {
            range = minPrice * 0.01;
            minPrice -= range / 2;
            maxPrice += range / 2;
            range = maxPrice - minPrice;
        }

        // Draw baseline
        g.DrawLine(pen, x, y + height, x + width, y + height);

        // Sample prices to fit the width (max ~60 bars)
        var barCount = Math.Min(prices.Length, 60);
        var barWidth = Math.Max(1, width / barCount);
        var step = prices.Length / (double)barCount;

        for (int i = 0; i < barCount; i++)
        {
            var priceIndex = (int)(i * step);
            if (priceIndex >= prices.Length) priceIndex = prices.Length - 1;

            var price = prices[priceIndex];
            var normalizedHeight = (price - minPrice) / range;
            var barHeight = (int)(normalizedHeight * (height - 2)) + 1;

            var barX = x + (i * barWidth);
            var barY = y + height - barHeight;

            // Draw bar as a vertical line
            g.DrawLine(pen, barX, barY, barX, y + height - 1);
        }
    }

    private string FormatPrice(double price, string currency)
    {
        var symbol = currency switch
        {
            "EUR" => "€",
            "GBP" => "£",
            "USD" => "$",
            _ => currency
        };

        return $"{symbol}{price:F2}";
    }

    private record StockDefinition(string DisplayName, string Ticker, string Isin);
}
