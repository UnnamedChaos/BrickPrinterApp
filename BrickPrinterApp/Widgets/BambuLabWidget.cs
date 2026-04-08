using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Models;
using BrickPrinterApp.Services;
using Meadow.Foundation.Graphics;
using Meadow.Foundation.Graphics.Buffers;
using Meadow.Peripherals.Displays;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;
using Color = Meadow.Color;

namespace BrickPrinterApp.Widgets;

public class BambuLabWidget : IWidget
{
    private readonly SettingService _settingService;
    private readonly Buffer1bpp _buffer;
    private readonly MicroGraphics _graphics;
    private readonly IFont _smallFont;
    private readonly IFont _mediumFont;
    private readonly IFont _largeFont;
    private readonly object _lock = new();

    private IMqttClient? _mqttClient;
    private bool _isConnected = false;
    private DateTime _lastConnectionAttempt = DateTime.MinValue;
    private System.Threading.Timer? _connectionCheckTimer;

    // Print status data
    private string _printName = "";
    private int _progress = 0; // 0-100
    private int _remainingMinutes = 0;
    private string _printStatus = "Idle";

    private const int ScreenWidth = 128;
    private const int ScreenHeight = 64;

    public string Name => "BambuLab";
    public TimeSpan UpdateInterval => TimeSpan.FromSeconds(10);

    /// <summary>
    /// Returns true if a print is currently active (running or paused)
    /// </summary>
    public bool IsPrintActive
    {
        get
        {
            lock (_lock)
            {
                // Print is active if:
                // 1. Connected to printer
                // 2. Has a print name
                // 3. Status is not Idle, Finished, or Failed
                return _isConnected &&
                       !string.IsNullOrEmpty(_printName) &&
                       _printStatus != "Idle" &&
                       _printStatus != "Fertig" &&
                       _printStatus != "Fehler";
            }
        }
    }

    public BambuLabWidget(SettingService settingService)
    {
        _settingService = settingService;
        _buffer = new Buffer1bpp(ScreenWidth, ScreenHeight);
        var displaySimulator = new DisplayBufferSimulator(_buffer);
        _smallFont = new Font4x8();
        _mediumFont = new Font8x12();
        _largeFont = new Font12x20();
        _graphics = new MicroGraphics(displaySimulator)
        {
            CurrentFont = _smallFont
        };
    }

    /// <summary>
    /// Initialize the widget and start MQTT connection
    /// Should be called after widget is registered
    /// </summary>
    public void Initialize()
    {
        Console.WriteLine("BambuLabWidget: Initialize() called");

        // Start connection attempt immediately
        _ = ConnectToMqttAsync();
        _lastConnectionAttempt = DateTime.Now;

        // Start periodic connection check timer (every 30 seconds)
        _connectionCheckTimer = new System.Threading.Timer(
            _ => CheckAndReconnect(),
            null,
            TimeSpan.FromSeconds(30),
            TimeSpan.FromSeconds(30)
        );
    }

    private void CheckAndReconnect()
    {
        lock (_lock)
        {
            if (!_isConnected && (DateTime.Now - _lastConnectionAttempt).TotalSeconds > 30)
            {
                _ = ConnectToMqttAsync();
                _lastConnectionAttempt = DateTime.Now;
            }
        }
    }

    public byte[] GetContent()
    {
        lock (_lock)
        {
            // Try to reconnect if not connected and enough time has passed
            if (!_isConnected && (DateTime.Now - _lastConnectionAttempt).TotalSeconds > 30)
            {
                _ = ConnectToMqttAsync();
                _lastConnectionAttempt = DateTime.Now;
            }

            _buffer.Clear();
            _graphics.Clear(true);
            _graphics.CurrentFont = _smallFont;

            if (!_isConnected)
            {
                DrawNotConnectedScreen();
            }
            else if (string.IsNullOrEmpty(_printName))
            {
                DrawIdleScreen();
            }
            else
            {
                DrawPrintingScreen();
            }

            _graphics.Show();

            var result = new byte[_buffer.Buffer.Length];
            Array.Copy(_buffer.Buffer, result, result.Length);
            return result;
        }
    }

    private void DrawNotConnectedScreen()
    {
        _graphics.CurrentFont = _mediumFont;
        _graphics.DrawText(2, 2, "BAMBULAB", Color.White);

        _graphics.CurrentFont = _smallFont;
        _graphics.DrawText(2, 20, "Nicht verbunden", Color.White);
        _graphics.DrawText(2, 32, "Pruefe Einst.", Color.White);
        _graphics.DrawText(2, 44, $"IP:{TruncateText(_settingService.BambuLabIp ?? "?", 24)}", Color.White);
    }

    private void DrawIdleScreen()
    {
        _graphics.CurrentFont = _mediumFont;
        _graphics.DrawText(2, 2, "BAMBULAB", Color.White);

        _graphics.CurrentFont = _smallFont;
        _graphics.DrawText(2, 24, "Bereit", Color.White);
        _graphics.DrawText(2, 36, "Kein Druck", Color.White);
    }

    private void DrawPrintingScreen()
    {
        // BambuLab logo in top left
        DrawBambuLabLogo(2, 2);

        // Big percentage number - use large font (12x20)
        var percentText = $"{_progress}%";
        _graphics.CurrentFont = _largeFont;

        // Calculate position to right-align the percentage in top right
        int charWidth = 12; // Font12x20 character width
        int textWidth = percentText.Length * charWidth;
        int x = ScreenWidth - textWidth - 4; // 4px padding from right
        int y = 4; // Top area

        _graphics.DrawText(x, y, percentText, Color.White);

        // Timer on the right side above the progress bar with medium font (8x12)
        var timeText = _remainingMinutes > 60
            ? $"{_remainingMinutes / 60}h{_remainingMinutes % 60}m"
            : $"{_remainingMinutes}m";

        _graphics.CurrentFont = _mediumFont;
        int timeWidth = timeText.Length * 8; // Font8x12 character width is 8
        int timeX = ScreenWidth - timeWidth - 4;
        _graphics.DrawText(timeX, 32, timeText, Color.White);

        // Progress bar at bottom
        DrawProgressBar(48, _progress);
    }

    private void DrawBambuLabLogo(int x, int y)
    {
        // Draw the BambuLab logo:
        // - Tall vertical rectangle
        // - Vertical line in the middle
        // - Two diagonal lines emerging from middle going outward, angled down ~20°
        // - Right line at 1/3 from top, left line at 2/3 from top

        const int logoHeight = 40;
        const int logoWidth = 30;
        int centerX = x + logoWidth / 2;

        // Draw tall vertical rectangle border
        _graphics.DrawRectangle(x, y, logoWidth, logoHeight, Color.White, true);

        // Draw vertical line in the middle
        _graphics.DrawLine(centerX, y, centerX, y + logoHeight, Color.Black);
        _graphics.DrawLine(centerX + 1, y, centerX + 1, y + logoHeight, Color.Black); // Make it thicker

        // Right diagonal line (1/3 from top, going right and down at ~20°)
        int rightLineY = y + (logoHeight / 3);
        int rightEndX = x + logoWidth;
        int rightEndY = rightLineY + 5; // ~20° angle downward
        _graphics.DrawLine(centerX, rightLineY, rightEndX, rightEndY, Color.Black);
        _graphics.DrawLine(centerX, rightLineY + 1, rightEndX, rightEndY + 1, Color.Black); // Thicker

        // Left diagonal line (2/3 from top, going left and down at ~20°)
        int leftLineY = y + (logoHeight / 3) + 7 ;
        int leftEndX = x;
        int leftEndY = leftLineY + 5; // ~20° angle downward
        _graphics.DrawLine(centerX, leftLineY, leftEndX, leftEndY, Color.Black);
        _graphics.DrawLine(centerX, leftLineY + 1, leftEndX, leftEndY + 1, Color.Black); // Thicker
    }

    private void DrawProgressBar(int y, int percent)
    {
        const int barWidth = 124; // Almost full width
        const int barHeight = 14; // Taller bar
        const int barX = 2;

        // Draw border
        _graphics.DrawRectangle(barX, y, barWidth, barHeight, Color.White, false);

        // Draw filled portion
        int fillWidth = (barWidth - 4) * percent / 100;
        if (fillWidth > 0)
        {
            _graphics.DrawRectangle(barX + 2, y + 2, fillWidth, barHeight - 4, Color.White, true);
        }
    }

    private string TruncateText(string text, int maxChars)
    {
        if (text.Length <= maxChars) return text;
        return text.Substring(0, maxChars - 2) + "..";
    }

    private async Task ConnectToMqttAsync()
    {
        try
        {
            // Validate settings
            if (string.IsNullOrEmpty(_settingService.BambuLabIp) ||
                string.IsNullOrEmpty(_settingService.BambuLabAccessCode) ||
                string.IsNullOrEmpty(_settingService.BambuLabSerial))
            {
                Console.WriteLine("BambuLab: Missing connection settings");
                return;
            }

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_settingService.BambuLabIp, 8883)
                .WithCredentials("bblp", _settingService.BambuLabAccessCode)
                .WithTlsOptions(tls => tls
                    .UseTls()
                    .WithAllowUntrustedCertificates()
                    .WithIgnoreCertificateChainErrors()
                    .WithIgnoreCertificateRevocationErrors()
                    .WithCertificateValidationHandler(_ => true) // Accept all certificates (printer uses self-signed cert)
                )
                .WithCleanSession()
                .Build();

            _mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

            await _mqttClient.ConnectAsync(options, CancellationToken.None);

            // Subscribe to printer status topic
            var topic = $"device/{_settingService.BambuLabSerial}/report";
            await _mqttClient.SubscribeAsync(topic);

            lock (_lock)
            {
                _isConnected = true;
            }
            Console.WriteLine($"BambuLab: *** CONNECTED *** to {_settingService.BambuLabIp} (topic: {topic})");
            Console.WriteLine($"BambuLab: Waiting for printer status messages...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BambuLab: Connection failed - {ex.Message}");
            _isConnected = false;
            _mqttClient?.Dispose();
            _mqttClient = null;
        }
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            var json = JObject.Parse(payload);

            lock (_lock)
            {
                // Extract print info from Bambulab MQTT message
                var print = json["print"];
                if (print != null)
                {
                    var oldPrintName = _printName;
                    var oldStatus = _printStatus;

                    _printName = print["subtask_name"]?.ToString() ?? "";
                    _progress = print["mc_percent"]?.ToObject<int>() ?? 0;
                    _remainingMinutes = print["mc_remaining_time"]?.ToObject<int>() ?? 0;

                    var gcodeState = print["gcode_state"]?.ToString() ?? "";
                    _printStatus = gcodeState switch
                    {
                        "RUNNING" => "Druckt",
                        "PAUSE" => "Pausiert",
                        "FINISH" => "Fertig",
                        "FAILED" => "Fehler",
                        _ => "Idle"
                    };

                    // Log if status changed
                    if (oldPrintName != _printName || oldStatus != _printStatus)
                    {
                        Console.WriteLine($"BambuLab: Status changed - Name: '{_printName}', Status: '{_printStatus}', Progress: {_progress}%, Remaining: {_remainingMinutes}min");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"BambuLab: Message parse error - {ex.Message}");
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _connectionCheckTimer?.Dispose();
        _connectionCheckTimer = null;

        _mqttClient?.DisconnectAsync().Wait(1000);
        _mqttClient?.Dispose();
    }
}
