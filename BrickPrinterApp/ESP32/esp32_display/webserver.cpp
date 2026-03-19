#include "webserver.h"
#include "display.h"
#include "lua_runtime.h"
#include <WiFi.h>
#include <ESPAsyncWebServer.h>
#include <HTTPClient.h>

static AsyncWebServer server(SERVER_PORT);

static uint8_t displayBuffers[NUM_DISPLAYS][DISPLAY_BUFFER_SIZE];
static volatile bool newDataAvailable[NUM_DISPLAYS] = {false, false, false};
static volatile bool firstContactEstablished = false;
static volatile unsigned long lastContactTime = 0;
static String serverIP = "";
static volatile bool hasServerIP = false;
static volatile unsigned long screenContentTime[NUM_DISPLAYS] = {0, 0, 0};
static volatile bool screenHasContent[NUM_DISPLAYS] = {false, false, false};
static volatile unsigned long lastRecoveryAttempt[NUM_DISPLAYS] = {0, 0, 0};
static const unsigned long RECOVERY_COOLDOWN_MS = 30000;
static const unsigned long SCREEN_IDLE_THRESHOLD_MS = 10000;
static uint8_t tempBuffer[DISPLAY_BUFFER_SIZE];
static size_t tempBufferIndex = 0;
static uint8_t pendingScreenId = 0;
static char luaScriptBuffer[4096];
static size_t luaScriptIndex = 0;
static uint8_t luaPendingScreenId = 0;

static void handleRoot(AsyncWebServerRequest *request);
static void handleStatus(AsyncWebServerRequest *request);
static void handlePing(AsyncWebServerRequest *request);
static void handleClear(AsyncWebServerRequest *request);
static void handleUploadComplete(AsyncWebServerRequest *request);
static void handleUploadBody(AsyncWebServerRequest *request, uint8_t *data, size_t len, size_t index, size_t total);
static void handleLuaComplete(AsyncWebServerRequest *request);
static void handleLuaStop(AsyncWebServerRequest *request);
static void serverMarkScreenContent(uint8_t screenId);

void serverInit() {
    server.on("/", HTTP_GET, handleRoot);
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

unsigned long serverGetLastContactAge() {
    if (lastContactTime == 0) return ULONG_MAX;
    return millis() - lastContactTime;
}

static void updateContactTime() {
    lastContactTime = millis();
    firstContactEstablished = true;
}

static void handleRoot(AsyncWebServerRequest *request) {
    request->send(200, "text/plain", "ESP32 Display");
}

static void handlePing(AsyncWebServerRequest *request) {
    updateContactTime();
    serverIP = request->client()->remoteIP().toString();
    hasServerIP = true;
    request->send(200, "text/plain", "ok");
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
    serverIP = request->client()->remoteIP().toString();
    hasServerIP = true;
    luaStopScript(pendingScreenId, false);
    memcpy(displayBuffers[pendingScreenId], tempBuffer, DISPLAY_BUFFER_SIZE);
    newDataAvailable[pendingScreenId] = true;
    tempBufferIndex = 0;
    serverMarkScreenContent(pendingScreenId);
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
    serverIP = request->client()->remoteIP().toString();
    hasServerIP = true;

    bool success = luaLoadScript(luaPendingScreenId, luaScriptBuffer);
    luaScriptIndex = 0;

    if (success) {
        serverMarkScreenContent(luaPendingScreenId);
        request->send(200, "text/plain", "ok");
    } else {
        request->send(400, "text/plain", luaGetLastError());
    }
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

static void serverMarkScreenContent(uint8_t screenId) {
    if (screenId < NUM_DISPLAYS) {
        screenContentTime[screenId] = millis();
        screenHasContent[screenId] = true;
    }
}

static bool serverRequestRecovery(uint8_t screenId) {
    if (!hasServerIP || screenId >= NUM_DISPLAYS) return false;

    unsigned long now = millis();
    if (now - lastRecoveryAttempt[screenId] < RECOVERY_COOLDOWN_MS) return false;
    lastRecoveryAttempt[screenId] = now;

    HTTPClient http;
    http.begin("http://" + serverIP + ":5225/recovery?screen=" + String(screenId));
    http.setTimeout(1000);
    int code = http.GET();
    http.end();
    yield();
    return (code == HTTP_CODE_OK);
}

void serverRequestRecoveryForEmptyScreens() {
    if (!hasServerIP || !firstContactEstablished) return;
    if (serverGetLastContactAge() < 5000) return;

    unsigned long now = millis();
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        bool hasContent = luaHasScript(i) || (screenHasContent[i] && (now - screenContentTime[i] < SCREEN_IDLE_THRESHOLD_MS));
        if (!hasContent) serverRequestRecovery(i);
    }
}
