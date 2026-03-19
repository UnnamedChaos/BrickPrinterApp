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

// Recovery system
// Check if server IP has been saved
bool serverHasServerIP();

// Get the saved server IP
String serverGetServerIP();

// Check if a screen has content (Lua script or display data received)
bool serverScreenHasContent(uint8_t screenId);

// Request widget recovery from server for a specific screen
// Returns true if request was sent successfully
bool serverRequestRecovery(uint8_t screenId);

// Request recovery for all empty screens
void serverRequestRecoveryForEmptyScreens();

// Mark a screen as having received content
void serverMarkScreenContent(uint8_t screenId);

// Get time since last content was received for a screen (ms)
unsigned long serverGetScreenIdleTime(uint8_t screenId);

#endif
