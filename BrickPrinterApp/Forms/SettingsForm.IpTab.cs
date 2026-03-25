using BrickPrinterApp.Services;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp;

public partial class SettingsForm
{
    // ============================================
    // IP Settings - Tab Fields
    // ============================================

    private MaterialTextBox txtIpAddress = null!;
    private MaterialButton btnSaveIp = null!;

    // ============================================
    // IP Settings - Tab Initialization
    // ============================================

    private void InitializeIpTab(TabPage tab)
    {
        var lblIpInfo = new MaterialLabel
        {
            Text = "ESP32 IP-Adresse:",
            Location = new Point(20, 30),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.H6
        };

        txtIpAddress = new MaterialTextBox
        {
            Location = new Point(20, 70),
            Width = 300,
            Hint = "z.B. 192.168.178.50"
        };

        btnSaveIp = new MaterialButton
        {
            Text = "Speichern",
            Location = new Point(20, 130),
            Width = 150,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Contained
        };
        btnSaveIp.Click += BtnSaveIp_Click;

        tab.Controls.Add(lblIpInfo);
        tab.Controls.Add(txtIpAddress);
        tab.Controls.Add(btnSaveIp);
    }

    // ============================================
    // IP Settings - Event Handlers
    // ============================================

    private void BtnSaveIp_Click(object? sender, EventArgs e)
    {
        _settings.EspIpAddress = txtIpAddress.Text;
        _settings.Save();
        MessageBox.Show("IP-Adresse gespeichert.", "Erfolg", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
}
