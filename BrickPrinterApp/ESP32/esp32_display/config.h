#ifndef CONFIG_H
#define CONFIG_H

#include <Arduino.h>

#define MAX_DISPLAYS 3

struct DisplayConfig {
    uint8_t numDisplays;
    uint8_t sdaPins[MAX_DISPLAYS];
    uint8_t sclPins[MAX_DISPLAYS];
};

void configInit();
bool configHasWiFi();
String configGetSSID();
String configGetPassword();
void configSetWiFi(const String& ssid, const String& password);
void configClear();
bool configProcessSerial();
void configPrintHelp();

DisplayConfig configGetDisplayConfig();
void configSetDisplayConfig(uint8_t numDisplays, const uint8_t* sdaPins, const uint8_t* sclPins);
bool configHasDisplayConfig();

#endif
