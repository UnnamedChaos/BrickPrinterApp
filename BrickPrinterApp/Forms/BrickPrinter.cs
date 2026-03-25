using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BrickPrinterApp.Forms;

public partial class BrickPrinter : Form
{
    private readonly IHost _host;
    private readonly ITransferService _transferService;
    private readonly WidgetService _widgetService;
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _trayMenu;

    public BrickPrinter(ITransferService transferService, WidgetService widgetService, IHost host)
    {
        _transferService = transferService;
        _widgetService = widgetService;
        _host = host;

        InitializeComponent();
        RegisterTrayIcon();

        // Fenster beim Start sofort verstecken
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;

        // Start keep-alive with recovery callback
        _transferService.StartKeepAlive(TimeSpan.FromSeconds(10), _widgetService.RecoverScreensAsync);
    }

    private void RegisterTrayIcon()
    {
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Widget Manager", null, OpenWidgetManager());
        _trayMenu.Items.Add("-");
        _trayMenu.Items.Add("Einstellungen", null, OpenSettings());
        _trayMenu.Items.Add("Beenden", null, (_, _) => Application.Exit());

        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "BrickPrinter";
        _trayIcon.Icon = new Icon("Resources/brick.ico");
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = true;
    }

    private EventHandler OpenWidgetManager()
    {
        return (_, _) =>
        {
            var form = _host.Services.GetRequiredService<WidgetManagerForm>();
            form.ShowDialog();
        };
    }

    private EventHandler OpenSettings()
    {
        return (_, _) =>
        {
            var settingsForm = _host.Services.GetRequiredService<SettingsForm>();
            settingsForm.ShowDialog();
        };
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _transferService.StopKeepAlive();
        _widgetService.Dispose();
        base.OnFormClosing(e);
    }
}
