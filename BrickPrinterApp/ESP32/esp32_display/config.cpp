#include "config.h"
#include <Preferences.h>

static Preferences prefs;
static String serialBuffer = "";
static const char* PREF_NAMESPACE = "wifi";
static const char* KEY_SSID = "ssid";
static const char* KEY_PASSWORD = "pass";
static const char* KEY_CONFIGURED = "configured";

void configInit() {
    prefs.begin(PREF_NAMESPACE, false);
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
}

void configClear() {
    prefs.clear();
}

void configPrintHelp() {
    Serial.println("WIFI:<ssid>:<pass> | STATUS | CLEAR | REBOOT");
}

static void processCommand(const String& command) {
    String cmd = command;
    cmd.trim();
    if (cmd.length() == 0) return;

    if (cmd.startsWith("WIFI:")) {
        String params = cmd.substring(5);
        int colonPos = params.indexOf(':');
        if (colonPos == -1 || colonPos == 0) return;
        configSetWiFi(params.substring(0, colonPos), params.substring(colonPos + 1));
        Serial.println("OK - REBOOT");
    }
    else if (cmd.equalsIgnoreCase("STATUS")) {
        Serial.println(configHasWiFi() ? configGetSSID() : "Not set");
    }
    else if (cmd.equalsIgnoreCase("CLEAR")) {
        configClear();
        Serial.println("OK - REBOOT");
    }
    else if (cmd.equalsIgnoreCase("REBOOT")) {
        ESP.restart();
    }
    else if (cmd.equalsIgnoreCase("HELP")) {
        configPrintHelp();
    }
}

bool configProcessSerial() {
    bool configChanged = false;
    while (Serial.available()) {
        char c = Serial.read();
        if (c == '\n' || c == '\r') {
            if (serialBuffer.length() > 0) {
                configChanged = serialBuffer.startsWith("WIFI:");
                processCommand(serialBuffer);
                serialBuffer = "";
            }
        } else if (serialBuffer.length() < 256) {
            serialBuffer += c;
        }
    }
    return configChanged;
}
