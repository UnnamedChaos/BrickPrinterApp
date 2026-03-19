#ifndef CONFIG_H
#define CONFIG_H

#include <Arduino.h>

void configInit();
bool configHasWiFi();
String configGetSSID();
String configGetPassword();
void configSetWiFi(const String& ssid, const String& password);
void configClear();
bool configProcessSerial();
void configPrintHelp();

#endif
