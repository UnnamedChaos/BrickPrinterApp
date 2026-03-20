#ifndef LUA_RUNTIME_H
#define LUA_RUNTIME_H

#include <Arduino.h>
#include "display.h"

void luaInit();
bool luaLoadScript(uint8_t screenId, const char* script);
void luaQueueScript(uint8_t screenId, const char* script);
bool luaIsQueueProcessing();
void luaStopScript(uint8_t screenId, bool clearDisplay = true);
bool luaHasScript(uint8_t screenId);
void luaTick();
const char* luaGetLastError();

#endif
