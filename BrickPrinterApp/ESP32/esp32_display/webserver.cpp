#include "webserver.h"
#include "display.h"
#include "lua_runtime.h"
#include <WiFi.h>
#include <ESPAsyncWebServer.h>

static AsyncWebServer server(SERVER_PORT);

static uint8_t displayBuffers[NUM_DISPLAYS][DISPLAY_BUFFER_SIZE];
static volatile bool newDataAvailable[NUM_DISPLAYS] = {false, false, false};
static volatile bool firstContactEstablished = false;
static volatile unsigned long lastContactTime = 0;
static uint8_t tempBuffer[DISPLAY_BUFFER_SIZE];
static size_t tempBufferIndex = 0;
static uint8_t pendingScreenId = 0;
static char luaScriptBuffer[4096];
static size_t luaScriptIndex = 0;
static uint8_t luaPendingScreenId = 0;

static void handleStatus(AsyncWebServerRequest *request);
static void handlePing(AsyncWebServerRequest *request);
static void handleClear(AsyncWebServerRequest *request);
static void handleUploadComplete(AsyncWebServerRequest *request);
static void handleUploadBody(AsyncWebServerRequest *request, uint8_t *data, size_t len, size_t index, size_t total);
static void handleLuaComplete(AsyncWebServerRequest *request);
static void handleLuaStop(AsyncWebServerRequest *request);

void serverInit() {
    server.on("/status", HTTP_GET, handleStatus);
    server.on("/ping", HTTP_GET, handlePing);
    server.on("/clear", HTTP_POST, handleClear);
    server.on("/upload", HTTP_POST, handleUploadComplete, NULL, handleUploadBody);
    server.on("/lua", HTTP_POST, handleLuaComplete);
    server.on("/lua/stop", HTTP_POST, handleLuaStop);
    DefaultHeaders::Instance().addHeader("Connection", "keep-alive");
    server.begin();
}

bool serverHasNewData(uint8_t screenId) {
    if (screenId >= NUM_DISPLAYS) return false;
    return newDataAvailable[screenId];
}

const uint8_t* serverGetDisplayBuffer(uint8_t screenId) {
    if (screenId >= NUM_DISPLAYS) return nullptr;
    return displayBuffers[screenId];
}

void serverClearNewDataFlag(uint8_t screenId) {
    if (screenId < NUM_DISPLAYS) {
        newDataAvailable[screenId] = false;
    }
}

void serverClearDisplayBuffer(uint8_t screenId) {
    if (screenId < NUM_DISPLAYS) {
        memset(displayBuffers[screenId], 0, DISPLAY_BUFFER_SIZE);
        newDataAvailable[screenId] = true;
    }
}

bool serverHasFirstContact() {
    return firstContactEstablished;
}

static void updateContactTime() {
    lastContactTime = millis();
    firstContactEstablished = true;
}

static void handlePing(AsyncWebServerRequest *request) {
    updateContactTime();

    // Return screen status for smart recovery
    String json = "{\"screens\":[";
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (i > 0) json += ",";
        bool hasContent = luaHasScript(i) || newDataAvailable[i];
        json += "{\"id\":" + String(i) + ",\"active\":" + (hasContent ? "true" : "false") + "}";
    }
    json += "]}";

    request->send(200, "application/json", json);
}

static void handleStatus(AsyncWebServerRequest *request) {
    updateContactTime();
    uint8_t screenId = request->hasParam("screen") ? request->getParam("screen")->value().toInt() : 0;
    uint8_t ss = displayCheckAllScreens();

    String j = "{\"ip\":\"" + WiFi.localIP().toString() + "\",\"rssi\":" + String(WiFi.RSSI());
    j += ",\"heap\":" + String(ESP.getFreeHeap());
    if (screenId < NUM_DISPLAYS) j += ",\"lua\":" + String(luaHasScript(screenId) ? 1 : 0);
    j += ",\"ok\":" + String(ss == 0x07 ? 1 : 0) + "}";
    request->send(200, "application/json", j);
}

static void handleClear(AsyncWebServerRequest *request) {
    updateContactTime();
    if (request->hasParam("screen")) {
        uint8_t screenId = request->getParam("screen")->value().toInt();
        if (!displayIsValidScreen(screenId)) {
            request->send(400, "text/plain", "err");
            return;
        }
        luaStopScript(screenId);
        serverClearDisplayBuffer(screenId);
    } else {
        for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
            luaStopScript(i);
            serverClearDisplayBuffer(i);
        }
    }
    request->send(200, "text/plain", "ok");
}

static void handleUploadComplete(AsyncWebServerRequest *request) {
    if (tempBufferIndex != DISPLAY_BUFFER_SIZE || !displayIsValidScreen(pendingScreenId)) {
        tempBufferIndex = 0;
        request->send(400, "text/plain", "err");
        return;
    }

    updateContactTime();
    luaStopScript(pendingScreenId, false);
    memcpy(displayBuffers[pendingScreenId], tempBuffer, DISPLAY_BUFFER_SIZE);
    newDataAvailable[pendingScreenId] = true;
    tempBufferIndex = 0;
    request->send(200, "text/plain", "ok");
}

static void handleUploadBody(AsyncWebServerRequest *request, uint8_t *data,
                             size_t len, size_t index, size_t total) {
    if (index == 0) {
        tempBufferIndex = 0;
        pendingScreenId = request->hasParam("screen") ? request->getParam("screen")->value().toInt() : 0;
    }
    for (size_t i = 0; i < len && tempBufferIndex < DISPLAY_BUFFER_SIZE; i++) {
        tempBuffer[tempBufferIndex++] = data[i];
    }
}

static void handleLuaComplete(AsyncWebServerRequest *request) {
    luaPendingScreenId = request->hasParam("screen") ? request->getParam("screen")->value().toInt() : 0;

    if (request->hasParam("script", true)) {
        String scriptBody = request->getParam("script", true)->value();
        luaScriptIndex = min(scriptBody.length(), sizeof(luaScriptBuffer) - 1);
        memcpy(luaScriptBuffer, scriptBody.c_str(), luaScriptIndex);
        luaScriptBuffer[luaScriptIndex] = '\0';
    }

    if (luaScriptIndex == 0 || !displayIsValidScreen(luaPendingScreenId)) {
        luaScriptIndex = 0;
        request->send(400, "text/plain", "err");
        return;
    }

    updateContactTime();

    // Queue the script instead of executing it directly
    // This prevents blocking the async_tcp task and causing watchdog timeout
    luaQueueScript(luaPendingScreenId, luaScriptBuffer);
    luaScriptIndex = 0;

    // Clear the binary data flag since Lua script will be in control
    newDataAvailable[luaPendingScreenId] = false;
    request->send(200, "text/plain", "ok");
}

static void handleLuaStop(AsyncWebServerRequest *request) {
    updateContactTime();
    uint8_t screenId = request->hasParam("screen") ? request->getParam("screen")->value().toInt() : 0;
    if (!displayIsValidScreen(screenId)) {
        request->send(400, "text/plain", "err");
        return;
    }
    luaStopScript(screenId);
    request->send(200, "text/plain", "ok");
}
