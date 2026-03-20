#include <WiFi.h>
#include <ArduinoOTA.h>
#include <time.h>
#include "display.h"
#include "webserver.h"
#include "config.h"
#include "lua_runtime.h"

#define NTP_SERVER "pool.ntp.org"
#define GMT_OFFSET_SEC 3600
#define DAYLIGHT_OFFSET_SEC 3600
#define OTA_HOSTNAME "ESP32_Display"

bool showingIPScreen = true;
bool wifiConnected = false;

void setupOTA() {
    ArduinoOTA.setHostname(OTA_HOSTNAME);

    ArduinoOTA.onStart([]() {
        // Stop lua scripts during OTA
        for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
            luaStopScript(i);
        }
        String type = (ArduinoOTA.getCommand() == U_FLASH) ? "firmware" : "filesystem";
        Serial.println("OTA Start: " + type);
        displayShowMessageAll("OTA Update", "", "Uploading...", "DO NOT POWER OFF");
    });

    ArduinoOTA.onEnd([]() {
        Serial.println("\nOTA Complete");
        displayShowMessageAll("OTA Update", "", "Complete!", "Rebooting...");
    });

    ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
        static uint8_t lastPercent = 0;
        uint8_t percent = (progress / (total / 100));
        if (percent != lastPercent && percent % 10 == 0) {
            lastPercent = percent;
            Serial.printf("OTA: %u%%\n", percent);
            char buf[20];
            sprintf(buf, "Progress: %d%%", percent);
            displayShowMessage(0, "OTA Update", "", buf, "DO NOT POWER OFF");
        }
    });

    ArduinoOTA.onError([](ota_error_t error) {
        const char* msg = "Unknown";
        if (error == OTA_AUTH_ERROR) msg = "Auth Failed";
        else if (error == OTA_BEGIN_ERROR) msg = "Begin Failed";
        else if (error == OTA_CONNECT_ERROR) msg = "Connect Failed";
        else if (error == OTA_RECEIVE_ERROR) msg = "Receive Failed";
        else if (error == OTA_END_ERROR) msg = "End Failed";
        Serial.printf("OTA Error: %s\n", msg);
        displayShowMessageAll("OTA Error", "", msg);
    });

    ArduinoOTA.begin();
    Serial.println("OTA ready");
}

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
    setupOTA();
    luaInit();
    displayShowIP(WiFi.localIP().toString().c_str());
    serverInit();
}

void loop() {
    ArduinoOTA.handle();
    configProcessSerial();

    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (serverHasNewData(i)) {
            const uint8_t* buffer = serverGetDisplayBuffer(i);
            if (buffer) displayUpdate(i, buffer);
            // Don't clear the flag - it should stay true to indicate this screen has content
            // Only clear when explicitly clearing the screen or when a Lua script takes over
            showingIPScreen = false;
        }
    }

    luaTick();

    yield();
    delay(10);
}
