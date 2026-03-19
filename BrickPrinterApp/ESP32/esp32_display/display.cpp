#include "display.h"
#include <Wire.h>
#include <Adafruit_GFX.h>
#include <Adafruit_SSD1306.h>

// All displays share the same Wire instance, we switch pins before each access
static Adafruit_SSD1306 display0(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);
static Adafruit_SSD1306 display1(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);
static Adafruit_SSD1306 display2(SCREEN_WIDTH, SCREEN_HEIGHT, &Wire, OLED_RESET);

// Array for easy access by index
static Adafruit_SSD1306* displays[NUM_DISPLAYS] = {&display0, &display1, &display2};

// Pin configurations
static const uint8_t sdaPins[NUM_DISPLAYS] = {SDA_PIN_0, SDA_PIN_1, SDA_PIN_2};
static const uint8_t sclPins[NUM_DISPLAYS] = {SCL_PIN_0, SCL_PIN_1, SCL_PIN_2};

// Track which displays initialized successfully
static bool displayInitialized[NUM_DISPLAYS] = {false, false, false};

// Screen 0 uses strapping pins (GPIO 2/3) - needs special handling
static bool isStrappingPinScreen(uint8_t screenId) {
    return (sdaPins[screenId] == 2 || sdaPins[screenId] == 3 ||
            sclPins[screenId] == 2 || sclPins[screenId] == 3);
}

// Switch I2C pins
static void switchBus(uint8_t screenId) {
    Wire.end();
    delay(1);
    Wire.begin(sdaPins[screenId], sclPins[screenId]);
}

bool displayIsValidScreen(uint8_t screenId) {
    return screenId < NUM_DISPLAYS && displayInitialized[screenId];
}

bool displayInit() {
    bool anySuccess = false;

    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        switchBus(i);

        if (displays[i]->begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS)) {
            displays[i]->clearDisplay();
            displays[i]->setTextSize(1);
            displays[i]->setTextColor(SSD1306_WHITE, SSD1306_BLACK);
            displays[i]->display();
            displayInitialized[i] = true;
            anySuccess = true;
            Serial.printf("Display %d initialized (SDA=%d, SCL=%d)\n", i, sdaPins[i], sclPins[i]);
        } else {
            Serial.printf("Display %d failed! (SDA=%d, SCL=%d)\n", i, sdaPins[i], sclPins[i]);
        }
    }

    return anySuccess;
}

void displayShowMessage(uint8_t screenId, const char* line1, const char* line2,
                        const char* line3, const char* line4,
                        const char* line5, const char* line6) {
    if (!displayIsValidScreen(screenId)) return;

    switchBus(screenId);

    // Re-init strapping pin screens to ensure clean state
    if (isStrappingPinScreen(screenId)) {
        displays[screenId]->begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS);
    }

    displays[screenId]->clearDisplay();
    displays[screenId]->setCursor(0, 0);

    if (line1) displays[screenId]->println(line1);
    if (line2) displays[screenId]->println(line2);
    if (line3) displays[screenId]->println(line3);
    if (line4) displays[screenId]->println(line4);
    if (line5) displays[screenId]->println(line5);
    if (line6) displays[screenId]->println(line6);

    displays[screenId]->display();
}

void displayShowMessageAll(const char* line1, const char* line2,
                           const char* line3, const char* line4,
                           const char* line5, const char* line6) {
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        displayShowMessage(i, line1, line2, line3, line4, line5, line6);
    }
}

void displayShowIP(const char* ip) {
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (!displayIsValidScreen(i)) continue;

        switchBus(i);

        // Re-init strapping pin screens to ensure clean state
        if (isStrappingPinScreen(i)) {
            displays[i]->begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS);
        }

        displays[i]->clearDisplay();
        displays[i]->setCursor(0, 0);
        displays[i]->print("Screen ");
        displays[i]->println(i);
        displays[i]->println("WiFi Connected!");
        displays[i]->print("IP: ");
        displays[i]->println(ip);
        displays[i]->println();
        displays[i]->println("POST /upload?screen=X");
        displays[i]->display();
    }
}

void displayUpdate(uint8_t screenId, const uint8_t* buffer) {
    if (!displayIsValidScreen(screenId)) {
        Serial.printf("Invalid screen ID: %d\n", screenId);
        return;
    }

    switchBus(screenId);

    // Re-init strapping pin screens to ensure clean state
    if (isStrappingPinScreen(screenId)) {
        displays[screenId]->begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS);
    }

    // Data is in SSD1306 page format:
    // 8 pages of 128 bytes each
    // Each byte = 8 vertical pixels (bit 0 = top, bit 7 = bottom)
    // Draw both white AND black pixels to avoid flicker (no clearDisplay needed)

    for (int page = 0; page < 8; page++) {
        for (int x = 0; x < SCREEN_WIDTH; x++) {
            uint8_t columnByte = buffer[page * SCREEN_WIDTH + x];

            for (int bit = 0; bit < 8; bit++) {
                int y = page * 8 + bit;
                displays[screenId]->drawPixel(x, y,
                    (columnByte & (1 << bit)) ? SSD1306_WHITE : SSD1306_BLACK);
            }
        }
    }

    displays[screenId]->display();
    Serial.printf("Display %d updated\n", screenId);
}

void displayClear(uint8_t screenId) {
    if (!displayIsValidScreen(screenId)) return;

    switchBus(screenId);
    displays[screenId]->clearDisplay();
    displays[screenId]->display();
}

void displayClearAll() {
    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        displayClear(i);
    }
}

bool displayCheckScreen(uint8_t screenId) {
    if (screenId >= NUM_DISPLAYS) return false;

    switchBus(screenId);

    // Try to communicate with the display via I2C
    Wire.beginTransmission(SCREEN_ADDRESS);
    uint8_t error = Wire.endTransmission();

    // Re-initialize display after raw I2C check to restore state
    if (error == 0 && displayInitialized[screenId]) {
        displays[screenId]->begin(SSD1306_SWITCHCAPVCC, SCREEN_ADDRESS);
    }

    // 0 = success, other = error
    return (error == 0);
}

uint8_t displayCheckAllScreens() {
    uint8_t result = 0;

    for (uint8_t i = 0; i < NUM_DISPLAYS; i++) {
        if (displayCheckScreen(i)) {
            result |= (1 << i);
        }
    }

    return result;
}

// ============ Lua Drawing Functions ============

// Track which screen is currently selected for Lua drawing
static uint8_t currentLuaScreen = 255;

// Switch to screen for Lua drawing (just switch I2C bus, no begin())
static void selectLuaScreen(uint8_t screenId) {
    if (currentLuaScreen != screenId) {
        switchBus(screenId);
        currentLuaScreen = screenId;
    }
}

void displayClearBuffer(uint8_t screenId) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    displays[screenId]->clearDisplay();
}

void displayDrawText(uint8_t screenId, int x, int y, const char* text) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    displays[screenId]->setTextColor(SSD1306_WHITE, SSD1306_BLACK);
    displays[screenId]->setCursor(x, y);
    displays[screenId]->print(text);
}

void displaySetFontSize(uint8_t screenId, int size) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    displays[screenId]->setTextSize(size > 0 ? size : 1);
}

void displayDrawPixel(uint8_t screenId, int x, int y, bool on) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    displays[screenId]->drawPixel(x, y, on ? SSD1306_WHITE : SSD1306_BLACK);
}

void displayDrawRect(uint8_t screenId, int x, int y, int w, int h, bool filled) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    if (filled) {
        displays[screenId]->fillRect(x, y, w, h, SSD1306_WHITE);
    } else {
        displays[screenId]->drawRect(x, y, w, h, SSD1306_WHITE);
    }
}

void displayDrawLine(uint8_t screenId, int x1, int y1, int x2, int y2) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    displays[screenId]->drawLine(x1, y1, x2, y2, SSD1306_WHITE);
}

void displayDrawCircle(uint8_t screenId, int x, int y, int r, bool filled) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    if (filled) {
        displays[screenId]->fillCircle(x, y, r, SSD1306_WHITE);
    } else {
        displays[screenId]->drawCircle(x, y, r, SSD1306_WHITE);
    }
}

void displayRefresh(uint8_t screenId) {
    if (!displayIsValidScreen(screenId)) return;
    selectLuaScreen(screenId);
    displays[screenId]->display();
}
