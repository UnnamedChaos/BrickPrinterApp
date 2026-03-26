using BrickPrinterApp.Services;
using MaterialSkin;
using MaterialSkin.Controls;
using System.Diagnostics;

namespace BrickPrinterApp;

public partial class SettingsForm
{
    // ============================================
    // Google Account - Tab Fields
    // ============================================

    private GoogleAuthService _googleAuth = null!;
    private MaterialLabel lblGoogleStatus = null!;
    private MaterialLabel lblGoogleEmail = null!;
    private MaterialButton btnGoogleLogin = null!;
    private MaterialButton btnGoogleLogout = null!;
    private MaterialButton btnOpenCredentialsFolder = null!;
    private Panel pnlGoogleStatus = null!;

    // ============================================
    // Google Account - Tab Initialization
    // ============================================

    public void SetGoogleAuthService(GoogleAuthService googleAuth)
    {
        _googleAuth = googleAuth;
    }

    private void InitializeGoogleTab(TabPage tab)
    {
        // Title
        var lblTitle = new MaterialLabel
        {
            Text = "Google Konto",
            Location = new Point(20, 20),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.H5
        };

        // Status panel
        pnlGoogleStatus = new Panel
        {
            Location = new Point(20, 70),
            Size = new Size(660, 80),
            BackColor = Color.FromArgb(240, 240, 240)
        };

        lblGoogleStatus = new MaterialLabel
        {
            Text = "Status: Wird geprüft...",
            Location = new Point(10, 10),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Subtitle1
        };

        lblGoogleEmail = new MaterialLabel
        {
            Text = "",
            Location = new Point(10, 40),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Body1
        };

        pnlGoogleStatus.Controls.Add(lblGoogleStatus);
        pnlGoogleStatus.Controls.Add(lblGoogleEmail);

        // Login button
        btnGoogleLogin = new MaterialButton
        {
            Text = "Mit Google anmelden",
            Location = new Point(20, 170),
            Width = 200,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Contained
        };
        btnGoogleLogin.Click += BtnGoogleLogin_Click;

        // Logout button
        btnGoogleLogout = new MaterialButton
        {
            Text = "Abmelden",
            Location = new Point(240, 170),
            Width = 150,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Outlined,
            Visible = false
        };
        btnGoogleLogout.Click += BtnGoogleLogout_Click;

        // Instructions section
        var lblInstructions = new MaterialLabel
        {
            Text = "Einrichtung:",
            Location = new Point(20, 240),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.H6
        };

        var lblStep1 = new MaterialLabel
        {
            Text = "1. Erstelle ein Projekt in der Google Cloud Console",
            Location = new Point(20, 280),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Body1
        };

        var lblStep2 = new MaterialLabel
        {
            Text = "2. Aktiviere die benötigten APIs (z.B. Google Calendar API)",
            Location = new Point(20, 310),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Body1
        };

        var lblStep3 = new MaterialLabel
        {
            Text = "3. Erstelle OAuth 2.0 Credentials (Desktop App)",
            Location = new Point(20, 340),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Body1
        };

        var lblStep4 = new MaterialLabel
        {
            Text = "4. Lade die credentials.json herunter und lege sie in den Ordner:",
            Location = new Point(20, 370),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Body1
        };

        btnOpenCredentialsFolder = new MaterialButton
        {
            Text = "Ordner öffnen",
            Location = new Point(20, 410),
            Width = 150,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Outlined
        };
        btnOpenCredentialsFolder.Click += BtnOpenCredentialsFolder_Click;

        var lblCredPath = new MaterialLabel
        {
            Text = "",
            Location = new Point(190, 418),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Caption,
            ForeColor = Color.Gray
        };

        // Scopes info
        var lblScopes = new MaterialLabel
        {
            Text = "Angeforderte Berechtigungen:",
            Location = new Point(20, 470),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.H6
        };

        var lblScopesList = new MaterialLabel
        {
            Text = "• Google Kalender (nur lesen)\n• E-Mail-Adresse & Profil",
            Location = new Point(20, 510),
            AutoSize = true,
            Depth = 0,
            FontType = MaterialSkinManager.fontType.Body1
        };

        // Add controls to tab
        tab.Controls.Add(lblTitle);
        tab.Controls.Add(pnlGoogleStatus);
        tab.Controls.Add(btnGoogleLogin);
        tab.Controls.Add(btnGoogleLogout);
        tab.Controls.Add(lblInstructions);
        tab.Controls.Add(lblStep1);
        tab.Controls.Add(lblStep2);
        tab.Controls.Add(lblStep3);
        tab.Controls.Add(lblStep4);
        tab.Controls.Add(btnOpenCredentialsFolder);
        tab.Controls.Add(lblCredPath);
        tab.Controls.Add(lblScopes);
        tab.Controls.Add(lblScopesList);

        // Set credential path text after googleAuth is available
        if (_googleAuth != null)
        {
            lblCredPath.Text = _googleAuth.ClientSecretsPath;
        }
    }

    // ============================================
    // Google Account - Status Updates
    // ============================================

    public async Task UpdateGoogleStatusAsync()
    {
        if (_googleAuth == null) return;

        // First update UI based on current status
        UpdateGoogleStatusUI();

        // Then verify connection if needed
        if (_googleAuth.Status == GoogleAuthStatus.Unknown ||
            _googleAuth.Status == GoogleAuthStatus.NotConfigured)
        {
            await _googleAuth.VerifyConnectionAsync();
            UpdateGoogleStatusUI();
        }
    }

    private void UpdateGoogleStatusUI()
    {
        if (_googleAuth == null) return;

        switch (_googleAuth.Status)
        {
            case GoogleAuthStatus.NotConfigured:
                SetStatusStyle(Color.FromArgb(255, 243, 205), Color.FromArgb(133, 100, 4));
                lblGoogleStatus.Text = "⚠ Nicht konfiguriert";
                lblGoogleEmail.Text = "credentials.json fehlt - siehe Einrichtung unten";
                btnGoogleLogin.Enabled = false;
                btnGoogleLogout.Visible = false;
                break;

            case GoogleAuthStatus.NotLoggedIn:
                SetStatusStyle(Color.FromArgb(240, 240, 240), Color.Gray);
                lblGoogleStatus.Text = "○ Nicht angemeldet";
                lblGoogleEmail.Text = "Klicke auf 'Mit Google anmelden'";
                btnGoogleLogin.Enabled = true;
                btnGoogleLogout.Visible = false;
                break;

            case GoogleAuthStatus.Unknown:
                SetStatusStyle(Color.FromArgb(240, 240, 240), Color.Gray);
                lblGoogleStatus.Text = "○ Wird überprüft...";
                lblGoogleEmail.Text = "";
                btnGoogleLogin.Enabled = false;
                btnGoogleLogout.Visible = false;
                break;

            case GoogleAuthStatus.Authenticating:
                SetStatusStyle(Color.FromArgb(207, 226, 243), Color.FromArgb(30, 100, 180));
                lblGoogleStatus.Text = "◐ Authentifizierung läuft...";
                lblGoogleEmail.Text = "Browser sollte sich öffnen";
                btnGoogleLogin.Enabled = false;
                btnGoogleLogout.Visible = false;
                break;

            case GoogleAuthStatus.Connected:
                SetStatusStyle(Color.FromArgb(212, 237, 218), Color.FromArgb(21, 87, 36));
                lblGoogleStatus.Text = "✓ Verbunden";
                lblGoogleEmail.Text = $"Angemeldet als: {_googleAuth.UserEmail ?? "Unbekannt"}";
                btnGoogleLogin.Enabled = false;
                btnGoogleLogout.Visible = true;
                break;

            case GoogleAuthStatus.TokenExpired:
                SetStatusStyle(Color.FromArgb(248, 215, 218), Color.FromArgb(114, 28, 36));
                lblGoogleStatus.Text = "✗ Token abgelaufen";
                lblGoogleEmail.Text = "Bitte erneut anmelden";
                btnGoogleLogin.Enabled = true;
                btnGoogleLogout.Visible = false;
                break;

            case GoogleAuthStatus.Error:
                SetStatusStyle(Color.FromArgb(248, 215, 218), Color.FromArgb(114, 28, 36));
                lblGoogleStatus.Text = "✗ Fehler";
                lblGoogleEmail.Text = "Verbindung fehlgeschlagen";
                btnGoogleLogin.Enabled = true;
                btnGoogleLogout.Visible = false;
                break;
        }
    }

    private void SetStatusStyle(Color bgColor, Color textColor)
    {
        pnlGoogleStatus.BackColor = bgColor;
        lblGoogleStatus.ForeColor = textColor;
    }

    // ============================================
    // Google Account - Event Handlers
    // ============================================

    private async void BtnGoogleLogin_Click(object? sender, EventArgs e)
    {
        if (_googleAuth == null) return;

        btnGoogleLogin.Enabled = false;
        UpdateGoogleStatusUI();

        var success = await _googleAuth.LoginAsync();

        UpdateGoogleStatusUI();

        if (success)
        {
            MessageBox.Show(
                $"Erfolgreich angemeldet als {_googleAuth.UserEmail}",
                "Google Anmeldung",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else if (_googleAuth.Status != GoogleAuthStatus.Connected)
        {
            MessageBox.Show(
                "Anmeldung fehlgeschlagen. Bitte versuche es erneut.",
                "Google Anmeldung",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }

    private void BtnGoogleLogout_Click(object? sender, EventArgs e)
    {
        if (_googleAuth == null) return;

        var result = MessageBox.Show(
            "Möchtest du dich wirklich abmelden?",
            "Google Abmeldung",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result == DialogResult.Yes)
        {
            _googleAuth.Logout();
            UpdateGoogleStatusUI();
        }
    }

    private void BtnOpenCredentialsFolder_Click(object? sender, EventArgs e)
    {
        if (_googleAuth == null) return;

        var folder = Path.GetDirectoryName(_googleAuth.ClientSecretsPath);
        if (folder != null)
        {
            Directory.CreateDirectory(folder);
            Process.Start("explorer.exe", folder);
        }
    }
}
