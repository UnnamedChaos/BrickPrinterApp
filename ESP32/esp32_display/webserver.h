#ifndef WEBSERVER_H
#define WEBSERVER_H

#include <Arduino.h>
#include "display.h"

#define SERVER_PORT 80

// Initialize the web server
void serverInit();

// Check if new display data is available for any screen
bool serverHasNewData();

// Check if new display data is available for a specific screen
bool serverHasNewData(uint8_t screenId);

// Get pointer to the display buffer for a specific screen (1024 bytes)
const uint8_t* serverGetDisplayBuffer(uint8_t screenId);

// Clear the new data flag for a specific screen
void serverClearNewDataFlag(uint8_t screenId);

// Clear all new data flags
void serverClearAllNewDataFlags();

// Clear the display buffer for a specific screen
void serverClearDisplayBuffer(uint8_t screenId);

// Check if first contact has been established
bool serverHasFirstContact();

// Get milliseconds since last contact
unsigned long serverGetLastContactAge();

// Lua script management
bool serverHasLuaScript(uint8_t screenId);

#endif
