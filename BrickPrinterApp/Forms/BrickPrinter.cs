using BrickPrinterApp.Interfaces;
using BrickPrinterApp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BrickPrinterApp.Forms;

public partial class BrickPrinter : Form
{
    private readonly IHost _host;
    private readonly ITransferService _transferService;
    private readonly ITextService _textService;
    private readonly SettingService _settings;
    private readonly WidgetService _widgetService;
    private readonly RecoveryListenerService _recoveryListener;
    private NotifyIcon? _trayIcon;
    private ContextMenuStrip? _trayMenu;

    public BrickPrinter(ITransferService transferService, ITextService textService, SettingService settings, WidgetService widgetService, RecoveryListenerService recoveryListener, IHost host)
    {
        _transferService = transferService;
        _textService = textService;
        _settings = settings;
        _widgetService = widgetService;
        _recoveryListener = recoveryListener;
        _host = host;

        InitializeComponent();
        RegisterTrayIcon();

        // Fenster beim Start sofort verstecken
        WindowState = FormWindowState.Minimized;
        ShowInTaskbar = false;

        // Start keep-alive to maintain connection (ping every 15 seconds)
        _transferService.StartKeepAlive(TimeSpan.FromSeconds(10));

        // Start recovery listener for ESP32 widget re-initialization requests
        _recoveryListener.Start();
    }

    private void RegisterTrayIcon()
    {
        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Widget Manager", null, OpenWidgetManager());
        _trayMenu.Items.Add("Test Senden", null, SendSampleText());
        _trayMenu.Items.Add("-");
        _trayMenu.Items.Add(CreateScreenSelectionMenu());
        _trayMenu.Items.Add("Einstellungen", null, OpenSettings());
        _trayMenu.Items.Add("WiFi Setup", null, OpenWiFiSetup());
        _trayMenu.Items.Add("Beenden", null, (_, _) => Application.Exit());

        _trayIcon = new NotifyIcon();
        _trayIcon.Text = "BrickPrinter";
        _trayIcon.Icon = new Icon("Resources/brick.ico");
        _trayIcon.ContextMenuStrip = _trayMenu;
        _trayIcon.Visible = true;
    }

    private ToolStripMenuItem CreateScreenSelectionMenu()
    {
        var screenMenu = new ToolStripMenuItem("Bildschirm");

        for (int i = 0; i < SettingService.NumScreens; i++)
        {
            var screenIndex = i;
            var item = new ToolStripMenuItem($"Screen {i}")
            {
                Checked = _settings.SelectedScreen == i
            };
            item.Click += (_, _) =>
            {
                foreach (ToolStripMenuItem menuItem in screenMenu.DropDownItems)
                {
                    menuItem.Checked = false;
                }
                item.Checked = true;
                _settings.SelectedScreen = screenIndex;
                _settings.Save();
            };
            screenMenu.DropDownItems.Add(item);
        }

        return screenMenu;
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

    private EventHandler OpenWiFiSetup()
    {
        return (_, _) =>
        {
            var wifiForm = _host.Services.GetRequiredService<WiFiSetupForm>();
            wifiForm.ShowDialog();
        };
    }

    private EventHandler SendSampleText()
    {
        return async (_, _) =>
        {
            var sampleLines = new[]
            {
                "BrickPrinter",
                "Status: OK",
                $"Zeit: {DateTime.Now:HH:mm:ss}",
                $"Screen: {_settings.SelectedScreen}",
            };

            var binaryData = _textService.ConvertTextToBinary(sampleLines);
            await _transferService.SendBinaryDataAsync(binaryData, _settings.SelectedScreen);
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
        _recoveryListener.Stop();
        _widgetService.Dispose();
        base.OnFormClosing(e);
    }
}
