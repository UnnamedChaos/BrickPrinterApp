#include "config.h"
#include <Preferences.h>

static Preferences prefs;
static String serialBuffer = "";

// Preferences keys
static const char* PREF_NAMESPACE = "wifi";
static const char* KEY_SSID = "ssid";
static const char* KEY_PASSWORD = "pass";
static const char* KEY_CONFIGURED = "configured";

void configInit() {
    prefs.begin(PREF_NAMESPACE, false);
    Serial.println("Config system initialized");
}

bool configHasWiFi() {
    return prefs.getBool(KEY_CONFIGURED, false);
}

String configGetSSID() {
    return prefs.getString(KEY_SSID, "");
}

String configGetPassword() {
    return prefs.getString(KEY_PASSWORD, "");
}

void configSetWiFi(const String& ssid, const String& password) {
    prefs.putString(KEY_SSID, ssid);
    prefs.putString(KEY_PASSWORD, password);
    prefs.putBool(KEY_CONFIGURED, true);
    Serial.println("WiFi credentials saved");
}

void configClear() {
    prefs.clear();
    Serial.println("Configuration cleared");
}

void configPrintHelp() {
    Serial.println();
    Serial.println("=== ESP32 Display Configuration ===");
    Serial.println("Commands:");
    Serial.println("  WIFI:<ssid>:<password>  - Set WiFi credentials");
    Serial.println("  STATUS                  - Show current config");
    Serial.println("  CLEAR                   - Clear stored config");
    Serial.println("  REBOOT                  - Restart ESP32");
    Serial.println("  HELP                    - Show this help");
    Serial.println();
    Serial.println("Example: WIFI:MyNetwork:MyPassword123");
    Serial.println("===================================");
    Serial.println();
}

static void processCommand(const String& command) {
    String cmd = command;
    cmd.trim();

    if (cmd.length() == 0) {
        return;
    }

    Serial.print("> ");
    Serial.println(cmd);

    if (cmd.startsWith("WIFI:")) {
        // Parse WIFI:ssid:password
        String params = cmd.substring(5);
        int colonPos = params.indexOf(':');

        if (colonPos == -1) {
            Serial.println("Error: Invalid format. Use WIFI:<ssid>:<password>");
            return;
        }

        String ssid = params.substring(0, colonPos);
        String password = params.substring(colonPos + 1);

        if (ssid.length() == 0) {
            Serial.println("Error: SSID cannot be empty");
            return;
        }

        configSetWiFi(ssid, password);
        Serial.print("SSID set to: ");
        Serial.println(ssid);
        Serial.println("Password set (hidden)");
        Serial.println("Reboot to apply changes (type REBOOT)");
    }
    else if (cmd.equalsIgnoreCase("STATUS")) {
        Serial.println("--- Current Configuration ---");
        if (configHasWiFi()) {
            Serial.print("SSID: ");
            Serial.println(configGetSSID());
            Serial.println("Password: (hidden)");
        } else {
            Serial.println("WiFi: Not configured");
        }
        Serial.println("-----------------------------");
    }
    else if (cmd.equalsIgnoreCase("CLEAR")) {
        configClear();
        Serial.println("Reboot to apply changes (type REBOOT)");
    }
    else if (cmd.equalsIgnoreCase("REBOOT")) {
        Serial.println("Rebooting...");
        delay(500);
        ESP.restart();
    }
    else if (cmd.equalsIgnoreCase("HELP")) {
        configPrintHelp();
    }
    else {
        Serial.print("Unknown command: ");
        Serial.println(cmd);
        Serial.println("Type HELP for available commands");
    }
}

bool configProcessSerial() {
    bool configChanged = false;

    while (Serial.available()) {
        char c = Serial.read();

        if (c == '\n' || c == '\r') {
            if (serialBuffer.length() > 0) {
                if (serialBuffer.startsWith("WIFI:")) {
                    configChanged = true;
                }
                processCommand(serialBuffer);
                serialBuffer = "";
            }
        } else {
            serialBuffer += c;

            // Prevent buffer overflow
            if (serialBuffer.length() > 256) {
                serialBuffer = "";
                Serial.println("Error: Command too long");
            }
        }
    }

    return configChanged;
}
