#ifndef CONFIG_H
#define CONFIG_H

#include <Arduino.h>

// Initialize configuration system
void configInit();

// Check if WiFi credentials are stored
bool configHasWiFi();

// Get stored WiFi credentials
String configGetSSID();
String configGetPassword();

// Save WiFi credentials
void configSetWiFi(const String& ssid, const String& password);

// Clear all stored configuration
void configClear();

// Process serial commands (call in loop)
// Returns true if WiFi config was changed
bool configProcessSerial();

// Print help to serial
void configPrintHelp();

#endif
