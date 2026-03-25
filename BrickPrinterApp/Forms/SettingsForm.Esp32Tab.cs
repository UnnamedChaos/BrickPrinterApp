using BrickPrinterApp.Services;
using System.IO.Ports;
using System.Text;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp;

public partial class SettingsForm
{
    // ============================================
    // ESP32 Configuration - Tab Fields
    // ============================================

    // Serial Settings
    private MaterialComboBox cmbComPort = null!;
    private MaterialComboBox cmbBaudRate = null!;
    private MaterialButton btnRefreshPorts = null!;
    private MaterialButton btnConnect = null!;
    private MaterialButton btnDisconnect = null!;
    private MaterialLabel lblConnectionStatus = null!;

    // WiFi Configuration
    private MaterialTextBox txtSsid = null!;
    private MaterialTextBox txtPassword = null!;
    private MaterialButton btnSendWifi = null!;

    // Display Configuration
    private NumericUpDown numDisplayCount = null!;
    private MaterialTextBox txtDisplayPins = null!;
    private MaterialButton btnSendDisplay = null!;
    private MaterialButton btnPreset1 = null!;
    private MaterialButton btnPreset3 = null!;

    // Commands
    private MaterialButton btnStatus = null!;
    private MaterialButton btnClear = null!;
    private MaterialButton btnReboot = null!;
    private MaterialButton btnHelp = null!;
    private MaterialButton btnTestConnection = null!;

    // Serial Monitor
    private MaterialMultiLineTextBox txtSerialOutput = null!;
    private MaterialButton btnClearOutput = null!;
    private MaterialCheckbox chkAutoScroll = null!;

    // ============================================
    // ESP32 Configuration - Tab Initialization
    // ============================================

    private void InitializeSerialTab(TabPage tab)
    {
        // Create scrollable panel for the tab content
        var scrollPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(10, 10, 10, 20)
        };

        // Info label
        var lblInfo = new MaterialLabel
        {
            Text = "Verbinden Sie das ESP32 per USB und konfigurieren Sie WiFi, Displays und mehr.",
            Dock = DockStyle.Top,
            Height = 35,
            FontType = MaterialSkinManager.fontType.Body1,
            Margin = new Padding(0, 0, 0, 10)
        };

        // Create all sections
        var grpConnection = CreateSerialConnectionSection();
        var configPanel = CreateWifiAndDisplaySection();
        var grpCommands = CreateCommandsSection();
        var grpMonitor = CreateSerialMonitorSection();

        // Add all sections to scroll panel in reverse order (Dock.Top stacks bottom to top)
        scrollPanel.Controls.Add(grpMonitor);
        scrollPanel.Controls.Add(grpCommands);
        scrollPanel.Controls.Add(configPanel);
        scrollPanel.Controls.Add(grpConnection);
        scrollPanel.Controls.Add(lblInfo);

        tab.Controls.Add(scrollPanel);
    }

    // ============================================
    // ESP32 Configuration - Section Builders
    // ============================================

    private GroupBox CreateSerialConnectionSection()
    {
        var grpConnection = new GroupBox
        {
            Text = "Serielle Verbindung",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 15)
        };

        var connectionTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 3,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(5)
        };

        // Configure column styles
        connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Label
        connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // ComboBox
        connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Refresh button
        connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Label
        connectionTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150)); // Baud ComboBox
        connectionTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Port selection row
        connectionTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Connect buttons row
        connectionTable.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Warning row

        // COM Port controls
        var lblComPort = new MaterialLabel
        {
            Text = "COM Port:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 10, 5)
        };

        cmbComPort = new MaterialComboBox
        {
            Width = 140,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 10, 5)
        };

        btnRefreshPorts = new MaterialButton
        {
            Text = "🔄",
            Width = 45,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Margin = new Padding(0, 0, 15, 5)
        };
        btnRefreshPorts.Click += (s, e) =>
        {
            AppendSerialOutput("Aktualisiere COM Port Liste...\r\n");
            LoadComPorts();
        };

        // Baud Rate controls
        var lblBaud = new MaterialLabel
        {
            Text = "Baud Rate:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 8, 10, 5)
        };

        cmbBaudRate = new MaterialComboBox
        {
            Width = 140,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 10, 5)
        };

        // Connection buttons panel
        var buttonPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 10, 0, 5)
        };

        btnConnect = new MaterialButton
        {
            Text = "Verbinden",
            Width = 120,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Contained,
            Margin = new Padding(0, 0, 10, 0)
        };
        btnConnect.Click += BtnConnect_Click;

        btnDisconnect = new MaterialButton
        {
            Text = "Trennen",
            Width = 120,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Margin = new Padding(0, 0, 15, 0)
        };
        btnDisconnect.Click += BtnDisconnect_Click;

        lblConnectionStatus = new MaterialLabel
        {
            Text = "Nicht verbunden",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            ForeColor = Color.FromArgb(244, 67, 54),
            Margin = new Padding(0, 8, 0, 0)
        };

        buttonPanel.Controls.Add(btnConnect);
        buttonPanel.Controls.Add(btnDisconnect);
        buttonPanel.Controls.Add(lblConnectionStatus);

        // Warning label
        var lblWarning = new MaterialLabel
        {
            Text = "⚠ Schließen Sie den Arduino IDE Serial Monitor vor dem Verbinden!",
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.Caption,
            Margin = new Padding(0, 5, 0, 0)
        };

        // Add controls to connection table
        connectionTable.Controls.Add(lblComPort, 0, 0);
        connectionTable.Controls.Add(cmbComPort, 1, 0);
        connectionTable.Controls.Add(btnRefreshPorts, 2, 0);
        connectionTable.Controls.Add(lblBaud, 3, 0);
        connectionTable.Controls.Add(cmbBaudRate, 4, 0);

        connectionTable.SetColumnSpan(buttonPanel, 5);
        connectionTable.Controls.Add(buttonPanel, 0, 1);

        connectionTable.SetColumnSpan(lblWarning, 5);
        connectionTable.Controls.Add(lblWarning, 0, 2);

        grpConnection.Controls.Add(connectionTable);
        return grpConnection;
    }

    private TableLayoutPanel CreateWifiAndDisplaySection()
    {
        var configPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 15)
        };

        configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        configPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        configPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var grpWifi = CreateWifiConfigurationGroup();
        var grpDisplay = CreateDisplayConfigurationGroup();

        configPanel.Controls.Add(grpWifi, 0, 0);
        configPanel.Controls.Add(grpDisplay, 1, 0);

        return configPanel;
    }

    private GroupBox CreateWifiConfigurationGroup()
    {
        var grpWifi = new GroupBox
        {
            Text = "WiFi Konfiguration",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 5, 0)
        };

        var wifiTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(5)
        };

        wifiTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        wifiTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wifiTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wifiTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wifiTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        wifiTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblSsid = new MaterialLabel
        {
            Text = "SSID:",
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 5)
        };

        txtSsid = new MaterialTextBox
        {
            Dock = DockStyle.Fill,
            Hint = "Ihr WiFi Name",
            Margin = new Padding(0, 0, 0, 15)
        };

        var lblPassword = new MaterialLabel
        {
            Text = "Passwort:",
            AutoSize = true,
            Margin = new Padding(0, 5, 0, 5)
        };

        txtPassword = new MaterialTextBox
        {
            Dock = DockStyle.Fill,
            Password = true,
            Hint = "WiFi Passwort",
            Margin = new Padding(0, 0, 0, 15)
        };

        btnSendWifi = new MaterialButton
        {
            Text = "WIFI Senden",
            Width = 140,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Contained,
            Margin = new Padding(0, 5, 0, 0)
        };
        btnSendWifi.Click += BtnSendWifi_Click;

        wifiTable.Controls.Add(lblSsid, 0, 0);
        wifiTable.Controls.Add(txtSsid, 0, 1);
        wifiTable.Controls.Add(lblPassword, 0, 2);
        wifiTable.Controls.Add(txtPassword, 0, 3);
        wifiTable.Controls.Add(btnSendWifi, 0, 4);

        grpWifi.Controls.Add(wifiTable);
        return grpWifi;
    }

    private GroupBox CreateDisplayConfigurationGroup()
    {
        var grpDisplay = new GroupBox
        {
            Text = "Display Konfiguration",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(5, 0, 0, 0)
        };

        var displayTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(5)
        };

        displayTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        displayTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        displayTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        displayTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        displayTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        displayTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblDisplayCount = new MaterialLabel
        {
            Text = "Anzahl:",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 10, 10, 5)
        };

        numDisplayCount = new NumericUpDown
        {
            Width = 80,
            Height = 30,
            Minimum = 1,
            Maximum = 3,
            Value = 1,
            Font = new Font("Segoe UI", 11f),
            BorderStyle = BorderStyle.FixedSingle,
            Margin = new Padding(0, 5, 0, 5)
        };

        // Preset buttons
        var presetPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Margin = new Padding(0, 5, 0, 15)
        };

        btnPreset1 = new MaterialButton
        {
            Text = "1x (6:7)",
            Width = 85,
            Height = 30,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Text,
            Margin = new Padding(0, 0, 5, 0)
        };
        btnPreset1.Click += (s, e) => { numDisplayCount.Value = 1; txtDisplayPins.Text = "6:7"; };

        btnPreset3 = new MaterialButton
        {
            Text = "3x Default",
            Width = 95,
            Height = 30,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Text,
            Margin = new Padding(5, 0, 0, 0)
        };
        btnPreset3.Click += (s, e) => { numDisplayCount.Value = 3; txtDisplayPins.Text = "10:21:4:5:8:9"; };

        presetPanel.Controls.Add(btnPreset1);
        presetPanel.Controls.Add(btnPreset3);

        var lblPins = new MaterialLabel
        {
            Text = "Pins:",
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 5)
        };

        txtDisplayPins = new MaterialTextBox
        {
            Hint = "6:7",
            Margin = new Padding(0, 5, 0, 15)
        };

        btnSendDisplay = new MaterialButton
        {
            Text = "DISPLAY Senden",
            Width = 160,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Contained,
            Margin = new Padding(0, 5, 0, 0)
        };
        btnSendDisplay.Click += BtnSendDisplay_Click;

        displayTable.Controls.Add(lblDisplayCount, 0, 0);
        displayTable.Controls.Add(numDisplayCount, 1, 0);
        displayTable.SetColumnSpan(presetPanel, 2);
        displayTable.Controls.Add(presetPanel, 0, 1);
        displayTable.SetColumnSpan(lblPins, 2);
        displayTable.Controls.Add(lblPins, 0, 2);
        displayTable.SetColumnSpan(txtDisplayPins, 2);
        displayTable.Controls.Add(txtDisplayPins, 0, 3);
        displayTable.SetColumnSpan(btnSendDisplay, 2);
        displayTable.Controls.Add(btnSendDisplay, 0, 4);

        grpDisplay.Controls.Add(displayTable);
        return grpDisplay;
    }

    private GroupBox CreateCommandsSection()
    {
        var grpCommands = new GroupBox
        {
            Text = "Befehle",
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 15)
        };

        var commandsFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(5)
        };

        btnStatus = new MaterialButton
        {
            Text = "STATUS",
            Width = 90,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Margin = new Padding(0, 5, 10, 5)
        };
        btnStatus.Click += (s, e) => SendCommand("STATUS");

        btnClear = new MaterialButton
        {
            Text = "CLEAR",
            Width = 90,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Margin = new Padding(0, 5, 10, 5)
        };
        btnClear.Click += (s, e) => SendCommand("CLEAR");

        btnReboot = new MaterialButton
        {
            Text = "REBOOT",
            Width = 90,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Margin = new Padding(0, 5, 10, 5)
        };
        btnReboot.Click += (s, e) => SendCommand("REBOOT");

        btnHelp = new MaterialButton
        {
            Text = "HELP",
            Width = 90,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Margin = new Padding(0, 5, 10, 5)
        };
        btnHelp.Click += (s, e) => SendCommand("HELP");

        btnTestConnection = new MaterialButton
        {
            Text = "Test",
            Width = 90,
            Height = 36,
            Enabled = false,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Margin = new Padding(0, 5, 0, 5)
        };
        btnTestConnection.Click += BtnTestConnection_Click;

        commandsFlow.Controls.Add(btnStatus);
        commandsFlow.Controls.Add(btnClear);
        commandsFlow.Controls.Add(btnReboot);
        commandsFlow.Controls.Add(btnHelp);
        commandsFlow.Controls.Add(btnTestConnection);

        grpCommands.Controls.Add(commandsFlow);
        return grpCommands;
    }

    private GroupBox CreateSerialMonitorSection()
    {
        var grpMonitor = new GroupBox
        {
            Text = "Serielle Ausgabe",
            Dock = DockStyle.Top,
            Height = 200,
            Padding = new Padding(10),
            Margin = new Padding(0, 0, 0, 20)
        };

        var monitorTable = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = false,
            Padding = new Padding(5)
        };

        monitorTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        monitorTable.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        monitorTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        txtSerialOutput = new MaterialMultiLineTextBox
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            Font = new Font("Consolas", 9),
            BackColor = Color.FromArgb(30, 30, 30),
            ForeColor = Color.FromArgb(0, 255, 0),
            Margin = new Padding(0, 0, 0, 10)
        };

        var controlPanel = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        chkAutoScroll = new MaterialCheckbox
        {
            Text = "Auto-Scroll",
            AutoSize = true,
            Checked = true,
            Margin = new Padding(0, 0, 20, 0)
        };

        btnClearOutput = new MaterialButton
        {
            Text = "Ausgabe löschen",
            Width = 140,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Text,
            Margin = new Padding(0)
        };
        btnClearOutput.Click += (s, e) => txtSerialOutput.Clear();

        controlPanel.Controls.Add(chkAutoScroll);
        controlPanel.Controls.Add(btnClearOutput);

        monitorTable.Controls.Add(txtSerialOutput, 0, 0);
        monitorTable.Controls.Add(controlPanel, 0, 1);

        grpMonitor.Controls.Add(monitorTable);
        return grpMonitor;
    }

    // ============================================
    // ESP32 Configuration - Serial Port Utilities
    // ============================================

    private void LoadComPorts()
    {
        cmbComPort.Items.Clear();
        var ports = SerialPort.GetPortNames().OrderBy(p => p).ToArray();

        if (ports.Length == 0)
        {
            cmbComPort.Items.Add("Keine Ports gefunden");
            AppendSerialOutput("Keine COM Ports gefunden. Bitte verbinden Sie das ESP32.\r\n");
        }
        else
        {
            cmbComPort.Items.AddRange(ports);
            if (cmbComPort.SelectedIndex == -1)
            {
                // Try to restore last used port
                if (!string.IsNullOrEmpty(_settings.LastComPort) && ports.Contains(_settings.LastComPort))
                {
                    cmbComPort.SelectedItem = _settings.LastComPort;
                }
                else
                {
                    cmbComPort.SelectedIndex = 0;
                }
            }

            if (txtSerialOutput != null && txtSerialOutput.Text.Length == 0)
            {
                AppendSerialOutput($"Gefundene COM Ports: {string.Join(", ", ports)}\r\n");
            }
        }
    }

    private void LoadBaudRates()
    {
        var rates = new[] { 9600, 19200, 38400, 57600, 115200, 230400, 460800, 921600 };
        cmbBaudRate.Items.AddRange(rates.Cast<object>().ToArray());
        cmbBaudRate.SelectedItem = 115200;
    }

    private void UpdateConnectionStatus(bool connected)
    {
        if (InvokeRequired)
        {
            Invoke(() => UpdateConnectionStatus(connected));
            return;
        }

        btnConnect.Enabled = !connected;
        btnDisconnect.Enabled = connected;
        btnSendWifi.Enabled = connected;
        btnSendDisplay.Enabled = connected;
        btnPreset1.Enabled = connected;
        btnPreset3.Enabled = connected;
        btnStatus.Enabled = connected;
        btnClear.Enabled = connected;
        btnReboot.Enabled = connected;
        btnHelp.Enabled = connected;
        btnTestConnection.Enabled = connected;
        cmbComPort.Enabled = !connected;
        cmbBaudRate.Enabled = !connected;
        btnRefreshPorts.Enabled = !connected;

        lblConnectionStatus.Text = connected ? "Verbunden" : "Nicht verbunden";
        lblConnectionStatus.ForeColor = connected ? Color.Green : Color.Red;
    }

    private void AppendSerialOutput(string text)
    {
        if (InvokeRequired)
        {
            Invoke(() => AppendSerialOutput(text));
            return;
        }

        txtSerialOutput.AppendText(text);

        if (chkAutoScroll.Checked)
        {
            txtSerialOutput.SelectionStart = txtSerialOutput.Text.Length;
            txtSerialOutput.ScrollToCaret();
        }
    }

    // ============================================
    // ESP32 Configuration - Serial Connection Events
    // ============================================

    private void BtnConnect_Click(object? sender, EventArgs e)
    {
        if (cmbComPort.SelectedItem == null)
        {
            MessageBox.Show("Bitte wählen Sie einen COM Port.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var portName = cmbComPort.SelectedItem.ToString()!;

        try
        {
            _serialPort = new SerialPort
            {
                PortName = portName,
                BaudRate = (int)cmbBaudRate.SelectedItem!,
                DataBits = 8,
                Parity = Parity.None,
                StopBits = StopBits.One,
                Encoding = Encoding.UTF8,
                DtrEnable = true,
                RtsEnable = true
            };

            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();

            // Wait a moment for the port to stabilize
            System.Threading.Thread.Sleep(100);

            // Clear any stale data in the buffer
            _serialPort.DiscardInBuffer();
            _serialPort.DiscardOutBuffer();

            // Save settings
            _settings.LastComPort = _serialPort.PortName;
            _settings.LastBaudRate = _serialPort.BaudRate;
            _settings.Save();

            UpdateConnectionStatus(true);
            AppendSerialOutput($"Verbunden mit {_serialPort.PortName} @ {_serialPort.BaudRate} baud\r\n");
            AppendSerialOutput("Bereit zum Senden von Befehlen.\r\n");
        }
        catch (UnauthorizedAccessException)
        {
            var result = MessageBox.Show(
                $"Port {portName} wird bereits verwendet.\n\n" +
                "Mögliche Ursachen:\n" +
                "• Arduino IDE Serial Monitor ist geöffnet\n" +
                "• PuTTY oder anderes Terminal-Programm verwendet den Port\n" +
                "• Vorherige Verbindung wurde nicht sauber geschlossen\n\n" +
                "Lösungsvorschläge:\n" +
                "1. Schließen Sie Arduino IDE (komplett beenden)\n" +
                "2. Schließen Sie alle anderen Serial Monitor Programme\n" +
                "3. Trennen und verbinden Sie das USB Kabel\n" +
                "4. Starten Sie diese Anwendung neu\n\n" +
                "Port-Liste aktualisieren und erneut versuchen?",
                "Port gesperrt - COM8 Problem",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                LoadComPorts();
            }
        }
        catch (IOException ioEx)
        {
            MessageBox.Show(
                $"Port {portName} konnte nicht geöffnet werden.\n\n" +
                $"Fehler: {ioEx.Message}\n\n" +
                "Versuchen Sie:\n" +
                "• Gerät trennen und wieder anschließen\n" +
                "• Port-Liste aktualisieren (↻ Button)\n" +
                "• Anderen USB-Port verwenden",
                "Verbindungsfehler",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (ArgumentException argEx)
        {
            MessageBox.Show(
                $"Ungültiger Port Name: {portName}\n\n" +
                $"Fehler: {argEx.Message}\n\n" +
                "Bitte aktualisieren Sie die Port-Liste mit dem ↻ Button.",
                "Ungültiger Port",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Unerwarteter Fehler beim Verbinden mit {portName}:\n\n" +
                $"{ex.GetType().Name}: {ex.Message}",
                "Fehler",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BtnDisconnect_Click(object? sender, EventArgs e)
    {
        DisconnectSerial();
    }

    private void DisconnectSerial()
    {
        if (_serialPort != null)
        {
            try
            {
                if (_serialPort.IsOpen)
                {
                    _serialPort.DataReceived -= SerialPort_DataReceived;
                    _serialPort.Close();
                }
            }
            catch
            {
                // Ignore errors during disconnect
            }
            finally
            {
                try
                {
                    _serialPort.Dispose();
                }
                catch
                {
                    // Ignore disposal errors
                }
                _serialPort = null;
            }

            UpdateConnectionStatus(false);
            if (!IsDisposed && txtSerialOutput != null)
            {
                AppendSerialOutput("Verbindung getrennt\r\n");
            }
        }
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        if (_serialPort?.IsOpen != true) return;

        try
        {
            var data = _serialPort.ReadExisting();
            AppendSerialOutput(data);
        }
        catch { }
    }

    // ============================================
    // ESP32 Configuration - Command Events
    // ============================================

    private void SendCommand(string command)
    {
        if (_serialPort?.IsOpen != true)
        {
            AppendSerialOutput($"[FEHLER] Nicht verbunden - Befehl '{command}' konnte nicht gesendet werden\r\n");
            return;
        }

        try
        {
            _serialPort.WriteLine(command);
            AppendSerialOutput($"> {command}\r\n");
        }
        catch (InvalidOperationException)
        {
            AppendSerialOutput($"[FEHLER] Port wurde geschlossen - Befehl '{command}' fehlgeschlagen\r\n");
            DisconnectSerial();
        }
        catch (TimeoutException)
        {
            AppendSerialOutput($"[FEHLER] Timeout beim Senden von '{command}'\r\n");
        }
        catch (Exception ex)
        {
            AppendSerialOutput($"[FEHLER] Sendefehler: {ex.Message}\r\n");
            MessageBox.Show($"Sendefehler: {ex.Message}", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void BtnTestConnection_Click(object? sender, EventArgs e)
    {
        if (_serialPort?.IsOpen != true)
        {
            MessageBox.Show("Keine Verbindung vorhanden.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        AppendSerialOutput("\r\n=== Verbindungstest ===\r\n");
        SendCommand("HELP");
        System.Threading.Thread.Sleep(100);
        SendCommand("STATUS");
        AppendSerialOutput("=== Test abgeschlossen ===\r\n\r\n");
    }

    // ============================================
    // ESP32 Configuration - WiFi/Display Events
    // ============================================

    private void BtnSendWifi_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtSsid.Text))
        {
            MessageBox.Show("Bitte geben Sie eine SSID ein.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var command = $"WIFI:{txtSsid.Text}:{txtPassword.Text}";
        SendCommand(command);

        if (MessageBox.Show("WIFI Befehl gesendet. ESP32 neu starten?", "Neustart",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            SendCommand("REBOOT");
        }
    }

    private void BtnSendDisplay_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtDisplayPins.Text))
        {
            MessageBox.Show("Bitte geben Sie die Display Pins ein.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var command = $"DISPLAY:{numDisplayCount.Value}:{txtDisplayPins.Text}";
        SendCommand(command);

        // Save the number of screens to settings
        _settings.NumScreens = (int)numDisplayCount.Value;
        _settings.Save();

        if (MessageBox.Show("DISPLAY Befehl gesendet. ESP32 neu starten?", "Neustart",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            SendCommand("REBOOT");
        }
    }
}
