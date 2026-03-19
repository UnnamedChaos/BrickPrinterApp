#ifndef LUA_RUNTIME_H
#define LUA_RUNTIME_H

#include <Arduino.h>
#include "display.h"

// Initialize Lua runtime
void luaInit();

// Load and execute a Lua script for a specific screen
// Returns true if script was loaded successfully
bool luaLoadScript(uint8_t screenId, const char* script);

// Stop script execution for a specific screen
// clearDisplay: if true, clears the display after stopping (default: true for backward compatibility)
void luaStopScript(uint8_t screenId, bool clearDisplay = true);

// Check if a script is running on a screen
bool luaHasScript(uint8_t screenId);

// Execute one tick of all running scripts
// Should be called from main loop
void luaTick();

// Get last error message
const char* luaGetLastError();

#endif
