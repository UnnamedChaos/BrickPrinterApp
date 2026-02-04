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
    private Timer? _minuteTimer;
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _trayMenu;

    public BrickPrinter(IDisplayService displayService, IHost host)
    {
        InitializeComponent();
        _image = Image.FromFile("Resources/img.PNG");
        RegisterTrayIcon();
        RegisterTimer();


        // Fenster beim Start sofort verstecken
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;

        _displayService = displayService;
        _host = host;
    }

    private void RegisterTrayIcon()
    {
        // 1. Das Tray-Menü erstellen (Rechtsklick-Optionen)
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Jetzt Updaten", null, async (_, _) => await _displayService.SendImageAsync(_image));
        _trayMenu.Items.Add("-"); // Trennlinie
        _trayMenu.Items.Add("Beenden", null, (_, _) => Application.Exit());

        // 2. Das Tray-Icon selbst erstellen
        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "BrickPrinter";

        // WICHTIG: Du brauchst eine .ico Datei im Ordner oder nutzt ein System-Icon:
        _trayIcon.Icon = new Icon("Resources/brick.ico");

        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = true;

        // Im Konstruktor von BrickPrinter oder wo du das Menü aufbaust:
        _trayMenu.Items.Add("Einstellungen", null, (s, e) =>
        {
            // Wir holen uns eine frische Instanz des SettingsForm
            var settingsForm = _host.Services.GetRequiredService<SettingsForm>();
            settingsForm.ShowDialog(); // Öffnet es als Modal-Fenster
        });
    }

    private void RegisterTimer()
    {
        // 3. Den Timer für jede Minute starten
        _minuteTimer = new Timer();
        _minuteTimer.Interval = 60000; // 60.000 ms = 1 Minute
        _minuteTimer.Tick += async (s, e) => await _displayService.SendImageAsync(_image);
        _minuteTimer.Start();
    }


    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
    }
}