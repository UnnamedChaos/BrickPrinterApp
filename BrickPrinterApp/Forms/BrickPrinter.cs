using BrickPrinterApp.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Timer = System.Windows.Forms.Timer;

namespace BrickPrinterApp.Forms;

public partial class BrickPrinter : Form
{
    private readonly IDisplayService _displayService;
    private readonly IHost _host;
    private readonly Image _image;
    private readonly ITransferService _transferService;
    private readonly ITextService _textService;
    private Timer? _minuteTimer;
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _trayMenu;

    public BrickPrinter(IDisplayService displayService, ITransferService transferService, ITextService textService, IHost host)
    {
        InitializeComponent();
        _image = Image.FromFile("Resources/img.PNG");
        RegisterTrayIcon();
        RegisterTimer();

        // Fenster beim Start sofort verstecken
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;

        _displayService = displayService;
        _transferService = transferService;
        _textService = textService;
        _host = host;

        // Start keep-alive to maintain connection (ping every 15 seconds)
        _transferService.StartKeepAlive(TimeSpan.FromSeconds(15));
    }

    private void RegisterTrayIcon()
    {
        // 1. Das Tray-Menü erstellen (Rechtsklick-Optionen)
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Jetzt Updaten", null, SendSampleText());
        _trayMenu.Items.Add("Text Senden", null, SendSampleText());
        _trayMenu.Items.Add("-"); // Trennlinie
        _trayMenu.Items.Add("Einstellungen", null, OpenSettings());
        _trayMenu.Items.Add("Beenden", null, (_, _) => Application.Exit());

        // 2. Das Tray-Icon selbst erstellen
        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "BrickPrinter";

        // WICHTIG: Du brauchst eine .ico Datei im Ordner oder nutzt ein System-Icon:
        _trayIcon.Icon = new Icon("Resources/brick.ico");

        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = true;
    }

    private EventHandler? OpenSettings()
    {
        return (s, e) =>
        {
            // Wir holen uns eine frische Instanz des SettingsForm
            var settingsForm = _host.Services.GetRequiredService<SettingsForm>();
            settingsForm.ShowDialog(); // Öffnet es als Modal-Fenster
        };
    }

    private EventHandler? SendSampleText()
    {
        return async (_, _) =>
        {
            var sampleLines = new[]
            {
                "BrickPrinter",
                "============",
                "",
                "Status: OK",
                $"Zeit: {DateTime.Now:HH:mm:ss}",
                $"Datum: {DateTime.Now:dd.MM.yyyy}",
                "============",
                "============",
            };

            var binaryData = _textService.ConvertTextToBinary(sampleLines);
            await _transferService.SendBinaryDataAsync(binaryData);
        };
    }

    private void RegisterTimer()
    {
        // 3. Den Timer für jede Minute starten
        _minuteTimer = new Timer();
        _minuteTimer.Interval = 60000; // 60.000 ms = 1 Minute
        _minuteTimer.Tick += async (s, e) =>
        {
            var binaryData = _displayService.ConvertImageToBinary(_image);
            //await _transferService.SendBinaryDataAsync(binaryData);
        };
        _minuteTimer.Start();
    }


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _transferService.StopKeepAlive();
        base.OnFormClosing(e);
    }
}