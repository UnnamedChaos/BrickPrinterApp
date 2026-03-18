using BrickPrinterApp.Services;

namespace BrickPrinterApp;

public partial class SettingsForm : Form
{
    private readonly SettingService _settings;
    private Button btnSave;
    private Label lblIpInfo;
    private Label lblScreenInfo;
    private TextBox txtIpAddress;
    private ComboBox cboScreen;

    public SettingsForm(SettingService settings)
    {
        InitializeComponentManual();
        _settings = settings;
        txtIpAddress.Text = _settings.EspIpAddress;
        cboScreen.SelectedIndex = _settings.SelectedScreen;
    }

    private void InitializeComponentManual()
    {
        // IP Address
        lblIpInfo = new Label { Text = "ESP32 IP-Adresse:", Location = new Point(20, 20), AutoSize = true };
        txtIpAddress = new TextBox { Location = new Point(20, 40), Width = 200 };

        // Screen selection
        lblScreenInfo = new Label { Text = "Bildschirm:", Location = new Point(20, 70), AutoSize = true };
        cboScreen = new ComboBox
        {
            Location = new Point(20, 90),
            Width = 200,
            DropDownStyle = ComboBoxStyle.DropDownList
        };
        cboScreen.Items.AddRange(new object[] { "Screen 0", "Screen 1", "Screen 2" });

        // Save button
        btnSave = new Button { Text = "Speichern", Location = new Point(20, 130), Width = 80 };
        btnSave.Click += btnSave_Click;

        Controls.Add(lblIpInfo);
        Controls.Add(txtIpAddress);
        Controls.Add(lblScreenInfo);
        Controls.Add(cboScreen);
        Controls.Add(btnSave);

        Text = "Einstellungen";
        Size = new Size(260, 210);
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        _settings.EspIpAddress = txtIpAddress.Text;
        _settings.SelectedScreen = cboScreen.SelectedIndex;
        Close();
    }
}