#include "config.h"
#include <Preferences.h>

static Preferences prefs;
static String serialBuffer = "";
static const char* PREF_NAMESPACE = "wifi";
static const char* KEY_SSID = "ssid";
static const char* KEY_PASSWORD = "pass";
static const char* KEY_CONFIGURED = "configured";
static const char* KEY_NUM_DISPLAYS = "num_disp";
static const char* KEY_DISPLAY_CONFIGURED = "disp_cfg";
static const char* KEY_SDA_PREFIX = "sda";
static const char* KEY_SCL_PREFIX = "scl";

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
    Serial.println("WIFI:<ssid>:<pass> | DISPLAY:<count>:<sda0>:<scl0>[:<sda1>:<scl1>...] | STATUS | CLEAR | REBOOT");
    Serial.println("Example 1 screen: DISPLAY:1:6:7");
    Serial.println("Example 3 screens: DISPLAY:3:10:21:4:5:8:9");
}

bool configHasDisplayConfig() {
    return prefs.getBool(KEY_DISPLAY_CONFIGURED, false);
}

DisplayConfig configGetDisplayConfig() {
    DisplayConfig config;
    config.numDisplays = prefs.getUChar(KEY_NUM_DISPLAYS, 3);

    if (config.numDisplays > MAX_DISPLAYS) {
        config.numDisplays = MAX_DISPLAYS;
    }

    for (uint8_t i = 0; i < config.numDisplays; i++) {
        String sdaKey = String(KEY_SDA_PREFIX) + String(i);
        String sclKey = String(KEY_SCL_PREFIX) + String(i);
        config.sdaPins[i] = prefs.getUChar(sdaKey.c_str(), 0);
        config.sclPins[i] = prefs.getUChar(sclKey.c_str(), 0);
    }

    return config;
}

void configSetDisplayConfig(uint8_t numDisplays, const uint8_t* sdaPins, const uint8_t* sclPins) {
    if (numDisplays > MAX_DISPLAYS) {
        numDisplays = MAX_DISPLAYS;
    }

    prefs.putUChar(KEY_NUM_DISPLAYS, numDisplays);

    for (uint8_t i = 0; i < numDisplays; i++) {
        String sdaKey = String(KEY_SDA_PREFIX) + String(i);
        String sclKey = String(KEY_SCL_PREFIX) + String(i);
        prefs.putUChar(sdaKey.c_str(), sdaPins[i]);
        prefs.putUChar(sclKey.c_str(), sclPins[i]);
    }

    prefs.putBool(KEY_DISPLAY_CONFIGURED, true);
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
    else if (cmd.startsWith("DISPLAY:")) {
        String params = cmd.substring(8);
        int values[1 + MAX_DISPLAYS * 2];
        int valueCount = 0;
        int startPos = 0;

        while (startPos < params.length() && valueCount < (1 + MAX_DISPLAYS * 2)) {
            int colonPos = params.indexOf(':', startPos);
            String token;
            if (colonPos == -1) {
                token = params.substring(startPos);
                startPos = params.length();
            } else {
                token = params.substring(startPos, colonPos);
                startPos = colonPos + 1;
            }
            values[valueCount++] = token.toInt();
        }

        if (valueCount >= 3 && valueCount % 2 == 1) {
            uint8_t numDisplays = values[0];
            if (numDisplays > 0 && numDisplays <= MAX_DISPLAYS && valueCount == (1 + numDisplays * 2)) {
                uint8_t sdaPins[MAX_DISPLAYS];
                uint8_t sclPins[MAX_DISPLAYS];
                for (uint8_t i = 0; i < numDisplays; i++) {
                    sdaPins[i] = values[1 + i * 2];
                    sclPins[i] = values[2 + i * 2];
                }
                configSetDisplayConfig(numDisplays, sdaPins, sclPins);
                Serial.println("OK - REBOOT");
                return;
            }
        }
        Serial.println("ERROR: Invalid DISPLAY format");
    }
    else if (cmd.equalsIgnoreCase("STATUS")) {
        Serial.print("WiFi: ");
        Serial.println(configHasWiFi() ? configGetSSID() : "Not set");
        if (configHasDisplayConfig()) {
            DisplayConfig dCfg = configGetDisplayConfig();
            Serial.print("Displays: ");
            Serial.println(dCfg.numDisplays);
            for (uint8_t i = 0; i < dCfg.numDisplays; i++) {
                Serial.print("  Screen ");
                Serial.print(i);
                Serial.print(": SDA=");
                Serial.print(dCfg.sdaPins[i]);
                Serial.print(" SCL=");
                Serial.println(dCfg.sclPins[i]);
            }
        } else {
            Serial.println("Displays: Using defaults");
        }
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
                configChanged = serialBuffer.startsWith("WIFI:") || serialBuffer.startsWith("DISPLAY:");
                processCommand(serialBuffer);
                serialBuffer = "";
            }
        } else if (serialBuffer.length() < 256) {
            serialBuffer += c;
        }
    }
    return configChanged;
}
