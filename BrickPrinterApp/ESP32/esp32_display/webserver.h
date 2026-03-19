#ifndef WEBSERVER_H
#define WEBSERVER_H

#include <Arduino.h>
#include "display.h"

#define SERVER_PORT 80

void serverInit();
bool serverHasNewData(uint8_t screenId);
const uint8_t* serverGetDisplayBuffer(uint8_t screenId);
void serverClearNewDataFlag(uint8_t screenId);
void serverClearDisplayBuffer(uint8_t screenId);
bool serverHasFirstContact();
unsigned long serverGetLastContactAge();
void serverRequestRecoveryForEmptyScreens();

#endif
