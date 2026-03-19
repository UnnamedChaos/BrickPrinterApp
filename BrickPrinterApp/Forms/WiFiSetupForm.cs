using System.IO.Ports;

namespace BrickPrinterApp.Forms;

public class WiFiSetupForm : Form
{
    private ComboBox cmbComPort;
    private TextBox txtSsid;
    private TextBox txtPassword;
    private CheckBox chkShowPassword;
    private Button btnRefreshPorts;
    private Button btnSend;
    private Button btnStatus;
    private TextBox txtLog;
    private Label lblComPort;
    private Label lblSsid;
    private Label lblPassword;

    public WiFiSetupForm()
    {
        InitializeComponents();
        RefreshComPorts();
    }

    private void InitializeComponents()
    {
        Text = "ESP32 WiFi Setup";
        Size = new Size(400, 380);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;

        // COM Port selection
        lblComPort = new Label
        {
            Text = "COM Port:",
            Location = new Point(20, 20),
            AutoSize = true
        };

        cmbComPort = new ComboBox
        {
            Location = new Point(20, 40),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        btnRefreshPorts = new Button
        {
            Text = "Refresh",
            Location = new Point(230, 38),
            Width = 80
        };
        btnRefreshPorts.Click += (_, _) => RefreshComPorts();

        // SSID
        lblSsid = new Label
        {
            Text = "SSID:",
            Location = new Point(20, 80),
            AutoSize = true
        };

        txtSsid = new TextBox
        {
            Location = new Point(20, 100),
            Width = 290
        };

        // Password
        lblPassword = new Label
        {
            Text = "Password:",
            Location = new Point(20, 140),
            AutoSize = true
        };

        txtPassword = new TextBox
        {
            Location = new Point(20, 160),
            Width = 290,
            UseSystemPasswordChar = true
        };

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

        // Buttons
        btnSend = new Button
        {
            Text = "Send WiFi Config",
            Location = new Point(20, 200),
            Width = 120
        };
        btnSend.Click += BtnSend_Click;

        btnStatus = new Button
        {
            Text = "Get Status",
            Location = new Point(150, 200),
            Width = 80
        };
        btnStatus.Click += BtnStatus_Click;

        // Log output
        txtLog = new TextBox
        {
            Location = new Point(20, 240),
            Size = new Size(340, 90),
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Font = new Font("Consolas", 9)
        };

        Controls.AddRange([
            lblComPort, cmbComPort, btnRefreshPorts,
            lblSsid, txtSsid,
            lblPassword, txtPassword, chkShowPassword,
            btnSend, btnStatus,
            txtLog
        ]);
    }

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

    private void BtnSend_Click(object? sender, EventArgs e)
    {
        if (cmbComPort.SelectedItem == null)
        {
            Log("ERROR: No COM port selected");
            return;
        }

        if (string.IsNullOrWhiteSpace(txtSsid.Text))
        {
            Log("ERROR: SSID is required");
            return;
        }

        var portName = cmbComPort.SelectedItem.ToString()!;
        var command = $"WIFI:{txtSsid.Text}:{txtPassword.Text}";

        SendSerialCommand(portName, command, "WiFi configuration sent");
    }

    private void BtnStatus_Click(object? sender, EventArgs e)
    {
        if (cmbComPort.SelectedItem == null)
        {
            Log("ERROR: No COM port selected");
            return;
        }

        var portName = cmbComPort.SelectedItem.ToString()!;
        SendSerialCommand(portName, "STATUS", "Status requested");
    }

    private void SendSerialCommand(string portName, string command, string successMessage)
    {
        try
        {
            using var port = new SerialPort(portName, 115200)
            {
                ReadTimeout = 2000,
                WriteTimeout = 2000
            };

            port.Open();
            port.WriteLine(command);
            Log(successMessage);

            // Try to read response
            Thread.Sleep(500);
            if (port.BytesToRead > 0)
            {
                var response = port.ReadExisting();
                foreach (var line in response.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    Log($"< {line.Trim()}");
                }
            }

            port.Close();
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
        }
    }
}
