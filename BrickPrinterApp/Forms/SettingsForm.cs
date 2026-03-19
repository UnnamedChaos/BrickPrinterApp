using BrickPrinterApp.Services;

namespace BrickPrinterApp;

public partial class SettingsForm : Form
{
    private readonly SettingService _settings;
    private Button btnSave;
    private Label lblIpInfo;
    private TextBox txtIpAddress;

    public SettingsForm(SettingService settings)
    {
        InitializeComponentManual();
        _settings = settings;
        txtIpAddress.Text = _settings.EspIpAddress;
    }

    private void InitializeComponentManual()
    {
        // IP Address
        lblIpInfo = new Label { Text = "ESP32 IP-Adresse:", Location = new Point(20, 20), AutoSize = true };
        txtIpAddress = new TextBox { Location = new Point(20, 40), Width = 200 };

        // Save button
        btnSave = new Button { Text = "Speichern", Location = new Point(20, 80), Width = 80 };
        btnSave.Click += btnSave_Click;

        Controls.Add(lblIpInfo);
        Controls.Add(txtIpAddress);
        Controls.Add(btnSave);

        Text = "Einstellungen";
        Size = new Size(260, 160);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
    }

    private void btnSave_Click(object? sender, EventArgs e)
    {
        _settings.EspIpAddress = txtIpAddress.Text;
        _settings.Save();
        Close();
    }
}
