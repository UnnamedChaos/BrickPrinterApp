using BrickPrinterApp.Services;
using System.IO.Ports;
using System.Text;

namespace BrickPrinterApp;

public partial class SettingsForm : Form
{
    private readonly SettingService _settings;
    private SerialPort? _serialPort;

    // IP Settings
    private TextBox txtIpAddress = null!;
    private Button btnSaveIp = null!;

    // Serial Settings
    private ComboBox cmbComPort = null!;
    private ComboBox cmbBaudRate = null!;
    private Button btnRefreshPorts = null!;
    private Button btnConnect = null!;
    private Button btnDisconnect = null!;
    private Label lblConnectionStatus = null!;

    // WiFi Configuration
    private TextBox txtSsid = null!;
    private TextBox txtPassword = null!;
    private Button btnSendWifi = null!;

    // Display Configuration
    private NumericUpDown numDisplayCount = null!;
    private TextBox txtDisplayPins = null!;
    private Button btnSendDisplay = null!;
    private Button btnPreset1 = null!;
    private Button btnPreset3 = null!;

    // Commands
    private Button btnStatus = null!;
    private Button btnClear = null!;
    private Button btnReboot = null!;
    private Button btnHelp = null!;
    private Button btnTestConnection = null!;

    // Serial Monitor
    private TextBox txtSerialOutput = null!;
    private Button btnClearOutput = null!;
    private CheckBox chkAutoScroll = null!;

    // Time Settings
    private NumericUpDown numTimeOffset = null!;
    private Button btnSaveTimeOffset = null!;

    public SettingsForm(SettingService settings)
    {
        _settings = settings;
        InitializeComponentManual();
        txtIpAddress.Text = _settings.EspIpAddress;
        numTimeOffset.Value = _settings.TimeOffsetHours;
        LoadBaudRates();
        LoadComPorts();  // LoadComPorts now handles restoring the last port
    }

    private void InitializeComponentManual()
    {
        var tabControl = new TabControl { Dock = DockStyle.Fill };

        // Tab 1: IP Settings
        var tabIp = new TabPage("IP Einstellungen");
        InitializeIpTab(tabIp);

        // Tab 2: Serial Configuration
        var tabSerial = new TabPage("ESP32 Konfiguration");
        InitializeSerialTab(tabSerial);

        // Tab 3: Time Settings
        var tabTime = new TabPage("Uhrzeit");
        InitializeTimeTab(tabTime);

        tabControl.TabPages.Add(tabIp);
        tabControl.TabPages.Add(tabSerial);
        tabControl.TabPages.Add(tabTime);

        Controls.Add(tabControl);

        Text = "Einstellungen";
        Size = new Size(700, 600);
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(700, 600);
        StartPosition = FormStartPosition.CenterParent;
    }

    private void InitializeIpTab(TabPage tab)
    {
        var lblIpInfo = new Label
        {
            Text = "ESP32 IP-Adresse:",
            Location = new Point(20, 20),
            AutoSize = true
        };

        txtIpAddress = new TextBox
        {
            Location = new Point(20, 45),
            Width = 200
        };

        btnSaveIp = new Button
        {
            Text = "Speichern",
            Location = new Point(20, 75),
            Width = 100
        };
        btnSaveIp.Click += BtnSaveIp_Click;

        tab.Controls.Add(lblIpInfo);
        tab.Controls.Add(txtIpAddress);
        tab.Controls.Add(btnSaveIp);
    }

    private void InitializeSerialTab(TabPage tab)
    {
        int y = 10;

        // Info Label
        var lblInfo = new Label
        {
            Text = "Verbinden Sie das ESP32 per USB und konfigurieren Sie WiFi, Displays und mehr.",
            Location = new Point(10, y),
            Size = new Size(660, 35),
            ForeColor = Color.Blue
        };
        tab.Controls.Add(lblInfo);
        y += 40;

        // Serial Connection Section
        var grpConnection = new GroupBox
        {
            Text = "Serielle Verbindung",
            Location = new Point(10, y),
            Size = new Size(660, 120)
        };

        var lblComPort = new Label { Text = "COM Port:", Location = new Point(10, 25), AutoSize = true };
        cmbComPort = new ComboBox { Location = new Point(80, 22), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };

        btnRefreshPorts = new Button { Text = "↻", Location = new Point(205, 21), Width = 30 };
        btnRefreshPorts.Click += (s, e) =>
        {
            AppendSerialOutput("Aktualisiere COM Port Liste...\r\n");
            LoadComPorts();
        };

        var lblBaud = new Label { Text = "Baud Rate:", Location = new Point(250, 25), AutoSize = true };
        cmbBaudRate = new ComboBox { Location = new Point(320, 22), Width = 100, DropDownStyle = ComboBoxStyle.DropDownList };

        btnConnect = new Button { Text = "Verbinden", Location = new Point(10, 55), Width = 100 };
        btnConnect.Click += BtnConnect_Click;

        btnDisconnect = new Button { Text = "Trennen", Location = new Point(120, 55), Width = 100, Enabled = false };
        btnDisconnect.Click += BtnDisconnect_Click;

        lblConnectionStatus = new Label
        {
            Text = "Nicht verbunden",
            Location = new Point(10, 85),
            AutoSize = true,
            ForeColor = Color.Red
        };

        var lblWarning = new Label
        {
            Text = "⚠ Schließen Sie den Arduino IDE Serial Monitor vor dem Verbinden!",
            Location = new Point(240, 85),
            Size = new Size(410, 20),
            ForeColor = Color.DarkOrange,
            Font = new Font(Font.FontFamily, 8f)
        };

        grpConnection.Controls.AddRange(new Control[]
        {
            lblComPort, cmbComPort, btnRefreshPorts, lblBaud, cmbBaudRate,
            btnConnect, btnDisconnect, lblConnectionStatus, lblWarning
        });

        y += 135;

        // WiFi Configuration Section
        var grpWifi = new GroupBox
        {
            Text = "WiFi Konfiguration",
            Location = new Point(10, y),
            Size = new Size(320, 120)
        };

        var lblSsid = new Label { Text = "SSID:", Location = new Point(10, 25), AutoSize = true };
        txtSsid = new TextBox { Location = new Point(80, 22), Width = 220, PlaceholderText = "Ihr WiFi Name" };

        var lblPassword = new Label { Text = "Passwort:", Location = new Point(10, 55), AutoSize = true };
        txtPassword = new TextBox { Location = new Point(80, 52), Width = 220, UseSystemPasswordChar = true, PlaceholderText = "WiFi Passwort" };

        btnSendWifi = new Button { Text = "WIFI Senden", Location = new Point(10, 85), Width = 120, Enabled = false };
        btnSendWifi.Click += BtnSendWifi_Click;

        grpWifi.Controls.AddRange(new Control[] { lblSsid, txtSsid, lblPassword, txtPassword, btnSendWifi });

        // Display Configuration Section
        var grpDisplay = new GroupBox
        {
            Text = "Display Konfiguration",
            Location = new Point(350, y),
            Size = new Size(320, 120)
        };

        var lblDisplayCount = new Label { Text = "Anzahl:", Location = new Point(10, 25), AutoSize = true };
        numDisplayCount = new NumericUpDown
        {
            Location = new Point(80, 22),
            Width = 60,
            Minimum = 1,
            Maximum = 3,
            Value = 1
        };

        var lblPins = new Label { Text = "Pins:", Location = new Point(10, 55), AutoSize = true };
        txtDisplayPins = new TextBox { Location = new Point(80, 52), Width = 120, PlaceholderText = "6:7" };

        btnPreset1 = new Button { Text = "1x (6:7)", Location = new Point(205, 51), Width = 70, Height = 23, Enabled = false };
        btnPreset1.Click += (s, e) => { numDisplayCount.Value = 1; txtDisplayPins.Text = "6:7"; };

        btnPreset3 = new Button { Text = "3x Default", Location = new Point(205, 76), Width = 70, Height = 23, Enabled = false };
        btnPreset3.Click += (s, e) => { numDisplayCount.Value = 3; txtDisplayPins.Text = "10:21:4:5:8:9"; };

        btnSendDisplay = new Button { Text = "DISPLAY Senden", Location = new Point(10, 85), Width = 140, Enabled = false };
        btnSendDisplay.Click += BtnSendDisplay_Click;

        grpDisplay.Controls.AddRange(new Control[]
        {
            lblDisplayCount, numDisplayCount, lblPins, txtDisplayPins, btnSendDisplay, btnPreset1, btnPreset3
        });

        y += 135;

        // Commands Section
        var grpCommands = new GroupBox
        {
            Text = "Befehle",
            Location = new Point(10, y),
            Size = new Size(660, 60)
        };

        btnStatus = new Button { Text = "STATUS", Location = new Point(10, 22), Width = 80, Enabled = false };
        btnStatus.Click += (s, e) => SendCommand("STATUS");

        btnClear = new Button { Text = "CLEAR", Location = new Point(100, 22), Width = 80, Enabled = false };
        btnClear.Click += (s, e) => SendCommand("CLEAR");

        btnReboot = new Button { Text = "REBOOT", Location = new Point(190, 22), Width = 80, Enabled = false };
        btnReboot.Click += (s, e) => SendCommand("REBOOT");

        btnHelp = new Button { Text = "HELP", Location = new Point(280, 22), Width = 80, Enabled = false };
        btnHelp.Click += (s, e) => SendCommand("HELP");

        btnTestConnection = new Button { Text = "Test", Location = new Point(370, 22), Width = 80, Enabled = false };
        btnTestConnection.Click += BtnTestConnection_Click;

        grpCommands.Controls.AddRange(new Control[] { btnStatus, btnClear, btnReboot, btnHelp, btnTestConnection });

        y += 75;

        // Serial Monitor Section
        var grpMonitor = new GroupBox
        {
            Text = "Serielle Ausgabe",
            Location = new Point(10, y),
            Size = new Size(660, 180)
        };

        txtSerialOutput = new TextBox
        {
            Location = new Point(10, 20),
            Size = new Size(640, 120),
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            ReadOnly = true,
            Font = new Font("Consolas", 9),
            BackColor = Color.Black,
            ForeColor = Color.Lime
        };

        chkAutoScroll = new CheckBox
        {
            Text = "Auto-Scroll",
            Location = new Point(10, 145),
            AutoSize = true,
            Checked = true
        };

        btnClearOutput = new Button { Text = "Ausgabe löschen", Location = new Point(120, 142), Width = 120 };
        btnClearOutput.Click += (s, e) => txtSerialOutput.Clear();

        grpMonitor.Controls.AddRange(new Control[] { txtSerialOutput, chkAutoScroll, btnClearOutput });

        tab.Controls.AddRange(new Control[] { grpConnection, grpWifi, grpDisplay, grpCommands, grpMonitor });
    }

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

        if (MessageBox.Show("DISPLAY Befehl gesendet. ESP32 neu starten?", "Neustart",
            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            SendCommand("REBOOT");
        }
    }

    private void BtnSaveIp_Click(object? sender, EventArgs e)
    {
        _settings.EspIpAddress = txtIpAddress.Text;
        _settings.Save();
        MessageBox.Show("IP-Adresse gespeichert.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void InitializeTimeTab(TabPage tab)
    {
        var lblInfo = new Label
        {
            Text = "Zeitverschiebung für alle Uhr-Widgets (z.B. Winter-/Sommerzeit):",
            Location = new Point(20, 20),
            AutoSize = true,
            Font = new Font(Font.FontFamily, 9f, FontStyle.Bold)
        };

        var lblOffset = new Label
        {
            Text = "Stunden Verschiebung:",
            Location = new Point(20, 60),
            AutoSize = true
        };

        numTimeOffset = new NumericUpDown
        {
            Location = new Point(180, 57),
            Width = 80,
            Minimum = -12,
            Maximum = 12,
            Value = 0
        };

        var lblExamples = new Label
        {
            Text = "+1 = Eine Stunde vorwärts (Sommerzeit)\n" +
                   " 0 = Keine Verschiebung\n" +
                   "-1 = Eine Stunde zurück (Winterzeit)",
            Location = new Point(40, 90),
            Size = new Size(400, 60),
            ForeColor = Color.DarkBlue
        };

        btnSaveTimeOffset = new Button
        {
            Text = "Speichern",
            Location = new Point(20, 160),
            Width = 120
        };
        btnSaveTimeOffset.Click += BtnSaveTimeOffset_Click;

        var lblNote = new Label
        {
            Text = "Hinweis: Die Änderung wird beim nächsten Update der Widgets wirksam.\n" +
                   "Verwenden Sie 'Jetzt Updaten' im Tray-Menü, um sofort zu aktualisieren.",
            Location = new Point(20, 200),
            Size = new Size(600, 40),
            ForeColor = Color.Gray,
            Font = new Font(Font.FontFamily, 8f, FontStyle.Italic)
        };

        tab.Controls.Add(lblInfo);
        tab.Controls.Add(lblOffset);
        tab.Controls.Add(numTimeOffset);
        tab.Controls.Add(lblExamples);
        tab.Controls.Add(btnSaveTimeOffset);
        tab.Controls.Add(lblNote);
    }

    private void BtnSaveTimeOffset_Click(object? sender, EventArgs e)
    {
        _settings.TimeOffsetHours = (int)numTimeOffset.Value;
        _settings.Save();
        MessageBox.Show(
            $"Zeitverschiebung auf {_settings.TimeOffsetHours} Stunden gesetzt.\n\n" +
            "Die Änderung wird beim nächsten Widget-Update aktiv.",
            "Gespeichert",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        DisconnectSerial();
        base.OnFormClosing(e);
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
