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

// Switch I2C pins (your working pattern)
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
            displays[i]->setTextColor(SSD1306_WHITE);
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

    displays[screenId]->clearDisplay();

    // Data is in SSD1306 page format:
    // 8 pages of 128 bytes each
    // Each byte = 8 vertical pixels (bit 0 = top, bit 7 = bottom)

    for (int page = 0; page < 8; page++) {
        for (int x = 0; x < SCREEN_WIDTH; x++) {
            uint8_t columnByte = buffer[page * SCREEN_WIDTH + x];

            for (int bit = 0; bit < 8; bit++) {
                int y = page * 8 + bit;
                if (columnByte & (1 << bit)) {
                    displays[screenId]->drawPixel(x, y, SSD1306_WHITE);
                }
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
