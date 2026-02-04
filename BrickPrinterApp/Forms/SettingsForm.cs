using BrickPrinterApp.Services;

namespace BrickPrinterApp;

public partial class SettingsForm : Form
{
    private readonly SettingService _settings;
    private Button btnSave;
    private Label lblInfo;

    // Diese Zeilen manuell hinzufügen, falls der Designer sie nicht erstellt hat:
    private TextBox txtIpAddress;

    public SettingsForm(SettingService settings)
    {
        InitializeComponentManual(); // Eigene Methode statt der Designer-Methode
        _settings = settings;
        txtIpAddress.Text = _settings.EspIpAddress;
    }

    private void InitializeComponentManual()
    {
        txtIpAddress = new TextBox { Location = new Point(20, 40), Width = 200 };
        btnSave = new Button { Text = "Speichern", Location = new Point(20, 70) };
        lblInfo = new Label { Text = "ESP32 IP-Adresse:", Location = new Point(20, 20) };

        btnSave.Click += btnSave_Click;

        Controls.Add(txtIpAddress);
        Controls.Add(btnSave);
        Controls.Add(lblInfo);

        Text = "Einstellungen";
        Size = new Size(260, 150);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        _settings.EspIpAddress = txtIpAddress.Text;
        Close();
    }
}