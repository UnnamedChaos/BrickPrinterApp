/*
 * ESP32 Multi-Display Receiver
 *
 * Receives 1024 bytes of binary display data via HTTP POST
 * and shows it on 3x 128x64 SSD1306 I2C OLED displays.
 *
 * Features:
 * - 3 independent displays on different I2C pins
 * - API accepts ?screen=X parameter to target specific display
 * - Shows IP address on all displays until first contact
 * - Keep-alive support for fast subsequent transfers
 * - WiFi credentials configurable via USB serial
 *
 * Wiring:
 * - Display 0: SDA=21, SCL=22
 * - Display 1: SDA=17, SCL=16
 * - Display 2: SDA=32, SCL=33
 * - All displays: VCC=3.3V, GND=GND
 *
 * Serial Commands (115200 baud):
 * - WIFI:ssid:password  - Set WiFi credentials
 * - STATUS              - Show current config
 * - CLEAR               - Clear stored config
 * - REBOOT              - Restart ESP32
 * - HELP                - Show help
 */

#include <WiFi.h>
#include "display.h"
#include "webserver.h"
#include "config.h"

// State tracking
bool showingIPScreen = true;
bool wifiConnected = false;

void setup() {
    Serial.begin(115200);
    Serial.println("\n\nESP32 Multi-Display Receiver Starting...");

    // Initialize config system
    configInit();

    // Initialize displays
    if (!displayInit()) {
        Serial.println("Display init failed!");
        while (true) delay(1000);
    }

    // Check if WiFi is configured
    if (!configHasWiFi()) {
        Serial.println("\n*** WiFi not configured ***");
        configPrintHelp();

        displayShowMessageAll(
            "WiFi not configured",
            "",
            "Connect via USB",
            "115200 baud",
            "",
            "Type HELP"
        );

        // Wait for serial configuration
        while (!configHasWiFi()) {
            configProcessSerial();
            delay(100);
        }
    }

    // Get stored credentials
    String ssid = configGetSSID();
    String password = configGetPassword();

    displayShowMessageAll("Connecting WiFi...", "", ssid.c_str());

    // Connect to WiFi
    WiFi.begin(ssid.c_str(), password.c_str());
    Serial.print("Connecting to WiFi: ");
    Serial.println(ssid);

    int attempts = 0;
    while (WiFi.status() != WL_CONNECTED && attempts < 30) {
        delay(500);
        Serial.print(".");
        // Check for serial commands during connection
        configProcessSerial();
        attempts++;
    }

    if (WiFi.status() != WL_CONNECTED) {
        Serial.println("\nWiFi connection failed!");
        Serial.println("Check credentials or type CLEAR to reset");
        displayShowMessageAll(
            "WiFi Failed!",
            "",
            "Check credentials",
            "or connect via USB",
            "and type CLEAR"
        );

        // Allow serial commands to fix config
        while (WiFi.status() != WL_CONNECTED) {
            if (configProcessSerial()) {
                // Config changed, reboot to apply
                delay(500);
                ESP.restart();
            }
            delay(100);
        }
    }

    wifiConnected = true;
    Serial.println("\nWiFi connected!");
    Serial.print("IP Address: ");
    Serial.println(WiFi.localIP());

    // Show IP on display (stays until first contact)
    displayShowIP(WiFi.localIP().toString().c_str());

    // Start web server
    serverInit();

    Serial.println("Ready. Type HELP for serial commands.");
}

void loop() {
    // Process serial commands (works even while running)
    configProcessSerial();

    // Handle new display data for each screen
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (serverHasNewData(i)) {
            const uint8_t* buffer = serverGetDisplayBuffer(i);
            if (buffer != nullptr) {
                displayUpdate(i, buffer);
            }
            serverClearNewDataFlag(i);
            showingIPScreen = false;
        }
    }

    // Keep showing IP screen until first contact
    // This ensures user can always see the IP before connection
    if (showingIPScreen && serverHasFirstContact()) {
        // First contact made but no display data yet
        // Keep showing IP - will update when actual data arrives
    }

    delay(10);
}
