#include <WiFi.h>
#include <time.h>
#include "display.h"
#include "webserver.h"
#include "config.h"
#include "lua_runtime.h"

#define NTP_SERVER "pool.ntp.org"
#define GMT_OFFSET_SEC 3600
#define DAYLIGHT_OFFSET_SEC 3600

bool showingIPScreen = true;
bool wifiConnected = false;
unsigned long lastRecoveryCheck = 0;
const unsigned long RECOVERY_CHECK_INTERVAL = 5000;

void setup() {
    Serial.begin(115200);
    configInit();

    if (!displayInit()) {
        Serial.println("Display init failed!");
        while (true) delay(1000);
    }

    if (!configHasWiFi()) {
        configPrintHelp();

        displayShowMessageAll("WiFi not set", "", "USB 115200", "Type HELP");

        while (!configHasWiFi()) {
            configProcessSerial();
            delay(100);
        }
    }

    String ssid = configGetSSID();
    String password = configGetPassword();

    displayShowMessageAll("Connecting...", "", ssid.c_str());
    WiFi.begin(ssid.c_str(), password.c_str());

    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 30) {
        delay(500);
        configProcessSerial();
        attempts++;
    }

    if (WiFi.status() != WL_CONNECTED) {
        displayShowMessageAll("WiFi Failed!", "", "Type CLEAR");
        while (WiFi.status() != WL_CONNECTED) {
            if (configProcessSerial()) {
                delay(500);
                ESP.restart();
            }
            delay(100);
        }
    }

    wifiConnected = true;
    Serial.println(WiFi.localIP());

    configTime(GMT_OFFSET_SEC, DAYLIGHT_OFFSET_SEC, NTP_SERVER);
    luaInit();
    displayShowIP(WiFi.localIP().toString().c_str());
    serverInit();
}

void loop() {
    configProcessSerial();

    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (serverHasNewData(i)) {
            const uint8_t* buffer = serverGetDisplayBuffer(i);
            if (buffer) displayUpdate(i, buffer);
            serverClearNewDataFlag(i);
            showingIPScreen = false;
        }
    }

    luaTick();

    unsigned long now = millis();
    if (now - lastRecoveryCheck >= RECOVERY_CHECK_INTERVAL) {
        lastRecoveryCheck = now;
        serverRequestRecoveryForEmptyScreens();
    }

    yield();
    delay(10);
}
