using System.IO.Ports;

namespace BrickPrinterApp.Forms;

public class WiFiSetupForm : Form
{
    // Layout constants
    private const int MarginLeft = 20;
    private const int ControlWidth = 290;
    private const int LabelHeight = 20;
    private const int ControlSpacing = 60;

    // Controls
    private ComboBox cmbComPort;
    private TextBox txtSsid;
    private TextBox txtPassword;
    private CheckBox chkShowPassword;
    private Button btnRefreshPorts;
    private Button btnSend;
    private Button btnStatus;
    private Button btnReboot;
    private TextBox txtLog;
    private Label lblComPort;
    private Label lblSsid;
    private Label lblPassword;

    public WiFiSetupForm()
    {
        InitializeComponents();
        RefreshComPorts();
    }

    #region UI Initialization

    private void InitializeComponents()
    {
        ConfigureForm();
        CreateComPortSection();
        CreateCredentialsSection();
        CreateActionButtons();
        CreateLogOutput();
        AddAllControls();
    }

    private void ConfigureForm()
    {
        Text = "ESP32 WiFi Setup";
        Size = new Size(400, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
    }

    private void CreateComPortSection()
    {
        lblComPort = CreateLabel("COM Port:", MarginLeft, 20);
        cmbComPort = new ComboBox
        {
            Location = new Point(MarginLeft, 40),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnRefreshPorts = CreateButton("Refresh", 230, 38, 80);
        btnRefreshPorts.Click += (_, _) => RefreshComPorts();
    }

    private void CreateCredentialsSection()
    {
        lblSsid = CreateLabel("SSID:", MarginLeft, 80);
        txtSsid = CreateTextBox(MarginLeft, 100, ControlWidth);

        lblPassword = CreateLabel("Password:", MarginLeft, 140);
        txtPassword = CreateTextBox(MarginLeft, 160, ControlWidth, usePasswordChar: true);

        chkShowPassword = new CheckBox
        {
            Text = "Show",
            Location = new Point(320, 162),
            AutoSize = true
        };
        chkShowPassword.CheckedChanged += (_, _) =>
        {
            txtPassword.UseSystemPasswordChar = !chkShowPassword.Checked;
        };
    }

    private void CreateActionButtons()
    {
        btnSend = CreateButton("Send WiFi Config", MarginLeft, 200, 120);
        btnSend.Click += BtnSend_Click;

        btnStatus = CreateButton("Get Status", 150, 200, 80);
        btnStatus.Click += BtnStatus_Click;

        btnReboot = CreateButton("Reboot", 240, 200, 70);
        btnReboot.Click += BtnReboot_Click;
    }

    private void CreateLogOutput()
    {
        txtLog = new TextBox
        {
            Location = new Point(MarginLeft, 240),
            Size = new Size(340, 90),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9)
        };
    }

    private void AddAllControls()
    {
        Controls.AddRange([
            lblComPort, cmbComPort, btnRefreshPorts,
            lblSsid, txtSsid,
            lblPassword, txtPassword, chkShowPassword,
            btnSend, btnStatus, btnReboot,
            txtLog
        ]);
    }

    private Label CreateLabel(string text, int x, int y)
    {
        return new Label
        {
            Text = text,
            Location = new Point(x, y),
            AutoSize = true
        };
    }

    private TextBox CreateTextBox(int x, int y, int width, bool usePasswordChar = false)
    {
        return new TextBox
        {
            Location = new Point(x, y),
            Width = width,
            UseSystemPasswordChar = usePasswordChar
        };
    }

    private Button CreateButton(string text, int x, int y, int width)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Width = width
        };
    }

    #endregion

    #region Event Handlers

    private void RefreshComPorts()
    {
        cmbComPort.Items.Clear();
        var ports = SerialPort.GetPortNames();
        foreach (var port in ports)
        {
            cmbComPort.Items.Add(port);
        }

        if (cmbComPort.Items.Count > 0)
        {
            cmbComPort.SelectedIndex = 0;
        }

        Log($"Found {ports.Length} COM port(s)");
    }

    private void Log(string message)
    {
        txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
    }

    #endregion

    #region Button Click Handlers

    private void BtnSend_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedPort(out var portName))
            return;

        if (string.IsNullOrWhiteSpace(txtSsid.Text))
        {
            Log("ERROR: SSID is required");
            return;
        }

        var command = $"WIFI:{txtSsid.Text}:{txtPassword.Text}";
        SendSerialCommand(portName, command, "WiFi configuration sent");
    }

    private void BtnStatus_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedPort(out var portName))
            return;

        SendSerialCommand(portName, "STATUS", "Status requested");
    }

    private void BtnReboot_Click(object? sender, EventArgs e)
    {
        if (!TryGetSelectedPort(out var portName))
            return;

        if (!ConfirmReboot())
            return;

        SendSerialCommand(portName, "REBOOT", "Reboot command sent");
    }

    private bool TryGetSelectedPort(out string portName)
    {
        portName = string.Empty;

        if (cmbComPort.SelectedItem == null)
        {
            Log("ERROR: No COM port selected");
            return false;
        }

        portName = cmbComPort.SelectedItem.ToString()!;
        return true;
    }

    private bool ConfirmReboot()
    {
        var result = MessageBox.Show(
            "Are you sure you want to reboot the ESP32?",
            "Confirm Reboot",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        return result == DialogResult.Yes;
    }

    #endregion

    #region Serial Communication

    private void SendSerialCommand(string portName, string command, string successMessage)
    {
        try
        {
            using var port = CreateSerialPort(portName);
            port.Open();
            port.WriteLine(command);
            Log(successMessage);

            ReadAndLogResponse(port);
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }
    }

    private SerialPort CreateSerialPort(string portName)
    {
        return new SerialPort(portName, 115200)
        {
            ReadTimeout = 2000,
            WriteTimeout = 2000
        };
    }

    private void ReadAndLogResponse(SerialPort port)
    {
        Thread.Sleep(500);

        if (port.BytesToRead <= 0)
            return;

        var response = port.ReadExisting();
        var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            Log($"< {line.Trim()}");
        }
    }

    #endregion
}
