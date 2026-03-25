using BrickPrinterApp.Services;
using MaterialSkin;
using MaterialSkin.Controls;

namespace BrickPrinterApp;

public partial class SettingsForm
{
    // ============================================
    // Time Settings - Tab Fields
    // ============================================

    private NumericUpDown numTimeOffset = null!;
    private MaterialButton btnSaveTimeOffset = null!;

    // ============================================
    // Time Settings - Tab Initialization
    // ============================================

    private void InitializeTimeTab(TabPage tab)
    {
        // Header info
        var lblInfo = new MaterialLabel
        {
            Text = "Zeitverschiebung für alle Uhr-Widgets (z.B. Winter-/Sommerzeit):",
            Location = new Point(20, 30),
            AutoSize = true,
            FontType = MaterialSkinManager.fontType.H6
        };

        // Offset label
        var lblOffset = new MaterialLabel
        {
            Text = "Stunden Verschiebung:",
            Location = new Point(20, 80),
            AutoSize = true
        };

        // Numeric input for offset
        numTimeOffset = new NumericUpDown
        {
            Location = new Point(200, 75),
            Width = 100,
            Minimum = -12,
            Maximum = 12,
            Value = 0
        };

        // Examples
        var lblExamples = new MaterialLabel
        {
            Text = "+1 = Eine Stunde vorwärts (Sommerzeit)\n" +
                   " 0 = Keine Verschiebung\n" +
                   "-1 = Eine Stunde zurück (Winterzeit)",
            Location = new Point(40, 120),
            Size = new Size(500, 80),
            FontType = MaterialSkinManager.fontType.Body1
        };

        // Save button
        btnSaveTimeOffset = new MaterialButton
        {
            Text = "Speichern",
            Location = new Point(20, 210),
            Width = 150,
            Height = 36,
            Type = MaterialButton.MaterialButtonType.Contained
        };
        btnSaveTimeOffset.Click += BtnSaveTimeOffset_Click;

        // Note about when changes take effect
        var lblNote = new MaterialLabel
        {
            Text = "Hinweis: Die Änderung wird beim nächsten Update der Widgets wirksam.\n" +
                   "Verwenden Sie 'Jetzt Updaten' im Tray-Menü, um sofort zu aktualisieren.",
            Location = new Point(20, 270),
            Size = new Size(600, 50),
            FontType = MaterialSkinManager.fontType.Caption
        };

        // Add all controls to tab
        tab.Controls.Add(lblInfo);
        tab.Controls.Add(lblOffset);
        tab.Controls.Add(numTimeOffset);
        tab.Controls.Add(lblExamples);
        tab.Controls.Add(btnSaveTimeOffset);
        tab.Controls.Add(lblNote);
    }

    // ============================================
    // Time Settings - Event Handlers
    // ============================================

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
}
