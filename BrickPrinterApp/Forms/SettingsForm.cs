using BrickPrinterApp.Services;
using System.IO.Ports;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp;

/// <summary>
/// Settings dialog for configuring ESP32 device, IP address, and time offset.
/// Split into partial classes for better maintainability:
/// - SettingsForm.cs (this file): Main form setup and shared infrastructure
/// - SettingsForm.IpTab.cs: IP address configuration
/// - SettingsForm.Esp32Tab.cs: ESP32 serial configuration (WiFi, Display, Commands)
/// - SettingsForm.TimeTab.cs: Time offset configuration
/// </summary>
public partial class SettingsForm : MaterialForm
{
    // ============================================
    // Shared Fields
    // ============================================

    private readonly SettingService _settings;
    private readonly MaterialSkinManager _materialSkinManager;
    private SerialPort? _serialPort;

    // ============================================
    // Constructor
    // ============================================

    public SettingsForm(SettingService settings)
    {
        _settings = settings;

        // Initialize Material Skin theme
        _materialSkinManager = MaterialSkinManager.Instance;
        _materialSkinManager.AddFormToManage(this);
        _materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
        _materialSkinManager.ColorScheme = new ColorScheme(
            Primary.BlueGrey800,
            Primary.BlueGrey900,
            Primary.BlueGrey500,
            Accent.LightBlue200,
            TextShade.WHITE
        );

        // Initialize UI
        InitializeComponentManual();

        // Load saved settings into UI
        txtIpAddress.Text = _settings.EspIpAddress;
        numTimeOffset.Value = _settings.TimeOffsetHours;
        numDisplayCount.Value = _settings.NumScreens;
        LoadBaudRates();
        LoadComPorts();
    }

    // ============================================
    // Main Form Initialization
    // ============================================

    private void InitializeComponentManual()
    {
        Text = "Einstellungen";
        Size = new Size(750, 950);
        MinimumSize = new Size(750, 950);
        StartPosition = FormStartPosition.CenterParent;
        Sizable = true;

        // Create tab selector (the clickable tab headers)
        var tabSelector = new MaterialTabSelector
        {
            BaseTabControl = null!, // Will be set below
            Depth = 0,
            Location = new Point(0, 64),
            Size = new Size(750, 48),
            Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
        };

        // Create tab control
        var tabControl = new MaterialTabControl
        {
            Location = new Point(10, 115),
            Size = new Size(720, 950),
            Depth = 0,
            MouseState = MaterialSkin.MouseState.HOVER,
            Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
        };

        // Create and initialize the three tabs
        // Each tab is initialized in its respective partial class file

        // Tab 1: IP Settings (SettingsForm.IpTab.cs)
        var tabIp = new TabPage("IP Einstellungen");
        InitializeIpTab(tabIp);

        // Tab 2: ESP32 Configuration (SettingsForm.Esp32Tab.cs)
        var tabSerial = new TabPage("ESP32 Konfiguration");
        InitializeSerialTab(tabSerial);

        // Tab 3: Time Settings (SettingsForm.TimeTab.cs)
        var tabTime = new TabPage("Uhrzeit");
        InitializeTimeTab(tabTime);

        // Add tabs to control
        tabControl.TabPages.Add(tabIp);
        tabControl.TabPages.Add(tabSerial);
        tabControl.TabPages.Add(tabTime);

        // Link tab selector to tab control
        tabSelector.BaseTabControl = tabControl;

        // Add controls to form
        Controls.Add(tabControl);
        Controls.Add(tabSelector);
    }

    // ============================================
    // Form Lifecycle
    // ============================================

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        DisconnectSerial();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            DisconnectSerial();
            _serialPort?.Dispose();
            components?.Dispose();
        }
        base.Dispose(disposing);
    }

    private System.ComponentModel.IContainer? components = null;
}
