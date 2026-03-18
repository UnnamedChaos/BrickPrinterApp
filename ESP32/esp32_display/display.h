#ifndef DISPLAY_H
#define DISPLAY_H

#include <Arduino.h>

// Display configuration
#define SCREEN_WIDTH 128
#define SCREEN_HEIGHT 64
#define OLED_RESET -1
#define SCREEN_ADDRESS 0x3C

// Number of displays
#define NUM_DISPLAYS 3

// I2C pins for each display
#define SDA_PIN_0 6
#define SCL_PIN_0 7

#define SDA_PIN_1 4
#define SCL_PIN_1 5

#define SDA_PIN_2 2
#define SCL_PIN_2 3

// Buffer size
#define DISPLAY_BUFFER_SIZE 1024

// Initialize all displays
bool displayInit();

// Show a text message on a specific display (default: display 0)
void displayShowMessage(uint8_t screenId, const char* line1, const char* line2 = nullptr,
                        const char* line3 = nullptr, const char* line4 = nullptr,
                        const char* line5 = nullptr, const char* line6 = nullptr);

// Show a text message on all displays
void displayShowMessageAll(const char* line1, const char* line2 = nullptr,
                           const char* line3 = nullptr, const char* line4 = nullptr,
                           const char* line5 = nullptr, const char* line6 = nullptr);

// Show IP address on all displays
void displayShowIP(const char* ip);

// Update specific display with binary buffer data
void displayUpdate(uint8_t screenId, const uint8_t* buffer);

// Clear a specific display
void displayClear(uint8_t screenId);

// Clear all displays
void displayClearAll();

// Check if screen ID is valid
bool displayIsValidScreen(uint8_t screenId);

#endif
