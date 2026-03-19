#include "webserver.h"
#include "display.h"
#include <WiFi.h>
#include <ESPAsyncWebServer.h>

static AsyncWebServer server(SERVER_PORT);

// Display buffers and state for each screen
static uint8_t displayBuffers[NUM_DISPLAYS][DISPLAY_BUFFER_SIZE];
static volatile bool newDataAvailable[NUM_DISPLAYS] = {false, false, false};

// Connection state
static volatile bool firstContactEstablished = false;
static volatile unsigned long lastContactTime = 0;

// Temporary buffer for receiving data
static uint8_t tempBuffer[DISPLAY_BUFFER_SIZE];
static size_t tempBufferIndex = 0;
static uint8_t pendingScreenId = 0;

// Request handlers
static void handleRoot(AsyncWebServerRequest *request);
static void handleStatus(AsyncWebServerRequest *request);
static void handlePing(AsyncWebServerRequest *request);
static void handleClear(AsyncWebServerRequest *request);
static void handleUploadComplete(AsyncWebServerRequest *request);
static void handleUploadBody(AsyncWebServerRequest *request, uint8_t *data,
                             size_t len, size_t index, size_t total);

void serverInit() {
    // Root - info page
    server.on("/", HTTP_GET, handleRoot);

    // Status endpoint
    server.on("/status", HTTP_GET, handleStatus);

    // Lightweight ping/keep-alive endpoint
    server.on("/ping", HTTP_GET, handlePing);

    // Clear endpoint
    server.on("/clear", HTTP_POST, handleClear);

    // Upload endpoint - handles binary POST data
    server.on("/upload", HTTP_POST, handleUploadComplete, NULL, handleUploadBody);

    // Enable keep-alive
    DefaultHeaders::Instance().addHeader("Connection", "keep-alive");
    DefaultHeaders::Instance().addHeader("Keep-Alive", "timeout=30, max=100");

    server.begin();
    Serial.println("HTTP server started on port " + String(SERVER_PORT));
}

bool serverHasNewData() {
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (newDataAvailable[i]) return true;
    }
    return false;
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

void serverClearAllNewDataFlags() {
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        newDataAvailable[i] = false;
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

// --- Request Handlers ---

static void handleRoot(AsyncWebServerRequest *request) {
    String html = R"(
<!DOCTYPE html>
<html>
<head><title>ESP32 Multi-Display</title></head>
<body>
    <h1>ESP32 Multi-Display Receiver</h1>
    <p>3 screens supported (0, 1, 2)</p>
    <p>POST 1024 bytes of binary data to <code>/upload?screen=X</code></p>
    <p>Screen: 128x64 pixels, SSD1306 page format</p>
    <h2>Endpoints:</h2>
    <ul>
        <li><code>POST /upload?screen=0</code> - Send display data to screen 0</li>
        <li><code>POST /upload?screen=1</code> - Send display data to screen 1</li>
        <li><code>POST /upload?screen=2</code> - Send display data to screen 2</li>
        <li><code>GET /ping</code> - Keep-alive ping (lightweight)</li>
        <li><code>GET /status</code> - Get device status</li>
        <li><code>POST /clear?screen=X</code> - Clear specific display</li>
        <li><code>POST /clear</code> - Clear all displays</li>
    </ul>
</body>
</html>
)";
    request->send(200, "text/html", html);
}

static void handlePing(AsyncWebServerRequest *request) {
    updateContactTime();
    // Minimal response for speed
    request->send(200, "text/plain", "ok");
}

static void handleStatus(AsyncWebServerRequest *request) {
    updateContactTime();

    unsigned long uptime = millis() / 1000;
    unsigned long lastContact = (lastContactTime > 0) ? (millis() - lastContactTime) / 1000 : 0;

    // Check all screens (I2C ping)
    uint8_t screenStatus = displayCheckAllScreens();

    String json = "{";
    json += "\"status\":\"ok\",";
    json += "\"ip\":\"" + WiFi.localIP().toString() + "\",";
    json += "\"rssi\":" + String(WiFi.RSSI()) + ",";
    json += "\"freeHeap\":" + String(ESP.getFreeHeap()) + ",";
    json += "\"uptime\":" + String(uptime) + ",";
    json += "\"numDisplays\":" + String(NUM_DISPLAYS) + ",";
    json += "\"screens\":[";
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (i > 0) json += ",";
        bool responding = (screenStatus & (1 << i)) != 0;
        json += "{\"id\":" + String(i);
        json += ",\"initialized\":" + String(displayIsValidScreen(i) ? "true" : "false");
        json += ",\"responding\":" + String(responding ? "true" : "false") + "}";
    }
    json += "],";
    json += "\"allScreensOk\":" + String(screenStatus == 0x07 ? "true" : "false") + ",";
    json += "\"connected\":" + String(firstContactEstablished ? "true" : "false") + ",";
    json += "\"lastContactSec\":" + String(lastContact);
    json += "}";
    request->send(200, "application/json", json);
}

static void handleClear(AsyncWebServerRequest *request) {
    updateContactTime();

    if (request->hasParam("screen")) {
        uint8_t screenId = request->getParam("screen")->value().toInt();
        if (!displayIsValidScreen(screenId)) {
            request->send(400, "application/json", "{\"error\":\"Invalid screen ID\"}");
            return;
        }
        serverClearDisplayBuffer(screenId);
        request->send(200, "application/json", "{\"message\":\"Display " + String(screenId) + " cleared\"}");
    } else {
        // Clear all displays
        for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
            serverClearDisplayBuffer(i);
        }
        request->send(200, "application/json", "{\"message\":\"All displays cleared\"}");
    }
}

static void handleUploadComplete(AsyncWebServerRequest *request) {
    Serial.printf("Upload complete for screen %d, received: %d bytes\n", pendingScreenId, tempBufferIndex);

    if (tempBufferIndex != DISPLAY_BUFFER_SIZE) {
        String error = "Expected 1024 bytes, received " + String(tempBufferIndex);
        Serial.println(error);
        request->send(400, "application/json", "{\"error\":\"" + error + "\"}");
        tempBufferIndex = 0;
        return;
    }

    if (!displayIsValidScreen(pendingScreenId)) {
        request->send(400, "application/json", "{\"error\":\"Invalid screen ID\"}");
        tempBufferIndex = 0;
        return;
    }

    updateContactTime();

    // Copy temp buffer to display buffer for the target screen
    memcpy(displayBuffers[pendingScreenId], tempBuffer, DISPLAY_BUFFER_SIZE);
    newDataAvailable[pendingScreenId] = true;
    tempBufferIndex = 0;

    Serial.printf("Display %d data received successfully\n", pendingScreenId);
    request->send(200, "application/json",
        "{\"message\":\"Image received\",\"screen\":" + String(pendingScreenId) + ",\"bytes\":1024}");
}

static void handleUploadBody(AsyncWebServerRequest *request, uint8_t *data,
                             size_t len, size_t index, size_t total) {
    // At start of new request, extract screen ID and reset buffer
    if (index == 0) {
        tempBufferIndex = 0;
        pendingScreenId = 0; // Default to screen 0

        if (request->hasParam("screen")) {
            pendingScreenId = request->getParam("screen")->value().toInt();
        }
        Serial.printf("Upload starting for screen %d, total=%d\n", pendingScreenId, total);
    }

    Serial.printf("Body: index=%d, len=%d, total=%d\n", index, len, total);

    // Copy data to temp buffer
    for (size_t i = 0; i < len && tempBufferIndex < DISPLAY_BUFFER_SIZE; i++) {
        tempBuffer[tempBufferIndex++] = data[i];
    }
}
