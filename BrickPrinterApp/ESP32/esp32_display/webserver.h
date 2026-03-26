#ifndef WEBSERVER_H
#define WEBSERVER_H

#include <Arduino.h>
#include "display.h"

#define SERVER_PORT 80

void serverInit();
bool serverHasNewData(uint8_t screenId);
bool serverHasContent(uint8_t screenId);  // Check if screen has any content (for /ping)
const uint8_t* serverGetDisplayBuffer(uint8_t screenId);
void serverClearNewDataFlag(uint8_t screenId);
void serverClearDisplayBuffer(uint8_t screenId);
bool serverHasFirstContact();

#endif
